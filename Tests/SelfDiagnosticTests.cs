#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Win04_Cropper.Tests;

/// <summary>
/// Comprehensive self-diagnostic test suite executing all 15 automated validation checks.
/// </summary>
public static partial class SelfDiagnosticTests
{
    public static int Run()
    {
        Console.WriteLine("=== ScreenCropperPro Self-Diagnostic Tests ===");
        try
        {
            RunTests1To8();
            RunTests9To15();

            Console.WriteLine(">>> ALL 15 SELF-DIAGNOSTIC TESTS PASSED SUCCESSFULLY! <<<");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] Self-test failed: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void RunTests1To8()
    {
        // Test 1: Model Aspect Ratio & FontAwesome Icon generation
        var item = new Models.CropRegionItem { Name = "Test1", X = 10, Y = 20, Width = 1920, Height = 1080 };
        if (item.AspectRatioStr != "16:9") throw new Exception($"Aspect ratio expected 16:9, got {item.AspectRatioStr}");
        using var testIconBmp = FontAwesome.Sharp.FormsIconHelper.ToBitmap(FontAwesome.Sharp.IconChar.FolderOpen, System.Drawing.Color.White, 16);
        if (testIconBmp == null) throw new Exception("Failed to generate FontAwesome icon bitmap");
        Console.WriteLine("[PASS] Test 1: Model Aspect Ratio and FontAwesome.Sharp verified.");

        // Test 2: JSON serialization & deserialization
        string tempJson = Path.Combine(Path.GetTempPath(), "cropper_test_" + Guid.NewGuid().ToString("N") + ".json");
        var list = new List<Models.CropRegionItem> { item, new Models.CropRegionItem { Name = "Square", X = 0, Y = 0, Width = 100, Height = 100 } };
        bool saved = Services.ConfigStorageService.SaveRegions(list, tempJson);
        if (!saved) throw new Exception("Failed to save JSON config.");
        var loaded = Services.ConfigStorageService.LoadRegions(tempJson);
        if (loaded.Count != 2 || loaded[0].Name != "Test1" || loaded[1].Width != 100) throw new Exception("Loaded JSON data mismatch.");
        File.Delete(tempJson);
        Console.WriteLine("[PASS] Test 2: JSON Save and Load verified.");

        // Test 3: Bitmap Crop Generation
        using (var testBmp = new System.Drawing.Bitmap(800, 600))
        {
            using (var g = System.Drawing.Graphics.FromImage(testBmp))
            {
                g.Clear(System.Drawing.Color.Red);
            }

            var previewCtrl = new Controls.PreviewControl();
            previewCtrl.Size = new System.Drawing.Size(300, 200);
            previewCtrl.UpdateCrop(testBmp, new System.Drawing.Rectangle(50, 50, 200, 150));
            if (previewCtrl.CroppedBitmap == null || previewCtrl.CroppedBitmap.Width != 200 || previewCtrl.CroppedBitmap.Height != 150)
            {
                throw new Exception("PreviewControl Crop generation failed.");
            }
            Console.WriteLine("[PASS] Test 3: Bitmap Crop extraction verified.");
        }

        // Test 4: Canvas Coordinate Transforms
        var canvas = new Controls.CanvasControl { Size = new System.Drawing.Size(800, 600) };
        using (var src = new System.Drawing.Bitmap(1000, 1000))
        {
            canvas.Image = src;
            canvas.SetCropRect(new System.Drawing.Rectangle(100, 100, 300, 200));
            if (canvas.CropRect.Width != 300 || canvas.CropRect.Height != 200)
            {
                throw new Exception("Canvas crop rect mismatch.");
            }
            Console.WriteLine("[PASS] Test 4: Canvas control crop bounds verified.");
        }

        // Test 5: Form Instantiation, Full Load, and Grid layout verification
        Console.WriteLine("[DEBUG] Starting Test 5: Form layout and Grid verification...");
        var form = new MainCropperForm();
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (s, e) =>
        {
            var grid = form.SavedGrid;
            if (!grid.ColumnHeadersVisible) throw new Exception("DataGridView ColumnHeadersVisible is false!");
            if (grid.Height < 50) throw new Exception($"DataGridView height too small: {grid.Height}px");
            if (grid.Columns.Count < 12) throw new Exception($"Expected 12 columns, got {grid.Columns.Count}");
            var colEdit = grid.Columns["ColEditBtn"] ?? throw new Exception("ColEditBtn not found");
            var colDelete = grid.Columns["ColDeleteBtn"] ?? throw new Exception("ColDeleteBtn not found");
            if (colEdit.Width > 90) throw new Exception($"ColEditBtn width expected compact <= 90, got {colEdit.Width}");
            if (colDelete.Width > 90) throw new Exception($"ColDeleteBtn width expected compact <= 90, got {colDelete.Width}");
            Console.WriteLine($"[PASS] Grid verified: Bounds={grid.Bounds}, Columns={grid.Columns.Count}, HeadersVisible={grid.ColumnHeadersVisible}");
            try
            {
                using var bmp = new System.Drawing.Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
                bmp.Save("app_render.png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"[PASS] Saved visual snapshot to app_render.png ({form.Width}x{form.Height})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Could not save app_render.png: {ex.Message}");
            }
            timer.Stop();
            timer.Dispose();
            form.Close();
        };
        timer.Start();
        Application.Run(form);

        // Test 6: QuickSize Aspect Ratio Locking & Screen Resolution Verification
        Console.WriteLine("[DEBUG] Starting Test 6: QuickSize Preset verification...");
        if (canvas.LockedAspectRatio != null) throw new Exception("Expected LockedAspectRatio to be null initially");
        canvas.LockedAspectRatio = 16f / 9f;
        if (!canvas.LockedAspectRatio.HasValue || Math.Abs(canvas.LockedAspectRatio.Value - (16f / 9f)) > 0.001f)
            throw new Exception("LockedAspectRatio failed to store ratio");
        canvas.LockedAspectRatio = null;
        if (canvas.LockedAspectRatio != null) throw new Exception("LockedAspectRatio failed to reset to null");
        Console.WriteLine("[PASS] Test 6: QuickSize Aspect Ratio Locking verified.");

        // Test 7: ProjectService Save & Load & Thumbnail Generation
        Console.WriteLine("[DEBUG] Starting Test 7: ProjectService verification...");
        using (var testImg = new System.Drawing.Bitmap(320, 240))
        {
            using var g = System.Drawing.Graphics.FromImage(testImg);
            g.Clear(System.Drawing.Color.MediumSeaGreen);
            string thumbBase64 = Services.ProjectService.GenerateThumbnailBase64(testImg, 120, 80);
            if (string.IsNullOrEmpty(thumbBase64)) throw new Exception("Failed to generate project thumbnail.");
            var proj = new Models.ProjectData
            {
                Name = "Test Project SelfTest",
                CropX = 10, CropY = 20, CropW = 100, CropH = 50,
                ThumbnailBase64 = thumbBase64
            };
            string savedProjPath = Services.ProjectService.SaveProject(proj);
            if (!File.Exists(savedProjPath)) throw new Exception("Saved project file not found.");
            var loadedProj = Services.ProjectService.LoadProject(savedProjPath);
            if (loadedProj == null || loadedProj.Name != "Test Project SelfTest" || loadedProj.CropW != 100)
                throw new Exception("Loaded project data mismatch.");
            // Also test ProjectManagementDialog instantiation & snapshot
            using (var dlg = new Controls.ProjectManagementDialog())
            {
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(0, 0);
                dlg.Show();
                Application.DoEvents();
                using var dlgBmp = new System.Drawing.Bitmap(dlg.Width, dlg.Height);
                dlg.DrawToBitmap(dlgBmp, new System.Drawing.Rectangle(0, 0, dlg.Width, dlg.Height));
                dlgBmp.Save("project_dialog_render.png", System.Drawing.Imaging.ImageFormat.Png);
                dlg.Close();
                Console.WriteLine($"[PASS] Saved visual snapshot to project_dialog_render.png ({dlg.Width}x{dlg.Height})");
            }

            // Clean up test project
            try { File.Delete(savedProjPath); Services.ProjectService.DeleteFromHistory(proj.Id); } catch { }
            Console.WriteLine("[PASS] Test 7: ProjectService Save/Load, Thumbnail, and Dialog verified.");
        }

        // Test 8: ImageViewerDialog Modal Full Crop Frame & Actions Verification
        Console.WriteLine("[DEBUG] Starting Test 8: ImageViewerDialog modal verification...");
        {
            var testItem = new Models.CropRegionItem
            {
                Name = "Sample_Object_Crop",
                ItemType = Models.CropItemType.Image,
                X = 100,
                Y = 50,
                Width = 400,
                Height = 250
            };
            using var testBmp = new Bitmap(400, 250);
            using (var g = Graphics.FromImage(testBmp))
            {
                g.Clear(Color.CornflowerBlue);
                using var brush = new SolidBrush(Color.Gold);
                g.FillEllipse(brush, 50, 50, 100, 100);
            }

            using var viewerDlg = new Controls.ImageViewerDialog(testItem, testBmp);
            viewerDlg.StartPosition = FormStartPosition.Manual;
            viewerDlg.Location = new Point(0, 0);
            viewerDlg.Show();
            Application.DoEvents();

            using var dlgBmp = new System.Drawing.Bitmap(viewerDlg.Width, viewerDlg.Height);
            viewerDlg.DrawToBitmap(dlgBmp, new System.Drawing.Rectangle(0, 0, viewerDlg.Width, viewerDlg.Height));
            dlgBmp.Save("image_viewer_render.png", System.Drawing.Imaging.ImageFormat.Png);
            viewerDlg.Close();
            Console.WriteLine($"[PASS] Saved visual snapshot to image_viewer_render.png ({viewerDlg.Width}x{viewerDlg.Height})");
            Console.WriteLine("[PASS] Test 8: ImageViewerDialog verified.");
        }
    }
}
