using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper.Controls;

public class ProjectManagementDialog : Form
{
    public bool IsNewProjectRequested { get; private set; }
    public string? SelectedProjectPath { get; private set; }
    public string? NewProjectName { get; private set; }

    private readonly float _dpiScale = 1.0f;
    private int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    private Panel pnlHistoryContainer = null!;
    private TextBox txtSearch = null!;
    private List<ProjectHistoryItem> _allHistory = new();

    public ProjectManagementDialog(float dpiScale = 1.0f)
    {
        _dpiScale = dpiScale > 0 ? dpiScale : 1.0f;

        this.Text = "Quản lý Dự án - Screen Cropper Pro";
        this.Size = new Size(DpiScale(780), DpiScale(580));
        this.MinimumSize = new Size(DpiScale(650), DpiScale(480));
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowInTaskbar = false;
        this.BackColor = Color.FromArgb(28, 31, 38);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 9F);

        InitializeLayout();
        LoadHistory();
    }

    private void InitializeLayout()
    {
        // ---------------------------------------------------------
        // Header
        // ---------------------------------------------------------
        Panel pnlHeader = new()
        {
            Dock = DockStyle.Top,
            Height = DpiScale(58),
            BackColor = Color.FromArgb(38, 42, 52),
            Padding = new Padding(DpiScale(16), DpiScale(10), DpiScale(16), DpiScale(10))
        };
        this.Controls.Add(pnlHeader);

        IconPictureBox picHeader = new()
        {
            IconChar = IconChar.FolderTree,
            IconColor = Color.FromArgb(0, 168, 255),
            IconSize = DpiScale(26),
            Size = new Size(DpiScale(30), DpiScale(30)),
            Location = new Point(DpiScale(16), DpiScale(14)),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picHeader);

        Label lblTitle = new()
        {
            Text = "QUẢN LÝ DỰ ÁN",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 11F),
            ForeColor = Color.White,
            Location = new Point(picHeader.Right + DpiScale(10), DpiScale(8)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        Label lblSubtitle = new()
        {
            Text = "Tạo dự án mới, mở file có sẵn hoặc chọn tiếp tục từ lịch sử",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Location = new Point(picHeader.Right + DpiScale(10), DpiScale(32)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblSubtitle);

        // ---------------------------------------------------------
        // Action Bar (Tạo mới, Mở file, Tìm kiếm)
        // ---------------------------------------------------------
        Panel pnlActionBar = new()
        {
            Dock = DockStyle.Top,
            Height = DpiScale(50),
            BackColor = Color.FromArgb(34, 38, 46),
            Padding = new Padding(DpiScale(16), DpiScale(8), DpiScale(16), DpiScale(8))
        };
        this.Controls.Add(pnlActionBar);
        pnlHeader.SendToBack();

        IconButton btnCreateNew = new()
        {
            Text = " Tạo dự án mới",
            UseMnemonic = false,
            IconChar = IconChar.Plus,
            IconColor = Color.White,
            IconSize = DpiScale(15),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(6), 0, DpiScale(8), 0),
            Location = new Point(DpiScale(16), DpiScale(9)),
            Size = new Size(DpiScale(165), DpiScale(32)),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Bold", 8.5F),
            Cursor = Cursors.Hand
        };
        btnCreateNew.FlatAppearance.BorderSize = 0;
        btnCreateNew.Click += (s, e) => HandleCreateNew();
        pnlActionBar.Controls.Add(btnCreateNew);

        IconButton btnOpenExisting = new()
        {
            Text = " Mở dự án có sẵn...",
            UseMnemonic = false,
            IconChar = IconChar.FolderOpen,
            IconColor = Color.White,
            IconSize = DpiScale(15),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(6), 0, DpiScale(8), 0),
            Location = new Point(btnCreateNew.Right + DpiScale(10), DpiScale(9)),
            Size = new Size(DpiScale(185), DpiScale(32)),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btnOpenExisting.FlatAppearance.BorderSize = 0;
        btnOpenExisting.Click += (s, e) => HandleOpenExisting();
        pnlActionBar.Controls.Add(btnOpenExisting);

        // Search text box
        txtSearch = new TextBox
        {
            Size = new Size(DpiScale(180), DpiScale(26)),
            Location = new Point(this.ClientSize.Width - DpiScale(206), DpiScale(12)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F),
            PlaceholderText = "Tìm dự án..."
        };
        txtSearch.TextChanged += (s, e) => RenderHistoryItems();
        pnlActionBar.Controls.Add(txtSearch);

        // ---------------------------------------------------------
        // Footer (Bottom)
        // ---------------------------------------------------------
        Panel pnlFooter = new()
        {
            Dock = DockStyle.Bottom,
            Height = DpiScale(44),
            BackColor = Color.FromArgb(34, 38, 46),
            Padding = new Padding(DpiScale(16), DpiScale(6), DpiScale(16), DpiScale(6))
        };
        this.Controls.Add(pnlFooter);

        Button btnClose = new()
        {
            Text = "Đóng",
            Dock = DockStyle.Right,
            Width = DpiScale(90),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnClose.FlatAppearance.BorderSize = 0;
        pnlFooter.Controls.Add(btnClose);
        this.CancelButton = btnClose;

        // ---------------------------------------------------------
        // Body: History Container
        // ---------------------------------------------------------
        Panel pnlBody = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(24, 27, 34),
            Padding = new Padding(DpiScale(16), DpiScale(10), DpiScale(16), DpiScale(10))
        };
        this.Controls.Add(pnlBody);
        pnlBody.BringToFront();

        Label lblHistoryTitle = new()
        {
            Text = "LỊCH SỬ CÁC DỰ ÁN",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 8.5F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(8))
        };
        pnlBody.Controls.Add(lblHistoryTitle);

        pnlHistoryContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(4), DpiScale(4), 0)
        };
        pnlBody.Controls.Add(pnlHistoryContainer);
        lblHistoryTitle.SendToBack();
    }

    private void LoadHistory()
    {
        _allHistory = ProjectService.GetHistory();
        RenderHistoryItems();
    }

    private void RenderHistoryItems()
    {
        pnlHistoryContainer.SuspendLayout();
        pnlHistoryContainer.Controls.Clear();

        string query = txtSearch.Text.Trim().ToLowerInvariant();
        var filtered = string.IsNullOrEmpty(query)
            ? _allHistory
            : _allHistory.Where(h => h.Name.ToLowerInvariant().Contains(query) || h.FilePath.ToLowerInvariant().Contains(query)).ToList();

        if (filtered.Count == 0)
        {
            Label lblEmpty = new()
            {
                Text = string.IsNullOrEmpty(query)
                    ? "Chưa có dự án nào trong lịch sử.\nNhấn 'Tạo dự án mới' hoặc 'Lưu dự án' để bắt đầu lưu tiến trình của bạn."
                    : "Không tìm thấy dự án nào phù hợp với từ khóa.",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(150, 160, 175),
                Font = new Font("Segoe UI", 9.5F)
            };
            pnlHistoryContainer.Controls.Add(lblEmpty);
            pnlHistoryContainer.ResumeLayout(true);
            return;
        }

        int itemHeight = DpiScale(96);
        int gap = DpiScale(8);
        int currentY = 0;

        foreach (var item in filtered)
        {
            Panel card = new()
            {
                Location = new Point(0, currentY),
                Width = pnlHistoryContainer.ClientSize.Width - DpiScale(10),
                Height = itemHeight,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(36, 40, 50),
                Padding = new Padding(DpiScale(8))
            };

            // Thumbnail (Main capture image)
            PictureBox picThumb = new()
            {
                Location = new Point(DpiScale(8), DpiScale(8)),
                Size = new Size(DpiScale(125), DpiScale(80)),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(20, 22, 28)
            };

            if (!string.IsNullOrEmpty(item.ThumbnailBase64))
            {
                var bmp = ProjectService.ImageFromBase64(item.ThumbnailBase64);
                if (bmp != null) picThumb.Image = bmp;
            }
            card.Controls.Add(picThumb);

            // Project Info
            Label lblName = new()
            {
                Text = string.IsNullOrEmpty(item.Name) ? "Dự án không tên" : item.Name,
                UseMnemonic = false,
                Font = new Font("Segoe UI Bold", 9.5F),
                ForeColor = Color.White,
                Location = new Point(picThumb.Right + DpiScale(12), DpiScale(10)),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblName.Click += (s, e) => SelectHistoryItem(item);
            card.Controls.Add(lblName);

            Label lblMeta = new()
            {
                Text = $"Cập nhật: {item.LastModified:dd/MM/yyyy HH:mm}   |   {item.RegionCount} vùng tọa độ   |   {item.ImageDimensions}",
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(170, 185, 205),
                Location = new Point(picThumb.Right + DpiScale(12), DpiScale(36)),
                AutoSize = true
            };
            card.Controls.Add(lblMeta);

            Label lblPath = new()
            {
                Text = item.FilePath,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(120, 135, 155),
                Location = new Point(picThumb.Right + DpiScale(12), DpiScale(58)),
                AutoSize = true,
                MaximumSize = new Size(DpiScale(320), DpiScale(20))
            };
            card.Controls.Add(lblPath);

            // Actions on right: Open and Delete
            IconButton btnOpen = new()
            {
                Text = " Mở",
                UseMnemonic = false,
                IconChar = IconChar.FolderOpen,
                IconColor = Color.White,
                IconSize = DpiScale(13),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(DpiScale(72), DpiScale(28)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - DpiScale(125), DpiScale(34)),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8F),
                Cursor = Cursors.Hand
            };
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Click += (s, e) => SelectHistoryItem(item);
            card.Controls.Add(btnOpen);

            IconButton btnDelete = new()
            {
                IconChar = IconChar.TrashCan,
                IconColor = Color.FromArgb(255, 140, 140),
                IconSize = DpiScale(13),
                ImageAlign = ContentAlignment.MiddleCenter,
                Size = new Size(DpiScale(32), DpiScale(28)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - DpiScale(44), DpiScale(34)),
                BackColor = Color.FromArgb(70, 40, 48),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) =>
            {
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa dự án '{item.Name}' khỏi lịch sử?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ProjectService.DeleteFromHistory(item.Id);
                    LoadHistory();
                }
            };
            card.Controls.Add(btnDelete);

            pnlHistoryContainer.Controls.Add(card);
            currentY += itemHeight + gap;
        }

        pnlHistoryContainer.ResumeLayout(true);
    }

    private void SelectHistoryItem(ProjectHistoryItem item)
    {
        if (!File.Exists(item.FilePath))
        {
            MessageBox.Show($"File dự án không tồn tại:\n{item.FilePath}", "Không tìm thấy file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SelectedProjectPath = item.FilePath;
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void HandleCreateNew()
    {
        using ProjectNameDialog dlg = new("Tạo dự án mới", "Nhập tên cho dự án mới:", $"Dự án_{DateTime.Now:yyyyMMdd_HHmm}");
        if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.ProjectName))
        {
            IsNewProjectRequested = true;
            NewProjectName = dlg.ProjectName;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    private void HandleOpenExisting()
    {
        using OpenFileDialog ofd = new()
        {
            Title = "Mở file dự án",
            Filter = "File dự án Cropper (*.cropperproj;*.json)|*.cropperproj;*.json|Tất cả file (*.*)|*.*",
            InitialDirectory = ProjectService.ProjectsDirectory
        };

        if (ofd.ShowDialog(this) == DialogResult.OK && File.Exists(ofd.FileName))
        {
            SelectedProjectPath = ofd.FileName;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
