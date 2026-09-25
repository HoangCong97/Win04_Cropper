#nullable enable
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper;

partial class MainCropperForm
{
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
}
