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

namespace Win04_Cropper;

public partial class MainCropperForm : Form
{
    private const int HOTKEY_ID_F9 = 9001;

    private static readonly Bitmap _editIconBmp = FormsIconHelper.ToBitmap(IconChar.PenToSquare, Color.FromArgb(0, 200, 240), 13);
    private static readonly Bitmap _deleteIconBmp = FormsIconHelper.ToBitmap(IconChar.TrashCan, Color.FromArgb(255, 120, 130), 13);
    private static readonly Bitmap _coordIconBmp = FormsIconHelper.ToBitmap(IconChar.LocationDot, Color.FromArgb(0, 215, 255), 13);
    private static readonly Bitmap _imageIconBmp = FormsIconHelper.ToBitmap(IconChar.Image, Color.FromArgb(50, 220, 150), 13);

    private readonly List<CropRegionItem> _savedRegions = new();
    private bool _isUpdatingInputs;
    private CropRegionItem? _currentlyEditingItem;

    internal DataGridView SavedGrid => dgvSavedRegions;

    public MainCropperForm()
    {
        InitializeComponent();

        // Canvas events
        canvas.CropRectChanged += OnCanvasCropRectChanged;
        canvas.CursorMovedOnImage += OnCanvasCursorMoved;
        canvas.ZoomChanged += OnCanvasZoomChanged;

        // Default splitter ratios
        Shown += (s, e) =>
        {
            try
            {
                if (splitMain.Height > 100)
                {
                    splitMain.SplitterDistance = Math.Clamp((int)(splitMain.Height * 0.65), 50, splitMain.Height - 50);
                }
                if (splitTop.Width > 100)
                {
                    splitTop.SplitterDistance = Math.Clamp((int)(splitTop.Width * 0.72), 50, splitTop.Width - 50);
                }
            }
            catch { }
        };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (File.Exists("app_icon.ico"))
        {
            try { this.Icon = new System.Drawing.Icon("app_icon.ico"); } catch { }
        }

        // Register Global Hotkey F9
        try
        {
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID_F9, NativeMethods.MOD_NOREPEAT, (uint)Keys.F9);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RegisterHotKey failed: {ex.Message}");
        }

        // Load saved regions from JSON
        LoadSavedRegions();

