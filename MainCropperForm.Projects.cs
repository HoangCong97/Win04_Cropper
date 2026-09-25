#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Win04_Cropper.Controls;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper;

partial class MainCropperForm
{
    #region Project Management

    private void UpdateAppTitle()
    {
        if (_currentProject != null && !string.IsNullOrWhiteSpace(_currentProject.Name))
        {
            string dirty = _isProjectDirty ? " *" : "";
            this.Text = $"Screen Cropper Pro - [{_currentProject.Name}{dirty}]";
        }
        else
        {
            string dirty = _isProjectDirty ? " *" : "";
            this.Text = _isProjectDirty ? $"Screen Cropper Pro - [Chưa lưu dự án]{dirty}" : "Screen Cropper Pro";
        }
    }

    private void ShowProjectDialog()
    {
        using var dlg = new ProjectManagementDialog(_dpiScale);
        var result = dlg.ShowDialog(this);

        // If the current active project was deleted in the dialog, reset workspace to blank
        if (_currentProject != null && dlg.DeletedProjectIds.Contains(_currentProject.Id))
        {
            ResetToBlankApp();
        }

        if (result == DialogResult.OK)
        {
            if (dlg.IsNewProjectRequested && !string.IsNullOrWhiteSpace(dlg.NewProjectName))
            {
                CreateNewProject(dlg.NewProjectName);
            }
            else if (!string.IsNullOrEmpty(dlg.SelectedProjectPath))
            {
                OpenProjectFromFile(dlg.SelectedProjectPath, showMessage: true);
            }
        }
    }

    private void ResetToBlankApp()
    {
        _activeMediaItem = null;
        _currentProject = null;
        _currentProjectFilePath = null;
        _savedRegions.Clear();
        RefreshSavedGrid();
        mediaPanel.ClearItems();

        // Reset canvas & crop
        canvas.Image?.Dispose();
        canvas.Image = null;
        lblImageInfo.Text = "Ảnh: Chưa nạp";
        canvas.ImageOverlayInfo = "Chưa nạp ảnh (Kéo & thả ảnh từ Media vào đây)";
        lblCursorInfo.Text = "Chuột: -";
        canvas.SetCropRect(Rectangle.Empty);
        _isUpdatingInputs = true;
        numX.Value = 0;
        numY.Value = 0;
        numW.Value = 0;
        numH.Value = 0;
        _isUpdatingInputs = false;

        _isProjectDirty = false;
        UpdateAppTitle();
        ProjectService.SetLastSessionProjectPath(null);
    }

