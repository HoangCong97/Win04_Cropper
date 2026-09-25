#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Controls;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper;

partial class MainCropperForm
{
    #region Crop Image Export and Save

    public Bitmap? GetCroppedBitmap()
    {
        if (canvas.Image == null) return null;
        Rectangle cropRect = canvas.CropRect;
        if (cropRect.Width <= 0 || cropRect.Height <= 0) return null;

        try
        {
            int imgW = canvas.Image.Width;
            int imgH = canvas.Image.Height;

            int x = Math.Clamp(cropRect.X, 0, Math.Max(0, imgW - 1));
            int y = Math.Clamp(cropRect.Y, 0, Math.Max(0, imgH - 1));
            int w = Math.Clamp(cropRect.Width, 1, imgW - x);
            int h = Math.Clamp(cropRect.Height, 1, imgH - y);

            Rectangle validRect = new(x, y, w, h);
            Bitmap cropped = new(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(canvas.Image, new Rectangle(0, 0, w, h), validRect, GraphicsUnit.Pixel);
            }
            return cropped;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetCroppedBitmap failed: {ex.Message}");
            return null;
        }
    }

    private void CropAndSaveImage()
    {
        using Bitmap? cropped = GetCroppedBitmap();
        if (canvas.Image == null || cropped == null)
        {
            MessageBox.Show("Chưa có ảnh hoặc vùng cắt hợp lệ để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string defaultName = _currentlyEditingItem?.Name ?? GenerateUniqueName("Crop ");
        string? origName = _currentlyEditingItem?.Name;

        using NameInputDialog dlg = new(
            canvas.CropRect,
            defaultName: defaultName,
            existingNames: _savedRegions.Select(r => r.Name),
            originalName: origName,
            title: _currentlyEditingItem != null ? $"Lưu / Cập nhật ảnh: {origName}" : "Lưu đối tượng ảnh",
            headerText: _currentlyEditingItem != null ? "Cập nhật hoặc Lưu mới đối tượng ảnh" : "Lưu đối tượng ảnh vào dự án",
            headerIcon: IconChar.Crop);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string chosenName = dlg.RegionName;
        bool isSaveAsNew = dlg.IsSaveAsNew || _currentlyEditingItem == null;

        Bitmap? sourceBmp = canvas.Image != null ? new Bitmap(canvas.Image) : null;
        string? sourceB64 = canvas.Image != null ? ProjectService.ImageToBase64(canvas.Image) : null;
        if (_currentProject == null)
        {
            _currentProject = new ProjectData
            {
                Name = GenerateDefaultProjectName(),
                IsCustomNamed = false
            };
            UpdateAppTitle();
        }
        string sourceName = !string.IsNullOrEmpty(_currentSourceName) ? _currentSourceName : _currentProject.Name;

        if (!isSaveAsNew && _currentlyEditingItem != null)
        {
            // Overwrite existing item
            _currentlyEditingItem.Name = chosenName;
            _currentlyEditingItem.X = canvas.CropRect.X;
            _currentlyEditingItem.Y = canvas.CropRect.Y;
            _currentlyEditingItem.Width = canvas.CropRect.Width;
            _currentlyEditingItem.Height = canvas.CropRect.Height;
            _currentlyEditingItem.Notes = dlg.Notes;
            _currentlyEditingItem.ItemType = CropItemType.Image;
            _currentlyEditingItem.SourceBitmap = sourceBmp;
            _currentlyEditingItem.SourceImageBase64 = sourceB64;
            _currentlyEditingItem.SourceImageName = sourceName;

            CancelEditing();
            RefreshSavedGrid();
            _isProjectDirty = true;
        }
        else
        {
            // Add to saved list as an Image item
            CropRegionItem item = new()
            {
                ItemType = CropItemType.Image,
                Name = chosenName,
                X = canvas.CropRect.X,
                Y = canvas.CropRect.Y,
                Width = canvas.CropRect.Width,
                Height = canvas.CropRect.Height,
                Notes = dlg.Notes,
                CreatedAt = DateTime.Now,
                SourceBitmap = sourceBmp,
                SourceImageBase64 = sourceB64,
                SourceImageName = sourceName
            };

            _savedRegions.Add(item);
            CancelEditing();
            RefreshSavedGrid();

            // Select newly added row
            if (dgvSavedRegions.Rows.Count > 0)
            {
                dgvSavedRegions.ClearSelection();
                dgvSavedRegions.Rows[^1].Selected = true;
            }
            _isProjectDirty = true;
        }
    }

    private void CopyCroppedImageToClipboard()
    {
        using Bitmap? cropped = GetCroppedBitmap();
        if (cropped == null)
        {
            MessageBox.Show("Chưa có vùng cắt để copy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Clipboard.SetImage(cropped);
            MessageBox.Show("Đã copy hình ảnh cắt vào Clipboard! (Bạn có thể nhấn Ctrl+V vào Paint, Zalo, Discord...)", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi copy vào Clipboard: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region Package and JSON Export

    private void ExportPackage()
    {
        if (_savedRegions.Count == 0 && mediaPanel.Items.Count == 0)
        {
            MessageBox.Show("Chưa có mục nào trong danh sách đã lưu hoặc trong Media để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int areaCount = _savedRegions.Count(r => r.ItemType == CropItemType.Coordinate);
        int cropCount = _savedRegions.Count(r => r.ItemType == CropItemType.Image);

        string defaultExportName = !string.IsNullOrWhiteSpace(_currentProject?.Name)
            ? $"{SanitizeFileName(_currentProject.Name)}_Export_{DateTime.Now:yyyyMMdd_HHmm}"
            : $"Cropper_Export_{DateTime.Now:yyyyMMdd_HHmm}";

        // Show export options modal dialog
        using ExportOptionsDialog optDlg = new(defaultExportName, areaCount, cropCount, _dpiScale);
        if (optDlg.ShowDialog(this) != DialogResult.OK) return;

        string packageName = optDlg.ExportPackageName;
        bool exportSources = optDlg.ExportSources;
        bool exportAreasWithImages = optDlg.ExportAreasWithImages;

        using FolderBrowserDialog fbd = new()
        {
            Description = "Chọn thư mục lưu gói xuất",
            UseDescriptionForTitle = true
        };

        if (fbd.ShowDialog(this) != DialogResult.OK) return;

        string exportRoot = Path.Combine(fbd.SelectedPath, packageName);
        ExportDatasetToFolder(exportRoot, packageName, exportSources, exportAreasWithImages, showSuccessDialog: true);
    }

    internal void ExportDatasetToFolder(string exportRoot, bool exportAreasWithImages, bool exportCropsWithCoords, bool showSuccessDialog = true)
    {
        string packageName = Path.GetFileName(exportRoot);
        ExportDatasetToFolder(exportRoot, packageName, exportSources: false, exportAreasWithImages, showSuccessDialog);
    }

    internal void ExportDatasetToFolder(string exportRoot, string packageName, bool exportSources, bool exportAreasWithImages, bool showSuccessDialog = true)
    {
        try
        {
            var result = ExportPackageService.Export(
                exportRoot,
                packageName,
                _currentProject?.Name ?? "Cropper",
                exportSources,
                exportAreasWithImages,
                canvas.Image,
                _currentSourceName,
                mediaPanel.Items,
                _savedRegions);

            // -----------------------------------------------------------------
            // Success Prompt
            // -----------------------------------------------------------------
            if (showSuccessDialog)
            {
                var ask = MessageBox.Show(
                    $"Xuất gói dữ liệu thành công!\n\n" +
                    $"Thư mục đích:\n{exportRoot}\n\n" +
                    $"• data.json : Tọa độ & thông tin ảnh ({result.ExportedAreas} areas, {result.ExportedCrops} crops)\n" +
                    $"• crops/     : {result.ExportedCrops + (exportAreasWithImages ? result.ExportedAreas : 0)} ảnh đối tượng đã cắt\n" +
                    (exportSources ? $"• sources/   : {result.ExportedSources} ảnh nguồn gốc\n" : "") +
                    $"• README.md  : Tài liệu mô tả cấu trúc cho AI & phần mềm ngoài\n\n" +
                    $"Bạn có muốn mở thư mục vừa xuất trong Windows Explorer không?",
                    "Xuất thành công",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (ask == DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", exportRoot) { UseShellExecute = true });
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất gói dữ liệu: {ex.Message}", "Lỗi xuất", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
        return string.IsNullOrEmpty(clean) ? "unnamed" : clean;
    }

    private static string EscapeCsv(string text)
    {
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
        return text;
    }

    private void ExportRegionsToJson()
    {
        using SaveFileDialog sfd = new()
        {
            Title = "Xuất danh sách tọa độ & ảnh ra tệp JSON",
            Filter = "JSON file (*.json)|*.json",
            FileName = $"CropProfiles_{DateTime.Now:yyyyMMdd_HHmm}.json"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            if (ConfigStorageService.SaveRegions(_savedRegions, sfd.FileName))
            {
                MessageBox.Show($"Đã xuất danh sách thành công ra:\n{sfd.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void ImportRegionsFromJson()
    {
        using OpenFileDialog ofd = new()
        {
            Title = "Nhập danh sách tọa độ & ảnh từ tệp JSON",
            Filter = "JSON file (*.json)|*.json"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            var imported = ConfigStorageService.LoadRegions(ofd.FileName);
            if (imported.Count > 0)
            {
                foreach (var r in imported)
                {
                    if (!string.IsNullOrEmpty(r.SourceImageBase64) && r.SourceBitmap == null)
                    {
                        r.SourceBitmap = ProjectService.BitmapFromBase64(r.SourceImageBase64);
                    }
                }
                _savedRegions.AddRange(imported);
                RefreshSavedGrid();
                MessageBox.Show($"Đã nhập {imported.Count} mục mới từ JSON!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Không tìm thấy dữ liệu hợp lệ trong file JSON!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    #endregion
}
