using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Models;
using Win04_Cropper.Services;

namespace Win04_Cropper.Controls;

public partial class ProjectManagementDialog
{
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

        int itemHeight = DpiScale(104);
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
                Size = new Size(DpiScale(130), DpiScale(88)),
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
                Font = new Font("Segoe UI Bold", 10F),
                ForeColor = Color.White,
                Location = new Point(picThumb.Right + DpiScale(12), DpiScale(10)),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblName.Click += (s, e) => SelectHistoryItem(item);
            card.Controls.Add(lblName);

            Label lblMeta = new()
            {
                Text = $"Cập nhật: {item.LastModified:dd/MM/yyyy HH:mm}   |   {item.RegionCount} mục   |   {item.ImageDimensions}",
                UseMnemonic = false,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(170, 185, 205),
                Location = new Point(picThumb.Right + DpiScale(12), DpiScale(38)),
                AutoSize = true
            };
            card.Controls.Add(lblMeta);

            Label lblPath = new()
            {
                Text = item.FilePath,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(120, 135, 155),
                Location = new Point(picThumb.Right + DpiScale(12), DpiScale(64)),
                AutoSize = true,
                MaximumSize = new Size(DpiScale(320), DpiScale(22))
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
                Size = new Size(DpiScale(72), DpiScale(30)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - DpiScale(125), DpiScale(37)),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F),
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
                Size = new Size(DpiScale(32), DpiScale(30)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - DpiScale(44), DpiScale(37)),
                BackColor = Color.FromArgb(70, 40, 48),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) =>
            {
                string fileName = !string.IsNullOrEmpty(item.FilePath) ? Path.GetFileName(item.FilePath) : $"{item.Name}.json";
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa vĩnh viễn dự án '{item.Name}' khỏi máy tính?\nTệp dự án '{fileName}' sẽ bị xóa hoàn toàn.", "Xác nhận xóa dự án", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    ProjectService.DeleteProject(item.Id, deleteFileOnDisk: true);
                    DeletedProjectIds.Add(item.Id);
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
        using ProjectNameDialog dlg = new("Tạo dự án mới", "Nhập tên cho dự án mới:", "");
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
            Filter = "File dự án Cropper (*.json;*.cropperproj)|*.json;*.cropperproj|File JSON (*.json)|*.json|File Cropper cũ (*.cropperproj)|*.cropperproj|Tất cả file (*.*)|*.*",
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