        // Load sample image if present
        string samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample_images", "sample_aoe.jpg");
        if (File.Exists(samplePath))
        {
            LoadImageFromPath(samplePath);
        }
        else
        {
            // Try parent sample_images
            string parentSample = Path.Combine(Directory.GetCurrentDirectory(), "sample_images", "sample_aoe.jpg");
            if (File.Exists(parentSample))
            {
                LoadImageFromPath(parentSample);
            }
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

        // Save regions to file
        ConfigStorageService.SaveRegions(_savedRegions);
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

        // Arrow keys nudge when not focusing inputs
        if (!numX.Focused && !numY.Focused && !numW.Focused && !numH.Focused && !dgvSavedRegions.Focused)
        {
            int step = GetNudgeStep();
            if ((keyData & Keys.Shift) == Keys.Shift) step *= 5;

            Keys key = keyData & Keys.KeyCode;
            switch (key)
            {
                case Keys.Left: NudgeCrop(-step, 0); return true;
                case Keys.Right: NudgeCrop(step, 0); return true;
                case Keys.Up: NudgeCrop(0, -step); return true;
                case Keys.Down: NudgeCrop(0, step); return true;
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
            Title = "Chọn ảnh màn hình hoặc hình ảnh cần cắt",
            Filter = "Ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif|Tất cả tệp (*.*)|*.*"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            LoadImageFromPath(ofd.FileName);
        }
    }

    private void LoadImageFromPath(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var srcBmp = new Bitmap(stream);
            // Clone into 32bppArgb to release file lock and ensure fast rendering
            Bitmap cloned = new(srcBmp.Width, srcBmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(cloned))
            {
                g.DrawImage(srcBmp, 0, 0, srcBmp.Width, srcBmp.Height);
            }

            SetSourceImage(cloned, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể mở ảnh: {ex.Message}", "Lỗi nạp ảnh", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    SetSourceImage(bmp, $"Clipboard_{DateTime.Now:HHmmss}");
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
        btnLiveCapture.Text = "⏳ Đang chụp...";
        btnLiveCapture.Enabled = false;

        try
        {
            // Capture all virtual screens, hiding our app briefly
            Bitmap? captured = await ScreenCaptureService.CaptureScreenAsync(this, allScreens: true, delayMs: 160);
            if (captured != null)
            {
                SetSourceImage(captured, $"LiveCapture_{DateTime.Now:HHmmss}");
            }
        }
        finally
        {
            btnLiveCapture.Text = "📸 Chụp Live (F9)";
            btnLiveCapture.Enabled = true;
        }
    }

    private async Task TriggerWindowCaptureAsync()
    {
        btnWindowCapture.Text = "⏳ Chụp cửa sổ...";
        btnWindowCapture.Enabled = false;

        try
        {
            Bitmap? captured = await ScreenCaptureService.CaptureForegroundWindowAsync(this, delayMs: 160);
            if (captured != null)
            {
                SetSourceImage(captured, $"Window_{DateTime.Now:HHmmss}");
            }
        }
        finally
        {
            btnWindowCapture.Text = "🪟 Cửa sổ khác";
            btnWindowCapture.Enabled = true;
        }
    }

    private void SetSourceImage(Bitmap bmp, string sourceName)
    {
        canvas.Image?.Dispose();
        canvas.Image = bmp;

        lblImageInfo.Text = $"Ảnh: {sourceName} ({bmp.Width} × {bmp.Height} px)";

        // Set max limits for coordinates
        numX.Maximum = Math.Max(0, bmp.Width - 1);
        numY.Maximum = Math.Max(0, bmp.Height - 1);
        numW.Maximum = bmp.Width;
        numH.Maximum = bmp.Height;

        // Sync initial crop
        UpdateInputsFromCropRect(canvas.CropRect);
        preview.UpdateCrop(canvas.Image, canvas.CropRect);
    }

    #endregion

    #region Canvas Event Handlers & Input Sync

    private void OnCanvasCropRectChanged(Rectangle rect)
    {
        if (_isUpdatingInputs) return;

        UpdateInputsFromCropRect(rect);
        preview.UpdateCrop(canvas.Image, rect);
    }

    private void OnCanvasCursorMoved(Point imgPt, Color? pixelColor)
    {
        if (canvas.Image == null)
        {
            lblCursorInfo.Text = "Tọa độ chuột: -";
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

            lblAspectRatio.Text = $"Tỉ lệ: {GetRatioStr(rect.Width, rect.Height)}";
        }
        finally
        {
            _isUpdatingInputs = false;
        }
    }

    private void OnNumericInputChanged()
    {
        if (_isUpdatingInputs || canvas.Image == null) return;

        Rectangle newRect = new((int)numX.Value, (int)numY.Value, (int)numW.Value, (int)numH.Value);
        canvas.SetCropRect(newRect);
        preview.UpdateCrop(canvas.Image, newRect);
        lblAspectRatio.Text = $"Tỉ lệ: {GetRatioStr(newRect.Width, newRect.Height)}";
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

    private void CropAndSaveImage()
    {
        if (canvas.Image == null || preview.CroppedBitmap == null)
        {
            MessageBox.Show("Chưa có ảnh hoặc vùng cắt hợp lệ để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string defaultName = _currentlyEditingItem?.Name ?? GenerateUniqueName("Crop_");
        string? origName = _currentlyEditingItem?.Name;

        using NameInputDialog dlg = new(
            canvas.CropRect,
            defaultName: defaultName,
            existingNames: _savedRegions.Select(r => r.Name),
            originalName: origName,
            title: _currentlyEditingItem != null ? $"Lưu / Cập nhật ảnh: {origName}" : "Lưu & Cắt hình ảnh",
            headerText: _currentlyEditingItem != null ? "Cập nhật hoặc Lưu mới hình ảnh" : "Lưu ảnh cắt vào danh sách",
            headerIcon: IconChar.Crop);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string chosenName = dlg.RegionName;
        bool isSaveAsNew = dlg.IsSaveAsNew || _currentlyEditingItem == null;

        using SaveFileDialog sfd = new()
        {
            Title = "Chọn nơi lưu tệp hình ảnh đã cắt",
            Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
            FileName = $"{chosenName}.png"
        };

        if (!isSaveAsNew && !string.IsNullOrEmpty(_currentlyEditingItem?.ImagePath))
        {
            try
            {
                string? dir = Path.GetDirectoryName(_currentlyEditingItem.ImagePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    sfd.InitialDirectory = dir;
                }
                sfd.FileName = Path.GetFileName(_currentlyEditingItem.ImagePath);
            }
            catch { }
        }

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                ImageFormat format = ImageFormat.Png;
                string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                if (ext is ".jpg" or ".jpeg") format = ImageFormat.Jpeg;
                else if (ext == ".bmp") format = ImageFormat.Bmp;

                preview.CroppedBitmap.Save(sfd.FileName, format);

                if (!isSaveAsNew && _currentlyEditingItem != null)
                {
                    // Overwrite existing item
                    _currentlyEditingItem.Name = chosenName;
                    _currentlyEditingItem.X = canvas.CropRect.X;
                    _currentlyEditingItem.Y = canvas.CropRect.Y;
                    _currentlyEditingItem.Width = canvas.CropRect.Width;
                    _currentlyEditingItem.Height = canvas.CropRect.Height;
                    _currentlyEditingItem.ImagePath = sfd.FileName;
                    _currentlyEditingItem.Notes = dlg.Notes;
                    _currentlyEditingItem.ItemType = CropItemType.Image;

                    CancelEditing();
                    RefreshSavedGrid();
                    ConfigStorageService.SaveRegions(_savedRegions);

                    MessageBox.Show($"Đã lưu đè hình ảnh [{chosenName}] thành công!", "Cập nhật thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        ImagePath = sfd.FileName,
                        Notes = dlg.Notes,
                        CreatedAt = DateTime.Now
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

                    ConfigStorageService.SaveRegions(_savedRegions);

                    MessageBox.Show($"Đã lưu ảnh '{chosenName}' thành công và thêm vào danh sách!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu ảnh: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void CopyCroppedImageToClipboard()
    {
        if (preview.CroppedBitmap == null)
        {
            MessageBox.Show("Chưa có vùng cắt để copy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Clipboard.SetImage(preview.CroppedBitmap);
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
        int index = 1;
        while (_savedRegions.Any(r => string.Equals(r.Name, $"{prefix}{index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }
        return $"{prefix}{index}";
    }

    private void SaveCurrentCoordinates()
    {
        Rectangle r = canvas.CropRect;
        string defaultName = _currentlyEditingItem?.Name ?? GenerateUniqueName("Vung_");
        string? origName = _currentlyEditingItem?.Name;

        using NameInputDialog dlg = new(
            r,
            defaultName: defaultName,
            existingNames: _savedRegions.Select(r => r.Name),
            originalName: origName,
            title: _currentlyEditingItem != null ? $"Lưu / Cập nhật tọa độ: {origName}" : "Lưu tọa độ vùng cắt",
            headerText: _currentlyEditingItem != null ? "Cập nhật hoặc Lưu mới tọa độ" : "Lưu tọa độ vào danh sách",
            headerIcon: IconChar.FloppyDisk);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        bool isSaveAsNew = dlg.IsSaveAsNew || _currentlyEditingItem == null;

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

            string updatedName = _currentlyEditingItem.Name;
            CancelEditing();
            RefreshSavedGrid();
            ConfigStorageService.SaveRegions(_savedRegions);
            MessageBox.Show($"Đã lưu đè (cập nhật) thành công tọa độ cho [{updatedName}]!", "Cập nhật thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                CreatedAt = DateTime.Now
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

            ConfigStorageService.SaveRegions(_savedRegions);
            MessageBox.Show($"Đã tạo đối tượng tọa độ mới [{item.Name}] thành công!", "Lưu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        // Apply coordinates to canvas
        Rectangle rect = new(item.X, item.Y, item.Width, item.Height);
        canvas.SetCropRect(rect);
        if (canvas.Image != null)
        {
            preview.UpdateCrop(canvas.Image, rect);
        }
        UpdateInputsFromCropRect(rect);

        // Update UI status banner in controls bar
        lblCoordSection.Text = $"ĐANG SỬA: [{item.Name.ToUpper()}] ({item.TypeDisplay}) - Thay đổi tọa độ/size rồi bấm Lưu:";
        lblCoordSection.ForeColor = Color.FromArgb(255, 205, 50); // Gold
        picCoordIcon.IconChar = IconChar.PenToSquare;
        picCoordIcon.IconColor = Color.FromArgb(255, 205, 50);

        btnCancelEdit.Location = new Point(Math.Min(lblCoordSection.Right + 10, pnlControlsBar.Width - 95), 5);
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
        lblCoordSection.Text = "ĐIỀU CHỈNH TỌA ĐỘ & KÍCH THƯỚC:";
        lblCoordSection.ForeColor = Color.FromArgb(0, 215, 255);
        picCoordIcon.IconChar = IconChar.VectorSquare;
        picCoordIcon.IconColor = Color.FromArgb(0, 215, 255);
        btnCancelEdit.Visible = false;
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
            ConfigStorageService.SaveRegions(_savedRegions);
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

        RefreshSavedGrid();
        ConfigStorageService.SaveRegions(_savedRegions);
        CancelEditing();

        MessageBox.Show($"Đã cập nhật tọa độ mới cho [{target.Name}] thành công!", "Cập nhật thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            ConfigStorageService.SaveRegions(_savedRegions);
        }
    }

    private void ExportPackage()
    {
        if (_savedRegions.Count == 0)
        {
            MessageBox.Show("Chưa có mục nào trong danh sách đã lưu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using FolderBrowserDialog fbd = new()
        {
            Description = "Chọn thư mục lưu gói xuất (Tool sẽ tạo thư mục 'images' và 'Coordinates')",
            UseDescriptionForTitle = true
        };

        if (fbd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string exportRoot = Path.Combine(fbd.SelectedPath, $"Cropper_Export_{timestamp}");
            string imagesDir = Path.Combine(exportRoot, "images");
            string coordsDir = Path.Combine(exportRoot, "Coordinates");

            Directory.CreateDirectory(imagesDir);
            Directory.CreateDirectory(coordsDir);

            int exportedImages = 0;
            int exportedCoords = 0;

            // 1. Export Images to images/
            for (int i = 0; i < _savedRegions.Count; i++)
            {
                var item = _savedRegions[i];
                string safeName = SanitizeFileName(item.Name);

                // If image file exists on disk, copy it
                if (!string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath))
                {
                    string ext = Path.GetExtension(item.ImagePath);
                    if (string.IsNullOrEmpty(ext)) ext = ".png";
                    string destPath = Path.Combine(imagesDir, $"{safeName}{ext}");
                    File.Copy(item.ImagePath, destPath, true);
                    exportedImages++;
                }
                // Otherwise if we have source canvas image, crop and save the region as PNG
                else if (canvas.Image != null)
                {
                    Rectangle r = new(item.X, item.Y, item.Width, item.Height);
                    Rectangle imgRect = new(0, 0, canvas.Image.Width, canvas.Image.Height);
                    Rectangle intersect = Rectangle.Intersect(r, imgRect);

                    if (intersect.Width > 0 && intersect.Height > 0)
                    {
                        using Bitmap cropped = new(intersect.Width, intersect.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(cropped))
                        {
                            g.DrawImage(canvas.Image, new Rectangle(0, 0, cropped.Width, cropped.Height), intersect, GraphicsUnit.Pixel);
                        }
                        string destPath = Path.Combine(imagesDir, $"{safeName}.png");
                        cropped.Save(destPath, ImageFormat.Png);
                        exportedImages++;
                    }
                }
            }

            // 2. Export Coordinates to Coordinates/
            // 2a. Consolidated Coordinates.json
            string jsonPath = Path.Combine(coordsDir, "Coordinates.json");
            ConfigStorageService.SaveRegions(_savedRegions, jsonPath);
            exportedCoords++;

            // 2b. Consolidated Coordinates.csv (with UTF-8 BOM for Excel)
            string csvPath = Path.Combine(coordsDir, "Coordinates.csv");
            using (var writer = new StreamWriter(csvPath, false, new System.Text.UTF8Encoding(true)))
            {
                writer.WriteLine("STT,Phân loại,Tên vùng / ảnh,Tọa độ X,Tọa độ Y,Chiều rộng (W),Chiều cao (H),Tỉ lệ,Thời gian tạo,Ghi chú");
                for (int i = 0; i < _savedRegions.Count; i++)
                {
                    var it = _savedRegions[i];
                    string escName = EscapeCsv(it.Name);
                    string escNotes = EscapeCsv(it.Notes ?? "");
                    writer.WriteLine($"{i + 1},{it.TypeDisplay},{escName},{it.X},{it.Y},{it.Width},{it.Height},{it.AspectRatioStr},{it.CreatedAt:yyyy-MM-dd HH:mm:ss},{escNotes}");
                }
            }
            exportedCoords++;

            // 2c. Consolidated Coordinates.txt (Human-readable overview)
            string txtPath = Path.Combine(coordsDir, "Coordinates.txt");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("==========================================================================");
            sb.AppendLine(" SCREEN CROPPER PRO - TỌA ĐỘ VÀ KÍCH THƯỚC CÁC VÙNG ĐÃ LƯU");
            sb.AppendLine($" Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($" Tổng số mục: {_savedRegions.Count}");
            sb.AppendLine("==========================================================================");
            sb.AppendLine();
            for (int i = 0; i < _savedRegions.Count; i++)
            {
                var it = _savedRegions[i];
                sb.AppendLine($"[{i + 1}] {it.Name} ({it.TypeDisplay})");
                sb.AppendLine($"    • Tọa độ (X, Y)       : X = {it.X}, Y = {it.Y}");
                sb.AppendLine($"    • Kích thước (W × H)  : {it.Width} × {it.Height} px (Tỉ lệ: {it.AspectRatioStr})");
                sb.AppendLine($"    • Thời gian tạo      : {it.CreatedAt:HH:mm:ss dd/MM/yyyy}");
                if (!string.IsNullOrEmpty(it.Notes))
                {
                    sb.AppendLine($"    • Ghi chú            : {it.Notes}");
                }
                if (!string.IsNullOrEmpty(it.ImagePath))
                {
                    sb.AppendLine($"    • Đường dẫn ảnh gốc   : {it.ImagePath}");
                }
                sb.AppendLine();
            }
            File.WriteAllText(txtPath, sb.ToString(), System.Text.Encoding.UTF8);
            exportedCoords++;

            // 2d. Individual text files per item: Coordinates/{Name}.txt
            for (int i = 0; i < _savedRegions.Count; i++)
            {
                var it = _savedRegions[i];
                string singleTxtPath = Path.Combine(coordsDir, $"{SanitizeFileName(it.Name)}.txt");
                string singleContent =
                    $"Name={it.Name}\n" +
                    $"Type={it.ItemType}\n" +
                    $"X={it.X}\n" +
                    $"Y={it.Y}\n" +
                    $"Width={it.Width}\n" +
                    $"Height={it.Height}\n" +
                    $"AspectRatio={it.AspectRatioStr}\n" +
                    $"CreatedAt={it.CreatedAt:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Notes={it.Notes ?? ""}\n";
                File.WriteAllText(singleTxtPath, singleContent, System.Text.Encoding.UTF8);
                exportedCoords++;
            }

            // Summary file in root
            string summaryPath = Path.Combine(exportRoot, "Export_Summary.txt");
            string summaryContent =
                $"GÓI XUẤT SCREEN CROPPER PRO\n" +
                $"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n\n" +
                $"- images/: {exportedImages} tệp hình ảnh\n" +
                $"- Coordinates/: {exportedCoords} tệp tọa độ (bao gồm Coordinates.json, Coordinates.csv, Coordinates.txt và các file .txt riêng cho từng mục)\n";
            File.WriteAllText(summaryPath, summaryContent, System.Text.Encoding.UTF8);

            var ask = MessageBox.Show(
                $"Xuất trọn gói thành công!\n\n" +
                $"Thư mục đích:\n{exportRoot}\n\n" +
                $"• images/       : {exportedImages} tệp ảnh đã xuất\n" +
                $"• Coordinates/  : {exportedCoords} tệp tọa độ (JSON, CSV, TXT)\n\n" +
                $"Bạn có muốn mở thư mục vừa xuất trong Windows Explorer không?",
                "Xuất trọn gói thành công",
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
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất gói dữ liệu: {ex.Message}", "Lỗi xuất", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _savedRegions.AddRange(imported);
                RefreshSavedGrid();
                ConfigStorageService.SaveRegions(_savedRegions);
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

        var item = _savedRegions[e.RowIndex];

        if (item.ItemType == CropItemType.Image)
        {
            // Open full Image Viewer Dialog
            using var viewer = new ImageViewerDialog(
                item.ImagePath,
                preview.CroppedBitmap,
                item.Name,
                new Rectangle(item.X, item.Y, item.Width, item.Height));
            viewer.ShowDialog(this);
        }
        else
        {
            // Load coordinate to editor
            EditRegionItem(e.RowIndex);
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

            using SolidBrush headerBg = new(Color.FromArgb(28, 33, 46));
            using Pen borderPen = new(Color.FromArgb(46, 54, 72), 1);
            using SolidBrush textBrush = new(Color.FromArgb(0, 215, 255));
            using Font font = new("Segoe UI Semibold", 8.5F);

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

            using SolidBrush btnBg = new(Color.FromArgb(32, 42, 60));
            using Pen btnBorder = new(Color.FromArgb(0, 180, 216), 1);

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

            using SolidBrush btnBg = new(Color.FromArgb(55, 30, 36));
            using Pen btnBorder = new(Color.FromArgb(220, 70, 80), 1);

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
        // Custom badge for Type column
        else if (colName == "ColType")
        {
            e.PaintBackground(e.ClipBounds, true);
            var item = _savedRegions[e.RowIndex];

            Rectangle rect = e.CellBounds;
            rect.Inflate(-6, -4);

            bool isImage = item.ItemType == CropItemType.Image;
            Color badgeBg = isImage ? Color.FromArgb(16, 85, 60) : Color.FromArgb(12, 60, 95);
            Color badgeBorder = isImage ? Color.FromArgb(16, 185, 129) : Color.FromArgb(0, 180, 216);
            Color textCol = isImage ? Color.FromArgb(160, 255, 215) : Color.FromArgb(170, 235, 255);
            Bitmap iconBmp = isImage ? _imageIconBmp : _coordIconBmp;

            using SolidBrush bg = new(badgeBg);
            using Pen border = new(badgeBorder, 1);
            using SolidBrush textBrush = new(textCol);
            using Font font = new("Segoe UI Semibold", 8F);

            e.Graphics?.FillRectangle(bg, rect);
            e.Graphics?.DrawRectangle(border, rect);

            if (e.Graphics != null)
            {
                int iconW = iconBmp.Width;
                int iconH = iconBmp.Height;
                string text = item.TypeDisplay;
                SizeF textSize = e.Graphics.MeasureString(text, font);
                float totalW = iconW + 5 + textSize.Width;
                float startX = rect.X + (rect.Width - totalW) / 2f;
                float iconY = rect.Y + (rect.Height - iconH) / 2f;
                float textY = rect.Y + (rect.Height - textSize.Height) / 2f;

                e.Graphics.DrawImage(iconBmp, startX, iconY);
                e.Graphics.DrawString(text, font, textBrush, startX + iconW + 5, textY);
            }

            e.Handled = true;
        }
    }

    private void RefreshSavedGrid()
    {
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

        lblSavedTitle.Text = $"DANH SÁCH ĐÃ LƯU (TỌA ĐỘ & HÌNH ẢNH): {_savedRegions.Count} mục";
    }

    private void LoadSavedRegions()
    {
        var list = ConfigStorageService.LoadRegions();
        _savedRegions.Clear();
        if (list.Count > 0)
        {
            _savedRegions.AddRange(list);
        }

        RefreshSavedGrid();
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
}
