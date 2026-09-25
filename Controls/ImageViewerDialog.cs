using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Models;

namespace Win04_Cropper.Controls;

public class ImageViewerDialog : Form
{
    private Bitmap? _image;
    private readonly string? _imagePath;
    private readonly CropRegionItem? _item;
    private readonly CanvasControl _canvas = new();
    private readonly Label _lblInfo = new();
    private readonly Label _lblTitle = new();
    private readonly IconButton _btnSaveChanges = new();
    private readonly IconButton _btnEditInMain = new();
    private readonly IconButton _btnCopy = new();
    private readonly IconButton _btnOpenFolder = new();
    private readonly IconButton _btnClose = new();

    public bool RequestedEditInMain { get; private set; }
    public bool HasSavedChanges { get; private set; }

    public ImageViewerDialog(CropRegionItem item, Bitmap? fallbackBitmap, string? imagePath = null)
        : this(imagePath ?? item.ImagePath, fallbackBitmap, item.Name, new Rectangle(item.X, item.Y, item.Width, item.Height), item)
    {
    }

    public ImageViewerDialog(string? imagePath, Bitmap? fallbackBitmap, string imageName, Rectangle originalCoords, CropRegionItem? item = null)
    {
        _imagePath = imagePath;
        _item = item;

        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                using var tempBmp = new Bitmap(stream);
                _image = new Bitmap(tempBmp.Width, tempBmp.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(_image);
                g.DrawImage(tempBmp, 0, 0, tempBmp.Width, tempBmp.Height);
            }
            catch
            {
                _image = fallbackBitmap != null ? new Bitmap(fallbackBitmap) : null;
            }
        }
        else if (fallbackBitmap != null)
        {
            _image = new Bitmap(fallbackBitmap);
        }

