#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Controls;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper;

partial class MainCropperForm
{
    #region Saved Regions Management

    private string GenerateUniqueName(string prefix)
    {
        int maxIndex = 0;
        foreach (var r in _savedRegions)
        {
            if (r.Name != null && r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string suffix = r.Name.Substring(prefix.Length).Trim();
                if (int.TryParse(suffix, out int n) && n > maxIndex)
                {
                    maxIndex = n;
                }
            }
        }
        int nextIndex = maxIndex + 1;
        while (_savedRegions.Any(r => string.Equals(r.Name, $"{prefix}{nextIndex}", StringComparison.OrdinalIgnoreCase)))
        {
            nextIndex++;
        }
        return $"{prefix}{nextIndex}";
    }

    private void SaveCurrentCoordinates()
    {
        Rectangle r = canvas.CropRect;
        string defaultName = _currentlyEditingItem?.Name ?? GenerateUniqueName("Area ");
        string? origName = _currentlyEditingItem?.Name;

        using NameInputDialog dlg = new(
            r,
            defaultName: defaultName,
            existingNames: _savedRegions.Select(r => r.Name),
            originalName: origName,
            title: _currentlyEditingItem != null ? $"Lưu / Cập nhật tọa độ: {origName}" : "Lưu đối tượng tọa độ",
            headerText: _currentlyEditingItem != null ? "Cập nhật hoặc Lưu mới tọa độ" : "Lưu đối tượng tọa độ vào dự án",
            headerIcon: IconChar.FloppyDisk);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        bool isSaveAsNew = dlg.IsSaveAsNew || _currentlyEditingItem == null;

        Bitmap? sourceBmp = canvas.Image != null ? new Bitmap(canvas.Image) : null;
        string? sourceB64 = canvas.Image != null ? ProjectService.ImageToBase64(canvas.Image) : null;
        if (_currentProject == null)
        {
            _currentProject = new ProjectData
            {
                Name = GenerateDefaultProjectName(),
                IsCustomNamed = false
            };
            UpdateAppTitle();
        }
        string sourceName = !string.IsNullOrEmpty(_currentSourceName) ? _currentSourceName : _currentProject.Name;

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
            _currentlyEditingItem.SourceBitmap = sourceBmp;
            _currentlyEditingItem.SourceImageBase64 = sourceB64;
            _currentlyEditingItem.SourceImageName = sourceName;

            CancelEditing();
            RefreshSavedGrid();
            _isProjectDirty = true;
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
                CreatedAt = DateTime.Now,
                SourceBitmap = sourceBmp,
                SourceImageBase64 = sourceB64,
                SourceImageName = sourceName
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
            _isProjectDirty = true;
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

        // Restore source image if saved with this object
        if (item.SourceBitmap != null || !string.IsNullOrEmpty(item.SourceImageBase64))
        {
            Bitmap? bmpToUse = item.SourceBitmap != null ? new Bitmap(item.SourceBitmap) : ProjectService.BitmapFromBase64(item.SourceImageBase64);
            if (bmpToUse != null)
            {
                SetSourceImage(bmpToUse, item.SourceImageName ?? item.Name);
            }
        }

        // Apply coordinates to canvas
        Rectangle rect = new(item.X, item.Y, item.Width, item.Height);
        canvas.SetCropRect(rect);
        UpdateInputsFromCropRect(rect);
        canvas.LockedAspectRatio = null;
        SetActiveRatioButton(btnRatioFree);

        // Update UI status banner in properties panel
        lblCoordSection.Text = $"ĐANG SỬA: [{item.Name.ToUpper()}]";
        lblCoordSection.ForeColor = Color.FromArgb(255, 205, 50); // Gold
        picCoordIcon.IconChar = IconChar.PenToSquare;
        picCoordIcon.IconColor = Color.FromArgb(255, 205, 50);

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
        lblCoordSection.Text = "Properties";
        lblCoordSection.ForeColor = Color.White;
        picCoordIcon.IconChar = IconChar.Sliders;
        picCoordIcon.IconColor = Color.White;
        btnCancelEdit.Visible = false;
        canvas.LockedAspectRatio = null;
        SetActiveRatioButton(btnRatioFree);
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
        if (canvas.Image != null)
        {
            target.SourceBitmap = new Bitmap(canvas.Image);
            target.SourceImageBase64 = ProjectService.ImageToBase64(canvas.Image);
            target.SourceImageName = !string.IsNullOrEmpty(_currentSourceName) ? _currentSourceName : target.SourceImageName;
        }

        RefreshSavedGrid();
        CancelEditing();
        _isProjectDirty = true;
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
        }
    }

    private void RefreshSavedGrid()
    {
        _isProjectDirty = true;
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

        lblSavedTitle.Text = $"Objects ({_savedRegions.Count})";
    }

    private void LoadSavedRegions()
    {
        var list = ConfigStorageService.LoadRegions();
        _savedRegions.Clear();
        if (list.Count > 0)
        {
            foreach (var r in list)
            {
                if (!string.IsNullOrEmpty(r.SourceImageBase64) && r.SourceBitmap == null)
                {
                    r.SourceBitmap = ProjectService.BitmapFromBase64(r.SourceImageBase64);
                }
            }
            _savedRegions.AddRange(list);
        }

        RefreshSavedGrid();
        _isProjectDirty = false;
    }

    #endregion
}
