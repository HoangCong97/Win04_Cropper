#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper.Controls;

public partial class MediaPanelControl
{
    private void InitializeBodyLayout()
    {
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
            if (c.Tag is MediaItem)
            {
                c.Invalidate();
            }
        }
    }
}
