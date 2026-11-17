using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;

namespace ProductAPI.Pages.Admin;

public class CategoriesModel(AppDbContext db) : PageModel
{
    public List<ProductCategory> Categories { get; private set; } = new();
    public string Error { get; private set; } = "";
    public int? EditId { get; private set; }
    public string EditName { get; private set; } = "";

    public async Task OnGetAsync(int? editId)
    {
        await LoadAsync();
        if (editId is not null)
        {
            var category = Categories.FirstOrDefault(c => c.Id == editId.Value);
            if (category is not null) { EditId = category.Id; EditName = category.Name; }
        }
    }

    public async Task<IActionResult> OnPostSaveAsync(int id, string name)
    {
        name = name?.Trim() ?? "";
        if (name.Length is < 1 or > 120) return await FailAsync("Category name must contain 1–120 characters.");
        if (await db.ProductCategories.AnyAsync(c => c.Name == name && c.Id != id))
            return await FailAsync("A category with that name already exists.");

        ProductCategory category;
        if (id == 0)
        {
            category = new ProductCategory();
            db.ProductCategories.Add(category);
        }
        else
        {
            category = await db.ProductCategories.FindAsync(id) ?? new ProductCategory();
            if (category.Id == 0) return NotFound();
        }
        category.Name = name;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var category = await db.ProductCategories.FindAsync(id);
        if (category is null) return NotFound();
        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            return await FailAsync("This category is assigned to products. Reassign them before deleting.");
        db.ProductCategories.Remove(category);
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync() =>
        Categories = await db.ProductCategories.AsNoTracking().Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();

    private async Task<IActionResult> FailAsync(string message)
    {
        Error = message;
        await LoadAsync();
        return Page();
    }
}
