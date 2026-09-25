#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Win04_Cropper.Models;

namespace Win04_Cropper.Services;

/// <summary>
/// Service responsible for exporting annotations, crops, and metadata into a standardized package structure.
/// Generates data.json, README.md, crops/ folder, and optional original sources/ folder.
/// </summary>
public static class ExportPackageService
{
    public record ExportResult(int ExportedSources, int ExportedCrops, int ExportedAreas);

    public static ExportResult Export(
        string exportRoot,
        string packageName,
        string projectName,
        bool exportSources,
        bool exportAreasWithImages,
        Bitmap? activeImage,
        string activeSourceName,
        IEnumerable<MediaItem>? mediaItems,
        IEnumerable<CropRegionItem> savedRegions)
    {
        string safeProjectName = !string.IsNullOrWhiteSpace(projectName)
            ? SanitizeFileName(projectName)
            : "Cropper";

        string cropsDir = Path.Combine(exportRoot, "crops");
        string sourcesDir = Path.Combine(exportRoot, "sources");

        Directory.CreateDirectory(exportRoot);
        Directory.CreateDirectory(cropsDir);
        if (exportSources)
        {
            Directory.CreateDirectory(sourcesDir);
        }

        int exportedSources = 0;
        int exportedCrops = 0;
        int exportedAreas = 0;

        // -----------------------------------------------------------------
        // Step 1: Collect & optionally save unique source images to sources/
        // -----------------------------------------------------------------
        Dictionary<string, (string FileName, int Width, int Height)> sourceInfoDict = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> usedSourceFiles = new(StringComparer.OrdinalIgnoreCase);

        string RegisterSource(string rawName, Bitmap? bmp)
        {
            if (bmp == null || bmp.Width <= 0 || bmp.Height <= 0) return "";
            string key = string.IsNullOrWhiteSpace(rawName) ? "image" : rawName;
            if (sourceInfoDict.TryGetValue(key, out var existing))
            {
                return existing.FileName;
            }

            string baseName = Path.GetFileNameWithoutExtension(key);
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "image";
            baseName = SanitizeFileName(baseName);

            string fileName = $"{baseName}.png";
            int counter = 1;
            while (usedSourceFiles.Contains(fileName))
            {
                fileName = $"{baseName}_{counter++}.png";
            }
            usedSourceFiles.Add(fileName);

            if (exportSources)
            {
                string targetPath = Path.Combine(sourcesDir, fileName);
                bmp.Save(targetPath, ImageFormat.Png);
                exportedSources++;
            }

            sourceInfoDict[key] = (fileName, bmp.Width, bmp.Height);
            return fileName;
        }

        // Register active source image
        if (activeImage != null)
        {
            RegisterSource(activeSourceName, activeImage);
        }

        // Register media items
        if (mediaItems != null)
        {
            foreach (var mi in mediaItems)
            {
                if (mi.Bitmap != null)
                {
                    RegisterSource(mi.Name, mi.Bitmap);
                }
            }
        }

        // Register source bitmaps from saved regions
        var regionsList = savedRegions.ToList();
        foreach (var r in regionsList)
        {
            if (r.SourceBitmap != null)
            {
                RegisterSource(r.SourceImageName ?? r.Name, r.SourceBitmap);
            }
            else if (!string.IsNullOrEmpty(r.SourceImageBase64))
            {
                using var tmp = ProjectService.BitmapFromBase64(r.SourceImageBase64);
                if (tmp != null)
                {
                    RegisterSource(r.SourceImageName ?? r.Name, tmp);
                }
            }
        }

        // -----------------------------------------------------------------
        // Step 2: Separate Regions into Areas & Crops
        // -----------------------------------------------------------------
        var areaItems = regionsList.Where(r => r.ItemType == CropItemType.Coordinate).ToList();
        var cropItems = regionsList.Where(r => r.ItemType == CropItemType.Image).ToList();

        var areasExportList = new List<object>();
        var cropsExportList = new List<object>();

        // Process Crops
        int cropSeq = 1;
        foreach (var r in cropItems)
        {
            string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(r.Name) ? $"Crop_{cropSeq}" : r.Name);
            string cropFileName = $"crop_{cropSeq:D3}_{safeName}.png";
            string cropFilePath = Path.Combine(cropsDir, cropFileName);

            Bitmap? srcBmp = r.SourceBitmap;
            bool disposeSrc = false;
            if (srcBmp == null && !string.IsNullOrEmpty(r.SourceImageBase64))
            {
                srcBmp = ProjectService.BitmapFromBase64(r.SourceImageBase64);
                disposeSrc = true;
            }
            if (srcBmp == null && activeImage != null)
            {
                srcBmp = activeImage;
            }

            int srcW = srcBmp?.Width ?? 0;
            int srcH = srcBmp?.Height ?? 0;
            string srcFileName = "";
            if (srcBmp != null)
            {
                string srcKey = r.SourceImageName ?? activeSourceName;
                if (sourceInfoDict.TryGetValue(srcKey, out var sInfo))
                {
                    srcFileName = sInfo.FileName;
                }
                else
                {
                    srcFileName = RegisterSource(srcKey, srcBmp);
                }
            }

            if (srcBmp != null)
            {
                Rectangle cropRect = new(r.X, r.Y, r.Width, r.Height);
                cropRect.Intersect(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height));
                if (cropRect.Width > 0 && cropRect.Height > 0)
                {
                    using var cropped = srcBmp.Clone(cropRect, PixelFormat.Format32bppArgb);
                    cropped.Save(cropFilePath, ImageFormat.Png);
                    exportedCrops++;
                }
            }

