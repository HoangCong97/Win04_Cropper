using System;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Win04_Cropper.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CropItemType
{
    Coordinate,
    Image
}

public class CropRegionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public CropItemType ItemType { get; set; } = CropItemType.Coordinate;

    public string Name { get; set; } = "Vùng_cắt";

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; } = 100;

    public int Height { get; set; } = 100;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public string? ImagePath { get; set; }

    /// <summary>
    /// Source capture image Base64 data saved with this object
    /// </summary>
    public string? SourceImageBase64 { get; set; }

    /// <summary>
    /// Name or title of the source capture image
    /// </summary>
    public string? SourceImageName { get; set; }

    /// <summary>
    /// In-memory cached bitmap of the source image for fast switching
    /// </summary>
    [JsonIgnore]
    public Bitmap? SourceBitmap { get; set; }

    [JsonIgnore]
    public string TypeDisplay => ItemType == CropItemType.Image ? "Hình ảnh" : "Tọa độ";

    [JsonIgnore]
    public string CoordinateStr => $"X:{X}, Y:{Y}";

    [JsonIgnore]
    public string SizeStr => $"{Width} x {Height}";

    [JsonIgnore]
    public string AspectRatioStr
    {
        get
        {
            if (Height <= 0) return "-";
            int gcd = GetGcd(Width, Height);
            return $"{Width / gcd}:{Height / gcd}";
        }
    }

    private static int GetGcd(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a == 0 ? 1 : a;
    }

    public CropRegionItem Clone()
    {
        return new CropRegionItem
        {
            Id = this.Id,
            ItemType = this.ItemType,
            Name = this.Name,
            X = this.X,
            Y = this.Y,
            Width = this.Width,
            Height = this.Height,
            CreatedAt = this.CreatedAt,
            Notes = this.Notes,
            ImagePath = this.ImagePath,
            SourceImageBase64 = this.SourceImageBase64,
            SourceImageName = this.SourceImageName,
            SourceBitmap = this.SourceBitmap != null ? new Bitmap(this.SourceBitmap) : null
        };
    }
}
