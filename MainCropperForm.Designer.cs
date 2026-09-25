#nullable enable
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Controls;

namespace Win04_Cropper;

partial class MainCropperForm
{
    private System.ComponentModel.IContainer components = null!;

    // Header Controls
    private Panel pnlHeader = null!;
    private Panel pnlHeaderDivider = null!;
    private IconPictureBox picAppLogo = null!;
    private Label lblAppTitle = null!;
    private IconButton btnProject = null!;
    private IconButton btnLiveCapture = null!;
    private IconButton btnWindowCapture = null!;
    private IconButton btnFitView = null!;
    private IconButton btn100View = null!;
    private Label lblImageInfo = null!;
    private Label lblCursorInfo = null!;
    private IconButton btnSaveProject = null!;

    // Body Panels
    private LiveSplitContainer splitMain = null!;
    private LiveSplitContainer splitTop = null!;
    private LiveSplitContainer splitMediaCanvas = null!;
    private MediaPanelControl mediaPanel = null!;

    // Left Panel (Canvas)
    private Panel pnlCanvasContainer = null!;
    private Panel pnlCanvasHeader = null!;
    private IconPictureBox picCanvasIcon = null!;
    private Label lblCanvasTitle = null!;
    private IconPictureBox picCanvasGuide = null!;
    private Label lblZoomValue = null!;
    private IconButton btnZoomIn = null!;
    private IconButton btnZoomOut = null!;
    private CanvasControl canvas = null!;

    // Right Panel: Properties & Adjustments (replacing Preview)
    private Panel pnlPropertiesContainer = null!;
    private Panel pnlPropertiesHeader = null!;
    private Panel pnlPropertiesBody = null!;
    private IconPictureBox picCoordIcon = null!;
    private Label lblCoordSection = null!;
    private IconButton btnCancelEdit = null!;

    // Coordinate inputs
    private Label lblX = null!;
    private NumericUpDown numX = null!;
    private Label lblY = null!;
    private NumericUpDown numY = null!;
    private Label lblW = null!;
    private NumericUpDown numW = null!;
    private Label lblH = null!;
    private NumericUpDown numH = null!;
    private Label? lblAspectRatio = null;

    // Nudge and resize buttons
    private IconButton btnNudgeLeft = null!;
    private IconButton btnNudgeRight = null!;
    private IconButton btnNudgeUp = null!;
    private IconButton btnNudgeDown = null!;
    private ComboBox cboNudgeStep = null!;
    private IconButton btnCenterBox = null!;

    // Quick size presets: aspect ratios and screen sizes
    private Button btnRatio1x1 = null!;
    private Button btnRatio3x4 = null!;
    private Button btnRatio4x6 = null!;
    private Button btnRatio9x16 = null!;
    private Button btnRatioFree = null!;
    private Button btnRatio4x3 = null!;
    private Button btnRatio6x4 = null!;
    private Button btnRatio16x9 = null!;

    private Button btnRes1920x1080 = null!;
    private Button btnRes1600x900 = null!;
    private Button btnRes1366x768 = null!;
    private Button btnRes1280x720 = null!;
    private Button btnRes2560x1440 = null!;
    private Button btnRes1440x900 = null!;
    private Button btnRes1024x768 = null!;
    private Button btnResAll = null!;

    private TableLayoutPanel tlpRatios = null!;
    private TableLayoutPanel tlpScreenSizes = null!;

    // Action buttons in properties panel
    private IconButton btnSaveCoordinates = null!;
    private IconButton btnCropAndSaveImage = null!;
    private IconButton btnCopyCroppedImage = null!;
    private Panel pnlPropertiesActions = null!;
    private ToolTip tipActions = null!;

    // Filter controls in properties panel
    private Panel cardFilters = null!;
    private CheckBox chkGrayscale = null!;
    private CheckBox chkThreshold = null!;
    private Panel pnlThresholdControls = null!;
    private TrackBar trkThreshold = null!;
    private Label lblThresholdVal = null!;
    private Button btnResetThreshold = null!;

    // Saved list section
    private Panel pnlSavedSection = null!;
    private Panel pnlSavedHeader = null!;
    private IconPictureBox picSavedIcon = null!;
    private Label lblSavedTitle = null!;
    private IconButton btnSortObjects = null!;
    private IconButton btnClearAll = null!;
    private IconButton btnExportPackage = null!;
    private IconButton btnExportJson = null!;
    private IconButton btnImportJson = null!;
    private DataGridView dgvSavedRegions = null!;

    internal float _dpiScale = 1.0f;
    internal int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            tipActions?.Dispose();
            _editIconBmp?.Dispose();
            _deleteIconBmp?.Dispose();
            _coordIconBmp?.Dispose();
            _imageIconBmp?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.SuspendLayout();

        _dpiScale = this.DeviceDpi > 0 ? (this.DeviceDpi / 96.0f) : 1.0f;
        if (_dpiScale < 1.0f) _dpiScale = 1.0f;

        this.AutoScaleMode = AutoScaleMode.None;

        // -------------------------------------------------------------
        // Main Form Properties
        // -------------------------------------------------------------
        this.Text = "Screen Cropper Pro - Định vị tọa độ & Cắt ảnh màn hình";
        this.Size = new Size(DpiScale(1400), DpiScale(880));
        this.MinimumSize = new Size(DpiScale(1020), DpiScale(680));
        this.BackColor = Color.FromArgb(32, 35, 42);
        this.ForeColor = Color.FromArgb(235, 238, 245);
        this.Font = new Font("Segoe UI", 9F);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.KeyPreview = true;

        tipActions = new ToolTip
        {
            InitialDelay = 150,
            ReshowDelay = 80,
            AutoPopDelay = 6000,
            ShowAlways = true
        };

