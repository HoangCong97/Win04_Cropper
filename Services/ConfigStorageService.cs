using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Win04_Cropper.Models;

namespace Win04_Cropper.Services;

public static class ConfigStorageService
{
    private static readonly string DefaultConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "saved_crops.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static List<CropRegionItem> LoadRegions(string? filePath = null)
    {
        string path = filePath ?? DefaultConfigPath;
        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<CropRegionItem>>(json, JsonOptions);
                if (list != null) return list;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load crop regions: {ex.Message}");
        }

        return new List<CropRegionItem>();
    }

    public static bool SaveRegions(List<CropRegionItem> items, string? filePath = null)
    {
        string path = filePath ?? DefaultConfigPath;
        try
        {
            string dir = Path.GetDirectoryName(path) ?? "";
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(items, JsonOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save crop regions: {ex.Message}");
            return false;
        }
    }
}
