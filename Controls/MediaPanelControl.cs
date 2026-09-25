using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper.Controls;

public class MediaPanelControl : UserControl
{
    private readonly List<MediaItem> _items = new();
    private MediaItem? _activeItem;
    private readonly float _dpiScale = 1.0f;
    private int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

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
    private Panel pnlScrollBar = null!;
    private Panel pnlDragGhost = null!;

    // Custom scroll state
    private int _scrollOffset;
    private int _maxScroll;
    private bool _isDraggingScrollThumb;
    private int _dragStartY;
    private int _dragStartScrollOffset;
    private bool _isThumbHovered;

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

        // -------------------------------------------------------------
        // Body: Empty State vs Cards Container vs Drag Ghost
        // -------------------------------------------------------------
        pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(24, 27, 34),
            Padding = Padding.Empty
        };
        this.Controls.Add(pnlBody);
        pnlBody.BringToFront();

        // Custom ScrollBar (6px width, modern dark look)
        pnlScrollBar = new Panel
        {
            Dock = DockStyle.Right,
            Width = DpiScale(6),
            BackColor = Color.FromArgb(20, 23, 30),
            Visible = false
        };
        pnlScrollBar.Paint += OnScrollBarPaint;
        pnlScrollBar.MouseDown += OnScrollBarMouseDown;
        pnlScrollBar.MouseMove += OnScrollBarMouseMove;
        pnlScrollBar.MouseUp += OnScrollBarMouseUp;
        pnlScrollBar.MouseLeave += (s, e) =>
        {
            if (!_isDraggingScrollThumb)
            {
                _isThumbHovered = false;
                pnlScrollBar.Invalidate();
            }
        };
        pnlScrollBar.MouseWheel += OnCardsMouseWheel;

        // Cards container (2-column layout, NO native scrollbars)
        pnlCardsContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Visible = false
        };
        pnlCardsContainer.Resize += (s, e) => LayoutCards();
        pnlCardsContainer.MouseWheel += OnCardsMouseWheel;

        pnlCardsContent = new Panel
        {
            Location = new Point(0, 0),
            BackColor = Color.Transparent
        };
        pnlCardsContent.MouseWheel += OnCardsMouseWheel;
        pnlCardsContainer.Controls.Add(pnlCardsContent);

        pnlBody.Controls.Add(pnlCardsContainer);
        pnlBody.Controls.Add(pnlScrollBar);

        // Empty state container
        pnlEmptyState = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(DpiScale(16))
        };
        pnlBody.Controls.Add(pnlEmptyState);

        // Drag Ghost Overlay
        pnlDragGhost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(210, 16, 26, 40),
            Visible = false
        };
        pnlDragGhost.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int m = DpiScale(12);
            using Pen dashPen = new(Color.FromArgb(0, 220, 255), 2.5f) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(dashPen, m, m, pnlDragGhost.Width - m * 2, pnlDragGhost.Height - m * 2);

            using Font titleFont = new("Segoe UI Semibold", 11F);
            using Font subFont = new("Segoe UI", 8.5F);
            using SolidBrush titleBrush = new(Color.White);
            using SolidBrush subBrush = new(Color.FromArgb(170, 195, 225));

            string mainText = "Thả ảnh để thêm vào Media";
            string subText = "Hỗ trợ PNG, JPG, BMP, WEBP...";
            SizeF mainSz = g.MeasureString(mainText, titleFont);
            SizeF subSz = g.MeasureString(subText, subFont);

            float totalH = mainSz.Height + DpiScale(6) + subSz.Height;
            float startY = (pnlDragGhost.Height - totalH) / 2f;

            g.DrawString(mainText, titleFont, titleBrush, (pnlDragGhost.Width - mainSz.Width) / 2f, startY);
            g.DrawString(subText, subFont, subBrush, (pnlDragGhost.Width - subSz.Width) / 2f, startY + mainSz.Height + DpiScale(6));
        };
        pnlBody.Controls.Add(pnlDragGhost);

        BuildEmptyStateUI();

        // Drag & drop support
        SetupDragDrop(this);
        SetupDragDrop(pnlBody);
        SetupDragDrop(pnlCardsContainer);
        SetupDragDrop(pnlEmptyState);
        SetupDragDrop(pnlDragGhost);
    }

    private void BuildEmptyStateUI()
    {
        pnlEmptyState.Controls.Clear();

        IconPictureBox picEmpty = new()
        {
            IconChar = IconChar.CloudArrowUp,
            IconColor = Color.FromArgb(95, 108, 130),
            IconSize = DpiScale(48),
            Size = new Size(DpiScale(56), DpiScale(48)),
            BackColor = Color.Transparent
        };

        Label lblEmptyTitle = new()
        {
            Text = "Chưa có ảnh trong Media",
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = Color.FromArgb(205, 215, 230),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        Label lblEmptyDesc = new()
        {
            Text = "Kéo & thả ảnh từ máy tính vào đây,\ndán ảnh (Ctrl+V) hoặc bấm nút bên dưới.",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(130, 140, 160),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        IconButton btnAddBig = new()
        {
            Text = " Nhập ảnh",
            UseMnemonic = false,
            IconChar = IconChar.FolderOpen,
            IconColor = Color.White,
            IconSize = DpiScale(15),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(DpiScale(130), DpiScale(34)),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Padding = new Padding(DpiScale(10), 0, DpiScale(10), 0)
        };
        btnAddBig.FlatAppearance.BorderSize = 0;
        btnAddBig.Click += (s, e) => ImportRequested?.Invoke();

        pnlEmptyState.Controls.Add(picEmpty);
        pnlEmptyState.Controls.Add(lblEmptyTitle);
        pnlEmptyState.Controls.Add(lblEmptyDesc);
        pnlEmptyState.Controls.Add(btnAddBig);

        void CenterEmptyState()
        {
            int w = pnlEmptyState.ClientSize.Width;
            int h = pnlEmptyState.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            int totalH = picEmpty.Height + DpiScale(10) + DpiScale(26) + DpiScale(6) + DpiScale(42) + DpiScale(16) + btnAddBig.Height;
            int startY = Math.Max(DpiScale(16), (h - totalH) / 2);

            picEmpty.Location = new Point((w - picEmpty.Width) / 2, startY);

            lblEmptyTitle.Location = new Point(DpiScale(10), picEmpty.Bottom + DpiScale(10));
            lblEmptyTitle.Size = new Size(Math.Max(100, w - DpiScale(20)), DpiScale(26));

            lblEmptyDesc.Location = new Point(DpiScale(10), lblEmptyTitle.Bottom + DpiScale(6));
            lblEmptyDesc.Size = new Size(Math.Max(100, w - DpiScale(20)), DpiScale(42));

            btnAddBig.Location = new Point((w - btnAddBig.Width) / 2, lblEmptyDesc.Bottom + DpiScale(16));
        }

        pnlEmptyState.Resize += (s, e) => CenterEmptyState();
        pnlEmptyState.Layout += (s, e) => CenterEmptyState();
        CenterEmptyState();
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
        _scrollOffset = 0;
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

    private void LayoutCards()
    {
        if (_items.Count == 0 || pnlCardsContainer.ClientSize.Width <= 0) return;

        pnlCardsContent.SuspendLayout();
        pnlCardsContent.Controls.Clear();

        int pad = DpiScale(6);
        int gap = DpiScale(6);
        int containerW = pnlCardsContainer.ClientSize.Width;

        // Two-column calculation: guarantee 2 columns
        int cardW = Math.Max(DpiScale(60), (containerW - pad * 2 - gap) / 2);
        int cardH = (int)(cardW * 0.82f) + DpiScale(32);

        int totalRows = (_items.Count + 1) / 2;
        int totalContentH = pad + totalRows * (cardH + gap) + pad;
        int viewH = pnlCardsContainer.ClientSize.Height;

        _maxScroll = Math.Max(0, totalContentH - viewH);
        bool needsScroll = _maxScroll > 0;

        if (pnlScrollBar.Visible != needsScroll)
        {
            pnlScrollBar.Visible = needsScroll;
            containerW = pnlCardsContainer.ClientSize.Width;
            cardW = Math.Max(DpiScale(60), (containerW - pad * 2 - gap) / 2);
            cardH = (int)(cardW * 0.82f) + DpiScale(32);
            totalContentH = pad + totalRows * (cardH + gap) + pad;
            _maxScroll = Math.Max(0, totalContentH - viewH);
        }

        pnlCardsContent.Size = new Size(containerW, Math.Max(viewH, totalContentH));
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
        pnlCardsContent.Top = -_scrollOffset;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            int col = i % 2;
            int row = i / 2;
            int x = pad + col * (cardW + gap);
            int y = pad + row * (cardH + gap);

            Control card = CreateCardControl(item, cardW, cardH);
            card.Location = new Point(x, y);
            pnlCardsContent.Controls.Add(card);
        }

        pnlCardsContent.ResumeLayout(true);
        HighlightActiveCard();
        pnlScrollBar.Invalidate();
    }

    private Control CreateCardControl(MediaItem item, int width, int height)
    {
        Panel card = new()
        {
            Size = new Size(width, height),
            BackColor = Color.FromArgb(32, 36, 46),
            Padding = Padding.Empty,
            Tag = item,
            Cursor = Cursors.Hand
        };

        // Thumbnail PictureBox with CenterCrop Cover scaling
        PictureBox pic = new()
        {
            Dock = DockStyle.Top,
            Size = new Size(width, height - DpiScale(32)),
            Height = height - DpiScale(32),
            BackColor = Color.FromArgb(20, 23, 30)
        };

        pic.Paint += (s, e) =>
        {
            var g = e.Graphics;
            Image? imgToDraw = item.Bitmap;
            if (imgToDraw == null && !string.IsNullOrEmpty(item.ThumbnailBase64))
            {
                try { item.Bitmap = ProjectService.BitmapFromBase64(item.ThumbnailBase64); imgToDraw = item.Bitmap; } catch { }
            }
            else if (imgToDraw == null && !string.IsNullOrEmpty(item.ImageBase64))
            {
                try { item.Bitmap = ProjectService.BitmapFromBase64(item.ImageBase64); imgToDraw = item.Bitmap; } catch { }
            }

            if (imgToDraw != null && imgToDraw.Width > 0 && imgToDraw.Height > 0)
            {
                DrawImageCover(g, imgToDraw, pic.ClientRectangle);
            }
            else
            {
                g.Clear(Color.FromArgb(20, 23, 30));
            }
        };

        // Top-right Delete Button on card
        IconButton btnDel = new()
        {
            IconChar = IconChar.TrashCan,
            IconColor = Color.FromArgb(255, 140, 140),
            IconSize = DpiScale(11),
            Size = new Size(DpiScale(22), DpiScale(22)),
            BackColor = Color.FromArgb(180, 40, 48, 56),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(width - DpiScale(25), DpiScale(3)),
            Cursor = Cursors.Hand
        };
        btnDel.FlatAppearance.BorderSize = 0;
        btnDel.Click += (s, e) =>
        {
            if (MessageBox.Show($"Xóa ảnh '{item.Name}' khỏi Media?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                RemoveItem(item);
            }
        };
        pic.Controls.Add(btnDel);

        // Bottom label bar
        Panel pnlInfo = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(34, 38, 48),
            Padding = new Padding(DpiScale(4), DpiScale(2), DpiScale(4), DpiScale(2))
        };

        Label lblName = new()
        {
            Text = item.Name,
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(220, 230, 240),
            Dock = DockStyle.Top,
            Height = DpiScale(15),
            AutoEllipsis = true
        };

        Label lblDim = new()
        {
            Text = item.Width > 0 ? $"{item.Width} × {item.Height} px" : "",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 7.5F),
            ForeColor = Color.FromArgb(130, 145, 165),
            Dock = DockStyle.Bottom,
            Height = DpiScale(13)
        };

        pnlInfo.Controls.Add(lblDim);
        pnlInfo.Controls.Add(lblName);

        card.Controls.Add(pnlInfo);
        card.Controls.Add(pic);
        pnlInfo.BringToFront();

        // Tooltips
        ToolTip tip = new();
        string tipText = $"{item.Name}\nKích thước: {item.Width} × {item.Height} px\n- Nhấp chuột hoặc kéo sang Main để hiển thị";
        tip.SetToolTip(card, tipText);
        tip.SetToolTip(pic, tipText);
        tip.SetToolTip(lblName, tipText);
        tip.SetToolTip(lblDim, tipText);

        // Mouse events: Click, DoubleClick, Drag, MouseWheel
        void AttachMouseInteractions(Control c)
        {
            c.MouseWheel += OnCardsMouseWheel;
            c.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _mouseDownLocation = e.Location;
                    _isDraggingOut = false;
                    MediaSelected?.Invoke(item);
                }
            };

            c.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && !_isDraggingOut)
                {
                    int dx = Math.Abs(e.X - _mouseDownLocation.X);
                    int dy = Math.Abs(e.Y - _mouseDownLocation.Y);
                    if (dx > SystemInformation.DragSize.Width || dy > SystemInformation.DragSize.Height)
                    {
                        _isDraggingOut = true;
                        // Start drag & drop with MediaItem data
                        DataObject data = new();
                        data.SetData(typeof(MediaItem), item);
                        if (item.Bitmap != null)
                        {
                            data.SetData(DataFormats.Bitmap, item.Bitmap);
                        }
                        c.DoDragDrop(data, DragDropEffects.Copy);
                        _isDraggingOut = false;
                    }
                }
            };

            c.DoubleClick += (s, e) =>
            {
                MediaDoubleClicked?.Invoke(item);
            };
        }

        AttachMouseInteractions(card);
        AttachMouseInteractions(pic);
        AttachMouseInteractions(pnlInfo);
        AttachMouseInteractions(lblName);
        AttachMouseInteractions(lblDim);

        // Card border painting (Active vs Normal)
        card.Paint += (s, e) =>
        {
            var g = e.Graphics;
            bool isActive = _activeItem == item;
            Color borderColor = isActive ? Color.FromArgb(0, 220, 255) : Color.FromArgb(50, 56, 70);
            int penWidth = isActive ? 2 : 1;
            using Pen pen = new(borderColor, penWidth);
            g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    private void HighlightActiveCard()
    {
        foreach (Control c in pnlCardsContent.Controls)
        {
            if (c.Tag is MediaItem m)
            {
                c.Invalidate();
            }
        }
    }

    private Rectangle GetScrollThumbRect()
    {
        int trackH = pnlScrollBar.ClientSize.Height;
        int totalH = pnlCardsContent.Height;
        int viewH = pnlCardsContainer.ClientSize.Height;
        if (_maxScroll <= 0 || trackH <= 0 || totalH <= 0) return Rectangle.Empty;

        float ratio = (float)viewH / totalH;
        int thumbH = Math.Max(DpiScale(28), (int)(trackH * ratio));
        int travel = trackH - thumbH;
        int thumbY = travel > 0 ? (int)((float)_scrollOffset / _maxScroll * travel) : 0;
        return new Rectangle(1, thumbY, Math.Max(2, pnlScrollBar.Width - 2), thumbH);
    }

    private void OnScrollBarPaint(object? sender, PaintEventArgs e)
    {
        if (_maxScroll <= 0) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle thumbRect = GetScrollThumbRect();
        if (thumbRect.IsEmpty) return;

        Color thumbColor = _isDraggingScrollThumb
            ? Color.FromArgb(160, 180, 210)
            : (_isThumbHovered ? Color.FromArgb(130, 145, 175) : Color.FromArgb(85, 95, 115));

        using GraphicsPath path = GetRoundedRect(new RectangleF(thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height), Math.Min(thumbRect.Width / 2f, 3f));
        using SolidBrush brush = new(thumbColor);
        g.FillPath(brush, path);
    }

    private void OnScrollBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _maxScroll <= 0) return;
        Rectangle thumbRect = GetScrollThumbRect();
        if (thumbRect.Contains(e.Location))
        {
            _isDraggingScrollThumb = true;
            _dragStartY = e.Y;
            _dragStartScrollOffset = _scrollOffset;
            pnlScrollBar.Capture = true;
        }
        else
        {
            int pageDelta = pnlCardsContainer.ClientSize.Height - DpiScale(40);
            if (e.Y < thumbRect.Y)
            {
                SetScrollOffset(_scrollOffset - pageDelta);
            }
            else
            {
                SetScrollOffset(_scrollOffset + pageDelta);
            }
        }
    }

    private void OnScrollBarMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDraggingScrollThumb)
        {
            int trackH = pnlScrollBar.ClientSize.Height;
            Rectangle thumbRect = GetScrollThumbRect();
            int travel = trackH - thumbRect.Height;
            if (travel > 0)
            {
                int deltaY = e.Y - _dragStartY;
                float scrollPerPixel = (float)_maxScroll / travel;
                SetScrollOffset((int)(_dragStartScrollOffset + deltaY * scrollPerPixel));
            }
            return;
        }

        bool hovered = GetScrollThumbRect().Contains(e.Location);
        if (hovered != _isThumbHovered)
        {
            _isThumbHovered = hovered;
            pnlScrollBar.Invalidate();
        }
    }

    private void OnScrollBarMouseUp(object? sender, MouseEventArgs e)
    {
        if (_isDraggingScrollThumb)
        {
            _isDraggingScrollThumb = false;
            pnlScrollBar.Capture = false;
            pnlScrollBar.Invalidate();
        }
    }

    private void OnCardsMouseWheel(object? sender, MouseEventArgs e)
    {
        if (_maxScroll <= 0) return;
        int delta = -Math.Sign(e.Delta) * DpiScale(48);
        SetScrollOffset(_scrollOffset + delta);
    }

    private void SetScrollOffset(int newOffset)
    {
        _scrollOffset = Math.Clamp(newOffset, 0, _maxScroll);
        pnlCardsContent.Top = -_scrollOffset;
        pnlScrollBar.Invalidate();
    }

    private static GraphicsPath GetRoundedRect(RectangleF rect, float radius)
    {
        GraphicsPath path = new();
        float d = radius * 2f;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void DrawImageCover(Graphics g, Image img, Rectangle destRect)
    {
        if (destRect.Width <= 0 || destRect.Height <= 0 || img.Width <= 0 || img.Height <= 0) return;

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.SmoothingMode = SmoothingMode.HighQuality;

        float scale = Math.Max((float)destRect.Width / img.Width, (float)destRect.Height / img.Height);
        float srcW = destRect.Width / scale;
        float srcH = destRect.Height / scale;
        float srcX = Math.Max(0f, (img.Width - srcW) / 2f);
        float srcY = Math.Max(0f, (img.Height - srcH) / 2f);
        srcW = Math.Min(img.Width - srcX, srcW);
        srcH = Math.Min(img.Height - srcY, srcH);

        RectangleF srcRect = new(srcX, srcY, srcW, srcH);
        g.DrawImage(img, destRect, srcRect, GraphicsUnit.Pixel);
    }
}
