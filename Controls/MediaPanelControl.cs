#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Models;

namespace Win04_Cropper.Controls;

public partial class MediaPanelControl : UserControl
{
    private readonly List<MediaItem> _items = new();
    private MediaItem? _activeItem;
    private readonly float _dpiScale = 1.0f;
    private int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    /// <summary>
    /// Set to true during window move/resize to skip expensive layout operations.
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public bool IsWindowResizing { get; set; }

    // UI Controls
    private Panel pnlHeader = null!;
    private IconPictureBox picIcon = null!;
    private Label lblTitle = null!;
    private IconButton btnImport = null!;
    private IconButton btnPaste = null!;
    private IconButton btnClear = null!;

    private Panel pnlBody = null!;
    private Panel pnlEmptyState = null!;
    private Panel pnlCardsContainer = null!;
    private Panel pnlCardsContent = null!;
    private SlimScrollBar pnlScrollBar = null!;
    private Panel pnlDragGhost = null!;

    public int ScrollOffset => pnlScrollBar.ScrollOffset;

    // Drag-out state
    private Point _mouseDownLocation;
    private bool _isDraggingOut;

    // Events
    public event Action<MediaItem>? MediaDoubleClicked;
    public event Action<MediaItem>? MediaSelected;
    public event Action<MediaItem>? MediaDeleted;
    public event Action? ImportRequested;
    public event Action? PasteRequested;
    public event Action? AllCleared;
    public event Action<string[]>? FilesDropped;
    public event Action<Bitmap>? BitmapDropped;

    public IReadOnlyList<MediaItem> Items => _items.AsReadOnly();
    public MediaItem? ActiveItem => _activeItem;

    public MediaPanelControl(float dpiScale = 1.0f)
    {
        _dpiScale = dpiScale > 0 ? dpiScale : 1.0f;
        this.DoubleBuffered = true;
        this.BackColor = Color.FromArgb(28, 31, 38);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 9F);
        this.AllowDrop = true;

