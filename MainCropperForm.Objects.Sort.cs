#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Win04_Cropper;

partial class MainCropperForm
{
    #region Object Sorting (Column Header Click & Objects Title Click)

    private void OnSavedGridColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.ColumnIndex >= dgvSavedRegions.Columns.Count) return;
        var col = dgvSavedRegions.Columns[e.ColumnIndex];
        if (col.Name is "ColEditBtn" or "ColDeleteBtn") return;

        SortSavedRegions(col.Name);
    }

    private void ToggleTitleSort()
    {
        if (_currentSortColumn != "ColName")
        {
            _currentSortColumn = "ColName";
            _currentSortOrder = SortOrder.Ascending;
        }
        else if (_currentSortOrder == SortOrder.Ascending)
        {
            _currentSortOrder = SortOrder.Descending;
        }
        else
        {
            _currentSortColumn = "ColCreated";
            _currentSortOrder = SortOrder.Ascending;
        }

        ApplySortingAndRefresh();
    }

    private void SortSavedRegions(string columnName)
    {
        if (_currentSortColumn == columnName)
        {
            _currentSortOrder = (_currentSortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            _currentSortColumn = columnName;
            _currentSortOrder = SortOrder.Ascending;
        }

        ApplySortingAndRefresh();
    }

    private void ApplySortingAndRefresh()
    {
        if (_savedRegions.Count > 1)
        {
            bool asc = _currentSortOrder == SortOrder.Ascending;

            switch (_currentSortColumn)
            {
                case "ColIndex":
                    _savedRegions.Sort((a, b) => asc ? a.CreatedAt.CompareTo(b.CreatedAt) : b.CreatedAt.CompareTo(a.CreatedAt));
                    break;
                case "ColType":
                    _savedRegions.Sort((a, b) => asc
                        ? string.Compare(a.TypeDisplay, b.TypeDisplay, StringComparison.CurrentCultureIgnoreCase)
                        : string.Compare(b.TypeDisplay, a.TypeDisplay, StringComparison.CurrentCultureIgnoreCase));
                    break;
                case "ColName":
                    _savedRegions.Sort((a, b) => asc
                        ? string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase)
                        : string.Compare(b.Name, a.Name, StringComparison.CurrentCultureIgnoreCase));
                    break;
                case "ColX":
                    _savedRegions.Sort((a, b) => asc ? a.X.CompareTo(b.X) : b.X.CompareTo(a.X));
                    break;
                case "ColY":
                    _savedRegions.Sort((a, b) => asc ? a.Y.CompareTo(b.Y) : b.Y.CompareTo(a.Y));
                    break;
                case "ColW":
                    _savedRegions.Sort((a, b) => asc ? a.Width.CompareTo(b.Width) : b.Width.CompareTo(a.Width));
                    break;
                case "ColH":
                    _savedRegions.Sort((a, b) => asc ? a.Height.CompareTo(b.Height) : b.Height.CompareTo(a.Height));
                    break;
                case "ColRatio":
                    _savedRegions.Sort((a, b) => asc
                        ? string.Compare(a.AspectRatioStr, b.AspectRatioStr, StringComparison.Ordinal)
                        : string.Compare(b.AspectRatioStr, a.AspectRatioStr, StringComparison.Ordinal));
                    break;
                case "ColCreated":
                    _savedRegions.Sort((a, b) => asc ? a.CreatedAt.CompareTo(b.CreatedAt) : b.CreatedAt.CompareTo(a.CreatedAt));
                    break;
                case "ColNotes":
                    _savedRegions.Sort((a, b) => asc
                        ? string.Compare(a.Notes ?? "", b.Notes ?? "", StringComparison.CurrentCultureIgnoreCase)
                        : string.Compare(b.Notes ?? "", a.Notes ?? "", StringComparison.CurrentCultureIgnoreCase));
                    break;
            }
        }

        UpdateSortGlyphs(_currentSortColumn);
        RefreshSavedGrid();
    }

    private void UpdateSortGlyphs(string activeColName)
    {
        foreach (DataGridViewColumn col in dgvSavedRegions.Columns)
        {
            if (col.Name is "ColEditBtn" or "ColDeleteBtn") continue;
            col.HeaderCell.SortGlyphDirection = (col.Name == activeColName) ? _currentSortOrder : SortOrder.None;
        }
        dgvSavedRegions.Invalidate();
    }

    private void ShowSortContextMenu(Control anchor)
    {
        ContextMenuStrip menu = new();
        menu.Items.Add("Tên: A → Z", null, (s, e) => { _currentSortColumn = "ColName"; _currentSortOrder = SortOrder.Ascending; ApplySortingAndRefresh(); });
        menu.Items.Add("Tên: Z → A", null, (s, e) => { _currentSortColumn = "ColName"; _currentSortOrder = SortOrder.Descending; ApplySortingAndRefresh(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Thời gian: Mới nhất trước", null, (s, e) => { _currentSortColumn = "ColCreated"; _currentSortOrder = SortOrder.Descending; ApplySortingAndRefresh(); });
        menu.Items.Add("Thời gian: Cũ nhất trước", null, (s, e) => { _currentSortColumn = "ColCreated"; _currentSortOrder = SortOrder.Ascending; ApplySortingAndRefresh(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Chiều rộng: Lớn → Nhỏ", null, (s, e) => { _currentSortColumn = "ColW"; _currentSortOrder = SortOrder.Descending; ApplySortingAndRefresh(); });
        menu.Items.Add("Chiều cao: Lớn → Nhỏ", null, (s, e) => { _currentSortColumn = "ColH"; _currentSortOrder = SortOrder.Descending; ApplySortingAndRefresh(); });
        menu.Show(anchor, new Point(0, anchor.Height));
    }

    #endregion
}
