#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper;

partial class MainCropperForm
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".webp", ".gif", ".ico", ".tiff", ".tif"
    };

    private static bool IsSupportedImageFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        string ext = Path.GetExtension(path);
        return SupportedImageExtensions.Contains(ext);
    }

    private void OpenImageFromFile()
    {
        using OpenFileDialog ofd = new()
        {
            Title = "Chọn ảnh nạp vào Media",
            Filter = "Ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif|Tất cả tệp (*.*)|*.*",
            Multiselect = true
        };

        if (ofd.ShowDialog(this) == DialogResult.OK && ofd.FileNames.Length > 0)
        {
            ImportFilesToMedia(ofd.FileNames, setAsMain: canvas.Image == null);
        }
    }

    private void ImportFilesToMedia(string[] filePaths, bool setAsMain = false)
    {
        if (filePaths.Length == 0) return;
        MediaItem? firstLoaded = null;

        foreach (var path in filePaths)
        {
            if (!IsSupportedImageFile(path)) continue;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var srcBmp = new Bitmap(stream);
                Bitmap cloned = new(srcBmp.Width, srcBmp.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(cloned))
                {
                    g.DrawImage(srcBmp, 0, 0, srcBmp.Width, srcBmp.Height);
                }

                MediaItem item = new()
                {
                    Name = Path.GetFileName(path),
                    Bitmap = cloned,
                    Width = cloned.Width,
                    Height = cloned.Height,
                    ThumbnailBase64 = ProjectService.GenerateThumbnailBase64(cloned)
                };

                mediaPanel.AddItem(item);
                firstLoaded ??= item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing {path}: {ex.Message}");
            }
        }

        if (firstLoaded != null && (setAsMain || canvas.Image == null))
        {
            LoadMediaItemToMain(firstLoaded);
        }

        _isProjectDirty = true;
    }

    private void ImportBitmapToMedia(Bitmap bmp, string name, bool setAsMain = true)
    {
        Bitmap cloned = new(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(cloned))
        {
            g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
        }

        MediaItem item = new()
        {
            Name = name,
            Bitmap = cloned,
            Width = cloned.Width,
            Height = cloned.Height,
            ThumbnailBase64 = ProjectService.GenerateThumbnailBase64(cloned)
        };

        mediaPanel.AddItem(item);

        if (setAsMain || canvas.Image == null)
        {
            LoadMediaItemToMain(item);
        }

        _isProjectDirty = true;
    }

    private void LoadMediaItemToMain(MediaItem item)
    {
        // Flush state of previous active media item
        FlushActiveMediaState();

        bool isAlive = false;
        if (item.Bitmap != null)
        {
            try { _ = item.Bitmap.Width; isAlive = true; } catch { isAlive = false; }
        }

        if (!isAlive && !string.IsNullOrEmpty(item.ImageBase64))
        {
            item.Bitmap = ProjectService.BitmapFromBase64(item.ImageBase64);
            isAlive = item.Bitmap != null;
        }

        if (isAlive && item.Bitmap != null)
        {
            _activeMediaItem = item;
            _currentSourceName = item.Name;

            Rectangle? savedCrop = (item.SavedCropW.HasValue && item.SavedCropW.Value > 0 && item.SavedCropH.HasValue && item.SavedCropH.Value > 0)
                ? new Rectangle(item.SavedCropX ?? 0, item.SavedCropY ?? 0, item.SavedCropW.Value, item.SavedCropH.Value)
                : null;
            float? savedZoom = item.SavedZoomFactor;
            PointF? savedPan = (item.SavedPanX.HasValue && item.SavedPanY.HasValue)
                ? new PointF(item.SavedPanX.Value, item.SavedPanY.Value)
                : null;

            canvas.SetImageWithState(item.Bitmap, savedCrop, savedZoom, savedPan);

            lblImageInfo.Text = $"Ảnh: {item.Name} ({item.Bitmap.Width} × {item.Bitmap.Height} px)";
            canvas.ImageOverlayInfo = lblImageInfo.Text;
            mediaPanel.SetActiveItem(item);

            numX.Maximum = Math.Max(0, item.Bitmap.Width - 1);
            numY.Maximum = Math.Max(0, item.Bitmap.Height - 1);
            numW.Maximum = item.Bitmap.Width;
            numH.Maximum = item.Bitmap.Height;

            UpdateInputsFromCropRect(canvas.CropRect);

            if (chkGrayscale.Checked || chkThreshold.Checked)
            {
                ApplyCurrentFilters();
            }
            else
            {
                if (_currentFilteredBitmap != null)
                {
                    _currentFilteredBitmap.Dispose();
                    _currentFilteredBitmap = null;
                }
            }

            if (_currentProject == null)
            {
                _currentProject = new ProjectData
                {
                    Name = GenerateDefaultProjectName(),
                    IsCustomNamed = false
                };
            }

            UpdateAppTitle();
            _isProjectDirty = true;
        }
    }

    private void FlushActiveMediaState()
    {
        if (_activeMediaItem != null && canvas.Image != null)
        {
            _activeMediaItem.SavedCropX = canvas.CropRect.X;
            _activeMediaItem.SavedCropY = canvas.CropRect.Y;
            _activeMediaItem.SavedCropW = canvas.CropRect.Width;
            _activeMediaItem.SavedCropH = canvas.CropRect.Height;
            _activeMediaItem.SavedZoomFactor = canvas.ZoomFactor;
            _activeMediaItem.SavedPanX = canvas.PanOffset.X;
            _activeMediaItem.SavedPanY = canvas.PanOffset.Y;
        }
    }

    private void OnMediaItemDoubleClicked(MediaItem item) => LoadMediaItemToMain(item);

    private void OnMediaItemDeleted(MediaItem item)
    {
        if (_activeMediaItem == item)
        {
            _activeMediaItem = null;
        }

        if (canvas.Image != null && (canvas.Image == item.Bitmap || string.Equals(_currentSourceName, item.Name, StringComparison.OrdinalIgnoreCase)))
        {
            canvas.Image = null;
            lblImageInfo.Text = "Ảnh: Chưa nạp";
            canvas.ImageOverlayInfo = "Chưa nạp ảnh (Kéo & thả ảnh từ Media vào đây)";
            canvas.SetCropRect(Rectangle.Empty);
            canvas.Invalidate();
        }
        try { item.Bitmap?.Dispose(); } catch { }
        item.Bitmap = null;
    }

    private void LoadImageFromPath(string filePath) => ImportFilesToMedia(new[] { filePath }, setAsMain: true);

    private void InitializeDragDropImageLoading()
    {
        // Canvas drag drop
        canvas.AllowDrop = true;
        canvas.DragEnter += OnDragDropFileEnter;
        canvas.DragOver += OnDragDropFileOver;
        canvas.DragDrop += OnDragDropFileDrop;

        // Container panel drag drop
        pnlCanvasContainer.AllowDrop = true;
        pnlCanvasContainer.DragEnter += OnDragDropFileEnter;
        pnlCanvasContainer.DragOver += OnDragDropFileOver;
        pnlCanvasContainer.DragDrop += OnDragDropFileDrop;

        // Form level drag drop
        this.AllowDrop = true;
        this.DragEnter += OnDragDropFileEnter;
        this.DragOver += OnDragDropFileOver;
        this.DragDrop += OnDragDropFileDrop;
    }

    private void OnDragDropFileEnter(object? sender, DragEventArgs e)
    {
        bool canDrop = e.Data != null && (
            e.Data.GetDataPresent(typeof(MediaItem)) ||
            e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetDataPresent(DataFormats.Bitmap));
        e.Effect = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragDropFileOver(object? sender, DragEventArgs e) => OnDragDropFileEnter(sender, e);

    private void OnDragDropFileDrop(object? sender, DragEventArgs e)
    {
        if (e.Data == null) return;

        if (e.Data.GetData(typeof(MediaItem)) is MediaItem mediaItem)
        {
            LoadMediaItemToMain(mediaItem);
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                ImportFilesToMedia(files, setAsMain: true);
            }
        }
        else if (e.Data.GetDataPresent(DataFormats.Bitmap))
        {
            if (e.Data.GetData(DataFormats.Bitmap) is Bitmap bmp)
            {
                ImportBitmapToMedia(bmp, $"Kéo_thả_{DateTime.Now:yyyyMMdd_HHmmss}", setAsMain: true);
            }
        }
    }

    private void PasteFromClipboard()
    {
        if (Clipboard.ContainsImage())
        {
            try
            {
                Image? img = Clipboard.GetImage();
                if (img != null)
                {
                    Bitmap bmp = new(img.Width, img.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(img, 0, 0, img.Width, img.Height);
                    }
                    img.Dispose();
                    ImportBitmapToMedia(bmp, $"Clipboard_{DateTime.Now:HHmmss}", setAsMain: true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi dán ảnh: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show("Clipboard không chứa hình ảnh. Hãy nhấn PrintScreen hoặc Copy một ảnh trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task TriggerLiveCaptureAsync()
    {
        btnLiveCapture.Text = " Đang chụp...";
        btnLiveCapture.Enabled = false;

        try
        {
            // Capture all virtual screens, hiding our app briefly
            Bitmap? captured = await ScreenCaptureService.CaptureScreenAsync(this, allScreens: true, delayMs: 160);
            if (captured != null)
            {
                ImportBitmapToMedia(captured, $"LiveCapture_{DateTime.Now:HHmmss}", setAsMain: true);
            }
        }
        finally
        {
            btnLiveCapture.Text = " Chụp Live (F9)";
            btnLiveCapture.Enabled = true;
        }
    }

    private async Task TriggerWindowCaptureAsync()
    {
        btnWindowCapture.Text = " Đang chụp...";
        btnWindowCapture.Enabled = false;

        try
        {
            Bitmap? captured = await ScreenCaptureService.CaptureForegroundWindowAsync(this, delayMs: 160);
            if (captured != null)
            {
                ImportBitmapToMedia(captured, $"Window_{DateTime.Now:HHmmss}", setAsMain: true);
            }
        }
        finally
        {
            btnWindowCapture.Text = " Cửa sổ khác";
            btnWindowCapture.Enabled = true;
        }
    }

    private void SetSourceImage(Bitmap bmp, string sourceName)
    {
        _currentSourceName = sourceName;
        // Do not dispose canvas.Image here because bitmaps are managed by mediaPanel
        canvas.Image = bmp;

        lblImageInfo.Text = $"Ảnh: {sourceName} ({bmp.Width} × {bmp.Height} px)";
        canvas.ImageOverlayInfo = lblImageInfo.Text;

        // Ensure image is also in Media Panel
        MediaItem? matching = mediaPanel.Items.FirstOrDefault(m => string.Equals(m.Name, sourceName, StringComparison.OrdinalIgnoreCase));
        if (matching == null)
        {
            matching = new()
            {
                Name = sourceName,
                Bitmap = bmp,
                Width = bmp.Width,
                Height = bmp.Height,
                ThumbnailBase64 = ProjectService.GenerateThumbnailBase64(bmp)
            };
            mediaPanel.AddItem(matching);
        }
        _activeMediaItem = matching;
        mediaPanel.SetActiveItemByName(sourceName);

        // Set max limits for coordinates
        numX.Maximum = Math.Max(0, bmp.Width - 1);
        numY.Maximum = Math.Max(0, bmp.Height - 1);
        numW.Maximum = bmp.Width;
        numH.Maximum = bmp.Height;

        // Sync initial crop
        UpdateInputsFromCropRect(canvas.CropRect);

        if (_currentProject == null)
        {
            _currentProject = new ProjectData
            {
                Name = GenerateDefaultProjectName(),
                IsCustomNamed = false
            };
        }

        UpdateAppTitle();
        _isProjectDirty = true;
    }
}
