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
    private SlimScrollBar scrollBarProperties = null!;
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
}
