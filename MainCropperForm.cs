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
    private Bitmap? _currentFilteredBitmap;
    private string _currentSortColumn = "ColCreated";
    private SortOrder _currentSortOrder = SortOrder.None;

    internal DataGridView SavedGrid => dgvSavedRegions;
    internal List<CropRegionItem> SavedRegions => _savedRegions;
    internal void SetImageForTesting(Bitmap bmp, string name)
    {
        _activeMediaItem = new MediaItem { Bitmap = bmp, Name = name };
        canvas.Image = bmp;
        _currentSourceName = name;
    }

    internal static string GenerateDefaultProjectName() => $"Dá»± Ã¡n_{DateTime.Now:yyyyMMdd_HHmm}";

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.Style |= 0x02000000; // WS_CLIPCHILDREN
            return cp;
        }
    }

    private static void EnableDoubleBufferingRecursive(Control control)
    {
        typeof(Control).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(control, true, null);

        foreach (Control child in control.Controls)
        {
            EnableDoubleBufferingRecursive(child);
        }
    }

    public MainCropperForm()
    {
        InitializeComponent();

        DoubleBuffered = true;
        EnableDoubleBufferingRecursive(this);

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
        mediaPanel.BitmapDropped += bmp => ImportBitmapToMedia(bmp, $"KÃ©o_tháº£_{DateTime.Now:yyyyMMdd_HHmmss}", setAsMain: true);
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
}
