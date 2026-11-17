namespace ProductAPI.Services;

public static class ProductImagePolicy
{
    public const int MaxImages = 20;
    public const long MaxBytes = 5 * 1024 * 1024;

    public static bool CanAdd(int currentCount, int incomingCount) =>
        currentCount >= 0 && incomingCount > 0 && incomingCount <= MaxImages &&
        currentCount + incomingCount <= MaxImages;

    // Checks file signatures rather than trusting extensions or Content-Type headers.
    public static string? DetectExtension(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ".jpg";
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return ".png";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8,4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }
}