        InitializeUi(imageName, originalCoords);
    }

    private void InitializeUi(string imageName, Rectangle originalCoords)
    {
        Text = $"Chi tiết đối tượng: {imageName}";
        Size = new Size(1000, 720);
        MinimumSize = new Size(680, 480);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(20, 22, 28);
        ForeColor = Color.FromArgb(235, 240, 250);
        Font = new Font("Segoe UI", 9F);

        // Header
        Panel pnlHeader = new()
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(42, 46, 56),
            Padding = new Padding(14, 8, 14, 8)
        };

        IconPictureBox picHeader = new()
        {
            IconChar = _item?.ItemType == CropItemType.Image ? IconChar.Image : IconChar.LocationDot,
            IconColor = _item?.ItemType == CropItemType.Image ? Color.FromArgb(50, 220, 150) : Color.White,
            IconSize = 22,
            Size = new Size(24, 24),
            Location = new Point(14, 14),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picHeader);

        _lblTitle.Text = imageName;
        _lblTitle.Font = new Font("Segoe UI Bold", 11.5F);
        _lblTitle.ForeColor = Color.White;
        _lblTitle.Location = new Point(44, 13);
        _lblTitle.AutoSize = true;
        pnlHeader.Controls.Add(_lblTitle);

        _lblInfo.ForeColor = Color.FromArgb(170, 185, 205);
        _lblInfo.Location = new Point(_lblTitle.Right + 20, 16);
        _lblInfo.AutoSize = true;
        pnlHeader.Controls.Add(_lblInfo);
        UpdateInfoLabel();

        Controls.Add(pnlHeader);

        // Footer Actions
        Panel pnlFooter = new()
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.FromArgb(26, 29, 38),
            Padding = new Padding(12, 8, 12, 8)
        };

        FlowLayoutPanel pnlFooterLeft = new()
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlFooter.Controls.Add(pnlFooterLeft);

        // 1. Nút Lưu lại thay đổi nếu thay đổi khung
        _btnSaveChanges.Text = " Lưu lại thay đổi";
        _btnSaveChanges.IconChar = IconChar.FloppyDisk;
        _btnSaveChanges.IconColor = Color.White;
        _btnSaveChanges.IconSize = 15;
        _btnSaveChanges.TextImageRelation = TextImageRelation.ImageBeforeText;
        _btnSaveChanges.ImageAlign = ContentAlignment.MiddleLeft;
        _btnSaveChanges.TextAlign = ContentAlignment.MiddleLeft;
        _btnSaveChanges.Padding = new Padding(10, 0, 10, 0);
        _btnSaveChanges.Margin = new Padding(0, 0, 8, 0);
        _btnSaveChanges.Height = 36;
        _btnSaveChanges.AutoSize = true;
        _btnSaveChanges.BackColor = Color.FromArgb(16, 185, 129);
        _btnSaveChanges.ForeColor = Color.White;
        _btnSaveChanges.FlatStyle = FlatStyle.Flat;
        _btnSaveChanges.FlatAppearance.BorderSize = 0;
        _btnSaveChanges.Font = new Font("Segoe UI Semibold", 9F);
        _btnSaveChanges.Cursor = Cursors.Hand;
        _btnSaveChanges.Click += (s, e) => SaveCropChanges();
        pnlFooterLeft.Controls.Add(_btnSaveChanges);

        // 2. Nút Sửa chi tiết để đưa vào main và sửa
        _btnEditInMain.Text = " Sửa chi tiết";
        _btnEditInMain.IconChar = IconChar.PenToSquare;
        _btnEditInMain.IconColor = Color.White;
        _btnEditInMain.IconSize = 15;
        _btnEditInMain.TextImageRelation = TextImageRelation.ImageBeforeText;
        _btnEditInMain.ImageAlign = ContentAlignment.MiddleLeft;
        _btnEditInMain.TextAlign = ContentAlignment.MiddleLeft;
        _btnEditInMain.Padding = new Padding(10, 0, 10, 0);
        _btnEditInMain.Margin = new Padding(0, 0, 8, 0);
        _btnEditInMain.Height = 36;
        _btnEditInMain.AutoSize = true;
        _btnEditInMain.BackColor = Color.FromArgb(0, 122, 204);
        _btnEditInMain.ForeColor = Color.White;
        _btnEditInMain.FlatStyle = FlatStyle.Flat;
        _btnEditInMain.FlatAppearance.BorderSize = 0;
        _btnEditInMain.Font = new Font("Segoe UI Semibold", 9F);
        _btnEditInMain.Cursor = Cursors.Hand;
        _btnEditInMain.Click += (s, e) =>
        {
            RequestedEditInMain = true;
            DialogResult = DialogResult.OK;
            Close();
        };
        pnlFooterLeft.Controls.Add(_btnEditInMain);

        // 3. Sao chép ảnh
        _btnCopy.Text = " Sao chép (Ctrl+C)";
        _btnCopy.IconChar = IconChar.Copy;
        _btnCopy.IconColor = Color.White;
        _btnCopy.IconSize = 15;
        _btnCopy.TextImageRelation = TextImageRelation.ImageBeforeText;
        _btnCopy.ImageAlign = ContentAlignment.MiddleLeft;
        _btnCopy.TextAlign = ContentAlignment.MiddleLeft;
        _btnCopy.Padding = new Padding(10, 0, 10, 0);
        _btnCopy.Margin = new Padding(0, 0, 8, 0);
        _btnCopy.Height = 36;
        _btnCopy.AutoSize = true;
        _btnCopy.BackColor = Color.FromArgb(50, 55, 68);
        _btnCopy.ForeColor = Color.White;
        _btnCopy.FlatStyle = FlatStyle.Flat;
        _btnCopy.FlatAppearance.BorderSize = 0;
        _btnCopy.Font = new Font("Segoe UI Semibold", 9F);
        _btnCopy.Cursor = Cursors.Hand;
        _btnCopy.Click += (s, e) => CopyImageToClipboard();
        pnlFooterLeft.Controls.Add(_btnCopy);

        if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
        {
            _btnOpenFolder.Text = " Mở thư mục";
            _btnOpenFolder.IconChar = IconChar.FolderOpen;
            _btnOpenFolder.IconColor = Color.White;
            _btnOpenFolder.IconSize = 15;
            _btnOpenFolder.TextImageRelation = TextImageRelation.ImageBeforeText;
            _btnOpenFolder.ImageAlign = ContentAlignment.MiddleLeft;
            _btnOpenFolder.TextAlign = ContentAlignment.MiddleLeft;
            _btnOpenFolder.Padding = new Padding(10, 0, 10, 0);
            _btnOpenFolder.Margin = new Padding(0, 0, 8, 0);
            _btnOpenFolder.Height = 36;
            _btnOpenFolder.AutoSize = true;
            _btnOpenFolder.BackColor = Color.FromArgb(50, 55, 68);
            _btnOpenFolder.ForeColor = Color.White;
            _btnOpenFolder.FlatStyle = FlatStyle.Flat;
            _btnOpenFolder.FlatAppearance.BorderSize = 0;
            _btnOpenFolder.Font = new Font("Segoe UI Semibold", 9F);
            _btnOpenFolder.Cursor = Cursors.Hand;
            _btnOpenFolder.Click += (s, e) => OpenContainingFolder();
            pnlFooterLeft.Controls.Add(_btnOpenFolder);
        }

        // Nút Đóng căn sát lề phải
        _btnClose.Text = " Đóng";
        _btnClose.IconChar = IconChar.Xmark;
        _btnClose.IconColor = Color.White;
        _btnClose.IconSize = 15;
        _btnClose.TextImageRelation = TextImageRelation.ImageBeforeText;
        _btnClose.ImageAlign = ContentAlignment.MiddleLeft;
        _btnClose.TextAlign = ContentAlignment.MiddleCenter;
        _btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnClose.Location = new Point(pnlFooter.Width - 105, 8);
        _btnClose.Size = new Size(95, 36);
        _btnClose.BackColor = Color.FromArgb(60, 65, 80);
        _btnClose.ForeColor = Color.White;
        _btnClose.FlatStyle = FlatStyle.Flat;
        _btnClose.FlatAppearance.BorderSize = 0;
        _btnClose.Font = new Font("Segoe UI Semibold", 9F);
        _btnClose.Cursor = Cursors.Hand;
        _btnClose.Click += (s, e) => Close();
        pnlFooter.Controls.Add(_btnClose);

        Controls.Add(pnlFooter);

        // Body Canvas
        _canvas.Dock = DockStyle.Fill;
        _canvas.Image = _image;
        Controls.Add(_canvas);
        _canvas.BringToFront();

        // Khung ảo phủ hết ảnh khi mở lên
        if (_image != null)
        {
            _canvas.SetCropRect(new Rectangle(0, 0, _image.Width, _image.Height));
        }

        _canvas.CropRectChanged += (r) =>
        {
            if (_image != null)
            {
                _lblInfo.Text = $"{_image.Width} × {_image.Height} px | Khung chọn: {r.Width} × {r.Height} px (tại X={r.X}, Y={r.Y})";
            }
        };

        Shown += (s, e) =>
        {
            _canvas.FitImageToView();
            if (_image != null)
            {
                _canvas.SetCropRect(new Rectangle(0, 0, _image.Width, _image.Height));
            }
        };

        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            if (e.Control && e.KeyCode == Keys.C) CopyImageToClipboard();
            if (e.Control && e.KeyCode == Keys.S) SaveCropChanges();
        };
        KeyPreview = true;
    }

    private void UpdateInfoLabel()
    {
        string resText = _image != null ? $"{_image.Width} × {_image.Height} px" : "0 × 0 px";
        string coordText = _item != null ? $" | Vị trí gốc: X={_item.X}, Y={_item.Y}" : "";
        string typeText = _item != null ? $" | Loại: {_item.TypeDisplay}" : "";
        string fileText = !string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath)
            ? $" | Dung lượng: {new FileInfo(_imagePath).Length / 1024.0:F1} KB"
            : "";

        _lblInfo.Text = $"{resText}{coordText}{typeText}{fileText}";
        _lblInfo.Location = new Point(_lblTitle.Right + 20, 16);
    }

    private void SaveCropChanges()
    {
        if (_image == null) return;

        Rectangle r = _canvas.CropRect;
        if (r.Width <= 0 || r.Height <= 0)
        {
            MessageBox.Show("Vùng cắt không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        r.Intersect(new Rectangle(0, 0, _image.Width, _image.Height));
        if (r.Width <= 0 || r.Height <= 0) return;

        if (_item != null)
        {
            _item.X += r.X;
            _item.Y += r.Y;
            _item.Width = r.Width;
            _item.Height = r.Height;
        }

        try
        {
            Bitmap newCropped = _image.Clone(r, PixelFormat.Format32bppArgb);
            _image.Dispose();
            _image = newCropped;

            _canvas.Image = _image;
            _canvas.SetCropRect(new Rectangle(0, 0, _image.Width, _image.Height));
            _canvas.FitImageToView();

            HasSavedChanges = true;
            UpdateInfoLabel();

            MessageBox.Show($"Đã cập nhật khung cắt ({r.Width} × {r.Height} px)!", "Lưu thay đổi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật ảnh: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyImageToClipboard()
    {
        if (_image != null)
        {
            try
            {
                Clipboard.SetImage(_image);
                MessageBox.Show("Đã copy ảnh vào Clipboard!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi copy: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OpenContainingFolder()
    {
        if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{_imagePath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
