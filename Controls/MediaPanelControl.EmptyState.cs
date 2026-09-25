#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper.Controls;

public partial class MediaPanelControl
{
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
}