        // -------------------------------------------------------------
        // Header Panel (Dock = Top, Height = 56)
        // -------------------------------------------------------------
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(56),
            BackColor = Color.FromArgb(32, 36, 45),
            Padding = Padding.Empty
        };
        pnlHeader.Paint += (s, e) =>
        {
            var g = e.Graphics;
            int w = pnlHeader.Width;
            int h = pnlHeader.Height;

            // Header top subtle highlight
            using Pen penHighlight = new(Color.FromArgb(35, 255, 255, 255), 1);
            g.DrawLine(penHighlight, 0, 0, w, 0);

            // Bottom elevation drop-shadow
            using Pen shadow3 = new(Color.FromArgb(60, 0, 0, 0), 1);
            using Pen shadow2 = new(Color.FromArgb(120, 0, 0, 0), 1);
            using Pen shadow1 = new(Color.FromArgb(180, 0, 0, 0), 1);
            using Pen darkBorder = new(Color.FromArgb(14, 16, 22), 1);

            g.DrawLine(shadow3, 0, h - 4, w, h - 4);
            g.DrawLine(shadow2, 0, h - 3, w, h - 3);
            g.DrawLine(shadow1, 0, h - 2, w, h - 2);
            g.DrawLine(darkBorder, 0, h - 1, w, h - 1);
        };

        picAppLogo = new IconPictureBox
        {
            IconChar = IconChar.CropSimple,
            IconColor = Color.White,
            IconSize = DpiScale(24),
            Size = new Size(DpiScale(26), DpiScale(26)),
            Location = new Point(DpiScale(14), DpiScale(15)),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picAppLogo);

        lblAppTitle = new Label
        {
            Text = "CROPPER PRO",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(picAppLogo.Right + DpiScale(8), DpiScale(15)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblAppTitle);

        FlowLayoutPanel pnlHeaderButtons = new()
        {
            Location = new Point(DpiScale(195), DpiScale(11)),
            Height = DpiScale(36),
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(pnlHeaderButtons);

        btnProject = CreateHeaderButton("Dự án", IconChar.FolderTree, Color.FromArgb(0, 122, 204));
        btnProject.Click += (s, e) => ShowProjectDialog();
        pnlHeaderButtons.Controls.Add(btnProject);

        btnLiveCapture = CreateHeaderButton("Chụp Live (F9)", IconChar.Camera, Color.FromArgb(0, 168, 150));
        btnLiveCapture.Click += async (s, e) => await TriggerLiveCaptureAsync();
        pnlHeaderButtons.Controls.Add(btnLiveCapture);

        btnWindowCapture = CreateHeaderButton("Cửa sổ khác", IconChar.WindowRestore);
        btnWindowCapture.Click += async (s, e) => await TriggerWindowCaptureAsync();
        pnlHeaderButtons.Controls.Add(btnWindowCapture);

        btnFitView = CreateHeaderButton("Vừa khung", IconChar.Expand);
        btnFitView.Click += (s, e) => canvas.FitImageToView();
        pnlHeaderButtons.Controls.Add(btnFitView);

        btn100View = CreateHeaderButton("100%", IconChar.MagnifyingGlass);
        btn100View.Click += (s, e) => canvas.SetZoom100();
        pnlHeaderButtons.Controls.Add(btn100View);

        // Header status and Save Project (Flow right-aligned, parallel with header buttons)
        FlowLayoutPanel pnlHeaderStatus = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(11), DpiScale(14), 0)
        };
        pnlHeader.Controls.Add(pnlHeaderStatus);

        btnSaveProject = CreateHeaderButton("Lưu dự án", IconChar.FloppyDisk, Color.FromArgb(16, 185, 129));
        btnSaveProject.Click += (s, e) => SaveCurrentProject();
        pnlHeaderStatus.Controls.Add(btnSaveProject);

        // -------------------------------------------------------------
        // Body Container: splitMain (Top vs Bottom)
        // -------------------------------------------------------------
        splitMain = new LiveSplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(56, 61, 74),
            Size = new Size(DpiScale(1400), DpiScale(820)),
            Panel1MinSize = DpiScale(150),
            Panel2MinSize = DpiScale(150),
            SplitterDistance = DpiScale(500)
        };
        pnlHeaderDivider = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(3),
            BackColor = Color.FromArgb(16, 18, 24)
        };
        pnlHeaderDivider.Paint += (s, e) =>
        {
            var g = e.Graphics;
            int w = pnlHeaderDivider.Width;
            using Pen p1 = new(Color.FromArgb(10, 12, 16), 1);
            using Pen p2 = new(Color.FromArgb(20, 23, 30), 1);
            using Pen p3 = new(Color.FromArgb(28, 32, 42), 1);
            g.DrawLine(p1, 0, 0, w, 0);
            g.DrawLine(p2, 0, 1, w, 1);
            g.DrawLine(p3, 0, 2, w, 2);
        };

        this.Controls.Add(splitMain);
        this.Controls.Add(pnlHeaderDivider);
        this.Controls.Add(pnlHeader);
        pnlHeaderDivider.SendToBack();
        pnlHeader.SendToBack();

        // -------------------------------------------------------------
        // Top Panel: splitTop (Left Canvas vs Right Properties)
        // -------------------------------------------------------------
        splitTop = new LiveSplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(56, 61, 74),
            Size = new Size(DpiScale(1400), DpiScale(530)),
            Panel1MinSize = DpiScale(500),
            Panel2MinSize = DpiScale(360),
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = DpiScale(1030)
        };
        splitMain.Panel1.Controls.Add(splitTop);

        // Left Canvas Container
        pnlCanvasContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 33, 40) };
        pnlCanvasHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(34),
            BackColor = Color.FromArgb(42, 46, 56),
            Padding = Padding.Empty
        };

        picCanvasIcon = new IconPictureBox
        {
            IconChar = IconChar.Image,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(7)),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasIcon);

        lblCanvasTitle = new Label
        {
            Text = "Main capture",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Location = new Point(picCanvasIcon.Right + DpiScale(6), DpiScale(7)),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasTitle);

        picCanvasGuide = new IconPictureBox
        {
            IconChar = IconChar.Lightbulb,
            IconColor = Color.FromArgb(245, 185, 30),
            IconSize = DpiScale(15),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(lblCanvasTitle.Right + DpiScale(8), DpiScale(8)),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        tipActions.SetToolTip(picCanvasGuide, "Lăn chuột: Zoom | Chuột giữa/Phải: Pan | Kéo cạnh/góc: Resize");
        pnlCanvasHeader.Controls.Add(picCanvasGuide);

        // Right side: Mouse coordinates
        lblCursorInfo = new Label
        {
            Text = "Chuột: -",
            ForeColor = Color.FromArgb(200, 215, 235),
            Font = new Font("Segoe UI", 9.5F),
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Padding = new Padding(0, DpiScale(7), DpiScale(12), 0)
        };
        pnlCanvasHeader.Controls.Add(lblCursorInfo);

        // Center: Zoom buttons (-, 100%, +)
        FlowLayoutPanel pnlCanvasZoom = new()
        {
            Size = new Size(DpiScale(110), DpiScale(26)),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        btnZoomOut = new IconButton
        {
            IconChar = IconChar.Minus,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            Size = new Size(DpiScale(28), DpiScale(24)),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(54, 59, 72),
            ForeColor = Color.White,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand
        };
        btnZoomOut.FlatAppearance.BorderSize = 0;
        btnZoomOut.Click += (s, e) => canvas.ZoomOut();
        pnlCanvasZoom.Controls.Add(btnZoomOut);

        lblZoomValue = new Label
        {
            Text = "100%",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Size = new Size(DpiScale(50), DpiScale(24)),
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlCanvasZoom.Controls.Add(lblZoomValue);

        btnZoomIn = new IconButton
        {
            IconChar = IconChar.Plus,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            Size = new Size(DpiScale(28), DpiScale(24)),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(54, 59, 72),
            ForeColor = Color.White,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand
        };
        btnZoomIn.FlatAppearance.BorderSize = 0;
        btnZoomIn.Click += (s, e) => canvas.ZoomIn();
        pnlCanvasZoom.Controls.Add(btnZoomIn);

        pnlCanvasHeader.Controls.Add(pnlCanvasZoom);
        pnlCanvasZoom.BringToFront();

        void CenterCanvasZoom()
        {
            if (pnlCanvasHeader.ClientSize.Width > 0)
            {
                int zW = pnlCanvasZoom.Width > 0 ? pnlCanvasZoom.Width : DpiScale(110);
                int zH = pnlCanvasZoom.Height > 0 ? pnlCanvasZoom.Height : DpiScale(26);
                pnlCanvasZoom.Location = new Point(
                    Math.Max(DpiScale(160), (pnlCanvasHeader.ClientSize.Width - zW) / 2),
                    Math.Max(0, (pnlCanvasHeader.ClientSize.Height - zH) / 2)
                );
            }
        }
        pnlCanvasHeader.Resize += (s, e) => CenterCanvasZoom();
        pnlCanvasHeader.Layout += (s, e) => CenterCanvasZoom();
        CenterCanvasZoom();

        canvas = new CanvasControl { Dock = DockStyle.Fill };
        pnlCanvasContainer.Controls.Add(canvas);
        pnlCanvasContainer.Controls.Add(pnlCanvasHeader);

        lblImageInfo = new Label { Visible = false };
        canvas.ImageOverlayInfo = "Chưa nạp ảnh (Kéo & thả ảnh vào đây)";

        splitMediaCanvas = new LiveSplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(56, 61, 74),
            Size = new Size(DpiScale(1030), DpiScale(530)),
            Panel1MinSize = DpiScale(180),
            Panel2MinSize = DpiScale(200),
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = DpiScale(350)
        };
        splitTop.Panel1.Controls.Add(splitMediaCanvas);

        mediaPanel = new MediaPanelControl(_dpiScale)
        {
            Dock = DockStyle.Fill
        };
        splitMediaCanvas.Panel1.Controls.Add(mediaPanel);
        splitMediaCanvas.Panel2.Controls.Add(pnlCanvasContainer);

        // Right Panel: Properties & Adjustments (replacing Preview)
        pnlPropertiesContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 35, 42) };
        splitTop.Panel2.Controls.Add(pnlPropertiesContainer);
        InitializePropertiesPanel();

        // -------------------------------------------------------------
        // Bottom Panel: Saved Regions Grid (occupies 100% of bottom)
        // -------------------------------------------------------------
        pnlSavedSection = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 35, 42) };
        splitMain.Panel2.Controls.Add(pnlSavedSection);
        InitializeSavedSection();

        this.ResumeLayout(true);
    }

    private void InitializePropertiesPanel()
    {
        pnlPropertiesHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(36),
            BackColor = Color.FromArgb(42, 46, 56),
            Padding = new Padding(DpiScale(10), DpiScale(4), DpiScale(8), DpiScale(4))
        };
        pnlPropertiesContainer.Controls.Add(pnlPropertiesHeader);

        picCoordIcon = new IconPictureBox
        {
            IconChar = IconChar.Sliders,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(8)),
            BackColor = Color.Transparent
        };
        pnlPropertiesHeader.Controls.Add(picCoordIcon);

        lblCoordSection = new Label
        {
            Text = "Properties",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.White,
            Location = new Point(picCoordIcon.Right + DpiScale(6), DpiScale(8)),
            AutoSize = true
        };
        pnlPropertiesHeader.Controls.Add(lblCoordSection);

        btnCancelEdit = new IconButton
        {
            Text = " Hủy sửa",
            IconChar = IconChar.Xmark,
            IconColor = Color.FromArgb(255, 140, 140),
            IconSize = DpiScale(13),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(6), 0, DpiScale(8), 0),
            Dock = DockStyle.Right,
            Width = DpiScale(85),
            BackColor = Color.FromArgb(70, 40, 48),
            ForeColor = Color.FromArgb(255, 175, 185),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnCancelEdit.FlatAppearance.BorderSize = 0;
        btnCancelEdit.Click += (s, e) => CancelEditing();
        pnlPropertiesHeader.Controls.Add(btnCancelEdit);

        // -------------------------------------------------------------
        // Fixed bottom panel for actions (Always visible, icon-only)
        // -------------------------------------------------------------
        pnlPropertiesActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = DpiScale(44),
            BackColor = Color.FromArgb(36, 40, 50),
            Padding = new Padding(DpiScale(8), DpiScale(6), DpiScale(8), DpiScale(6))
        };
        pnlPropertiesContainer.Controls.Add(pnlPropertiesActions);

        TableLayoutPanel tlpActionButtons = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        tlpActionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        tlpActionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        tlpActionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        tlpActionButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pnlPropertiesActions.Controls.Add(tlpActionButtons);

        btnSaveCoordinates = new IconButton
        {
            Text = string.Empty,
            IconChar = IconChar.FloppyDisk,
            IconColor = Color.White,
            IconSize = DpiScale(17),
            ImageAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, DpiScale(3), 0),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSaveCoordinates.FlatAppearance.BorderSize = 0;
        btnSaveCoordinates.Click += (s, e) => SaveCurrentCoordinates();
        tipActions.SetToolTip(btnSaveCoordinates, "Lưu tọa độ (Ctrl+Shift+S / Ctrl+D)");
        tlpActionButtons.Controls.Add(btnSaveCoordinates, 0, 0);

        btnCropAndSaveImage = new IconButton
        {
            Text = string.Empty,
            IconChar = IconChar.Crop,
            IconColor = Color.White,
            IconSize = DpiScale(17),
            ImageAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = new Padding(DpiScale(3), 0, DpiScale(3), 0),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCropAndSaveImage.FlatAppearance.BorderSize = 0;
        btnCropAndSaveImage.Click += (s, e) => CropAndSaveImage();
        tipActions.SetToolTip(btnCropAndSaveImage, "Cắt & Lưu ảnh (Ctrl+S)");
        tlpActionButtons.Controls.Add(btnCropAndSaveImage, 1, 0);

        btnCopyCroppedImage = new IconButton
        {
            Text = string.Empty,
            IconChar = IconChar.Copy,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            ImageAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = new Padding(DpiScale(3), 0, 0, 0),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCopyCroppedImage.FlatAppearance.BorderSize = 0;
        btnCopyCroppedImage.Click += (s, e) => CopyCroppedImageToClipboard();
        tipActions.SetToolTip(btnCopyCroppedImage, "Copy ảnh vào Clipboard (Ctrl+C)");
        tlpActionButtons.Controls.Add(btnCopyCroppedImage, 2, 0);

        // -------------------------------------------------------------
        // Scrollable body (Between Header and Bottom Actions)
        // -------------------------------------------------------------
        pnlPropertiesBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(32, 35, 42),
            Padding = new Padding(DpiScale(8), DpiScale(6), DpiScale(8), DpiScale(6))
        };
        pnlPropertiesContainer.Controls.Add(pnlPropertiesBody);
        pnlPropertiesHeader.SendToBack();
        pnlPropertiesActions.SendToBack();

        TableLayoutPanel tlpCards = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnlPropertiesBody.Controls.Add(tlpCards);

        // ---------------------------------------------------------
        // CARD 0: TỈ LỆ / KÍCH THƯỚC (Aspect Ratios & Dimensions)
        // ---------------------------------------------------------
        Panel cardRatioAndSize = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(40, 44, 54),
            Padding = new Padding(DpiScale(10), DpiScale(7), DpiScale(10), DpiScale(8)),
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpCards.Controls.Add(cardRatioAndSize, 0, 0);

        Label lblRatioAndSizeTitle = new()
        {
            Text = "TỈ LỆ / KÍCH THƯỚC",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(4))
        };

        // --- SECTION 1: TỈ LỆ ---
        Label lblRatioTitle = new()
        {
            Text = "Tỉ lệ:",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, DpiScale(4), 0, DpiScale(3))
        };

        tlpRatios = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));
        tlpRatios.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));

        // Section 1 Row 1: Tự do, 3:4, 4:6, 9:16
        btnRatioFree = CreatePresetButton("Tự do");
        btnRatioFree.Click += (s, e) => ApplyAspectRatio(0, 0, btnRatioFree);
        tlpRatios.Controls.Add(btnRatioFree, 0, 0);

        btnRatio3x4 = CreatePresetButton("3:4");
        btnRatio3x4.Click += (s, e) => ApplyAspectRatio(3, 4, btnRatio3x4);
        tlpRatios.Controls.Add(btnRatio3x4, 1, 0);

        btnRatio4x6 = CreatePresetButton("4:6");
        btnRatio4x6.Click += (s, e) => ApplyAspectRatio(4, 6, btnRatio4x6);
        tlpRatios.Controls.Add(btnRatio4x6, 2, 0);

        btnRatio9x16 = CreatePresetButton("9:16");
        btnRatio9x16.Click += (s, e) => ApplyAspectRatio(9, 16, btnRatio9x16);
        tlpRatios.Controls.Add(btnRatio9x16, 3, 0);

        // Section 1 Row 2: 1:1, 4:3, 6:4, 16:9
        btnRatio1x1 = CreatePresetButton("1:1");
        btnRatio1x1.Click += (s, e) => ApplyAspectRatio(1, 1, btnRatio1x1);
        tlpRatios.Controls.Add(btnRatio1x1, 0, 1);

        btnRatio4x3 = CreatePresetButton("4:3");
        btnRatio4x3.Click += (s, e) => ApplyAspectRatio(4, 3, btnRatio4x3);
        tlpRatios.Controls.Add(btnRatio4x3, 1, 1);

        btnRatio6x4 = CreatePresetButton("6:4");
        btnRatio6x4.Click += (s, e) => ApplyAspectRatio(6, 4, btnRatio6x4);
        tlpRatios.Controls.Add(btnRatio6x4, 2, 1);

        btnRatio16x9 = CreatePresetButton("16:9");
        btnRatio16x9.Click += (s, e) => ApplyAspectRatio(16, 9, btnRatio16x9);
        tlpRatios.Controls.Add(btnRatio16x9, 3, 1);

        // --- SECTION 2: KÍCH THƯỚC (PX) ---
        Label lblSizeTitle = new()
        {
            Text = "Kích thước (px):",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, DpiScale(4), 0, DpiScale(3))
        };

        tlpScreenSizes = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));
        tlpScreenSizes.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));

        // Section 2 Row 1 (Col 0..3): Toàn bộ, 1024x768, 1280x720, 1366x768
        btnResAll = CreatePresetButton("Toàn bộ");
        btnResAll.Click += (s, e) => ApplyFullImageResolution();
        tlpScreenSizes.Controls.Add(btnResAll, 0, 0);

        btnRes1024x768 = CreatePresetButton("1024x768");
        btnRes1024x768.Click += (s, e) => ApplyScreenResolution(1024, 768);
        tlpScreenSizes.Controls.Add(btnRes1024x768, 1, 0);

        btnRes1280x720 = CreatePresetButton("1280x720");
        btnRes1280x720.Click += (s, e) => ApplyScreenResolution(1280, 720);
        tlpScreenSizes.Controls.Add(btnRes1280x720, 2, 0);

        btnRes1366x768 = CreatePresetButton("1366x768");
        btnRes1366x768.Click += (s, e) => ApplyScreenResolution(1366, 768);
        tlpScreenSizes.Controls.Add(btnRes1366x768, 3, 0);

        // Section 2 Row 2 (Col 0..3): 1440x900, 1600x900, 1920x1080, 2560x1440
        btnRes1440x900 = CreatePresetButton("1440x900");
        btnRes1440x900.Click += (s, e) => ApplyScreenResolution(1440, 900);
        tlpScreenSizes.Controls.Add(btnRes1440x900, 0, 1);

        btnRes1600x900 = CreatePresetButton("1600x900");
        btnRes1600x900.Click += (s, e) => ApplyScreenResolution(1600, 900);
        tlpScreenSizes.Controls.Add(btnRes1600x900, 1, 1);

        btnRes1920x1080 = CreatePresetButton("1920x1080");
        btnRes1920x1080.Click += (s, e) => ApplyScreenResolution(1920, 1080);
        tlpScreenSizes.Controls.Add(btnRes1920x1080, 2, 1);

        btnRes2560x1440 = CreatePresetButton("2560x1440");
        btnRes2560x1440.Click += (s, e) => ApplyScreenResolution(2560, 1440);
        tlpScreenSizes.Controls.Add(btnRes2560x1440, 3, 1);

        // Stack controls in cardRatioAndSize (reverse order for Dock = Top)
        cardRatioAndSize.Controls.Add(tlpScreenSizes);
        cardRatioAndSize.Controls.Add(lblSizeTitle);
        cardRatioAndSize.Controls.Add(tlpRatios);
        cardRatioAndSize.Controls.Add(lblRatioTitle);
        cardRatioAndSize.Controls.Add(lblRatioAndSizeTitle);

        // ---------------------------------------------------------
        // CARD 1: TỌA ĐỘ, KÍCH THƯỚC & DI CHUYỂN (Merged Card)
        // ---------------------------------------------------------
        Panel cardCoords = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(40, 44, 54),
            Padding = new Padding(DpiScale(10), DpiScale(7), DpiScale(10), DpiScale(8)),
            Margin = Padding.Empty
        };
        tlpCards.Controls.Add(cardCoords, 0, 1);

        Label lblCoordsTitle = new()
        {
            Text = "TỌA ĐỘ & KÍCH THƯỚC (px)",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(5))
        };

        TableLayoutPanel tlpCoords = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, DpiScale(2), 0, 0)
        };
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpCoords.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCoords.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Left Column: X (Row 0), Y (Row 1)
        lblX = CreateParamLabel("X:", 0, 0);
        lblX.Dock = DockStyle.Fill;
        lblX.TextAlign = ContentAlignment.MiddleRight;
        lblX.Margin = new Padding(0, 0, DpiScale(4), DpiScale(4));
        tlpCoords.Controls.Add(lblX, 0, 0);

        numX = CreateNumberBox(0, 0, DpiScale(75));
        numX.Dock = DockStyle.Fill;
        numX.Margin = new Padding(0, 0, DpiScale(10), DpiScale(4));
        numX.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numX, 1, 0);

        lblY = CreateParamLabel("Y:", 0, 0);
        lblY.Dock = DockStyle.Fill;
        lblY.TextAlign = ContentAlignment.MiddleRight;
        lblY.Margin = new Padding(0, 0, DpiScale(4), 0);
        tlpCoords.Controls.Add(lblY, 0, 1);

        numY = CreateNumberBox(0, 0, DpiScale(75));
        numY.Dock = DockStyle.Fill;
        numY.Margin = new Padding(0, 0, DpiScale(10), 0);
        numY.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numY, 1, 1);

        // Right Column: W (Row 0), H (Row 1)
        lblW = CreateParamLabel("W:", 0, 0);
        lblW.Dock = DockStyle.Fill;
        lblW.TextAlign = ContentAlignment.MiddleRight;
        lblW.Margin = new Padding(0, 0, DpiScale(4), DpiScale(4));
        tlpCoords.Controls.Add(lblW, 2, 0);

        numW = CreateNumberBox(0, 0, DpiScale(75));
        numW.Dock = DockStyle.Fill;
        numW.Margin = new Padding(0, 0, 0, DpiScale(4));
        numW.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numW, 3, 0);

        lblH = CreateParamLabel("H:", 0, 0);
        lblH.Dock = DockStyle.Fill;
        lblH.TextAlign = ContentAlignment.MiddleRight;
        lblH.Margin = new Padding(0, 0, DpiScale(4), 0);
        tlpCoords.Controls.Add(lblH, 2, 1);

        numH = CreateNumberBox(0, 0, DpiScale(75));
        numH.Dock = DockStyle.Fill;
        numH.Margin = Padding.Empty;
        numH.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numH, 3, 1);

        // Row 1: Directional arrow pad + Center button
        FlowLayoutPanel rowNudgeControls = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, DpiScale(6), 0, DpiScale(4))
        };

        btnNudgeLeft = CreateToolButton(IconChar.ArrowLeft, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeLeft.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeLeft.Click += (s, e) => NudgeCrop(-GetNudgeStep(), 0);
        tipActions.SetToolTip(btnNudgeLeft, "Di chuyển sang trái (Mũi tên Trái)");
        rowNudgeControls.Controls.Add(btnNudgeLeft);

        btnNudgeUp = CreateToolButton(IconChar.ArrowUp, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeUp.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeUp.Click += (s, e) => NudgeCrop(0, -GetNudgeStep());
        tipActions.SetToolTip(btnNudgeUp, "Di chuyển lên trên (Mũi tên Lên)");
        rowNudgeControls.Controls.Add(btnNudgeUp);

        btnNudgeDown = CreateToolButton(IconChar.ArrowDown, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeDown.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeDown.Click += (s, e) => NudgeCrop(0, GetNudgeStep());
        tipActions.SetToolTip(btnNudgeDown, "Di chuyển xuống dưới (Mũi tên Xuống)");
        rowNudgeControls.Controls.Add(btnNudgeDown);

        btnNudgeRight = CreateToolButton(IconChar.ArrowRight, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeRight.Margin = new Padding(0, 0, DpiScale(10), 0);
        btnNudgeRight.Click += (s, e) => NudgeCrop(GetNudgeStep(), 0);
        tipActions.SetToolTip(btnNudgeRight, "Di chuyển sang phải (Mũi tên Phải)");
        rowNudgeControls.Controls.Add(btnNudgeRight);

        btnCenterBox = CreateCenterButton("Căn giữa", IconChar.Bullseye, 0, 0, DpiScale(100), DpiScale(28));
        btnCenterBox.Margin = Padding.Empty;
        btnCenterBox.Click += (s, e) => CenterCropBox();
        tipActions.SetToolTip(btnCenterBox, "Căn giữa vùng chọn vào khung ảnh");
        rowNudgeControls.Controls.Add(btnCenterBox);

        // Row 2: Nudge step setting
        FlowLayoutPanel rowStepControls = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, 0, 0, DpiScale(4))
        };

        Label lblStep = new()
        {
            Text = "Bước di chuyển:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(170, 185, 205),
            AutoSize = true,
            Margin = new Padding(0, DpiScale(4), DpiScale(6), 0)
        };
        rowStepControls.Controls.Add(lblStep);

        cboNudgeStep = new ComboBox
        {
            Width = DpiScale(75),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = DpiScale(18),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Margin = Padding.Empty
        };
        cboNudgeStep.Items.AddRange(["1px", "5px", "10px", "50px"]);
        cboNudgeStep.SelectedIndex = 1;
        cboNudgeStep.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            using SolidBrush bg = new(Color.FromArgb(50, 55, 68));
            using SolidBrush fg = new(Color.White);
            e.Graphics.FillRectangle(bg, e.Bounds);
            StringFormat sf = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(cboNudgeStep.Items[e.Index]?.ToString() ?? "", cboNudgeStep.Font, fg, e.Bounds, sf);
        };
        rowStepControls.Controls.Add(cboNudgeStep);

        // Stack controls in cardCoords (reverse order for Dock = Top)
        cardCoords.Controls.Add(rowStepControls);
        cardCoords.Controls.Add(rowNudgeControls);
        cardCoords.Controls.Add(tlpCoords);
        cardCoords.Controls.Add(lblCoordsTitle);

        // ---------------------------------------------------------
        // CARD 2: BỘ LỌC HÌNH ẢNH (Filters: Grayscale & Threshold)
        // ---------------------------------------------------------
        cardFilters = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(40, 44, 54),
            Padding = new Padding(DpiScale(10), DpiScale(7), DpiScale(10), DpiScale(8)),
            Margin = Padding.Empty
        };
        tlpCards.Controls.Add(cardFilters, 0, 2);

        Label lblFilterTitle = new()
        {
            Text = "BỘ LỌC HÌNH ẢNH",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };

        // Checkbox 1: Ảnh đen trắng (Grayscale)
        chkGrayscale = new CheckBox
        {
            Text = "Ảnh đen trắng (Grayscale)",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, DpiScale(5))
        };
        chkGrayscale.CheckedChanged += (s, e) => OnGrayscaleFilterToggled();

        // Checkbox 2: Ngưỡng nhị phân (Threshold)
        chkThreshold = new CheckBox
        {
            Text = "Ngưỡng nhị phân (Threshold)",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, DpiScale(4))
        };
        chkThreshold.CheckedChanged += (s, e) => OnThresholdFilterToggled();

        // Threshold Controls Panel
        pnlThresholdControls = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent,
            Enabled = false,
            Margin = Padding.Empty,
            Padding = new Padding(DpiScale(2), DpiScale(2), DpiScale(2), DpiScale(2))
        };

        TableLayoutPanel tlpThreshHeader = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, DpiScale(2))
        };
        tlpThreshHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        tlpThreshHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

        lblThresholdVal = new Label
        {
            Text = "Điểm ngưỡng: 128",
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.FromArgb(180, 210, 240),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };
        tlpThreshHeader.Controls.Add(lblThresholdVal, 0, 0);

        btnResetThreshold = new Button
        {
            Text = "Mặc định (128)",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(200, 215, 230),
            BackColor = Color.FromArgb(50, 55, 68),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right,
            Width = DpiScale(88),
            Height = DpiScale(22)
        };
        btnResetThreshold.FlatAppearance.BorderSize = 0;
        btnResetThreshold.Click += (s, e) =>
        {
            trkThreshold.Value = 128;
            OnThresholdValueChanged();
        };
        tlpThreshHeader.Controls.Add(btnResetThreshold, 1, 0);

        trkThreshold = new TrackBar
        {
            Dock = DockStyle.Top,
            Minimum = 0,
            Maximum = 255,
            Value = 128,
            TickFrequency = 32,
            TickStyle = TickStyle.None,
            Height = DpiScale(26),
            BackColor = Color.FromArgb(40, 44, 54),
            Cursor = Cursors.Hand
        };
        trkThreshold.ValueChanged += (s, e) => OnThresholdValueChanged();

        Label lblThresholdHint = new()
        {
            Text = "Điểm ảnh < Ngưỡng: Đen | ≥ Ngưỡng: Trắng",
            Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
            ForeColor = Color.FromArgb(140, 155, 175),
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(2))
        };

        pnlThresholdControls.Controls.Add(lblThresholdHint);
        pnlThresholdControls.Controls.Add(trkThreshold);
        pnlThresholdControls.Controls.Add(tlpThreshHeader);

        // Stack controls in cardFilters (reverse order for Dock = Top)
        cardFilters.Controls.Add(pnlThresholdControls);
        cardFilters.Controls.Add(chkThreshold);
        cardFilters.Controls.Add(chkGrayscale);
        cardFilters.Controls.Add(lblFilterTitle);
    }

    private void InitializeSavedSection()
    {
        pnlSavedHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(38),
            BackColor = Color.FromArgb(42, 46, 56),
            Padding = new Padding(DpiScale(14), 0, 0, 0)
        };
        pnlSavedSection.Controls.Add(pnlSavedHeader);

        picSavedIcon = new IconPictureBox
        {
            IconChar = IconChar.RectangleList,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(DpiScale(14), DpiScale(10)),
            BackColor = Color.Transparent
        };
        pnlSavedHeader.Controls.Add(picSavedIcon);

        lblSavedTitle = new Label
        {
            Text = "Objects (0)",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Location = new Point(picSavedIcon.Right + DpiScale(8), DpiScale(9)),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        lblSavedTitle.Click += (s, e) => ToggleTitleSort();
        tipActions.SetToolTip(lblSavedTitle, "Nhấn vào đây để sắp xếp các item (A-Z / Z-A / Thứ tự ban đầu)");
        pnlSavedHeader.Controls.Add(lblSavedTitle);

        FlowLayoutPanel pnlSavedHeaderActions = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, DpiScale(4), 0, DpiScale(4))
        };
        pnlSavedHeader.Controls.Add(pnlSavedHeaderActions);
        pnlSavedHeaderActions.BringToFront();

        btnSortObjects = CreateListHeaderButton("Sắp xếp", IconChar.ArrowDownAZ, 0);
        btnSortObjects.Click += (s, e) => ShowSortContextMenu(btnSortObjects);
        pnlSavedHeaderActions.Controls.Add(btnSortObjects);

        btnExportPackage = CreateListHeaderButton("Export", IconChar.BoxesPacking, 0, Color.FromArgb(16, 185, 129), Color.White);
        btnExportPackage.Font = new Font("Segoe UI Bold", 9.5F);
        btnExportPackage.Click += (s, e) => ExportPackage();
        pnlSavedHeaderActions.Controls.Add(btnExportPackage);

        btnExportJson = CreateListHeaderButton("Xuất JSON", IconChar.FileExport, 0);
        btnExportJson.Click += (s, e) => ExportRegionsToJson();
        pnlSavedHeaderActions.Controls.Add(btnExportJson);

        btnImportJson = CreateListHeaderButton("Nhập JSON", IconChar.FileImport, 0);
        btnImportJson.Click += (s, e) => ImportRegionsFromJson();
        pnlSavedHeaderActions.Controls.Add(btnImportJson);

        btnClearAll = CreateListHeaderButton("Xóa hết", IconChar.TrashCan, 0, Color.FromArgb(70, 40, 48), Color.FromArgb(255, 160, 160));
        btnClearAll.Margin = Padding.Empty;
        btnClearAll.Click += (s, e) => ClearAllSavedRegions();
        pnlSavedHeaderActions.Controls.Add(btnClearAll);

        // DataGridView for Saved Regions
        dgvSavedRegions = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(28, 31, 38),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(48, 52, 64),
            RowHeadersVisible = false,
            ColumnHeadersVisible = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = false,
            EditMode = DataGridViewEditMode.EditProgrammatically,
            AutoGenerateColumns = false,
            ShowCellToolTips = true,
            Font = new Font("Segoe UI", 9F)
        };

        dgvSavedRegions.EnableHeadersVisualStyles = false;
        dgvSavedRegions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvSavedRegions.ColumnHeadersHeight = DpiScale(32);
        dgvSavedRegions.RowTemplate.Height = DpiScale(28);
        dgvSavedRegions.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(42, 46, 56);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        dgvSavedRegions.DefaultCellStyle.BackColor = Color.FromArgb(34, 38, 46);
        dgvSavedRegions.DefaultCellStyle.ForeColor = Color.White;
        dgvSavedRegions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(54, 70, 96);
        dgvSavedRegions.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvSavedRegions.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 42, 52);

        // Columns definition with clear titles and alignment
        var colIdx = new DataGridViewTextBoxColumn { Name = "ColIndex", HeaderText = "#", Width = DpiScale(42), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colIdx.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colIdx);

        var colType = new DataGridViewTextBoxColumn { Name = "ColType", HeaderText = "Loại", Width = DpiScale(50), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colType.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colType);

        var colName = new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Tên vùng / ảnh", Width = DpiScale(175), ReadOnly = false, SortMode = DataGridViewColumnSortMode.Programmatic };
        colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgvSavedRegions.Columns.Add(colName);

        var colX = new DataGridViewTextBoxColumn { Name = "ColX", HeaderText = "Tọa độ X", Width = DpiScale(75), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colX.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colX);

        var colY = new DataGridViewTextBoxColumn { Name = "ColY", HeaderText = "Tọa độ Y", Width = DpiScale(75), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colY.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colY);

        var colW = new DataGridViewTextBoxColumn { Name = "ColW", HeaderText = "Rộng (W)", Width = DpiScale(80), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colW.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colW);

        var colH = new DataGridViewTextBoxColumn { Name = "ColH", HeaderText = "Cao (H)", Width = DpiScale(80), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colH.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colH);

        var colRatio = new DataGridViewTextBoxColumn { Name = "ColRatio", HeaderText = "Tỉ lệ", Width = DpiScale(75), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colRatio.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colRatio);

        var colCreated = new DataGridViewTextBoxColumn { Name = "ColCreated", HeaderText = "Thời gian tạo", Width = DpiScale(140), ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colCreated.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colCreated);

        var colNotes = new DataGridViewTextBoxColumn { Name = "ColNotes", HeaderText = "Ghi chú / Đường dẫn", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic };
        colNotes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgvSavedRegions.Columns.Add(colNotes);

        // Action Buttons Columns (compact icon-only buttons)
        var colEditBtn = new DataGridViewButtonColumn
        {
            Name = "ColEditBtn",
            HeaderText = "Sửa",
            Text = "",
            UseColumnTextForButtonValue = false,
            Width = DpiScale(45),
            FlatStyle = FlatStyle.Flat,
            ReadOnly = true
        };
        colEditBtn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colEditBtn);

        var colDeleteBtn = new DataGridViewButtonColumn
        {
            Name = "ColDeleteBtn",
            HeaderText = "Xóa",
            Text = "",
            UseColumnTextForButtonValue = false,
            Width = DpiScale(45),
            FlatStyle = FlatStyle.Flat,
            ReadOnly = true
        };
        colDeleteBtn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colDeleteBtn);

        dgvSavedRegions.SelectionChanged += (s, e) => OnSavedGridSelectionChanged();
        dgvSavedRegions.CellDoubleClick += (s, e) => OnSavedGridDoubleClick(e);
        dgvSavedRegions.CellEndEdit += (s, e) => OnSavedGridCellEndEdit(e);
        dgvSavedRegions.EditingControlShowing += (s, e) => OnSavedGridEditingControlShowing(e);
        dgvSavedRegions.CellContentClick += (s, e) => OnSavedGridCellContentClick(e);
        dgvSavedRegions.CellToolTipTextNeeded += (s, e) => OnSavedGridToolTipTextNeeded(e);
        dgvSavedRegions.CellPainting += (s, e) => OnSavedGridCellPainting(e);
        dgvSavedRegions.ColumnHeaderMouseClick += (s, e) => OnSavedGridColumnHeaderMouseClick(e);

        pnlSavedSection.Controls.Add(dgvSavedRegions);
        pnlSavedSection.Controls.Add(pnlSavedHeader);
        pnlSavedHeader.SendToBack();
    }

    #region UI Helper Creators

    private IconButton CreateHeaderButton(string text, IconChar icon, Color? backColor = null)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            UseMnemonic = false,
            IconChar = icon,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(8), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, DpiScale(6), 0),
            Height = DpiScale(34),
            AutoSize = true,
            BackColor = backColor ?? Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Label CreateParamLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.White
        };
    }

    private NumericUpDown CreateNumberBox(int x, int y, int width)
    {
        return new NumericUpDown
        {
            Location = new Point(x, y),
            Width = width,
            Height = DpiScale(28),
            Minimum = 0,
            Maximum = 10000,
            Value = 0,
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI Bold", 9F)
        };
    }

    private IconButton CreateToolButton(IconChar icon, int x, int y, int w, int h, Color? iconColor = null)
    {
        IconButton btn = new()
        {
            IconChar = icon,
            IconColor = iconColor ?? Color.White,
            IconSize = DpiScale(14),
            ImageAlign = ContentAlignment.MiddleCenter,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private IconButton CreateCenterButton(string text, IconChar icon, int x, int y, int w, int h)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            UseMnemonic = false,
            IconChar = icon,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(6), 0, DpiScale(6), 0),
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Button CreateMiniButton(string text, int x, int y, int w, int h)
    {
        Button btn = new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private IconButton CreateListHeaderButton(string text, IconChar icon, int x, Color? bg = null, Color? iconColor = null)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            UseMnemonic = false,
            IconChar = icon,
            IconColor = iconColor ?? Color.White,
            IconSize = DpiScale(14),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(8), 0, DpiScale(8), 0),
            Margin = new Padding(0, 0, DpiScale(6), 0),
            Location = new Point(x, DpiScale(5)),
            AutoSize = true,
            Height = DpiScale(30),
            BackColor = bg ?? Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private Button CreatePresetButton(string text)
    {
        Button btn = new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = DpiScale(26),
            Margin = new Padding(DpiScale(2)),
            Padding = Padding.Empty,
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            UseCompatibleTextRendering = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    #endregion
}
