using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper.Controls;

public class ImageViewerDialog : Form
{
    private readonly Bitmap? _image;
    private readonly string? _imagePath;
    private readonly CanvasControl _canvas = new();
    private readonly Label _lblInfo = new();
    private readonly IconButton _btnCopy = new();
    private readonly IconButton _btnOpenFolder = new();
    private readonly IconButton _btnClose = new();

    public ImageViewerDialog(string? imagePath, Bitmap? fallbackBitmap, string imageName, Rectangle originalCoords)
    {
        _imagePath = imagePath;

        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                using var tempBmp = new Bitmap(stream);
                _image = new Bitmap(tempBmp.Width, tempBmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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
        Text = $"Xem ảnh đã cắt: {imageName}";
        Size = new Size(960, 680);
        MinimumSize = new Size(600, 450);
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
            IconChar = IconChar.Image,
            IconColor = Color.White,
            IconSize = 22,
            Size = new Size(24, 24),
            Location = new Point(14, 14),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picHeader);

        Label lblTitle = new()
        {
            Text = imageName,
            Font = new Font("Segoe UI Bold", 11.5F),
            ForeColor = Color.White,
            Location = new Point(42, 13),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        string resText = _image != null ? $"{_image.Width} × {_image.Height} px" : "0 × 0 px";
        string coordText = originalCoords.Width > 0 ? $" | Tọa độ gốc: X={originalCoords.X}, Y={originalCoords.Y}" : "";
        string fileText = !string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath)
            ? $" | Dung lượng: {new FileInfo(_imagePath).Length / 1024.0:F1} KB"
            : "";

        _lblInfo.Text = $"{resText}{coordText}{fileText}";
        _lblInfo.ForeColor = Color.FromArgb(170, 185, 205);
        _lblInfo.Location = new Point(lblTitle.Right + 20, 16);
        _lblInfo.AutoSize = true;
        pnlHeader.Controls.Add(_lblInfo);

        Controls.Add(pnlHeader);

        // Footer Actions
        Panel pnlFooter = new()
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = Color.FromArgb(26, 29, 38),
            Padding = new Padding(12, 8, 12, 8)
        };

        _btnCopy.Text = " Sao chép ảnh (Ctrl+C)";
        _btnCopy.IconChar = IconChar.Copy;
        _btnCopy.IconColor = Color.White;
        _btnCopy.IconSize = 15;
        _btnCopy.TextImageRelation = TextImageRelation.ImageBeforeText;
        _btnCopy.ImageAlign = ContentAlignment.MiddleLeft;
        _btnCopy.TextAlign = ContentAlignment.MiddleLeft;
        _btnCopy.Padding = new Padding(8, 0, 0, 0);
        _btnCopy.Location = new Point(14, 8);
        _btnCopy.Size = new Size(185, 34);
        _btnCopy.BackColor = Color.FromArgb(45, 52, 68);
        _btnCopy.ForeColor = Color.White;
        _btnCopy.FlatStyle = FlatStyle.Flat;
        _btnCopy.FlatAppearance.BorderSize = 0;
        _btnCopy.Cursor = Cursors.Hand;
        _btnCopy.Click += (s, e) => CopyImageToClipboard();
        pnlFooter.Controls.Add(_btnCopy);

        if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
        {
            _btnOpenFolder.Text = " Mở thư mục chứa ảnh";
            _btnOpenFolder.IconChar = IconChar.FolderOpen;
            _btnOpenFolder.IconColor = Color.White;
            _btnOpenFolder.IconSize = 15;
            _btnOpenFolder.TextImageRelation = TextImageRelation.ImageBeforeText;
            _btnOpenFolder.ImageAlign = ContentAlignment.MiddleLeft;
            _btnOpenFolder.TextAlign = ContentAlignment.MiddleLeft;
            _btnOpenFolder.Padding = new Padding(8, 0, 0, 0);
            _btnOpenFolder.Location = new Point(208, 8);
            _btnOpenFolder.Size = new Size(185, 34);
            _btnOpenFolder.BackColor = Color.FromArgb(45, 52, 68);
            _btnOpenFolder.ForeColor = Color.White;
            _btnOpenFolder.FlatStyle = FlatStyle.Flat;
            _btnOpenFolder.FlatAppearance.BorderSize = 0;
            _btnOpenFolder.Cursor = Cursors.Hand;
            _btnOpenFolder.Click += (s, e) => OpenContainingFolder();
            pnlFooter.Controls.Add(_btnOpenFolder);
        }

        _btnClose.Text = " Đóng";
        _btnClose.IconChar = IconChar.Xmark;
        _btnClose.IconColor = Color.White;
        _btnClose.IconSize = 15;
        _btnClose.TextImageRelation = TextImageRelation.ImageBeforeText;
        _btnClose.ImageAlign = ContentAlignment.MiddleLeft;
        _btnClose.TextAlign = ContentAlignment.MiddleCenter;
        _btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnClose.Location = new Point(pnlFooter.Width - 110, 8);
        _btnClose.Size = new Size(95, 34);
        _btnClose.BackColor = Color.FromArgb(60, 65, 80);
        _btnClose.ForeColor = Color.White;
        _btnClose.FlatStyle = FlatStyle.Flat;
        _btnClose.FlatAppearance.BorderSize = 0;
        _btnClose.Cursor = Cursors.Hand;
        _btnClose.Click += (s, e) => Close();
        pnlFooter.Controls.Add(_btnClose);

        Controls.Add(pnlFooter);

        // Body Canvas
        _canvas.Dock = DockStyle.Fill;
        _canvas.Image = _image;
        Controls.Add(_canvas);

        _canvas.BringToFront();

        Shown += (s, e) =>
        {
            _canvas.FitImageToView();
        };

        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            if (e.Control && e.KeyCode == Keys.C) CopyImageToClipboard();
        };
        KeyPreview = true;
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
