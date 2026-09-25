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

public static partial class ProjectService
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

    public static string SanitizeFileName(string name)
    {
        var invalids = Path.GetInvalidFileNameChars();
        string clean = new(name.Select(c => invalids.Contains(c) ? '_' : c).ToArray());
        return clean.Trim();
    }

    public static string GetSafeProjectFileName(string projectName)
    {
        string safe = SanitizeFileName(projectName);
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = $"Project_{DateTime.Now:yyyyMMdd_HHmmss}";
        }
        return $"{safe}.json";
    }

    public static bool IsProjectNameExists(string projectName, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return false;
        string trimmed = projectName.Trim();
        string safeName = SanitizeFileName(trimmed);

        // 1. Check in history
        var history = GetHistory();
        if (history.Any(h => (!string.IsNullOrEmpty(excludeId) && h.Id == excludeId)
            ? false
            : string.Equals(h.Name?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 2. Check files in ProjectsDirectory (.json and .cropperproj)
        string jsonPath = Path.Combine(ProjectsDirectory, $"{safeName}.json");
        if (File.Exists(jsonPath))
        {
            if (!string.IsNullOrEmpty(excludeId))
            {
                var p = LoadProject(jsonPath);
                if (p != null && p.Id == excludeId) return false;
            }
            return true;
        }

        string oldProjPath = Path.Combine(ProjectsDirectory, $"{safeName}.cropperproj");
        if (File.Exists(oldProjPath))
        {
            if (!string.IsNullOrEmpty(excludeId))
            {
                var p = LoadProject(oldProjPath);
                if (p != null && p.Id == excludeId) return false;
            }
            return true;
        }

        return false;
    }

    public static string SaveProject(ProjectData project, string? filePath = null)
    {
        string? targetPath = filePath;
        string? oldFilePathToDelete = null;

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            targetPath = Path.Combine(ProjectsDirectory, GetSafeProjectFileName(project.Name));
        }
        else if (Path.GetExtension(targetPath).Equals(".cropperproj", StringComparison.OrdinalIgnoreCase))
        {
            oldFilePathToDelete = targetPath;
            targetPath = Path.ChangeExtension(targetPath, ".json");
        }

        project.LastModified = DateTime.Now;

        string json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(targetPath, json);

        // If upgraded from .cropperproj to .json, clean up old file
        if (oldFilePathToDelete != null && File.Exists(oldFilePathToDelete) && !oldFilePathToDelete.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.Delete(oldFilePathToDelete);
            }
            catch { }
        }

        // Update history
        var history = GetHistory();
        var existing = history.FirstOrDefault(h => h.Id == project.Id || h.FilePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase) || (oldFilePathToDelete != null && h.FilePath.Equals(oldFilePathToDelete, StringComparison.OrdinalIgnoreCase)));
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
        SetLastSessionProjectPath(targetPath);
        return targetPath;
    }

    public static string SessionFilePath => Path.Combine(ProjectsDirectory, "last_session.json");

    public static string? GetLastSessionProjectPath()
    {
        try
        {
            if (File.Exists(SessionFilePath))
            {
                string path = File.ReadAllText(SessionFilePath).Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read last_session.json: {ex.Message}");
        }

        return null;
    }

    public static void SetLastSessionProjectPath(string? filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                if (File.Exists(SessionFilePath)) File.Delete(SessionFilePath);
            }
            else
            {
                File.WriteAllText(SessionFilePath, filePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to write last_session.json: {ex.Message}");
        }
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
                SetLastSessionProjectPath(filePath);
                return project;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load project from {filePath}: {ex.Message}");
        }

        return null;
    }

    public static bool DeleteProject(string id, bool deleteFileOnDisk = true)
    {
        var history = GetHistory();
        var item = history.FirstOrDefault(h => h.Id == id);
        string? filePath = item?.FilePath;
        string? projName = item?.Name;

        if (item != null)
        {
            history.Remove(item);
            SaveHistory(history);
        }

        if (deleteFileOnDisk)
        {
            // 1. Delete by item.FilePath
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { }
            }

            // 2. Delete by safeName in ProjectsDirectory (.json and .cropperproj)
            if (!string.IsNullOrWhiteSpace(projName))
            {
                string safeName = SanitizeFileName(projName);
                string jsonPath = Path.Combine(ProjectsDirectory, $"{safeName}.json");
                if (File.Exists(jsonPath))
                {
                    try { File.Delete(jsonPath); } catch { }
                }

                string oldProjPath = Path.Combine(ProjectsDirectory, $"{safeName}.cropperproj");
                if (File.Exists(oldProjPath))
                {
                    try { File.Delete(oldProjPath); } catch { }
                }
            }

            // 3. Delete by id in ProjectsDirectory
            if (!string.IsNullOrWhiteSpace(id))
            {
                string idProjPath = Path.Combine(ProjectsDirectory, $"{id}.cropperproj");
                if (File.Exists(idProjPath))
                {
                    try { File.Delete(idProjPath); } catch { }
                }
                string idJsonPath = Path.Combine(ProjectsDirectory, $"{id}.json");
                if (File.Exists(idJsonPath))
                {
                    try { File.Delete(idJsonPath); } catch { }
                }
            }
        }

        // Clean up last session if it pointed to the deleted project or points to a non-existent file
        try
        {
            if (File.Exists(SessionFilePath))
            {
                string sessionPath = File.ReadAllText(SessionFilePath).Trim();
                if (string.IsNullOrEmpty(sessionPath) ||
                    (!string.IsNullOrEmpty(filePath) && sessionPath.Equals(filePath, StringComparison.OrdinalIgnoreCase)) ||
                    !File.Exists(sessionPath))
                {
                    SetLastSessionProjectPath(null);
                }
            }
        }
        catch { }

        return true;
    }

    public static void DeleteFromHistory(string id)
    {
        DeleteProject(id, deleteFileOnDisk: true);
    }
}
