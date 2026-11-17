using ProductAPI.Services;
using Xunit;

namespace ProductAPI.Tests.Unit;

public class ProductImagePolicyTests
{
    [Theory]
    [InlineData(0, 20, true)]
    [InlineData(19, 1, true)]
    [InlineData(19, 2, false)]
    [InlineData(20, 1, false)]
    [InlineData(0, 0, false)]
    [InlineData(0, 21, false)]
    public void UploadLimit_EnforcesTwentyImages(int existing, int incoming, bool allowed)
        => Assert.Equal(allowed, ProductImagePolicy.CanAdd(existing, incoming));

    [Fact]
    public void RecognizesSupportedImageSignatures()
    {
        Assert.Equal(".jpg", ProductImagePolicy.DetectExtension(new byte[] {255,216,255,0}));
        Assert.Equal(".png", ProductImagePolicy.DetectExtension(new byte[] {137,80,78,71,13,10,26,10}));
        Assert.Equal(".webp", ProductImagePolicy.DetectExtension("RIFFxxxxWEBP"u8));
    }

    [Fact]
    public void RejectsUnsupportedSignatures()
    {
        Assert.Null(ProductImagePolicy.DetectExtension("GIF89a"u8));
        Assert.Null(ProductImagePolicy.DetectExtension("<svg/>"u8));
        Assert.Null(ProductImagePolicy.DetectExtension(Array.Empty<byte>()));
    }

    [Fact]
    public void EnforcesFiveMegabyteLimit() => Assert.Equal(5 * 1024 * 1024, ProductImagePolicy.MaxBytes);
}