    private void CreateNewProject(string projectName)
    {
        _currentProject = new ProjectData
        {
            Name = projectName,
            IsCustomNamed = true,
            CreatedAt = DateTime.Now,
            LastModified = DateTime.Now
        };
        _currentProjectFilePath = null;
        _savedRegions.Clear();
        RefreshSavedGrid();
        mediaPanel.ClearItems();

        // Reset canvas & crop
        canvas.Image?.Dispose();
        canvas.Image = null;
        lblImageInfo.Text = "Ảnh: Chưa nạp";
        canvas.ImageOverlayInfo = "Chưa nạp ảnh (Kéo & thả ảnh từ Media vào đây)";
        lblCursorInfo.Text = "Chuột: -";
        canvas.SetCropRect(Rectangle.Empty);
        _isUpdatingInputs = true;
        numX.Value = 0;
        numY.Value = 0;
        numW.Value = 0;
        numH.Value = 0;
        _isUpdatingInputs = false;

        _isProjectDirty = false;
        UpdateAppTitle();
        MessageBox.Show($"Đã tạo dự án mới: '{projectName}'!\nBạn có thể nạp ảnh, kéo thả ảnh hoặc dán ảnh vào để bắt đầu làm việc.", "Dự án mới", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenProjectFromFile(string filePath, bool showMessage = true)
    {
        try
        {
            var proj = ProjectService.LoadProject(filePath);
            if (proj == null)
            {
                if (showMessage)
                {
                    MessageBox.Show("Không thể đọc tệp dự án đã chọn.", "Lỗi tải dự án", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            proj.IsCustomNamed = true;
            _currentProject = proj;
            _currentProjectFilePath = filePath;

            // Restore media items
            mediaPanel.ClearItems();
            if (proj.MediaItems != null && proj.MediaItems.Count > 0)
            {
                foreach (var m in proj.MediaItems)
                {
                    if (!string.IsNullOrEmpty(m.ImageBase64) && m.Bitmap == null)
                    {
                        m.Bitmap = ProjectService.BitmapFromBase64(m.ImageBase64);
                    }
                }
                mediaPanel.SetItems(proj.MediaItems);
            }

            // Restore main image
            if (!string.IsNullOrEmpty(proj.ImageBase64))
            {
                var img = ProjectService.ImageFromBase64(proj.ImageBase64);
                if (img is Bitmap bmp)
                {
                    SetSourceImage(bmp, proj.Name);
                    mediaPanel.SetActiveItemByName(proj.Name);
                }
            }
            else
            {
                canvas.Image = null;
                lblImageInfo.Text = "Ảnh: Chưa nạp";
                canvas.ImageOverlayInfo = "Chưa nạp ảnh (Kéo & thả ảnh từ Media vào đây)";
            }

            // Restore saved regions
            _savedRegions.Clear();
            if (proj.SavedRegions != null && proj.SavedRegions.Count > 0)
            {
                foreach (var r in proj.SavedRegions)
                {
                    if (!string.IsNullOrEmpty(r.SourceImageBase64) && r.SourceBitmap == null)
                    {
                        r.SourceBitmap = ProjectService.BitmapFromBase64(r.SourceImageBase64);
                    }
                }
                _savedRegions.AddRange(proj.SavedRegions);
            }
            RefreshSavedGrid();

            // Restore crop rect & aspect ratio
            canvas.LockedAspectRatio = proj.LockedAspectRatio;
            if (proj.CropW > 0 && proj.CropH > 0)
            {
                Rectangle r = new(proj.CropX, proj.CropY, proj.CropW, proj.CropH);
                canvas.SetCropRect(r);
                UpdateInputsFromCropRect(r);
            }

            _isProjectDirty = false;
            UpdateAppTitle();
        }
        catch (Exception ex)
        {
            if (showMessage)
            {
                MessageBox.Show($"Lỗi khi mở dự án: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void SaveCurrentProject()
    {
        bool isDefaultOrRandom = _currentProject == null ||
                                 !_currentProject.IsCustomNamed ||
                                 string.IsNullOrWhiteSpace(_currentProject.Name) ||
                                 _currentProject.Name.StartsWith("Dự án_", StringComparison.OrdinalIgnoreCase);

        if (isDefaultOrRandom)
        {
            using ProjectNameDialog dlg = new("Lưu dự án", "Dự án đang dùng tên tạm thời. Vui lòng đặt tên chính thức cho dự án:", "", _currentProject?.Id);
            if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.ProjectName))
            {
                return;
            }

            _currentProject ??= new ProjectData();
            _currentProject.Name = dlg.ProjectName;
            _currentProject.IsCustomNamed = true;
        }

        _currentProject ??= new ProjectData();
        FlushActiveMediaState();

        // Capture main capture image & thumbnail if canvas.Image is present
        if (canvas.Image != null)
        {
            _currentProject.ImageBase64 = ProjectService.ImageToBase64(canvas.Image);
            _currentProject.ThumbnailBase64 = ProjectService.GenerateThumbnailBase64(canvas.Image);
            _currentProject.ImageWidth = canvas.Image.Width;
            _currentProject.ImageHeight = canvas.Image.Height;
        }
        else
        {
            _currentProject.ImageBase64 = null;
            _currentProject.ThumbnailBase64 = null;
            _currentProject.ImageWidth = 0;
            _currentProject.ImageHeight = 0;
        }

        Rectangle crop = canvas.CropRect;
        _currentProject.CropX = crop.X;
        _currentProject.CropY = crop.Y;
        _currentProject.CropW = crop.Width;
        _currentProject.CropH = crop.Height;
        _currentProject.LockedAspectRatio = canvas.LockedAspectRatio;

        // Serialize MediaItems
        _currentProject.MediaItems.Clear();
        foreach (var m in mediaPanel.Items)
        {
            if (m.Bitmap != null && string.IsNullOrEmpty(m.ImageBase64))
            {
                m.ImageBase64 = ProjectService.ImageToBase64(m.Bitmap);
            }
            if (m.Bitmap != null && string.IsNullOrEmpty(m.ThumbnailBase64))
            {
                m.ThumbnailBase64 = ProjectService.GenerateThumbnailBase64(m.Bitmap);
            }
            _currentProject.MediaItems.Add(m);
        }

        // Ensure all saved regions have their SourceImageBase64 serialized
        foreach (var r in _savedRegions)
        {
            if (r.SourceBitmap != null && string.IsNullOrEmpty(r.SourceImageBase64))
            {
                r.SourceImageBase64 = ProjectService.ImageToBase64(r.SourceBitmap);
            }
        }
        _currentProject.SavedRegions = new List<CropRegionItem>(_savedRegions);

        try
        {
            string savedPath = ProjectService.SaveProject(_currentProject, _currentProjectFilePath);
            _currentProjectFilePath = savedPath;
            _isProjectDirty = false;
            UpdateAppTitle();
            tipActions.SetToolTip(btnSaveProject, $"Lưu dự án (Đã lưu lúc {DateTime.Now:HH:mm:ss})");
            MessageBox.Show($"Đã lưu dự án '{_currentProject.Name}' thành công!\nĐường dẫn: {savedPath}", "Lưu dự án thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu dự án: {ex.Message}", "Lỗi lưu dự án", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AutoSaveProjectSilently()
    {
        if (!_isProjectDirty) return;
        if (canvas.Image == null && _savedRegions.Count == 0 && mediaPanel.Items.Count == 0) return;
        if (_currentProject == null || !_currentProject.IsCustomNamed || string.IsNullOrWhiteSpace(_currentProject.Name))
        {
            // Do NOT silently auto-save if project hasn't been named yet
            return;
        }

        try
        {
            FlushActiveMediaState();
            if (canvas.Image != null)
            {
                _currentProject.ImageBase64 = ProjectService.ImageToBase64(canvas.Image);
                _currentProject.ThumbnailBase64 = ProjectService.GenerateThumbnailBase64(canvas.Image);
                _currentProject.ImageWidth = canvas.Image.Width;
                _currentProject.ImageHeight = canvas.Image.Height;
            }
            else
            {
                _currentProject.ImageBase64 = null;
                _currentProject.ThumbnailBase64 = null;
                _currentProject.ImageWidth = 0;
                _currentProject.ImageHeight = 0;
            }

            Rectangle crop = canvas.CropRect;
            _currentProject.CropX = crop.X;
            _currentProject.CropY = crop.Y;
            _currentProject.CropW = crop.Width;
            _currentProject.CropH = crop.Height;
            _currentProject.LockedAspectRatio = canvas.LockedAspectRatio;

            // Serialize MediaItems
            _currentProject.MediaItems.Clear();
            foreach (var m in mediaPanel.Items)
            {
                if (m.Bitmap != null && string.IsNullOrEmpty(m.ImageBase64))
                {
                    m.ImageBase64 = ProjectService.ImageToBase64(m.Bitmap);
                }
                if (m.Bitmap != null && string.IsNullOrEmpty(m.ThumbnailBase64))
                {
                    m.ThumbnailBase64 = ProjectService.GenerateThumbnailBase64(m.Bitmap);
                }
                _currentProject.MediaItems.Add(m);
            }

            foreach (var r in _savedRegions)
            {
                if (r.SourceBitmap != null && string.IsNullOrEmpty(r.SourceImageBase64))
                {
                    r.SourceImageBase64 = ProjectService.ImageToBase64(r.SourceBitmap);
                }
            }
            _currentProject.SavedRegions = new List<CropRegionItem>(_savedRegions);

            string savedPath = ProjectService.SaveProject(_currentProject, _currentProjectFilePath);
            _currentProjectFilePath = savedPath;
            _isProjectDirty = false;

            UpdateAppTitle();
            tipActions.SetToolTip(btnSaveProject, $"Lưu dự án (Tự động lưu lúc {DateTime.Now:HH:mm:ss})");
        }
        catch
        {
            // Silent error suppression for background auto-save
        }
    }

    #endregion
}
