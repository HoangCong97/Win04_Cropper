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

public partial class ProjectManagementDialog : Form
{
    public bool IsNewProjectRequested { get; private set; }
    public string? SelectedProjectPath { get; private set; }
    public string? NewProjectName { get; private set; }
    public List<string> DeletedProjectIds { get; } = new();

    private readonly float _dpiScale = 1.0f;
    private int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    private Panel pnlHistoryContainer = null!;
    private TextBox txtSearch = null!;
    private List<ProjectHistoryItem> _allHistory = new();

    public ProjectManagementDialog(float dpiScale = 1.0f)
    {
        _dpiScale = dpiScale > 0 ? dpiScale : 1.0f;

        this.Text = "Quáº£n lÃ½ Dá»± Ã¡n - Screen Cropper Pro";
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
            Text = "QUáº¢N LÃ Dá»° ÃN",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 11F),
            ForeColor = Color.White,
            Location = new Point(picHeader.Right + DpiScale(10), DpiScale(8)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        Label lblSubtitle = new()
        {
            Text = "Táº¡o dá»± Ã¡n má»›i, má»Ÿ file cÃ³ sáºµn hoáº·c chá»n tiáº¿p tá»¥c tá»« lá»‹ch sá»­",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Location = new Point(picHeader.Right + DpiScale(10), DpiScale(32)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblSubtitle);

        // ---------------------------------------------------------
        // Action Bar (Táº¡o má»›i, Má»Ÿ file, TÃ¬m kiáº¿m)
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
            Text = " Táº¡o dá»± Ã¡n má»›i",
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
            Font = new Font("Segoe UI Bold", 9.5F),
            Cursor = Cursors.Hand
        };
        btnCreateNew.FlatAppearance.BorderSize = 0;
        btnCreateNew.Click += (s, e) => HandleCreateNew();
        pnlActionBar.Controls.Add(btnCreateNew);

        IconButton btnOpenExisting = new()
        {
            Text = " Má»Ÿ dá»± Ã¡n cÃ³ sáºµn...",
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
            Font = new Font("Segoe UI Semibold", 9.5F),
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
            PlaceholderText = "TÃ¬m dá»± Ã¡n..."
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
            Text = "ÄÃ³ng",
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
            Text = "Lá»ŠCH Sá»¬ CÃC Dá»° ÃN",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9.5F),
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
}
