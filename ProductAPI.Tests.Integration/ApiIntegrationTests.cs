using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;
using Testcontainers.MySql;
using Xunit;

namespace ProductAPI.Tests.Integration;

/// <summary>Runs real HTTP requests against a disposable MySQL 8 container. Requires Docker.</summary>
public sealed class ApiIntegrationTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .WithDatabase("productapi_test")
        .WithUsername("productapi")
        .WithPassword("integration_only_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _resellerKey = null!;
    private string _readOnlyKey = null!;
    private int _productId;

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseEnvironment("Development");
            web.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _mysql.GetConnectionString(),
                ["Admin:Username"] = "test-admin",
                ["Admin:PasswordSha256"] = ApiKeyService.Hash("not_a_production_password")
            }));
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var supplier = new Supplier { Name = "Test Supplier", Country = "China" };
        var reseller = new Reseller { Name = "Test Reseller" };
        var category = new ProductCategory { Name = "Adapters" };
        db.AddRange(supplier, reseller, category);
        await db.SaveChangesAsync();
        var product = new Product
        {
            Sku = "INTEGRATION-001", Name = "USB-C Adapter", CategoryId = category.Id,
            SupplierId = supplier.Id, PurchasePrice = 10m, RetailPrice = 30m,
            ResellerPrice = 20m, Currency = "SEK", StockQuantity = 10,
            TargetStockQuantity = 50, ReorderThresholdPercent = 20
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        _productId = product.Id;
        var keys = scope.ServiceProvider.GetRequiredService<ApiKeyService>();
        (_, _resellerKey) = await keys.Create("order test", 30,
            ["products:read", "inventory:read", "orders:create", "orders:read"], reseller.Id);
        (_, _readOnlyKey) = await keys.Create("read only", 30, ["products:read"], reseller.Id);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _mysql.DisposeAsync();
    }

    private HttpRequestMessage Request(HttpMethod method, string path, string? key = null)
    {
        var req = new HttpRequestMessage(method, path);
        if (key != null) req.Headers.Add("X-API-Key", key);
        return req;
    }

    [Fact]
    public async Task Products_RequireToken_AndDoNotExposeSupplierCost()
    {
        using var noKey = await _client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.Unauthorized, noKey.StatusCode);

        using var req = Request(HttpMethod.Get, "/api/v1/products", _resellerKey);
        using var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("USB-C Adapter", json);
        Assert.Contains("resellerPrice", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("purchasePrice", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadOnlyToken_CannotCreateOrders()
    {
        using var req = Request(HttpMethod.Post, "/api/v1/orders", _readOnlyKey);
        req.Content = JsonContent.Create(new { Items = new[] { new { ProductId = _productId, Quantity = 1 } } });
        using var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredAndRevokedTokens_AreRejected()
    {
        string revoked, expired;
        using (var scope = _factory.Services.CreateScope())
        {
            var keys = scope.ServiceProvider.GetRequiredService<ApiKeyService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var (record, secret) = await keys.Create("revoke test", 1, ["products:read"], null, true);
            record.Revoked = true;
            await db.SaveChangesAsync();
            revoked = secret;
            var (old, oldSecret) = await keys.Create("expired test", 1, ["products:read"], null, true);
            old.ExpiresUtc = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
            expired = oldSecret;
        }
        foreach (var key in new[] { revoked, expired })
        {
            using var req = Request(HttpMethod.Get, "/api/v1/products", key);
            using var response = await _client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task ResellerOrder_ReservesStock_AndRejectsOversell()
    {
        using var first = Request(HttpMethod.Post, "/api/v1/orders", _resellerKey);
        first.Content = JsonContent.Create(new { Items = new[] { new { ProductId = _productId, Quantity = 8 } } });
        using var firstResponse = await _client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var second = Request(HttpMethod.Post, "/api/v1/orders", _resellerKey);
        second.Content = JsonContent.Create(new { Items = new[] { new { ProductId = _productId, Quantity = 3 } } });
        using var secondResponse = await _client.SendAsync(second);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products.SingleAsync(p => p.Id == _productId);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(8, product.ReservedQuantity);
        Assert.Equal(1, await db.ResellerOrders.CountAsync());
    }

    [Fact]
    public async Task DisabledResellerToken_IsRejected()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reseller = await db.Resellers.SingleAsync();
            reseller.Active = false;
            await db.SaveChangesAsync();
        }
        using var req = Request(HttpMethod.Get, "/api/v1/products", _resellerKey);
        using var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task VariantOrder_ReservesVariantWithoutReservingParent()
    {
        int variantId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = new ProductVariant
            {
                ProductId = _productId, Sku = "INTEGRATION-001-BLACK",
                Color = "Black", StockQuantity = 5, ResellerPrice = 15m
            };
            db.ProductVariants.Add(variant);
            await db.SaveChangesAsync();
            variantId = variant.Id;
        }
        using var request = Request(HttpMethod.Post, "/api/v1/orders", _resellerKey);
        request.Content = JsonContent.Create(new { Items = new[] { new { ProductId = _productId, ProductVariantId = variantId, Quantity = 4 } } });
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope2 = _factory.Services.CreateScope();
        var verify = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(4, (await verify.ProductVariants.SingleAsync()).ReservedQuantity);
        Assert.Equal(0, (await verify.Products.SingleAsync()).ReservedQuantity);
        Assert.Equal(variantId, (await verify.ResellerOrderItems.SingleAsync()).ProductVariantId);
    }

    [Fact]
    public async Task ProductDetail_IncludesGalleryAndVariants()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ProductVariants.Add(new ProductVariant { ProductId = _productId, Sku = "INTEGRATION-001-S", Size = "S" });
            db.ProductImages.Add(new ProductImage { ProductId = _productId, FileName = "test.jpg",
                ImageUrl = "/uploads/products/test.jpg", IsPrimary = true });
            await db.SaveChangesAsync();
        }
        using var request = Request(HttpMethod.Get, $"/api/v1/products/{_productId}", _resellerKey);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("INTEGRATION-001-S", json);
        Assert.Contains("/uploads/products/test.jpg", json);
    }

}
