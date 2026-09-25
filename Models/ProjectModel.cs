using System;
using System.Collections.Generic;

namespace Win04_Cropper.Models;

public class ProjectData
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Dự án mới";
    public bool IsCustomNamed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public string? ImageBase64 { get; set; }
    public string? ThumbnailBase64 { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public int CropX { get; set; }
    public int CropY { get; set; }
    public int CropW { get; set; }
    public int CropH { get; set; }
    public float? LockedAspectRatio { get; set; }
    public List<CropRegionItem> SavedRegions { get; set; } = new();
    public List<MediaItem> MediaItems { get; set; } = new();
}

public class MediaItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Ảnh";
    public string? ImageBase64 { get; set; }
    public string? ThumbnailBase64 { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int? SavedCropX { get; set; }
    public int? SavedCropY { get; set; }
    public int? SavedCropW { get; set; }
    public int? SavedCropH { get; set; }
    public float? SavedZoomFactor { get; set; }
    public float? SavedPanX { get; set; }
    public float? SavedPanY { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public System.Drawing.Bitmap? Bitmap { get; set; }
}

public class ProjectHistoryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailBase64 { get; set; }
    public DateTime LastModified { get; set; } = DateTime.Now;
    public int RegionCount { get; set; }
    public string ImageDimensions { get; set; } = string.Empty;
}
