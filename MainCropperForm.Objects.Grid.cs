#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Win04_Cropper.Controls;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper;

partial class MainCropperForm
{
    #region Grid Interaction & Custom Painting

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
                if (col.HeaderCell.SortGlyphDirection == SortOrder.Ascending)
                {
                    text += " ▲";
                }
                else if (col.HeaderCell.SortGlyphDirection == SortOrder.Descending)
                {
                    text += " ▼";
                }
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

    #endregion
}
