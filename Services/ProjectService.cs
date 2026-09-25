using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using Win04_Cropper.Models;

namespace Win04_Cropper.Services;

public static class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string ProjectsDirectory
    {
        get
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "projects");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }

    public static string HistoryFilePath => Path.Combine(ProjectsDirectory, "history.json");

    public static List<ProjectHistoryItem> GetHistory()
    {
        try
        {
            if (File.Exists(HistoryFilePath))
            {
                string json = File.ReadAllText(HistoryFilePath);
                var list = JsonSerializer.Deserialize<List<ProjectHistoryItem>>(json, JsonOptions);
                if (list != null)
                {
                    // Sort descending by last modified
                    return list.OrderByDescending(p => p.LastModified).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load project history: {ex.Message}");
        }

        return new List<ProjectHistoryItem>();
    }

    public static bool SaveHistory(List<ProjectHistoryItem> history)
    {
        try
        {
            string json = JsonSerializer.Serialize(history, JsonOptions);
            File.WriteAllText(HistoryFilePath, json);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save project history: {ex.Message}");
            return false;
        }
    }

    public static string GenerateThumbnailBase64(Image image, int targetW = 240, int targetH = 150)
    {
        try
        {
            using var thumb = new Bitmap(targetW, targetH);
            using (var g = Graphics.FromImage(thumb))
            {
                g.Clear(Color.FromArgb(28, 31, 38));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float ratioSrc = (float)image.Width / image.Height;
                float ratioDst = (float)targetW / targetH;

                int drawW, drawH;
                if (ratioSrc > ratioDst)
                {
                    drawW = targetW;
                    drawH = (int)Math.Round(targetW / ratioSrc);
                }
                else
                {
                    drawH = targetH;
                    drawW = (int)Math.Round(targetH * ratioSrc);
                }

                int x = (targetW - drawW) / 2;
                int y = (targetH - drawH) / 2;
                g.DrawImage(image, x, y, drawW, drawH);
            }

            using var ms = new MemoryStream();
            thumb.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to generate thumbnail: {ex.Message}");
            return string.Empty;
        }
    }

    public static Image? ImageFromBase64(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);
            return Image.FromStream(ms);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to decode image from Base64: {ex.Message}");
            return null;
        }
    }

    public static string ImageToBase64(Image image, ImageFormat? format = null)
    {
        try
        {
            using var ms = new MemoryStream();
            image.Save(ms, format ?? ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to encode image to Base64: {ex.Message}");
            return string.Empty;
        }
    }

    public static string SaveProject(ProjectData project, string? filePath = null)
    {
        string targetPath = filePath ?? Path.Combine(ProjectsDirectory, $"{project.Id}.cropperproj");
        project.LastModified = DateTime.Now;

        string json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(targetPath, json);

        // Update history
        var history = GetHistory();
        var existing = history.FirstOrDefault(h => h.Id == project.Id || h.FilePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            history.Remove(existing);
        }

        history.Insert(0, new ProjectHistoryItem
        {
            Id = project.Id,
            Name = project.Name,
            FilePath = targetPath,
            ThumbnailBase64 = project.ThumbnailBase64,
            LastModified = project.LastModified,
            RegionCount = project.SavedRegions.Count,
            ImageDimensions = $"{project.ImageWidth} x {project.ImageHeight} px"
        });

        SaveHistory(history);
        return targetPath;
    }

    public static ProjectData? LoadProject(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            string json = File.ReadAllText(filePath);
            var project = JsonSerializer.Deserialize<ProjectData>(json, JsonOptions);
            if (project != null)
            {
                // Update history timestamp
                var history = GetHistory();
                var existing = history.FirstOrDefault(h => h.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.LastModified = DateTime.Now;
                    existing.RegionCount = project.SavedRegions.Count;
                    existing.Name = project.Name;
                    SaveHistory(history);
                }
                return project;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load project from {filePath}: {ex.Message}");
        }

        return null;
    }

    public static void DeleteFromHistory(string id)
    {
        var history = GetHistory();
        int removed = history.RemoveAll(h => h.Id == id);
        if (removed > 0)
        {
            SaveHistory(history);
        }
    }
}