            if (disposeSrc) srcBmp?.Dispose();

            cropsExportList.Add(new
            {
                id = cropSeq,
                name = r.Name,
                file_name = cropFileName,
                relative_path = $"crops/{cropFileName}",
                source_image = string.IsNullOrEmpty(srcFileName) ? (r.SourceImageName ?? "") : srcFileName,
                source_width = srcW,
                source_height = srcH,
                x = r.X,
                y = r.Y,
                width = r.Width,
                height = r.Height,
                aspect_ratio = r.AspectRatioStr
            });

            cropSeq++;
        }

        // Process Areas
        int areaSeq = 1;
        foreach (var r in areaItems)
        {
            Bitmap? srcBmp = r.SourceBitmap;
            bool disposeSrc = false;
            if (srcBmp == null && !string.IsNullOrEmpty(r.SourceImageBase64))
            {
                srcBmp = ProjectService.BitmapFromBase64(r.SourceImageBase64);
                disposeSrc = true;
            }
            if (srcBmp == null && activeImage != null)
            {
                srcBmp = activeImage;
            }

            int srcW = srcBmp?.Width ?? 0;
            int srcH = srcBmp?.Height ?? 0;
            string srcFileName = "";
            if (srcBmp != null)
            {
                string srcKey = r.SourceImageName ?? activeSourceName;
                if (sourceInfoDict.TryGetValue(srcKey, out var sInfo))
                {
                    srcFileName = sInfo.FileName;
                }
                else
                {
                    srcFileName = RegisterSource(srcKey, srcBmp);
                }
            }

            string? areaCropFile = null;
            if (exportAreasWithImages && srcBmp != null)
            {
                string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(r.Name) ? $"Area_{areaSeq}" : r.Name);
                string areaFileName = $"area_{areaSeq:D3}_{safeName}.png";
                string areaFilePath = Path.Combine(cropsDir, areaFileName);

                Rectangle cropRect = new(r.X, r.Y, r.Width, r.Height);
                cropRect.Intersect(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height));
                if (cropRect.Width > 0 && cropRect.Height > 0)
                {
                    using var cropped = srcBmp.Clone(cropRect, PixelFormat.Format32bppArgb);
                    cropped.Save(areaFilePath, ImageFormat.Png);
                    areaCropFile = $"crops/{areaFileName}";
                }
            }

            if (disposeSrc) srcBmp?.Dispose();

            areasExportList.Add(new
            {
                id = areaSeq,
                name = r.Name,
                source_image = string.IsNullOrEmpty(srcFileName) ? (r.SourceImageName ?? "") : srcFileName,
                source_width = srcW,
                source_height = srcH,
                x = r.X,
                y = r.Y,
                width = r.Width,
                height = r.Height,
                aspect_ratio = r.AspectRatioStr,
                crop_file = areaCropFile
            });

            areaSeq++;
            exportedAreas++;
        }

        // -----------------------------------------------------------------
        // Step 3: Write data.json
        // -----------------------------------------------------------------
        var exportPayload = new
        {
            project_name = safeProjectName,
            package_name = packageName,
            export_time = DateTime.Now.ToString("s"),
            summary = new
            {
                total_areas = areasExportList.Count,
                total_crops = cropsExportList.Count,
                has_source_images = exportSources
            },
            areas = areasExportList,
            crops = cropsExportList
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        string jsonData = JsonSerializer.Serialize(exportPayload, jsonOptions);
        File.WriteAllText(Path.Combine(exportRoot, "data.json"), jsonData, Encoding.UTF8);

        // -----------------------------------------------------------------
        // Step 4: Write README.md
        // -----------------------------------------------------------------
        var sbReadme = new StringBuilder();
        sbReadme.AppendLine($"# Dữ liệu xuất: {packageName}");
        sbReadme.AppendLine();
        sbReadme.AppendLine($"- **Dự án**: {safeProjectName}");
        sbReadme.AppendLine($"- **Thời gian xuất**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sbReadme.AppendLine($"- **Tổng số vùng tọa độ (Areas)**: {areasExportList.Count}");
        sbReadme.AppendLine($"- **Tổng số ảnh cắt (Crops)**: {cropsExportList.Count}");
        sbReadme.AppendLine($"- **Kèm ảnh gốc**: {(exportSources ? "Có (trong sources/)" : "Không")}");
        sbReadme.AppendLine();
        sbReadme.AppendLine("## 1. Cấu trúc thư mục");
        sbReadme.AppendLine("```");
        sbReadme.AppendLine($"{packageName}/");
        sbReadme.AppendLine("├── README.md        <- Tài liệu hướng dẫn & mô tả dữ liệu");
        sbReadme.AppendLine("├── data.json        <- Danh sách tọa độ và thông tin ảnh (sắp xếp theo Areas, Crops)");
        sbReadme.AppendLine("└── crops/           <- Thư mục chứa toàn bộ ảnh đã cắt (PNG)");
        if (exportSources)
        {
            sbReadme.AppendLine("└── sources/         <- Thư mục chứa các ảnh gốc ban đầu (PNG)");
        }
        sbReadme.AppendLine("```");
        sbReadme.AppendLine();
        sbReadme.AppendLine("## 2. Quy cách tệp data.json");
        sbReadme.AppendLine("Tệp `data.json` chứa thông tin chi tiết được phân nhóm rõ ràng theo 2 danh mục:");
        sbReadme.AppendLine("- **`areas`**: Danh sách các vùng tọa độ quan tâm (ROI - Region of Interest).");
        sbReadme.AppendLine("- **`crops`**: Danh sách các đối tượng đã được cắt ra tệp ảnh trong thư mục `crops/`.");
        sbReadme.AppendLine();
        sbReadme.AppendLine("### Hệ tọa độ:");
        sbReadme.AppendLine("- Gốc tọa độ `(0, 0)` nằm ở góc trên bên trái (Top-Left) của ảnh nguồn.");
        sbReadme.AppendLine("- `x`, `y`: Tọa độ góc trên bên trái của khung.");
        sbReadme.AppendLine("- `width`, `height`: Kích thước pixel chiều rộng và chiều cao.");
        sbReadme.AppendLine();
        sbReadme.AppendLine("## 3. Cách đọc dữ liệu bằng Python");
        sbReadme.AppendLine("```python");
        sbReadme.AppendLine("import json");
        sbReadme.AppendLine();
        sbReadme.AppendLine("with open('data.json', 'r', encoding='utf-8') as f:");
        sbReadme.AppendLine("    dataset = json.load(f)");
        sbReadme.AppendLine();
        sbReadme.AppendLine("print('Project:', dataset['project_name'])");
        sbReadme.AppendLine("print('Crops count:', len(dataset['crops']))");
        sbReadme.AppendLine("for crop in dataset['crops']:");
        sbReadme.AppendLine("    print(f\"ID {crop['id']}: {crop['name']} -> {crop['relative_path']} ({crop['width']}x{crop['height']})\")");
        sbReadme.AppendLine("```");

        File.WriteAllText(Path.Combine(exportRoot, "README.md"), sbReadme.ToString(), Encoding.UTF8);

        return new ExportResult(exportedSources, exportedCrops, exportedAreas);
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
        return string.IsNullOrEmpty(clean) ? "unnamed" : clean;
    }
}
