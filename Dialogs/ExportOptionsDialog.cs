using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper.Controls;

public class ExportOptionsDialog : Form
{
    private readonly TextBox txtExportName;
    private readonly CheckBox chkExportSources;
    private readonly CheckBox chkAreasWithImages;
    private readonly IconButton btnExport;
    private readonly IconButton btnCancel;
    private readonly float _dpiScale;

    private int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    public string ExportPackageName => !string.IsNullOrWhiteSpace(txtExportName.Text) 
        ? txtExportName.Text.Trim() 
        : $"Cropper_Export_{DateTime.Now:yyyyMMdd_HHmm}";

    public bool ExportSources => chkExportSources.Checked;
    public bool ExportAreasWithImages => chkAreasWithImages.Checked;

    public ExportOptionsDialog(int areaCount, int cropCount, float dpiScale = 1.0f)
        : this($"Export_{DateTime.Now:yyyyMMdd_HHmm}", areaCount, cropCount, dpiScale)
    {
    }

    public ExportOptionsDialog(string defaultName, int areaCount, int cropCount, float dpiScale = 1.0f)
    {
        _dpiScale = dpiScale > 0 ? dpiScale : 1.0f;

        Text = "Tùy chọn xuất gói dữ liệu (Export)";
        Size = new Size(DpiScale(530), DpiScale(485));
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(28, 31, 38);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        ShowInTaskbar = false;

        // -------------------------------------------------------------
        // Header
        // -------------------------------------------------------------
        Panel pnlHeader = new()
        {
            Dock = DockStyle.Top,
            Height = DpiScale(62),
            BackColor = Color.FromArgb(36, 40, 50),
            Padding = new Padding(DpiScale(16), DpiScale(12), DpiScale(16), DpiScale(12))
        };

        IconPictureBox picHeader = new()
        {
            IconChar = IconChar.BoxesPacking,
            IconColor = Color.FromArgb(16, 185, 129),
            IconSize = DpiScale(26),
            Size = new Size(DpiScale(30), DpiScale(30)),
            Location = new Point(DpiScale(16), DpiScale(16)),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picHeader);

        Label lblHeaderTitle = new()
        {
            Text = "TÙY CHỌN XUẤT GÓI DỮ LIỆU",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 11F),
            ForeColor = Color.White,
            Location = new Point(picHeader.Right + DpiScale(10), DpiScale(10)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblHeaderTitle);

        Label lblHeaderSub = new()
        {
            Text = "Thiết lập tên gói xuất và tùy chọn thành phần dữ liệu xuất ra",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Location = new Point(picHeader.Right + DpiScale(10), DpiScale(34)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblHeaderSub);

        // -------------------------------------------------------------
        // Body Content Panel
        // -------------------------------------------------------------
        Panel pnlBody = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DpiScale(20), DpiScale(10), DpiScale(20), DpiScale(10)),
            BackColor = Color.Transparent
        };

        int cardW = DpiScale(474);

        // Section 1: Export Name Input
        Label lblNameTitle = new()
        {
            Text = "Tên gói xuất (tên thư mục):",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(220, 230, 245),
            Location = new Point(DpiScale(20), DpiScale(10)),
            AutoSize = true
        };
        pnlBody.Controls.Add(lblNameTitle);

        txtExportName = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(defaultName) ? $"Export_{DateTime.Now:yyyyMMdd_HHmm}" : defaultName,
            Location = new Point(DpiScale(20), DpiScale(32)),
            Size = new Size(cardW, DpiScale(28)),
            BackColor = Color.FromArgb(42, 46, 56),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        pnlBody.Controls.Add(txtExportName);

        // Section 2: Stats
        Label lblStats = new()
        {
            Text = $"Dự án hiện có: {areaCount} mục Tọa độ (Areas) • {cropCount} mục Hình ảnh (Crops)",
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.FromArgb(100, 200, 255),
            Location = new Point(DpiScale(20), DpiScale(66)),
            AutoSize = true
        };
        pnlBody.Controls.Add(lblStats);

