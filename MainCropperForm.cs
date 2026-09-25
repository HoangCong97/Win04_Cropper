using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Controls;
using Win04_Cropper.Models;
using Win04_Cropper.Services;
using System.Text.Json;
using System.Text;
using System.Text.Encodings.Web;

namespace Win04_Cropper;

public partial class MainCropperForm : Form
{
    private const int HOTKEY_ID_F9 = 9001;

    private readonly Bitmap _editIconBmp;
    private readonly Bitmap _deleteIconBmp;
    private readonly Bitmap _coordIconBmp;
    private readonly Bitmap _imageIconBmp;

    private readonly List<CropRegionItem> _savedRegions = new();
    private bool _isUpdatingInputs;
    private CropRegionItem? _currentlyEditingItem;
    private readonly List<Button> _ratioButtons = [];
    private ProjectData? _currentProject;
    private string? _currentProjectFilePath;
    private string _currentSourceName = "";
    private readonly System.Windows.Forms.Timer _autoSaveTimer = new();
    private bool _isProjectDirty;
    private MediaItem? _activeMediaItem;

    internal DataGridView SavedGrid => dgvSavedRegions;
    internal List<CropRegionItem> SavedRegions => _savedRegions;
    internal void SetImageForTesting(Bitmap bmp, string name)
    {
        canvas.Image = bmp;
        _currentSourceName = name;
    }

    public MainCropperForm()
    {
        InitializeComponent();

        int gridIconSize = Math.Max(12, DpiScale(14));
        _editIconBmp = FormsIconHelper.ToBitmap(IconChar.PenToSquare, Color.White, gridIconSize);
        _deleteIconBmp = FormsIconHelper.ToBitmap(IconChar.TrashCan, Color.FromArgb(255, 120, 130), gridIconSize);
        _coordIconBmp = FormsIconHelper.ToBitmap(IconChar.LocationDot, Color.White, gridIconSize);
        _imageIconBmp = FormsIconHelper.ToBitmap(IconChar.Image, Color.White, gridIconSize);

        // Auto-save setup (every 15 seconds silently if project has changes)
        _autoSaveTimer.Interval = 15000;
        _autoSaveTimer.Tick += (s, e) => AutoSaveProjectSilently();
        _autoSaveTimer.Start();
        FormClosing += (s, e) => AutoSaveProjectSilently();

        // Ratio buttons collection
        _ratioButtons.AddRange([btnRatioFree, btnRatio3x4, btnRatio4x6, btnRatio9x16, btnRatio1x1, btnRatio4x3, btnRatio6x4, btnRatio16x9]);
        SetActiveRatioButton(btnRatioFree);

        // Canvas events
        canvas.CropRectChanged += OnCanvasCropRectChanged;
        canvas.CursorMovedOnImage += OnCanvasCursorMoved;
        canvas.ZoomChanged += OnCanvasZoomChanged;

        // Initialize Drag & Drop for images
        InitializeDragDropImageLoading();

        // Wire Media Panel events
        mediaPanel.MediaSelected += LoadMediaItemToMain;
        mediaPanel.MediaDoubleClicked += OnMediaItemDoubleClicked;
        mediaPanel.ImportRequested += () => OpenImageFromFile();
        mediaPanel.PasteRequested += () => PasteFromClipboard();
        mediaPanel.FilesDropped += files => ImportFilesToMedia(files, setAsMain: true);
        mediaPanel.BitmapDropped += bmp => ImportBitmapToMedia(bmp, $"Kéo_thả_{DateTime.Now:yyyyMMdd_HHmmss}", setAsMain: true);
        mediaPanel.MediaDeleted += OnMediaItemDeleted;

        // Initialize default project as blank
        _currentProject = null;
        _currentProjectFilePath = null;
        UpdateAppTitle();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            string appIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
            if (File.Exists(appIconPath))
            {
                this.Icon = new System.Drawing.Icon(appIconPath);
            }
            else
            {
                var exeIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (exeIcon != null) this.Icon = exeIcon;
            }
        }
        catch { }

