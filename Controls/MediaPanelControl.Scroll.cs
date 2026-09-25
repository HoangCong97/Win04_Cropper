#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

public partial class MediaPanelControl
{
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