        // Section 3: Options Card (Ảnh gốc & Areas crop)
        Panel pnlOptionsCard = new()
        {
            Location = new Point(DpiScale(20), DpiScale(90)),
            Size = new Size(cardW, DpiScale(125)),
            BackColor = Color.FromArgb(36, 40, 50),
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlBody.Controls.Add(pnlOptionsCard);

        // Checkbox: Ảnh gốc (default: false)
        chkExportSources = new CheckBox
        {
            Text = "Xuất kèm ảnh gốc toàn phần (lưu vào sources/)",
            Checked = false, // Mặc định không chọn
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(240, 240, 245),
            Location = new Point(DpiScale(14), DpiScale(12)),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        pnlOptionsCard.Controls.Add(chkExportSources);

        Label lblSourcesHint = new()
        {
            Text = "Default: Không chọn (chỉ xuất file data.json và thư mục ảnh crops/)",
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.FromArgb(140, 150, 165),
            Location = new Point(DpiScale(34), DpiScale(36)),
            AutoSize = true
        };
        pnlOptionsCard.Controls.Add(lblSourcesHint);

        // Checkbox: Areas with images (default: false)
        chkAreasWithImages = new CheckBox
        {
            Text = "Cắt & xuất ảnh tương ứng cho các mục Tọa độ (Areas) vào crops/",
            Checked = false, // Mặc định không chọn
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(240, 240, 245),
            Location = new Point(DpiScale(14), DpiScale(66)),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        pnlOptionsCard.Controls.Add(chkAreasWithImages);

        Label lblAreasHint = new()
        {
            Text = "Default: Không chọn (chỉ xuất thông số tọa độ vào file data.json)",
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.FromArgb(140, 150, 165),
            Location = new Point(DpiScale(34), DpiScale(90)),
            AutoSize = true
        };
        pnlOptionsCard.Controls.Add(lblAreasHint);

        // Section 4: Structure Preview Card (Clean and simple)
        Panel pnlStructureCard = new()
        {
            Location = new Point(DpiScale(20), DpiScale(223)),
            Size = new Size(cardW, DpiScale(96)),
            BackColor = Color.FromArgb(30, 34, 44),
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlBody.Controls.Add(pnlStructureCard);

        Label lblStructTitle = new()
        {
            Text = "CẤU TRÚC GÓI XUẤT ĐƠN GIẢN & CHUẨN HÓA:",
            Font = new Font("Segoe UI Bold", 8.5F),
            ForeColor = Color.FromArgb(16, 185, 129),
            Location = new Point(DpiScale(12), DpiScale(8)),
            AutoSize = true
        };
        pnlStructureCard.Controls.Add(lblStructTitle);

        Label lblStructDesc = new()
        {
            Text = "• README.md: Tài liệu hướng dẫn định dạng và hệ tọa độ\n" +
                   "• data.json: Tệp dữ liệu chứa tọa độ & thông tin ảnh (sắp xếp theo loại: Areas, Crops)\n" +
                   "• crops/: Thư mục chứa toàn bộ các ảnh đã cắt (PNG)\n" +
                   "• sources/: Thư mục ảnh gốc (chỉ tạo khi tích chọn 'Xuất kèm ảnh gốc' ở trên)",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(180, 195, 215),
            Location = new Point(DpiScale(12), DpiScale(28)),
            Size = new Size(cardW - DpiScale(24), DpiScale(62))
        };
        pnlStructureCard.Controls.Add(lblStructDesc);

        // -------------------------------------------------------------
        // Bottom Action Panel
        // -------------------------------------------------------------
        Panel pnlBottom = new()
        {
            Dock = DockStyle.Bottom,
            Height = DpiScale(58),
            BackColor = Color.FromArgb(24, 27, 34),
            Padding = new Padding(DpiScale(16), DpiScale(11), DpiScale(20), DpiScale(11))
        };

        btnCancel = new IconButton
        {
            Text = " Hủy",
            DialogResult = DialogResult.Cancel,
            IconChar = IconChar.Xmark,
            IconColor = Color.White,
            IconSize = DpiScale(15),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(DpiScale(90), DpiScale(34)),
            Location = new Point(DpiScale(404), DpiScale(12)),
            BackColor = Color.FromArgb(60, 65, 78),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F)
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        pnlBottom.Controls.Add(btnCancel);

        btnExport = new IconButton
        {
            Text = " Chọn thư mục & Xuất",
            IconChar = IconChar.FolderOpen,
            IconColor = Color.White,
            IconSize = DpiScale(15),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(DpiScale(185), DpiScale(34)),
            Location = new Point(btnCancel.Left - DpiScale(195), DpiScale(12)),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Bold", 9F)
        };
        btnExport.FlatAppearance.BorderSize = 0;
        btnExport.Click += (s, e) =>
        {
            string name = ExportPackageName;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên gói xuất!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("Tên gói xuất không được chứa ký tự đặc biệt (\\ / : * ? \" < > |)!", "Tên không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        };
        pnlBottom.Controls.Add(btnExport);

        // Add controls in correct docking order: Body between Header and Bottom
        Controls.Add(pnlBody);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);

        AcceptButton = btnExport;
        CancelButton = btnCancel;
    }
}
