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
    private IconPictureBox picAppLogo = null!;
    private Label lblAppTitle = null!;
    private IconButton btnOpenFile = null!;
    private IconButton btnPasteClipboard = null!;
    private IconButton btnLiveCapture = null!;
    private IconButton btnWindowCapture = null!;
    private IconButton btnFitView = null!;
    private IconButton btn100View = null!;
    private Label lblImageInfo = null!;
    private Label lblCursorInfo = null!;

    // Body Panels
    private SplitContainer splitMain = null!;
    private SplitContainer splitTop = null!;

    // Left Panel (Canvas)
    private Panel pnlCanvasContainer = null!;
    private Panel pnlCanvasHeader = null!;
    private IconPictureBox picCanvasIcon = null!;
    private Label lblCanvasTitle = null!;
    private IconPictureBox picCanvasGuide = null!;
    private Label lblCanvasGuide = null!;
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
    private Label lblAspectRatio = null!;

    // Nudge and resize buttons
    private IconButton btnNudgeLeft = null!;
    private IconButton btnNudgeRight = null!;
    private IconButton btnNudgeUp = null!;
    private IconButton btnNudgeDown = null!;
    private ComboBox cboNudgeStep = null!;

    private Button btnWMinus10 = null!;
    private Button btnWMinus1 = null!;
    private Button btnWPlus1 = null!;
    private Button btnWPlus10 = null!;
    private Button btnHMinus10 = null!;
    private Button btnHMinus1 = null!;
    private Button btnHPlus1 = null!;
    private Button btnHPlus10 = null!;
    private IconButton btnCenterBox = null!;

    // Action buttons in properties panel
    private IconButton btnSaveCoordinates = null!;
    private IconButton btnCropAndSaveImage = null!;
    private IconButton btnCopyCroppedImage = null!;

    // Saved list section
    private Panel pnlSavedSection = null!;
    private Panel pnlSavedHeader = null!;
    private IconPictureBox picSavedIcon = null!;
    private Label lblSavedTitle = null!;
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
        this.BackColor = Color.FromArgb(20, 22, 28);
        this.ForeColor = Color.FromArgb(235, 238, 245);
        this.Font = new Font("Segoe UI", 9F);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.KeyPreview = true;

        // -------------------------------------------------------------
        // Header Panel (Dock = Top, Height = 56)
        // -------------------------------------------------------------
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(56),
            BackColor = Color.FromArgb(28, 31, 40),
            Padding = new Padding(DpiScale(12), DpiScale(8), DpiScale(12), DpiScale(8))
        };

        picAppLogo = new IconPictureBox
        {
            IconChar = IconChar.CropSimple,
            IconColor = Color.FromArgb(0, 215, 255),
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
            ForeColor = Color.FromArgb(0, 215, 255),
            Location = new Point(picAppLogo.Right + DpiScale(8), DpiScale(15)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblAppTitle);

        FlowLayoutPanel pnlHeaderButtons = new()
        {
            Location = new Point(DpiScale(195), DpiScale(10)),
            Height = DpiScale(36),
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(pnlHeaderButtons);

        btnOpenFile = CreateHeaderButton("Nạp ảnh", IconChar.FolderOpen);
        btnOpenFile.Click += (s, e) => OpenImageFromFile();
        pnlHeaderButtons.Controls.Add(btnOpenFile);

        btnPasteClipboard = CreateHeaderButton("Dán (Ctrl+V)", IconChar.Paste);
        btnPasteClipboard.Click += (s, e) => PasteFromClipboard();
        pnlHeaderButtons.Controls.Add(btnPasteClipboard);

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

        // Header status labels (Flow right-aligned)
        FlowLayoutPanel pnlHeaderStatus = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(18), DpiScale(14), 0)
        };
        pnlHeader.Controls.Add(pnlHeaderStatus);

        lblImageInfo = new Label
        {
            Text = "Ảnh: Chưa nạp",
            ForeColor = Color.FromArgb(170, 185, 205),
            AutoSize = true,
            Margin = new Padding(0, 0, DpiScale(16), 0)
        };
        pnlHeaderStatus.Controls.Add(lblImageInfo);

        lblCursorInfo = new Label
        {
            Text = "Tọa độ chuột: -",
            ForeColor = Color.FromArgb(0, 220, 255),
            AutoSize = true
        };
        pnlHeaderStatus.Controls.Add(lblCursorInfo);

        // -------------------------------------------------------------
        // Body Container: splitMain (Top vs Bottom)
        // -------------------------------------------------------------
        splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(38, 42, 54),
            Size = new Size(DpiScale(1400), DpiScale(820)),
            Panel1MinSize = DpiScale(150),
            Panel2MinSize = DpiScale(150),
            SplitterDistance = DpiScale(530)
        };
        this.Controls.Add(splitMain);
        this.Controls.Add(pnlHeader);
        pnlHeader.SendToBack();

        // -------------------------------------------------------------
        // Top Panel: splitTop (Left Canvas vs Right Properties)
        // -------------------------------------------------------------
        splitTop = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(38, 42, 54),
            Size = new Size(DpiScale(1400), DpiScale(530)),
            Panel1MinSize = DpiScale(250),
            Panel2MinSize = DpiScale(280),
            SplitterDistance = DpiScale(1040)
        };
        splitMain.Panel1.Controls.Add(splitTop);

        // Left Canvas Container
        pnlCanvasContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 20, 26) };
        pnlCanvasHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(34),
            BackColor = Color.FromArgb(24, 27, 35),
            Padding = new Padding(DpiScale(8), DpiScale(5), DpiScale(8), DpiScale(5))
        };

        picCanvasIcon = new IconPictureBox
        {
            IconChar = IconChar.Image,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(7)),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasIcon);

        lblCanvasTitle = new Label
        {
            Text = "Ảnh gốc & Khung cắt ảo",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(240, 245, 255),
            Location = new Point(picCanvasIcon.Right + DpiScale(6), DpiScale(7)),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasTitle);

        picCanvasGuide = new IconPictureBox
        {
            IconChar = IconChar.Lightbulb,
            IconColor = Color.FromArgb(245, 185, 30),
            IconSize = DpiScale(14),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(lblCanvasTitle.Right + DpiScale(20), DpiScale(8)),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasGuide);

        lblCanvasGuide = new Label
        {
            Text = "Lăn chuột: Zoom | Chuột giữa/Phải: Pan | Kéo cạnh/góc: Resize",
            ForeColor = Color.FromArgb(140, 150, 170),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(picCanvasGuide.Right + DpiScale(6), DpiScale(8)),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasGuide);

        FlowLayoutPanel pnlCanvasZoom = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(4), DpiScale(8), 0)
        };
        pnlCanvasHeader.Controls.Add(pnlCanvasZoom);

        btnZoomOut = new IconButton
        {
            IconChar = IconChar.Minus,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            Size = new Size(DpiScale(28), DpiScale(24)),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnZoomOut.FlatAppearance.BorderSize = 0;
        btnZoomOut.Click += (s, e) => canvas.ZoomOut();
        pnlCanvasZoom.Controls.Add(btnZoomOut);

        lblZoomValue = new Label
        {
            Text = "100%",
            ForeColor = Color.FromArgb(0, 220, 255),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Size = new Size(DpiScale(50), DpiScale(24)),
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
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnZoomIn.FlatAppearance.BorderSize = 0;
        btnZoomIn.Click += (s, e) => canvas.ZoomIn();
        pnlCanvasZoom.Controls.Add(btnZoomIn);

        canvas = new CanvasControl { Dock = DockStyle.Fill };
        pnlCanvasContainer.Controls.Add(canvas);
        pnlCanvasContainer.Controls.Add(pnlCanvasHeader);
        splitTop.Panel1.Controls.Add(pnlCanvasContainer);

        // Right Panel: Properties & Adjustments (replacing Preview)
        pnlPropertiesContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 22, 28) };
        splitTop.Panel2.Controls.Add(pnlPropertiesContainer);
        InitializePropertiesPanel();

        // -------------------------------------------------------------
        // Bottom Panel: Saved Regions Grid (occupies 100% of bottom)
        // -------------------------------------------------------------
        pnlSavedSection = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 22, 28) };
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
            BackColor = Color.FromArgb(28, 31, 42),
            Padding = new Padding(DpiScale(10), DpiScale(4), DpiScale(8), DpiScale(4))
        };
        pnlPropertiesContainer.Controls.Add(pnlPropertiesHeader);

        picCoordIcon = new IconPictureBox
        {
            IconChar = IconChar.Sliders,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(8)),
            BackColor = Color.Transparent
        };
        pnlPropertiesHeader.Controls.Add(picCoordIcon);

        lblCoordSection = new Label
        {
            Text = "THÔNG SỐ & ĐIỀU CHỈNH",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.FromArgb(0, 215, 255),
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
            BackColor = Color.FromArgb(58, 30, 38),
            ForeColor = Color.FromArgb(255, 175, 185),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8F),
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnCancelEdit.FlatAppearance.BorderSize = 0;
        btnCancelEdit.Click += (s, e) => CancelEditing();
        pnlPropertiesHeader.Controls.Add(btnCancelEdit);

        pnlPropertiesBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(20, 22, 28),
            Padding = new Padding(DpiScale(8), DpiScale(6), DpiScale(8), DpiScale(6))
        };
        pnlPropertiesContainer.Controls.Add(pnlPropertiesBody);
        pnlPropertiesHeader.SendToBack();

        TableLayoutPanel tlpCards = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnlPropertiesBody.Controls.Add(tlpCards);

        // ---------------------------------------------------------
        // CARD 1: TỌA ĐỘ & KÍCH THƯỚC (Coordinates & Size)
        // ---------------------------------------------------------
        Panel cardCoords = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(26, 29, 39),
            Padding = new Padding(DpiScale(10), DpiScale(6), DpiScale(10), DpiScale(8)),
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpCards.Controls.Add(cardCoords, 0, 0);

        Label lblCard1Title = new()
        {
            Text = "TỌA ĐỘ & KÍCH THƯỚC (px)",
            Font = new Font("Segoe UI Bold", 8F),
            ForeColor = Color.FromArgb(142, 154, 168),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(4))
        };
        cardCoords.Controls.Add(lblCard1Title);

        FlowLayoutPanel pnlCard1Body = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, DpiScale(2), 0, 0)
        };
        cardCoords.Controls.Add(pnlCard1Body);
        lblCard1Title.SendToBack();

        // Row 1: X & Y
        FlowLayoutPanel rowXY = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, DpiScale(3))
        };
        pnlCard1Body.Controls.Add(rowXY);

        lblX = CreateParamLabel("X:", 0, 0);
        lblX.Margin = new Padding(0, DpiScale(4), DpiScale(2), 0);
        numX = CreateNumberBox(0, 0, DpiScale(78));
        numX.Margin = new Padding(0, 0, DpiScale(12), 0);
        numX.ValueChanged += (s, e) => OnNumericInputChanged();
        rowXY.Controls.Add(lblX);
        rowXY.Controls.Add(numX);

        lblY = CreateParamLabel("Y:", 0, 0);
        lblY.Margin = new Padding(0, DpiScale(4), DpiScale(2), 0);
        numY = CreateNumberBox(0, 0, DpiScale(78));
        numY.Margin = Padding.Empty;
        numY.ValueChanged += (s, e) => OnNumericInputChanged();
        rowXY.Controls.Add(lblY);
        rowXY.Controls.Add(numY);

        // Row 2: W & H
        FlowLayoutPanel rowWH = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, DpiScale(3))
        };
        pnlCard1Body.Controls.Add(rowWH);

        lblW = CreateParamLabel("W:", 0, 0);
        lblW.Margin = new Padding(0, DpiScale(4), DpiScale(2), 0);
        numW = CreateNumberBox(0, 0, DpiScale(78));
        numW.Margin = new Padding(0, 0, DpiScale(12), 0);
        numW.ValueChanged += (s, e) => OnNumericInputChanged();
        rowWH.Controls.Add(lblW);
        rowWH.Controls.Add(numW);

        lblH = CreateParamLabel("H:", 0, 0);
        lblH.Margin = new Padding(0, DpiScale(4), DpiScale(2), 0);
        numH = CreateNumberBox(0, 0, DpiScale(78));
        numH.Margin = Padding.Empty;
        numH.ValueChanged += (s, e) => OnNumericInputChanged();
        rowWH.Controls.Add(lblH);
        rowWH.Controls.Add(numH);

        // Row 3: Ratio
        lblAspectRatio = new Label
        {
            Text = "Tỉ lệ: 4:3",
            ForeColor = Color.FromArgb(0, 215, 255),
            Font = new Font("Segoe UI Semibold", 8.5F),
            AutoSize = true,
            Margin = new Padding(DpiScale(2), DpiScale(1), 0, 0)
        };
        pnlCard1Body.Controls.Add(lblAspectRatio);

        // ---------------------------------------------------------
        // CARD 2: DI CHUYỂN VÙNG CHỌN (Nudge / D-Pad)
        // ---------------------------------------------------------
        Panel cardNudge = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(26, 29, 39),
            Padding = new Padding(DpiScale(10), DpiScale(6), DpiScale(10), DpiScale(8)),
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpCards.Controls.Add(cardNudge, 0, 1);

        FlowLayoutPanel rowNudgeHead = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        cardNudge.Controls.Add(rowNudgeHead);

        Label lblNudgeTitle = new()
        {
            Text = "DI CHUYỂN (D-PAD)",
            Font = new Font("Segoe UI Bold", 8F),
            ForeColor = Color.FromArgb(142, 154, 168),
            AutoSize = true,
            Margin = new Padding(0, DpiScale(3), DpiScale(14), 0)
        };
        rowNudgeHead.Controls.Add(lblNudgeTitle);

        Label lblStep = new()
        {
            Text = "Bước:",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(170, 185, 205),
            AutoSize = true,
            Margin = new Padding(0, DpiScale(3), DpiScale(4), 0)
        };
        rowNudgeHead.Controls.Add(lblStep);

        cboNudgeStep = new ComboBox
        {
            Width = DpiScale(64),
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = DpiScale(18),
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 8F),
            Margin = Padding.Empty
        };
        cboNudgeStep.Items.AddRange(["1px", "5px", "10px", "50px"]);
        cboNudgeStep.SelectedIndex = 1;
        cboNudgeStep.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            using SolidBrush bg = new(Color.FromArgb(40, 44, 56));
            using SolidBrush fg = new(Color.White);
            e.Graphics.FillRectangle(bg, e.Bounds);
            StringFormat sf = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(cboNudgeStep.Items[e.Index].ToString(), cboNudgeStep.Font, fg, e.Bounds, sf);
        };
        rowNudgeHead.Controls.Add(cboNudgeStep);

        FlowLayoutPanel rowNudgeBtns = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, DpiScale(5), 0, 0)
        };
        cardNudge.Controls.Add(rowNudgeBtns);
        rowNudgeHead.SendToBack();

        btnNudgeLeft = CreateToolButton(IconChar.ArrowLeft, 0, 0, DpiScale(32), DpiScale(26));
        btnNudgeLeft.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeLeft.Click += (s, e) => NudgeCrop(-GetNudgeStep(), 0);
        rowNudgeBtns.Controls.Add(btnNudgeLeft);

        btnNudgeUp = CreateToolButton(IconChar.ArrowUp, 0, 0, DpiScale(32), DpiScale(26));
        btnNudgeUp.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeUp.Click += (s, e) => NudgeCrop(0, -GetNudgeStep());
        rowNudgeBtns.Controls.Add(btnNudgeUp);

        btnNudgeDown = CreateToolButton(IconChar.ArrowDown, 0, 0, DpiScale(32), DpiScale(26));
        btnNudgeDown.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeDown.Click += (s, e) => NudgeCrop(0, GetNudgeStep());
        rowNudgeBtns.Controls.Add(btnNudgeDown);

        btnNudgeRight = CreateToolButton(IconChar.ArrowRight, 0, 0, DpiScale(32), DpiScale(26));
        btnNudgeRight.Margin = new Padding(0, 0, DpiScale(8), 0);
        btnNudgeRight.Click += (s, e) => NudgeCrop(GetNudgeStep(), 0);
        rowNudgeBtns.Controls.Add(btnNudgeRight);

        btnCenterBox = CreateCenterButton("Căn giữa", IconChar.Bullseye, 0, 0, DpiScale(90), DpiScale(26));
        btnCenterBox.Margin = Padding.Empty;
        btnCenterBox.Click += (s, e) => CenterCropBox();
        rowNudgeBtns.Controls.Add(btnCenterBox);

        // ---------------------------------------------------------
        // CARD 3: THAY ĐỔI SIZE NHANH (Quick Size Adjust)
        // ---------------------------------------------------------
        Panel cardSize = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(26, 29, 39),
            Padding = new Padding(DpiScale(10), DpiScale(6), DpiScale(10), DpiScale(8)),
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpCards.Controls.Add(cardSize, 0, 2);

        Label lblCard3Title = new()
        {
            Text = "THAY ĐỔI SIZE NHANH",
            Font = new Font("Segoe UI Bold", 8F),
            ForeColor = Color.FromArgb(142, 154, 168),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(4))
        };
        cardSize.Controls.Add(lblCard3Title);

        FlowLayoutPanel pnlCard3Body = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, DpiScale(2), 0, 0)
        };
        cardSize.Controls.Add(pnlCard3Body);
        lblCard3Title.SendToBack();

        // Row W
        FlowLayoutPanel rowW = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, DpiScale(3))
        };
        pnlCard3Body.Controls.Add(rowW);

        Label lblWTitle = new()
        {
            Text = "Rộng (W):",
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Width = DpiScale(58),
            AutoSize = false,
            Margin = new Padding(0, DpiScale(4), 0, 0)
        };
        rowW.Controls.Add(lblWTitle);

        btnWMinus10 = CreateMiniButton("-10", 0, 0, DpiScale(40), DpiScale(23));
        btnWMinus10.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnWMinus10.Click += (s, e) => ResizeCrop(-10, 0);
        rowW.Controls.Add(btnWMinus10);

        btnWMinus1 = CreateMiniButton("-1", 0, 0, DpiScale(34), DpiScale(23));
        btnWMinus1.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnWMinus1.Click += (s, e) => ResizeCrop(-1, 0);
        rowW.Controls.Add(btnWMinus1);

        btnWPlus1 = CreateMiniButton("+1", 0, 0, DpiScale(34), DpiScale(23));
        btnWPlus1.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnWPlus1.Click += (s, e) => ResizeCrop(1, 0);
        rowW.Controls.Add(btnWPlus1);

        btnWPlus10 = CreateMiniButton("+10", 0, 0, DpiScale(40), DpiScale(23));
        btnWPlus10.Margin = Padding.Empty;
        btnWPlus10.Click += (s, e) => ResizeCrop(10, 0);
        rowW.Controls.Add(btnWPlus10);

        // Row H
        FlowLayoutPanel rowH = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        pnlCard3Body.Controls.Add(rowH);

        Label lblHTitle = new()
        {
            Text = "Cao (H):",
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Width = DpiScale(58),
            AutoSize = false,
            Margin = new Padding(0, DpiScale(4), 0, 0)
        };
        rowH.Controls.Add(lblHTitle);

        btnHMinus10 = CreateMiniButton("-10", 0, 0, DpiScale(40), DpiScale(23));
        btnHMinus10.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnHMinus10.Click += (s, e) => ResizeCrop(0, -10);
        rowH.Controls.Add(btnHMinus10);

        btnHMinus1 = CreateMiniButton("-1", 0, 0, DpiScale(34), DpiScale(23));
        btnHMinus1.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnHMinus1.Click += (s, e) => ResizeCrop(0, -1);
        rowH.Controls.Add(btnHMinus1);

        btnHPlus1 = CreateMiniButton("+1", 0, 0, DpiScale(34), DpiScale(23));
        btnHPlus1.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnHPlus1.Click += (s, e) => ResizeCrop(0, 1);
        rowH.Controls.Add(btnHPlus1);

        btnHPlus10 = CreateMiniButton("+10", 0, 0, DpiScale(40), DpiScale(23));
        btnHPlus10.Margin = Padding.Empty;
        btnHPlus10.Click += (s, e) => ResizeCrop(0, 10);
        rowH.Controls.Add(btnHPlus10);

        // ---------------------------------------------------------
        // CARD 4: HÀNH ĐỘNG (Actions)
        // ---------------------------------------------------------
        Panel cardActions = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(26, 29, 39),
            Padding = new Padding(DpiScale(10), DpiScale(6), DpiScale(10), DpiScale(8)),
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpCards.Controls.Add(cardActions, 0, 3);

        Label lblCard4Title = new()
        {
            Text = "HÀNH ĐỘNG",
            Font = new Font("Segoe UI Bold", 8F),
            ForeColor = Color.FromArgb(142, 154, 168),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(4))
        };
        cardActions.Controls.Add(lblCard4Title);

        FlowLayoutPanel pnlCard4Body = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, DpiScale(2), 0, 0)
        };
        cardActions.Controls.Add(pnlCard4Body);
        lblCard4Title.SendToBack();

        btnSaveCoordinates = new IconButton
        {
            Text = " LƯU TỌA ĐỘ",
            UseMnemonic = false,
            IconChar = IconChar.FloppyDisk,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(10), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, 0, DpiScale(4)),
            Width = DpiScale(250),
            Height = DpiScale(32),
            BackColor = Color.FromArgb(0, 180, 216),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Bold", 8.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSaveCoordinates.FlatAppearance.BorderSize = 0;
        btnSaveCoordinates.Click += (s, e) => SaveCurrentCoordinates();
        pnlCard4Body.Controls.Add(btnSaveCoordinates);

        btnCropAndSaveImage = new IconButton
        {
            Text = " CẮT & LƯU ẢNH (Ctrl+S)",
            UseMnemonic = false,
            IconChar = IconChar.Crop,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(10), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, 0, DpiScale(4)),
            Width = DpiScale(250),
            Height = DpiScale(32),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Bold", 8.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCropAndSaveImage.FlatAppearance.BorderSize = 0;
        btnCropAndSaveImage.Click += (s, e) => CropAndSaveImage();
        pnlCard4Body.Controls.Add(btnCropAndSaveImage);

        btnCopyCroppedImage = new IconButton
        {
            Text = " COPY ẢNH (Ctrl+C)",
            UseMnemonic = false,
            IconChar = IconChar.Copy,
            IconColor = Color.White,
            IconSize = DpiScale(15),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(10), 0, DpiScale(10), 0),
            Margin = Padding.Empty,
            Width = DpiScale(250),
            Height = DpiScale(28),
            BackColor = Color.FromArgb(48, 54, 70),
            ForeColor = Color.FromArgb(235, 240, 255),
            Font = new Font("Segoe UI Semibold", 8.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCopyCroppedImage.FlatAppearance.BorderSize = 0;
        btnCopyCroppedImage.Click += (s, e) => CopyCroppedImageToClipboard();
        pnlCard4Body.Controls.Add(btnCopyCroppedImage);

        // Adjust action buttons width on body resize
        pnlPropertiesBody.Resize += (s, e) =>
        {
            int w = Math.Max(DpiScale(200), pnlPropertiesBody.ClientSize.Width - DpiScale(24));
            btnSaveCoordinates.Width = w;
            btnCropAndSaveImage.Width = w;
            btnCopyCroppedImage.Width = w;
        };
    }

    private void InitializeSavedSection()
    {
        pnlSavedHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(40),
            BackColor = Color.FromArgb(30, 34, 46),
            Padding = new Padding(DpiScale(14), DpiScale(6), DpiScale(14), DpiScale(6))
        };
        pnlSavedSection.Controls.Add(pnlSavedHeader);

        picSavedIcon = new IconPictureBox
        {
            IconChar = IconChar.RectangleList,
            IconColor = Color.FromArgb(0, 220, 255),
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(DpiScale(14), DpiScale(11)),
            BackColor = Color.Transparent
        };
        pnlSavedHeader.Controls.Add(picSavedIcon);

        lblSavedTitle = new Label
        {
            Text = "DANH SÁCH ĐÃ LƯU (TỌA ĐỘ & HÌNH ẢNH): 0 mục",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9.5F),
            ForeColor = Color.FromArgb(0, 220, 255),
            Location = new Point(picSavedIcon.Right + DpiScale(8), DpiScale(10)),
            AutoSize = true
        };
        pnlSavedHeader.Controls.Add(lblSavedTitle);

        FlowLayoutPanel pnlSavedHeaderActions = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(2), DpiScale(6), DpiScale(2))
        };
        pnlSavedHeader.Controls.Add(pnlSavedHeaderActions);

        btnExportPackage = CreateListHeaderButton("Export", IconChar.BoxesPacking, 0, Color.FromArgb(16, 185, 129), Color.White);
        btnExportPackage.Font = new Font("Segoe UI Bold", 8.5F);
        btnExportPackage.Click += (s, e) => ExportPackage();
        pnlSavedHeaderActions.Controls.Add(btnExportPackage);

        btnExportJson = CreateListHeaderButton("Xuất JSON", IconChar.FileExport, 0);
        btnExportJson.Click += (s, e) => ExportRegionsToJson();
        pnlSavedHeaderActions.Controls.Add(btnExportJson);

        btnImportJson = CreateListHeaderButton("Nhập JSON", IconChar.FileImport, 0);
        btnImportJson.Click += (s, e) => ImportRegionsFromJson();
        pnlSavedHeaderActions.Controls.Add(btnImportJson);

        btnClearAll = CreateListHeaderButton("Xóa hết", IconChar.TrashCan, 0, Color.FromArgb(70, 45, 55), Color.FromArgb(255, 160, 160));
        btnClearAll.Click += (s, e) => ClearAllSavedRegions();
        pnlSavedHeaderActions.Controls.Add(btnClearAll);

        // DataGridView for Saved Regions
        dgvSavedRegions = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(18, 20, 26),
            ForeColor = Color.FromArgb(230, 235, 245),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(34, 38, 48),
            RowHeadersVisible = false,
            ColumnHeadersVisible = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            AutoGenerateColumns = false,
            ShowCellToolTips = true,
            Font = new Font("Segoe UI", 9F)
        };

        dgvSavedRegions.EnableHeadersVisualStyles = false;
        dgvSavedRegions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvSavedRegions.ColumnHeadersHeight = DpiScale(34);
        dgvSavedRegions.RowTemplate.Height = DpiScale(28);
        dgvSavedRegions.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 33, 46);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(0, 215, 255);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        dgvSavedRegions.DefaultCellStyle.BackColor = Color.FromArgb(22, 25, 33);
        dgvSavedRegions.DefaultCellStyle.ForeColor = Color.FromArgb(230, 235, 245);
        dgvSavedRegions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 119, 182);
        dgvSavedRegions.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvSavedRegions.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(26, 30, 40);

        // Columns definition with clear titles and alignment
        var colIdx = new DataGridViewTextBoxColumn { Name = "ColIndex", HeaderText = "#", Width = DpiScale(42) };
        colIdx.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colIdx);

        var colType = new DataGridViewTextBoxColumn { Name = "ColType", HeaderText = "Phân loại", Width = DpiScale(105) };
        colType.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colType);

        var colName = new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Tên vùng / ảnh", Width = DpiScale(175) };
        colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgvSavedRegions.Columns.Add(colName);

        var colX = new DataGridViewTextBoxColumn { Name = "ColX", HeaderText = "Tọa độ X", Width = DpiScale(75) };
        colX.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colX);

        var colY = new DataGridViewTextBoxColumn { Name = "ColY", HeaderText = "Tọa độ Y", Width = DpiScale(75) };
        colY.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colY);

        var colW = new DataGridViewTextBoxColumn { Name = "ColW", HeaderText = "Rộng (W)", Width = DpiScale(80) };
        colW.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colW);

        var colH = new DataGridViewTextBoxColumn { Name = "ColH", HeaderText = "Cao (H)", Width = DpiScale(80) };
        colH.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colH);

        var colRatio = new DataGridViewTextBoxColumn { Name = "ColRatio", HeaderText = "Tỉ lệ", Width = DpiScale(75) };
        colRatio.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colRatio);

        var colCreated = new DataGridViewTextBoxColumn { Name = "ColCreated", HeaderText = "Thời gian tạo", Width = DpiScale(140) };
        colCreated.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colCreated);

        var colNotes = new DataGridViewTextBoxColumn { Name = "ColNotes", HeaderText = "Ghi chú / Đường dẫn", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
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
            FlatStyle = FlatStyle.Flat
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
            FlatStyle = FlatStyle.Flat
        };
        colDeleteBtn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colDeleteBtn);

        dgvSavedRegions.SelectionChanged += (s, e) => OnSavedGridSelectionChanged();
        dgvSavedRegions.CellDoubleClick += (s, e) => OnSavedGridDoubleClick(e);
        dgvSavedRegions.CellContentClick += (s, e) => OnSavedGridCellContentClick(e);
        dgvSavedRegions.CellToolTipTextNeeded += (s, e) => OnSavedGridToolTipTextNeeded(e);
        dgvSavedRegions.CellPainting += (s, e) => OnSavedGridCellPainting(e);

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
            BackColor = backColor ?? Color.FromArgb(42, 47, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F),
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
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(200, 210, 230)
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
            BackColor = Color.FromArgb(40, 44, 56),
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
            IconColor = iconColor ?? Color.FromArgb(230, 235, 245),
            IconSize = DpiScale(15),
            ImageAlign = ContentAlignment.MiddleCenter,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(44, 49, 64),
            ForeColor = Color.FromArgb(230, 235, 245),
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
            IconColor = Color.FromArgb(230, 235, 245),
            IconSize = DpiScale(14),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(6), 0, DpiScale(6), 0),
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(44, 49, 64),
            ForeColor = Color.FromArgb(230, 235, 245),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
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
            BackColor = Color.FromArgb(36, 40, 52),
            ForeColor = Color.FromArgb(200, 215, 235),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
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
            BackColor = bg ?? Color.FromArgb(44, 49, 64),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    #endregion
}