        InitializeLayout();
        UpdateView();
    }

    private void InitializeLayout()
    {
        InitializeHeaderLayout();
        InitializeBodyLayout();
    }

    private void InitializeHeaderLayout()
    {
        // -------------------------------------------------------------
        // Header
        // -------------------------------------------------------------
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(34),
            BackColor = Color.FromArgb(42, 46, 56),
            Padding = Padding.Empty
        };
        this.Controls.Add(pnlHeader);

        picIcon = new IconPictureBox
        {
            IconChar = IconChar.PhotoFilm,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(8), DpiScale(7)),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picIcon);

        lblTitle = new Label
        {
            Text = "Media (0)",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Location = new Point(picIcon.Right + DpiScale(6), DpiScale(7)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        FlowLayoutPanel pnlHeaderButtons = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(5), DpiScale(8), DpiScale(5))
        };
        pnlHeader.Controls.Add(pnlHeaderButtons);

        int headerBtnH = DpiScale(26);

        btnImport = new IconButton
        {
            Text = " Nhập ảnh",
            UseMnemonic = false,
            IconChar = IconChar.FolderOpen,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = headerBtnH,
            AutoSize = true,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand,
            Padding = new Padding(DpiScale(8), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, DpiScale(4), 0)
        };
        btnImport.FlatAppearance.BorderSize = 0;
        btnImport.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 145, 235);
        btnImport.Click += (s, e) => ImportRequested?.Invoke();
        pnlHeaderButtons.Controls.Add(btnImport);

        btnPaste = new IconButton
        {
            Text = " Dán",
            UseMnemonic = false,
            IconChar = IconChar.Paste,
            IconColor = Color.FromArgb(220, 230, 245),
            IconSize = DpiScale(13),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = headerBtnH,
            AutoSize = true,
            BackColor = Color.FromArgb(48, 54, 66),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand,
            Padding = new Padding(DpiScale(8), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, DpiScale(4), 0)
        };
        btnPaste.FlatAppearance.BorderSize = 0;
        btnPaste.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 72, 88);
        btnPaste.Click += (s, e) => PasteRequested?.Invoke();
        pnlHeaderButtons.Controls.Add(btnPaste);

        btnClear = new IconButton
        {
            IconChar = IconChar.TrashCan,
            IconColor = Color.FromArgb(255, 110, 110),
            IconSize = DpiScale(13),
            Height = headerBtnH,
            Width = DpiScale(28),
            AutoSize = false,
            BackColor = Color.FromArgb(48, 54, 66),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, DpiScale(2), 0),
            Padding = Padding.Empty,
            Visible = false
        };
        btnClear.FlatAppearance.BorderSize = 0;
        btnClear.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 36, 44);
        btnClear.Click += (s, e) =>
        {
            if (_items.Count > 0 && MessageBox.Show("Bạn có chắc muốn xóa tất cả ảnh khỏi Media?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ClearItems();
                AllCleared?.Invoke();
            }
        };
        pnlHeaderButtons.Controls.Add(btnClear);
    }

    private void SetupDragDrop(Control c)
    {
        c.AllowDrop = true;
        c.DragEnter += (s, e) =>
        {
            if (e.Data != null && (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Bitmap)))
            {
                e.Effect = DragDropEffects.Copy;
                ShowDragGhost(true);
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        };
        c.DragOver += (s, e) =>
        {
            if (e.Data != null && (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Bitmap)))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        };
        c.DragLeave += (s, e) =>
        {
            Point pt = this.PointToClient(Cursor.Position);
            if (!this.ClientRectangle.Contains(pt))
            {
                ShowDragGhost(false);
            }
        };
        c.DragDrop += (s, e) =>
        {
            ShowDragGhost(false);
            if (e.Data == null) return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                FilesDropped?.Invoke(files);
            }
            else if (e.Data.GetDataPresent(DataFormats.Bitmap) && e.Data.GetData(DataFormats.Bitmap) is Bitmap bmp)
            {
                BitmapDropped?.Invoke(bmp);
            }
        };
    }

    private void ShowDragGhost(bool show)
    {
        if (pnlDragGhost != null && pnlDragGhost.Visible != show)
        {
            pnlDragGhost.Visible = show;
            if (show)
            {
                pnlDragGhost.BringToFront();
                pnlDragGhost.Invalidate();
            }
        }
    }

    public void SetItems(IEnumerable<MediaItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        UpdateView();
    }

    public void AddItem(MediaItem item)
    {
        _items.Insert(0, item);
        UpdateView();
    }

    public void RemoveItem(MediaItem item)
    {
        _items.Remove(item);
        if (_activeItem == item)
        {
            _activeItem = null;
        }
        UpdateView();
        MediaDeleted?.Invoke(item);
        try { item.Bitmap?.Dispose(); } catch { }
        item.Bitmap = null;
    }

    public void ClearItems()
    {
        foreach (var item in _items)
        {
            try { item.Bitmap?.Dispose(); } catch { }
            item.Bitmap = null;
        }
        _items.Clear();
        _activeItem = null;
        pnlScrollBar?.ResetScroll();
        UpdateView();
    }

    public void SetActiveItem(MediaItem? item)
    {
        _activeItem = item;
        HighlightActiveCard();
    }

    public void SetActiveItemByName(string? name)
    {
        _activeItem = !string.IsNullOrEmpty(name)
            ? _items.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))
            : null;
        HighlightActiveCard();
    }

    public void UpdateView()
    {
        lblTitle.Text = $"Media ({_items.Count})";
        btnClear.Visible = _items.Count > 0;

        if (_items.Count == 0)
        {
            pnlEmptyState.Visible = true;
            pnlCardsContainer.Visible = false;
            pnlScrollBar.Visible = false;
        }
        else
        {
            pnlEmptyState.Visible = false;
            pnlCardsContainer.Visible = true;
            LayoutCards();
        }
    }
}