        // Register Global Hotkey F9
        try
        {
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID_F9, NativeMethods.MOD_NOREPEAT, (uint)Keys.F9);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RegisterHotKey failed: {ex.Message}");
        }

        // Check and restore project from previous session if available
        string? lastProject = ProjectService.GetLastSessionProjectPath();
        if (!string.IsNullOrEmpty(lastProject) && File.Exists(lastProject))
        {
            OpenProjectFromFile(lastProject, showMessage: false);
        }
        else
        {
            ResetToBlankApp();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            if (splitMain.Height > 100)
            {
                splitMain.SplitterDistance = Math.Clamp((int)(splitMain.Height * 0.62), 50, splitMain.Height - 50);
            }
            if (splitTop.Width > 0)
            {
                splitTop.Panel1MinSize = 0;
                splitTop.Panel2MinSize = 0;
                int desiredPropsWidth = DpiScale(370);
                splitTop.SplitterDistance = Math.Clamp(splitTop.Width - desiredPropsWidth, 100, Math.Max(100, splitTop.Width - 100));
                splitTop.Panel1MinSize = DpiScale(500);
                splitTop.Panel2MinSize = DpiScale(360);
            }
            this.PerformLayout();
            if (splitMediaCanvas.Width > 0)
            {
                splitMediaCanvas.Panel1MinSize = 0;
                splitMediaCanvas.Panel2MinSize = 0;
                int desiredMediaWidth = DpiScale(350);
                if (splitMediaCanvas.Width > desiredMediaWidth + 50)
                {
                    splitMediaCanvas.SplitterDistance = desiredMediaWidth;
                }
                else if (splitMediaCanvas.Width > 150)
                {
                    splitMediaCanvas.SplitterDistance = Math.Clamp(desiredMediaWidth, 100, Math.Max(100, splitMediaCanvas.Width - 100));
                }
                splitMediaCanvas.Panel1MinSize = DpiScale(180);
                splitMediaCanvas.Panel2MinSize = DpiScale(200);
            }
            mediaPanel.UpdateView();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnShown layout error: {ex}");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        // Unregister Hotkey
        try
        {
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID_F9);
        }
        catch { }

        // Save last session project path
        try
        {
            if (_currentProject != null && _currentProject.IsCustomNamed && !string.IsNullOrEmpty(_currentProjectFilePath) && File.Exists(_currentProjectFilePath))
            {
                ProjectService.SetLastSessionProjectPath(_currentProjectFilePath);
            }
            else
            {
                ProjectService.SetLastSessionProjectPath(null);
            }
        }
        catch { }
    }

    private const int WM_ENTERSIZEMOVE = 0x0231;
    private const int WM_EXITSIZEMOVE = 0x0232;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID_F9)
        {
            _ = TriggerLiveCaptureAsync();
            return;
        }

        switch (m.Msg)
        {
            case WM_ENTERSIZEMOVE:
                this.SuspendLayout();
                break;
            case WM_EXITSIZEMOVE:
                this.ResumeLayout(true);
                this.Invalidate(true);
                break;
        }

        base.WndProc(ref m);
    }

    #region Keyboard Shortcuts

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Ctrl+O: Open file
        if (keyData == (Keys.Control | Keys.O))
        {
            OpenImageFromFile();
            return true;
        }

        // Ctrl+Shift+P: Manage Projects
        if (keyData == (Keys.Control | Keys.Shift | Keys.P))
        {
            ShowProjectDialog();
            return true;
        }

        // Ctrl+Alt+S: Save Project
        if (keyData == (Keys.Control | Keys.Alt | Keys.S))
        {
            SaveCurrentProject();
            return true;
        }

        // Ctrl+V: Paste from clipboard
        if (keyData == (Keys.Control | Keys.V))
        {
            PasteFromClipboard();
            return true;
        }

        // Ctrl+S: Crop and save image
        if (keyData == (Keys.Control | Keys.S))
        {
            CropAndSaveImage();
            return true;
        }

        // Ctrl+Shift+S or Ctrl+D: Save coordinates
        if (keyData == (Keys.Control | Keys.Shift | Keys.S) || keyData == (Keys.Control | Keys.D))
        {
            SaveCurrentCoordinates();
            return true;
        }

        // Ctrl+C: Copy cropped image
        if (keyData == (Keys.Control | Keys.C))
        {
            CopyCroppedImageToClipboard();
            return true;
        }

        // F9: Live Capture
        if (keyData == Keys.F9)
        {
            _ = TriggerLiveCaptureAsync();
            return true;
        }

        // Arrow keys nudge when not focusing inputs or grid
        Keys keyCode = keyData & Keys.KeyCode;
        if (keyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            if (!numX.Focused && !numY.Focused && !numW.Focused && !numH.Focused && !dgvSavedRegions.Focused)
            {
                int step = 1;
                if ((keyData & Keys.Shift) == Keys.Shift) step = 50;
                else if ((keyData & Keys.Control) == Keys.Control) step = 10;

                switch (keyCode)
                {
                    case Keys.Left: NudgeCrop(-step, 0); return true;
                    case Keys.Right: NudgeCrop(step, 0); return true;
                    case Keys.Up: NudgeCrop(0, -step); return true;
                    case Keys.Down: NudgeCrop(0, step); return true;
                }
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    #endregion

    #region Image Loading & Screen Capture

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

    private void OnMediaItemDoubleClicked(MediaItem item)
    {
        LoadMediaItemToMain(item);
    }

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

    private void LoadImageFromPath(string filePath)
    {
        ImportFilesToMedia(new[] { filePath }, setAsMain: true);
    }

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

    private static string GenerateDefaultProjectName() => $"Dự án_{DateTime.Now:yyyyMMdd_HHmm}";

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
        if (e.Data != null && (
            e.Data.GetDataPresent(typeof(MediaItem)) ||
            e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetDataPresent(DataFormats.Bitmap)))
        {
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void OnDragDropFileOver(object? sender, DragEventArgs e)
    {
        if (e.Data != null && (
            e.Data.GetDataPresent(typeof(MediaItem)) ||
            e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetDataPresent(DataFormats.Bitmap)))
        {
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

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

    #endregion

    #region Canvas Event Handlers & Input Sync

    private void OnCanvasCropRectChanged(Rectangle rect)
    {
        if (_isUpdatingInputs) return;

        UpdateInputsFromCropRect(rect);
        _isProjectDirty = true;
    }

    private void OnCanvasCursorMoved(Point imgPt, Color? pixelColor)
    {
        if (canvas.Image == null)
        {
            lblCursorInfo.Text = "Chuột: -";
            return;
        }

        if (imgPt.X >= 0 && imgPt.X < canvas.Image.Width && imgPt.Y >= 0 && imgPt.Y < canvas.Image.Height)
        {
            string colorHex = pixelColor.HasValue
                ? $" | #{pixelColor.Value.R:X2}{pixelColor.Value.G:X2}{pixelColor.Value.B:X2}"
                : "";
            lblCursorInfo.Text = $"Chuột: X={imgPt.X}, Y={imgPt.Y}{colorHex}";
        }
        else
        {
            lblCursorInfo.Text = $"Chuột: X={imgPt.X}, Y={imgPt.Y} (ngoài ảnh)";
        }
    }

    private void OnCanvasZoomChanged(float zoom)
    {
        lblZoomValue.Text = $"{Math.Round(zoom * 100)}%";
    }

    private void UpdateInputsFromCropRect(Rectangle rect)
    {
        _isUpdatingInputs = true;
        try
        {
            numX.Value = Math.Clamp(rect.X, numX.Minimum, numX.Maximum);
            numY.Value = Math.Clamp(rect.Y, numY.Minimum, numY.Maximum);
            numW.Value = Math.Clamp(rect.Width, numW.Minimum, numW.Maximum);
            numH.Value = Math.Clamp(rect.Height, numH.Minimum, numH.Maximum);

            if (lblAspectRatio != null)
            {
                lblAspectRatio.Text = $"Tỉ lệ: {GetRatioStr(rect.Width, rect.Height)}";
            }
        }
        finally
        {
            _isUpdatingInputs = false;
        }
    }

    private void OnNumericInputChanged()
    {
        if (_isUpdatingInputs || canvas.Image == null) return;

        int newX = (int)numX.Value;
        int newY = (int)numY.Value;
        int newW = (int)numW.Value;
        int newH = (int)numH.Value;

        if (canvas.LockedAspectRatio is float ratio && ratio > 0)
        {
            Rectangle curCrop = canvas.CropRect;
            bool wChanged = (newW != curCrop.Width);
            bool hChanged = (newH != curCrop.Height);

            if (numW.Focused || (wChanged && !hChanged))
            {
                newH = (int)Math.Round(newW / ratio);
                _isUpdatingInputs = true;
                numH.Value = Math.Clamp(newH, numH.Minimum, numH.Maximum);
                _isUpdatingInputs = false;
            }
            else if (numH.Focused || (hChanged && !wChanged))
            {
                newW = (int)Math.Round(newH * ratio);
                _isUpdatingInputs = true;
                numW.Value = Math.Clamp(newW, numW.Minimum, numW.Maximum);
                _isUpdatingInputs = false;
            }
            else if (wChanged)
            {
                newH = (int)Math.Round(newW / ratio);
                _isUpdatingInputs = true;
                numH.Value = Math.Clamp(newH, numH.Minimum, numH.Maximum);
                _isUpdatingInputs = false;
            }
        }

        Rectangle newRect = new(newX, newY, newW, newH);
        canvas.SetCropRect(newRect);
        if (lblAspectRatio != null)
        {
            lblAspectRatio.Text = $"Tỉ lệ: {GetRatioStr(newRect.Width, newRect.Height)}";
        }
    }

    private void SetActiveRatioButton(Button? activeBtn)
    {
        foreach (var b in _ratioButtons)
        {
            if (b == activeBtn)
            {
                b.BackColor = Color.FromArgb(0, 122, 204);
                b.ForeColor = Color.White;
            }
            else
            {
                b.BackColor = Color.FromArgb(50, 55, 68);
                b.ForeColor = Color.White;
            }
        }
    }

    private void ApplyAspectRatio(int rw, int rh, Button btn)
    {
        SetActiveRatioButton(btn);

        if (rw <= 0 || rh <= 0)
        {
            // Tự do: cho phép thay đổi cả kích thước lẫn tỉ lệ
            canvas.LockedAspectRatio = null;
            return;
        }

        float targetRatio = (float)rw / rh;
        canvas.LockedAspectRatio = targetRatio;

        if (canvas.Image != null)
        {
            Rectangle cur = canvas.CropRect;
            int imgW = canvas.Image.Width;
            int imgH = canvas.Image.Height;

            int newW = cur.Width;
            int newH = (int)Math.Round(newW / targetRatio);

            if (newH > imgH)
            {
                newH = imgH;
                newW = (int)Math.Round(newH * targetRatio);
            }
            if (newW > imgW)
            {
                newW = imgW;
                newH = (int)Math.Round(newW / targetRatio);
            }

            int newX = Math.Clamp(cur.X + (cur.Width - newW) / 2, 0, Math.Max(0, imgW - newW));
            int newY = Math.Clamp(cur.Y + (cur.Height - newH) / 2, 0, Math.Max(0, imgH - newH));

            Rectangle newRect = new(newX, newY, Math.Max(1, newW), Math.Max(1, newH));
            canvas.SetCropRect(newRect);
            UpdateInputsFromCropRect(newRect);
        }
    }

    private void ApplyScreenResolution(int targetW, int targetH)
    {
        // Khi chọn section 2: có thể thay đổi được cả kích thước lẫn tỉ lệ -> chuyển về Tự do
        canvas.LockedAspectRatio = null;
        SetActiveRatioButton(btnRatioFree);

        if (canvas.Image != null)
        {
            int imgW = canvas.Image.Width;
            int imgH = canvas.Image.Height;

            int w = Math.Min(targetW, imgW);
            int h = Math.Min(targetH, imgH);

            Rectangle cur = canvas.CropRect;
            int centerX = cur.Width > 0 ? cur.X + cur.Width / 2 : imgW / 2;
            int centerY = cur.Height > 0 ? cur.Y + cur.Height / 2 : imgH / 2;

            int x = Math.Clamp(centerX - w / 2, 0, Math.Max(0, imgW - w));
            int y = Math.Clamp(centerY - h / 2, 0, Math.Max(0, imgH - h));

            Rectangle newRect = new(x, y, Math.Max(1, w), Math.Max(1, h));
            canvas.SetCropRect(newRect);
            UpdateInputsFromCropRect(newRect);
        }
        else
        {
            _isUpdatingInputs = true;
            numW.Value = Math.Clamp(targetW, numW.Minimum, numW.Maximum);
            numH.Value = Math.Clamp(targetH, numH.Minimum, numH.Maximum);
            _isUpdatingInputs = false;
        }
    }

    private void ApplyFullImageResolution()
    {
        canvas.LockedAspectRatio = null;
        SetActiveRatioButton(btnRatioFree);

        if (canvas.Image != null)
        {
            Rectangle fullRect = new(0, 0, canvas.Image.Width, canvas.Image.Height);
            canvas.SetCropRect(fullRect);
            UpdateInputsFromCropRect(fullRect);
        }
        else
        {
            _isUpdatingInputs = true;
            numX.Value = 0;
            numY.Value = 0;
            numW.Value = numW.Maximum;
            numH.Value = numH.Maximum;
            _isUpdatingInputs = false;
        }
    }

    #endregion

    #region Nudge and Resize Actions

    private int GetNudgeStep()
    {
        return cboNudgeStep.SelectedIndex switch
        {
            0 => 1,
            1 => 5,
            2 => 10,
            3 => 50,
            _ => 5
        };
    }

    private void NudgeCrop(int dx, int dy)
    {
        if (canvas.Image == null) return;

        Rectangle r = canvas.CropRect;
        int imgW = canvas.Image.Width;
        int imgH = canvas.Image.Height;

        int newX = Math.Clamp(r.X + dx, 0, Math.Max(0, imgW - r.Width));
        int newY = Math.Clamp(r.Y + dy, 0, Math.Max(0, imgH - r.Height));

        Rectangle moved = new(newX, newY, r.Width, r.Height);
        canvas.SetCropRect(moved);
    }

    private void ResizeCrop(int dw, int dh)
    {
        if (canvas.Image == null) return;

        Rectangle r = canvas.CropRect;
        int imgW = canvas.Image.Width;
        int imgH = canvas.Image.Height;

        int newW = Math.Clamp(r.Width + dw, 1, imgW - r.X);
        int newH = Math.Clamp(r.Height + dh, 1, imgH - r.Y);

        Rectangle resized = new(r.X, r.Y, newW, newH);
        canvas.SetCropRect(resized);
    }

    private void CenterCropBox()
    {
        if (canvas.Image == null) return;

        Rectangle r = canvas.CropRect;
        int cx = (canvas.Image.Width - r.Width) / 2;
        int cy = (canvas.Image.Height - r.Height) / 2;

        canvas.SetCropRect(new Rectangle(cx, cy, r.Width, r.Height));
    }

    #endregion

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

    #region Saved Regions Management

    private string GenerateUniqueName(string prefix)
    {
        int maxIndex = 0;
        foreach (var r in _savedRegions)
        {
            if (r.Name != null && r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string suffix = r.Name.Substring(prefix.Length).Trim();
                if (int.TryParse(suffix, out int n) && n > maxIndex)
                {
                    maxIndex = n;
                }
            }
        }
        int nextIndex = maxIndex + 1;
        while (_savedRegions.Any(r => string.Equals(r.Name, $"{prefix}{nextIndex}", StringComparison.OrdinalIgnoreCase)))
        {
            nextIndex++;
        }
        return $"{prefix}{nextIndex}";
    }

    private void SaveCurrentCoordinates()
    {
        Rectangle r = canvas.CropRect;
        string defaultName = _currentlyEditingItem?.Name ?? GenerateUniqueName("Area ");
        string? origName = _currentlyEditingItem?.Name;

        using NameInputDialog dlg = new(
            r,
            defaultName: defaultName,
            existingNames: _savedRegions.Select(r => r.Name),
            originalName: origName,
            title: _currentlyEditingItem != null ? $"Lưu / Cập nhật tọa độ: {origName}" : "Lưu đối tượng tọa độ",
            headerText: _currentlyEditingItem != null ? "Cập nhật hoặc Lưu mới tọa độ" : "Lưu đối tượng tọa độ vào dự án",
            headerIcon: IconChar.FloppyDisk);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

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
            _currentlyEditingItem.Name = dlg.RegionName;
            _currentlyEditingItem.X = r.X;
            _currentlyEditingItem.Y = r.Y;
            _currentlyEditingItem.Width = r.Width;
            _currentlyEditingItem.Height = r.Height;
            _currentlyEditingItem.Notes = dlg.Notes;
            _currentlyEditingItem.ItemType = CropItemType.Coordinate;
            _currentlyEditingItem.SourceBitmap = sourceBmp;
            _currentlyEditingItem.SourceImageBase64 = sourceB64;
            _currentlyEditingItem.SourceImageName = sourceName;

            CancelEditing();
            RefreshSavedGrid();
            _isProjectDirty = true;
        }
        else
        {
            CropRegionItem item = new()
            {
                ItemType = CropItemType.Coordinate,
                Name = dlg.RegionName,
                X = r.X,
                Y = r.Y,
                Width = r.Width,
                Height = r.Height,
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

    private void EditRegionItem(int index)
    {
        if (index < 0 || index >= _savedRegions.Count) return;
        LoadRegionToEditor(_savedRegions[index]);
    }

    private void LoadRegionToEditor(CropRegionItem item)
    {
        _currentlyEditingItem = item;

        // Restore source image if saved with this object
        if (item.SourceBitmap != null || !string.IsNullOrEmpty(item.SourceImageBase64))
        {
            Bitmap? bmpToUse = item.SourceBitmap != null ? new Bitmap(item.SourceBitmap) : ProjectService.BitmapFromBase64(item.SourceImageBase64);
            if (bmpToUse != null)
            {
                SetSourceImage(bmpToUse, item.SourceImageName ?? item.Name);
            }
        }

        // Apply coordinates to canvas
        Rectangle rect = new(item.X, item.Y, item.Width, item.Height);
        canvas.SetCropRect(rect);
        UpdateInputsFromCropRect(rect);
        canvas.LockedAspectRatio = null;
        SetActiveRatioButton(btnRatioFree);

        // Update UI status banner in properties panel
        lblCoordSection.Text = $"ĐANG SỬA: [{item.Name.ToUpper()}]";
        lblCoordSection.ForeColor = Color.FromArgb(255, 205, 50); // Gold
        picCoordIcon.IconChar = IconChar.PenToSquare;
        picCoordIcon.IconColor = Color.FromArgb(255, 205, 50);

        btnCancelEdit.Visible = true;

        // Highlight in grid
        int rowIndex = _savedRegions.IndexOf(item);
        if (rowIndex >= 0 && rowIndex < dgvSavedRegions.Rows.Count)
        {
            dgvSavedRegions.ClearSelection();
            dgvSavedRegions.Rows[rowIndex].Selected = true;
        }
    }

    private void CancelEditing()
    {
        _currentlyEditingItem = null;
        lblCoordSection.Text = "Properties";
        lblCoordSection.ForeColor = Color.White;
        picCoordIcon.IconChar = IconChar.Sliders;
        picCoordIcon.IconColor = Color.White;
        btnCancelEdit.Visible = false;
        canvas.LockedAspectRatio = null;
        SetActiveRatioButton(btnRatioFree);
    }

    private void DeleteRegionItem(int index)
    {
        if (index < 0 || index >= _savedRegions.Count) return;

        var item = _savedRegions[index];
        if (MessageBox.Show($"Bạn có chắc chắn muốn xóa {item.TypeDisplay} '{item.Name}' khỏi danh sách?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _savedRegions.RemoveAt(index);
            if (_currentlyEditingItem == item) CancelEditing();
            RefreshSavedGrid();
        }
    }

    private void LoadSelectedRegionToEditor()
    {
        if (dgvSavedRegions.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn một dòng trong danh sách đã lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int index = dgvSavedRegions.SelectedRows[0].Index;
        if (index >= 0 && index < _savedRegions.Count)
        {
            EditRegionItem(index);
        }
    }

    private void UpdateSelectedRegionFromEditor()
    {
        if (dgvSavedRegions.SelectedRows.Count == 0 && _currentlyEditingItem == null)
        {
            MessageBox.Show("Vui lòng chọn một mục trong danh sách để cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        CropRegionItem target;
        if (_currentlyEditingItem != null)
        {
            target = _currentlyEditingItem;
        }
        else
        {
            int index = dgvSavedRegions.SelectedRows[0].Index;
            target = _savedRegions[index];
        }

        Rectangle r = canvas.CropRect;
        target.X = r.X;
        target.Y = r.Y;
        target.Width = r.Width;
        target.Height = r.Height;
        if (canvas.Image != null)
        {
            target.SourceBitmap = new Bitmap(canvas.Image);
            target.SourceImageBase64 = ProjectService.ImageToBase64(canvas.Image);
            target.SourceImageName = !string.IsNullOrEmpty(_currentSourceName) ? _currentSourceName : target.SourceImageName;
        }

        RefreshSavedGrid();
        CancelEditing();
        _isProjectDirty = true;
    }

    private void DeleteSelectedRegion()
    {
        if (dgvSavedRegions.SelectedRows.Count == 0) return;
        int index = dgvSavedRegions.SelectedRows[0].Index;
        DeleteRegionItem(index);
    }

    private void ClearAllSavedRegions()
    {
        if (_savedRegions.Count == 0) return;

        if (MessageBox.Show("Bạn có chắc chắn muốn xóa toàn bộ danh sách đã lưu?", "Xác nhận xóa hết", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            _savedRegions.Clear();
            CancelEditing();
            RefreshSavedGrid();
        }
    }

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
            string safeProjectName = !string.IsNullOrWhiteSpace(_currentProject?.Name)
                ? SanitizeFileName(_currentProject.Name)
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

            // Register canvas / active source image
            if (canvas.Image != null)
            {
                RegisterSource(_currentSourceName, canvas.Image);
            }

            // Register media panel items
            foreach (var mi in mediaPanel.Items)
            {
                if (mi.Bitmap != null)
                {
                    RegisterSource(mi.Name, mi.Bitmap);
                }
            }

            // Register source bitmaps from saved regions
            foreach (var r in _savedRegions)
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
            var areaItems = _savedRegions.Where(r => r.ItemType == CropItemType.Coordinate).ToList();
            var cropItems = _savedRegions.Where(r => r.ItemType == CropItemType.Image).ToList();

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
                if (srcBmp == null && canvas.Image != null)
                {
                    srcBmp = canvas.Image;
                }

                int srcW = srcBmp?.Width ?? 0;
                int srcH = srcBmp?.Height ?? 0;
                string srcFileName = "";
                if (srcBmp != null)
                {
                    string srcKey = r.SourceImageName ?? _currentSourceName;
                    if (sourceInfoDict.TryGetValue(srcKey, out var sInfo))
                    {
                        srcFileName = sInfo.FileName;
                    }
                    else
                    {
                        srcFileName = RegisterSource(srcKey, srcBmp);
                    }
                }

                // Extract and save crop image
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
                if (srcBmp == null && canvas.Image != null)
                {
                    srcBmp = canvas.Image;
                }

                int srcW = srcBmp?.Width ?? 0;
                int srcH = srcBmp?.Height ?? 0;
                string srcFileName = "";
                if (srcBmp != null)
                {
                    string srcKey = r.SourceImageName ?? _currentSourceName;
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
            sbReadme.AppendLine($"# D\u1EEF li\u1EC7u xu\u1EA5t: {packageName}");
            sbReadme.AppendLine();
            sbReadme.AppendLine($"- **D\u1EF1 \u00E1n**: {safeProjectName}");
            sbReadme.AppendLine($"- **Th\u1EDDi gian xu\u1EA5t**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sbReadme.AppendLine($"- **T\u1ED5ng s\u1ED1 v\u00F9ng t\u1ECDa \u0111\u1ED9 (Areas)**: {areasExportList.Count}");
            sbReadme.AppendLine($"- **T\u1ED5ng s\u1ED1 \u1EA3nh c\u1EAFt (Crops)**: {cropsExportList.Count}");
            sbReadme.AppendLine($"- **K\u00E8m \u1EA3nh g\u1ED1c**: {(exportSources ? "C\u00F3 (trong sources/)" : "Kh\u00F4ng")}");
            sbReadme.AppendLine();
            sbReadme.AppendLine("## 1. C\u1EA5u tr\u00FAc th\u01B0 m\u1EE5c");
            sbReadme.AppendLine("```");
            sbReadme.AppendLine($"{packageName}/");
            sbReadme.AppendLine("\u251C\u2500\u2500 README.md        <- T\u00E0i li\u1EC7u h\u01B0\u1EDBng d\u1EABn & m\u00F4 t\u1EA3 d\u1EEF li\u1EC7u");
            sbReadme.AppendLine("\u251C\u2500\u2500 data.json        <- Danh s\u00E1ch t\u1ECDa \u0111\u1ED9 v\u00E0 th\u00F4ng tin \u1EA3nh (s\u1EAFp x\u1EBFp theo Areas, Crops)");
            sbReadme.AppendLine("\u2514\u2500\u2500 crops/           <- Th\u01B0 m\u1EE5c ch\u1EE9a to\u00E0n b\u1ED9 \u1EA3nh \u0111\u00E3 c\u1EAFt (PNG)");
            if (exportSources)
            {
                sbReadme.AppendLine("\u2514\u2500\u2500 sources/         <- Th\u01B0 m\u1EE5c ch\u1EE9a c\u00E1c \u1EA3nh g\u1ED1c ban \u0111\u1EA7u (PNG)");
            }
            sbReadme.AppendLine("```");
            sbReadme.AppendLine();
            sbReadme.AppendLine("## 2. Quy c\u00E1ch t\u1EC7p data.json");
            sbReadme.AppendLine("T\u1EC7p `data.json` ch\u1EE9a th\u00F4ng tin chi ti\u1EBFt \u0111\u01B0\u1EE3c ph\u00E2n nh\u00F3m r\u00F5 r\u00E0ng theo 2 danh m\u1EE5c:");
            sbReadme.AppendLine("- **`areas`**: Danh s\u00E1ch c\u00E1c v\u00F9ng t\u1ECDa \u0111\u1ED9 quan t\u00E2m (ROI - Region of Interest).");
            sbReadme.AppendLine("- **`crops`**: Danh s\u00E1ch c\u00E1c \u0111\u1ED1i t\u01B0\u1EE3ng \u0111\u00E3 \u0111\u01B0\u1EE3c c\u1EAFt ra t\u1EC7p \u1EA3nh trong th\u01B0 m\u1EE5c `crops/`.");
            sbReadme.AppendLine();
            sbReadme.AppendLine("### H\u1EC7 t\u1ECDa \u0111\u1ED9:");
            sbReadme.AppendLine("- G\u1ED1c t\u1ECDa \u0111\u1ED9 `(0, 0)` n\u1EB1m \u1EDF g\u00F3c tr\u00EAn b\u00EAn tr\u00E1i (Top-Left) c\u1EE7a \u1EA3nh ngu\u1ED3n.");
            sbReadme.AppendLine("- `x`, `y`: T\u1ECDa \u0111\u1ED9 g\u00F3c tr\u00EAn b\u00EAn tr\u00E1i c\u1EE7a khung.");
            sbReadme.AppendLine("- `width`, `height`: K\u00EDch th\u01B0\u1EDBc pixel chi\u1EC1u r\u1ED9ng v\u00E0 chi\u1EC1u cao.");
            sbReadme.AppendLine();
            sbReadme.AppendLine("## 3. C\u00E1ch \u0111\u1ECDc d\u1EEF li\u1EC7u b\u1EB1ng Python");
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

            // -----------------------------------------------------------------
            // Step 5: Success Prompt
            // -----------------------------------------------------------------
            if (showSuccessDialog)
            {
                var ask = MessageBox.Show(
                    $"Xu\u1EA5t g\u00F3i d\u1EEF li\u1EC7u th\u00E0nh c\u00F4ng!\n\n" +
                    $"Th\u01B0 m\u1EE5c \u0111\u00EDch:\n{exportRoot}\n\n" +
                    $"\u2022 data.json : T\u1ECDa \u0111\u1ED9 & th\u00F4ng tin \u1EA3nh ({exportedAreas} areas, {exportedCrops} crops)\n" +
                    $"\u2022 crops/     : {exportedCrops + (exportAreasWithImages ? exportedAreas : 0)} \u1EA3nh \u0111\u1ED1i t\u01B0\u1EE3ng \u0111\u00E3 c\u1EAFt\n" +
                    (exportSources ? $"\u2022 sources/   : {exportedSources} \u1EA3nh ngu\u1ED3n g\u1ED1c\n" : "") +
                    $"\u2022 README.md  : T\u00E0i li\u1EC7u m\u00F4 t\u1EA3 c\u1EA5u tr\u00FAc cho AI & ph\u1EA7n m\u1EC1m ngo\u00E0i\n\n" +
                    $"B\u1EA1n c\u00F3 mu\u1ED1n m\u1EDF th\u01B0 m\u1EE5c v\u1EEBa xu\u1EA5t trong Windows Explorer kh\u00F4ng?",
                    "Xu\u1EA5t th\u00E0nh c\u00F4ng",
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
            MessageBox.Show($"Lá»—i khi xuáº¥t gÃ³i dá»¯ liá»‡u: {ex.Message}", "Lá»—i xuáº¥t", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private void OnSavedGridSelectionChanged()
    {
        if (dgvSavedRegions.SelectedRows.Count == 0) return;

        int index = dgvSavedRegions.SelectedRows[0].Index;
        if (index >= 0 && index < _savedRegions.Count)
        {
            var item = _savedRegions[index];

            if (item.SourceBitmap != null || !string.IsNullOrEmpty(item.SourceImageBase64))
            {
                string targetSourceName = item.SourceImageName ?? item.Name;
                if (!string.Equals(_currentSourceName, targetSourceName, StringComparison.OrdinalIgnoreCase))
                {
                    Bitmap? bmpToUse = item.SourceBitmap != null ? new Bitmap(item.SourceBitmap) : ProjectService.BitmapFromBase64(item.SourceImageBase64);
                    if (bmpToUse != null)
                    {
                        SetSourceImage(bmpToUse, targetSourceName);
                    }
                }
            }

            Rectangle rect = new(item.X, item.Y, item.Width, item.Height);

            // Move canvas crop box to this region
            canvas.SetCropRect(rect);
        }
    }

    private void OnSavedGridDoubleClick(DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _savedRegions.Count) return;

        string? colName = e.ColumnIndex >= 0 && e.ColumnIndex < dgvSavedRegions.Columns.Count
            ? dgvSavedRegions.Columns[e.ColumnIndex].Name
            : null;

        // Skip button columns
        if (colName is "ColEditBtn" or "ColDeleteBtn") return;
        // Double-click on ColName triggers in-place cell editing!
        if (colName == "ColName")
        {
            dgvSavedRegions.CurrentCell = dgvSavedRegions.Rows[e.RowIndex].Cells[e.ColumnIndex];
            dgvSavedRegions.BeginEdit(true);
            return;
        }

        var item = _savedRegions[e.RowIndex];

        // For both Image and Area (Coordinate), open the modal dialog
        Bitmap? sourceToCrop = item.SourceBitmap ?? canvas.Image;
        Bitmap? cropped = null;
        if (sourceToCrop != null)
        {
            Rectangle r = new(item.X, item.Y, item.Width, item.Height);
            r.Intersect(new Rectangle(0, 0, sourceToCrop.Width, sourceToCrop.Height));
            if (r.Width > 0 && r.Height > 0)
            {
                cropped = sourceToCrop.Clone(r, PixelFormat.Format32bppArgb);
            }
        }

        using var viewer = new ImageViewerDialog(
            item,
            cropped,
            item.ImagePath);

        viewer.ShowDialog(this);

        if (viewer.RequestedEditInMain)
        {
            EditRegionItem(e.RowIndex);
        }
        else if (viewer.HasSavedChanges)
        {
            RefreshSavedGrid();
        }

        cropped?.Dispose();
    }

    private void OnSavedGridEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is TextBox tb)
        {
            tb.BackColor = Color.FromArgb(42, 46, 56);
            tb.ForeColor = Color.White;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = new Font("Segoe UI Semibold", 9F);
        }
    }

    private void OnSavedGridCellEndEdit(DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _savedRegions.Count) return;
        if (e.ColumnIndex < 0 || e.ColumnIndex >= dgvSavedRegions.Columns.Count) return;

        var col = dgvSavedRegions.Columns[e.ColumnIndex];
        if (col.Name != "ColName") return;

        var item = _savedRegions[e.RowIndex];
        var cell = dgvSavedRegions.Rows[e.RowIndex].Cells[e.ColumnIndex];
        string? newName = cell.Value?.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            MessageBox.Show("Tên không được để trống!", "Lỗi đổi tên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cell.Value = item.Name;
            return;
        }

        if (!string.Equals(item.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            // Check for duplicates
            bool isDuplicate = _savedRegions.Where((r, idx) => idx != e.RowIndex)
                                           .Any(r => string.Equals(r.Name, newName, StringComparison.OrdinalIgnoreCase));
            if (isDuplicate)
            {
                MessageBox.Show($"Tên '{newName}' đã tồn tại trong danh sách! Vui lòng chọn tên khác.", "Trùng tên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cell.Value = item.Name;
                return;
            }

            item.Name = newName;
            _isProjectDirty = true;
            UpdateAppTitle();
        }
    }

    private void OnSavedGridCellContentClick(DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _savedRegions.Count) return;

        string? colName = e.ColumnIndex >= 0 && e.ColumnIndex < dgvSavedRegions.Columns.Count
            ? dgvSavedRegions.Columns[e.ColumnIndex].Name
            : null;

        if (colName == "ColEditBtn")
        {
            EditRegionItem(e.RowIndex);
        }
        else if (colName == "ColDeleteBtn")
        {
            DeleteRegionItem(e.RowIndex);
        }
    }

    private void OnSavedGridToolTipTextNeeded(DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _savedRegions.Count) return;

        string? colName = e.ColumnIndex >= 0 && e.ColumnIndex < dgvSavedRegions.Columns.Count
            ? dgvSavedRegions.Columns[e.ColumnIndex].Name
            : null;

        if (colName == "ColEditBtn")
        {
            e.ToolTipText = "Sửa: Nạp lên panel trên để chỉnh sửa (có thể lưu đè hoặc lưu mới)";
        }
        else if (colName == "ColDeleteBtn")
        {
            e.ToolTipText = "Xóa mục này khỏi danh sách";
        }
        else if (colName == "ColType")
        {
            var item = _savedRegions[e.RowIndex];
            e.ToolTipText = item.ItemType == CropItemType.Image
                ? "Hình ảnh đã cắt (Nhấp đúp dòng này để mở cửa sổ xem ảnh lớn)"
                : "Tọa độ vùng cắt (Nhấp để chọn, nhấp đúp để nạp lên sửa)";
        }
    }

    private void OnSavedGridCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        // 1. Custom Header Cell Painting
        if (e.RowIndex == -1)
        {
            e.PaintBackground(e.ClipBounds, true);
            Rectangle rect = e.CellBounds;

            using SolidBrush headerBg = new(Color.FromArgb(42, 46, 56));
            using Pen borderPen = new(Color.FromArgb(56, 62, 76), 1);
            using SolidBrush textBrush = new(Color.White);
            using Font font = new("Segoe UI Semibold", 9.5F);

            e.Graphics?.FillRectangle(headerBg, rect);
            e.Graphics?.DrawLine(borderPen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            e.Graphics?.DrawLine(borderPen, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom);

            if (e.Graphics != null && !string.IsNullOrEmpty(e.FormattedValue?.ToString()))
            {
                string text = e.FormattedValue.ToString()!;
                var col = dgvSavedRegions.Columns[e.ColumnIndex];
                bool isCenter = col.Name is "ColIndex" or "ColType" or "ColX" or "ColY" or "ColW" or "ColH" or "ColRatio" or "ColCreated" or "ColEditBtn" or "ColDeleteBtn";

                StringFormat sf = new()
                {
                    Alignment = isCenter ? StringAlignment.Center : StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                RectangleF textRect = new(rect.X + 4, rect.Y, rect.Width - 8, rect.Height);
                e.Graphics.DrawString(text, font, textBrush, textRect, sf);
            }
            e.Handled = true;
            return;
        }

        if (e.RowIndex < 0) return;

        string? colName = e.ColumnIndex >= 0 && e.ColumnIndex < dgvSavedRegions.Columns.Count
            ? dgvSavedRegions.Columns[e.ColumnIndex].Name
            : null;

        // Custom render for Edit Button: Icon-only, centered, NO text
        if (colName == "ColEditBtn")
        {
            e.PaintBackground(e.ClipBounds, true);
            Rectangle rect = e.CellBounds;
            rect.Inflate(-4, -4);

            using SolidBrush btnBg = new(Color.FromArgb(50, 55, 68));
            using Pen btnBorder = new(Color.FromArgb(70, 78, 96), 1);

            e.Graphics?.FillRectangle(btnBg, rect);
            e.Graphics?.DrawRectangle(btnBorder, rect);

            if (e.Graphics != null)
            {
                int iconW = _editIconBmp.Width;
                int iconH = _editIconBmp.Height;
                float startX = rect.X + (rect.Width - iconW) / 2f;
                float iconY = rect.Y + (rect.Height - iconH) / 2f;

                e.Graphics.DrawImage(_editIconBmp, startX, iconY);
            }

            e.Handled = true;
        }
        // Custom render for Delete Button: Icon-only, centered, NO text
        else if (colName == "ColDeleteBtn")
        {
            e.PaintBackground(e.ClipBounds, true);
            Rectangle rect = e.CellBounds;
            rect.Inflate(-4, -4);

            using SolidBrush btnBg = new(Color.FromArgb(70, 40, 48));
            using Pen btnBorder = new(Color.FromArgb(200, 70, 80), 1);

            e.Graphics?.FillRectangle(btnBg, rect);
            e.Graphics?.DrawRectangle(btnBorder, rect);

            if (e.Graphics != null)
            {
                int iconW = _deleteIconBmp.Width;
                int iconH = _deleteIconBmp.Height;
                float startX = rect.X + (rect.Width - iconW) / 2f;
                float iconY = rect.Y + (rect.Height - iconH) / 2f;

                e.Graphics.DrawImage(_deleteIconBmp, startX, iconY);
            }

            e.Handled = true;
        }
        // Custom render for Type column: Icon-only, NO badge background, NO border
        else if (colName == "ColType")
        {
            e.PaintBackground(e.ClipBounds, true);
            var item = _savedRegions[e.RowIndex];

            bool isImage = item.ItemType == CropItemType.Image;
            Bitmap iconBmp = isImage ? _imageIconBmp : _coordIconBmp;

            if (e.Graphics != null)
            {
                Rectangle rect = e.CellBounds;
                int iconW = iconBmp.Width;
                int iconH = iconBmp.Height;
                float startX = rect.X + (rect.Width - iconW) / 2f;
                float iconY = rect.Y + (rect.Height - iconH) / 2f;

                e.Graphics.DrawImage(iconBmp, startX, iconY);
            }

            e.Handled = true;
        }
    }

    private void RefreshSavedGrid()
    {
        _isProjectDirty = true;
        dgvSavedRegions.Rows.Clear();
        for (int i = 0; i < _savedRegions.Count; i++)
        {
            var item = _savedRegions[i];
            int rowIndex = dgvSavedRegions.Rows.Add(
                (i + 1).ToString(),
                item.TypeDisplay,
                item.Name,
                item.X.ToString(),
                item.Y.ToString(),
                item.Width.ToString(),
                item.Height.ToString(),
                item.AspectRatioStr,
                item.CreatedAt.ToString("HH:mm:ss dd/MM/yyyy"),
                item.Notes ?? (item.ImagePath != null ? Path.GetFileName(item.ImagePath) : "")
            );

            // Set tooltips on the buttons of this row
            dgvSavedRegions.Rows[rowIndex].Cells["ColEditBtn"].ToolTipText = "Chỉnh sửa tên, ghi chú hoặc tọa độ của mục này";
            dgvSavedRegions.Rows[rowIndex].Cells["ColDeleteBtn"].ToolTipText = "Xóa mục này khỏi danh sách";
        }

        lblSavedTitle.Text = $"Objects ({_savedRegions.Count})";
    }

    private void LoadSavedRegions()
    {
        var list = ConfigStorageService.LoadRegions();
        _savedRegions.Clear();
        if (list.Count > 0)
        {
            foreach (var r in list)
            {
                if (!string.IsNullOrEmpty(r.SourceImageBase64) && r.SourceBitmap == null)
                {
                    r.SourceBitmap = ProjectService.BitmapFromBase64(r.SourceImageBase64);
                }
            }
            _savedRegions.AddRange(list);
        }

        RefreshSavedGrid();
        _isProjectDirty = false;
    }

    private static string GetRatioStr(int w, int h)
    {
        if (h <= 0) return "-";
        int gcd = GetGcd(w, h);
        return $"{w / gcd}:{h / gcd}";
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

    #endregion

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
