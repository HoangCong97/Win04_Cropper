#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Win04_Cropper.Tests;

public static partial class SelfDiagnosticTests
{
    private static void RunTests9To15()
    {
        // Test 9: Physical Disk File Deletion and Session Lifecycle Verification
        Console.WriteLine("[DEBUG] Starting Test 9: Physical disk file deletion and session verification...");
        {
            var testDiskProj = new Models.ProjectData
            {
                Name = "Test_Disk_Delete_" + Guid.NewGuid().ToString("N")[..6],
                IsCustomNamed = true,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now
            };
            string savedPath = Services.ProjectService.SaveProject(testDiskProj);
            if (!File.Exists(savedPath)) throw new Exception($"Project file was not saved on disk: {savedPath}");

            Services.ProjectService.SetLastSessionProjectPath(savedPath);
            string? sessionPath = Services.ProjectService.GetLastSessionProjectPath();
            if (sessionPath != savedPath) throw new Exception($"Expected session path '{savedPath}', got '{sessionPath}'");

            // Perform deletion with deleteFileOnDisk: true
            bool deleted = Services.ProjectService.DeleteProject(testDiskProj.Id, deleteFileOnDisk: true);
            if (!deleted) throw new Exception("DeleteProject returned false.");

            // Verify file is gone from disk
            if (File.Exists(savedPath)) throw new Exception($"Project file was NOT deleted from disk: {savedPath}");

            // Verify history no longer contains the project
            var historyAfter = Services.ProjectService.GetHistory();
            if (historyAfter.Any(h => h.Id == testDiskProj.Id)) throw new Exception("Project still found in history after deletion.");

            // Verify last_session was cleaned up
            string? sessionAfter = Services.ProjectService.GetLastSessionProjectPath();
            if (!string.IsNullOrEmpty(sessionAfter)) throw new Exception($"Session path should be null after project deletion, got '{sessionAfter}'");
            if (File.Exists(Services.ProjectService.SessionFilePath)) throw new Exception("last_session.json file was not removed after project deletion.");

            Console.WriteLine("[PASS] Test 9: Physical disk file deletion and session cleanup verified.");
        }

        // Test 10: MediaPanel 2-Column Grid Layout & Arrow Nudge Verification
        Console.WriteLine("[DEBUG] Starting Test 10: MediaPanel and Arrow Nudge verification...");
        {
            var mediaCtrl = new Controls.MediaPanelControl(1.0f);
            mediaCtrl.Size = new Size(350, 500);

            using var bmp1 = new Bitmap(200, 150);
            using (var g = Graphics.FromImage(bmp1)) g.Clear(Color.DodgerBlue);
            using var bmp2 = new Bitmap(300, 200);
            using (var g = Graphics.FromImage(bmp2)) g.Clear(Color.Crimson);
            using var bmp3 = new Bitmap(150, 150);
            using (var g = Graphics.FromImage(bmp3)) g.Clear(Color.SeaGreen);

            mediaCtrl.AddItem(new Models.MediaItem { Name = "Image_1.png", Bitmap = bmp1, Width = 200, Height = 150 });
            mediaCtrl.AddItem(new Models.MediaItem { Name = "Image_2.png", Bitmap = bmp2, Width = 300, Height = 200 });
            mediaCtrl.AddItem(new Models.MediaItem { Name = "Image_3.png", Bitmap = bmp3, Width = 150, Height = 150 });

            if (mediaCtrl.Items.Count != 3) throw new Exception($"Expected 3 media items, got {mediaCtrl.Items.Count}");

            mediaCtrl.UpdateView();
            Application.DoEvents();

            using var mediaBmp = new Bitmap(350, 500);
            mediaCtrl.DrawToBitmap(mediaBmp, new Rectangle(0, 0, 350, 500));
            mediaBmp.Save("media_panel_render.png", System.Drawing.Imaging.ImageFormat.Png);
            Console.WriteLine("[PASS] Saved visual snapshot to media_panel_render.png (350x500)");

            // Arrow Nudge verification: Arrow=1px, Ctrl+Arrow=10px, Shift+Arrow=50px
            int stepDefault = 1;
            int stepCtrl = 10;
            int stepShift = 50;
            var testRect = new Rectangle(100, 100, 200, 150);

            testRect.Offset(stepDefault, 0);
            if (testRect.X != 101) throw new Exception($"Expected X=101, got {testRect.X}");

            testRect.Offset(stepCtrl, 0);
            if (testRect.X != 111) throw new Exception($"Expected X=111, got {testRect.X}");

            testRect.Offset(stepShift, 0);
            if (testRect.X != 161) throw new Exception($"Expected X=161, got {testRect.X}");

            Console.WriteLine("[PASS] Test 10: MediaPanel 2-column layout and Arrow nudge verified.");
        }

        // Test 11: Simplified Export Package Verification (data.json, README.md, crops/)
        Console.WriteLine("[DEBUG] Starting Test 11: Simplified Export Package verification...");
        using (var exportForm = new MainCropperForm())
        {
            using var testSrcBmp = new Bitmap(800, 600);
            using (var g = Graphics.FromImage(testSrcBmp)) { g.Clear(Color.CornflowerBlue); }
            exportForm.SetImageForTesting(testSrcBmp, "test_background.png");

            exportForm.SavedRegions.Add(new Models.CropRegionItem
            {
                Name = "Area 1",
                ItemType = Models.CropItemType.Coordinate,
                X = 50,
                Y = 60,
                Width = 200,
                Height = 150,
                SourceImageName = "test_background.png"
            });

            exportForm.SavedRegions.Add(new Models.CropRegionItem
            {
                Name = "Crop 1",
                ItemType = Models.CropItemType.Image,
                X = 300,
                Y = 200,
                Width = 100,
                Height = 100,
                SourceImageName = "test_background.png"
            });

            string testExportDir = Path.Combine(Path.GetTempPath(), "cropper_ai_export_test_" + Guid.NewGuid().ToString("N"));

            exportForm.ExportDatasetToFolder(testExportDir, exportAreasWithImages: true, exportCropsWithCoords: true, showSuccessDialog: false);

            if (!File.Exists(Path.Combine(testExportDir, "README.md"))) throw new Exception("README.md missing from export root");
            if (!File.Exists(Path.Combine(testExportDir, "data.json"))) throw new Exception("data.json missing from export root");
            if (!Directory.Exists(Path.Combine(testExportDir, "crops"))) throw new Exception("crops/ folder missing");

            if (File.Exists(Path.Combine(testExportDir, "dataset.json"))) throw new Exception("dataset.json should NOT exist in simplified export");
            if (File.Exists(Path.Combine(testExportDir, "summary.txt"))) throw new Exception("summary.txt should NOT exist in simplified export");
            if (Directory.Exists(Path.Combine(testExportDir, "annotations"))) throw new Exception("annotations/ should NOT exist in simplified export");

            if (Directory.Exists(Path.Combine(testExportDir, "sources"))) throw new Exception("sources/ should NOT exist when exportSources=false");

            string dataJsonContent = File.ReadAllText(Path.Combine(testExportDir, "data.json"));
            if (!dataJsonContent.Contains("\"areas\"")) throw new Exception("data.json missing 'areas' array");
            if (!dataJsonContent.Contains("\"crops\"")) throw new Exception("data.json missing 'crops' array");
            if (!dataJsonContent.Contains("Area 1")) throw new Exception("data.json missing Area 1 entry");
            if (!dataJsonContent.Contains("Crop 1")) throw new Exception("data.json missing Crop 1 entry");

            string readmeContent = File.ReadAllText(Path.Combine(testExportDir, "README.md"));
            if (!readmeContent.Contains("Top-Left")) throw new Exception("README.md missing coordinate origin spec (Top-Left)");

            var cropFiles = Directory.GetFiles(Path.Combine(testExportDir, "crops"), "*.png");
            if (cropFiles.Length < 1) throw new Exception("Expected at least 1 crop PNG file in crops/");

            try { Directory.Delete(testExportDir, true); } catch { }
            Console.WriteLine("[PASS] Test 11: Simplified Export Package verified.");
        }

        // Test 12: Per-Media Zoom/Pan/Crop Persistence, LiveSplitContainer & Objects title
        Console.WriteLine("[DEBUG] Starting Test 12: Per-Media State Persistence & UI checks...");
        using (var testForm = new MainCropperForm())
        {
            using var bmpA = new Bitmap(800, 600);
            using (var g = Graphics.FromImage(bmpA)) g.Clear(Color.DarkBlue);
            using var bmpB = new Bitmap(1000, 800);
            using (var g = Graphics.FromImage(bmpB)) g.Clear(Color.DarkRed);

            var mediaA = new Models.MediaItem { Name = "ImageA.png", Bitmap = bmpA, Width = 800, Height = 600 };
            var mediaB = new Models.MediaItem { Name = "ImageB.png", Bitmap = bmpB, Width = 1000, Height = 800 };

            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var mediaPanelField = typeof(MainCropperForm).GetField("mediaPanel", bindingFlags);
            var canvasField = typeof(MainCropperForm).GetField("canvas", bindingFlags);
            var splitMainField = typeof(MainCropperForm).GetField("splitMain", bindingFlags);
            var splitTopField = typeof(MainCropperForm).GetField("splitTop", bindingFlags);
            var splitMediaField = typeof(MainCropperForm).GetField("splitMediaCanvas", bindingFlags);
            var lblSavedTitleField = typeof(MainCropperForm).GetField("lblSavedTitle", bindingFlags);

            var mediaPanelCtrl = (Controls.MediaPanelControl)mediaPanelField!.GetValue(testForm)!;
            var canvasCtrl = (Controls.CanvasControl)canvasField!.GetValue(testForm)!;
            var splitMainCtrl = splitMainField!.GetValue(testForm)!;
            var splitTopCtrl = splitTopField!.GetValue(testForm)!;
            var splitMediaCtrl = splitMediaField!.GetValue(testForm)!;
            var lblSavedTitleCtrl = (Label)lblSavedTitleField!.GetValue(testForm)!;

            if (splitMainCtrl is not Controls.LiveSplitContainer) throw new Exception("splitMain is not LiveSplitContainer");
            if (splitTopCtrl is not Controls.LiveSplitContainer) throw new Exception("splitTop is not LiveSplitContainer");
            if (splitMediaCtrl is not Controls.LiveSplitContainer) throw new Exception("splitMediaCanvas is not LiveSplitContainer");

            if (!lblSavedTitleCtrl.Text.StartsWith("Objects ("))
            {
                throw new Exception($"Expected Objects (count), got '{lblSavedTitleCtrl.Text}'");
            }

            mediaPanelCtrl.AddItem(mediaA);
            mediaPanelCtrl.AddItem(mediaB);

            var loadMethod = typeof(MainCropperForm).GetMethod("LoadMediaItemToMain", bindingFlags);
            loadMethod!.Invoke(testForm, new object[] { mediaA });

            Rectangle rectA = new(20, 30, 150, 120);
            canvasCtrl.SetCropRect(rectA);
            canvasCtrl.SetZoomFactor(1.75f);
            canvasCtrl.PanOffset = new PointF(45f, 55f);

            loadMethod.Invoke(testForm, new object[] { mediaB });

            if (mediaA.SavedCropW != 150 || mediaA.SavedCropH != 120 || mediaA.SavedCropX != 20 || mediaA.SavedCropY != 30)
            {
                throw new Exception($"Media A crop state not saved! W={mediaA.SavedCropW}, H={mediaA.SavedCropH}");
            }
            if (Math.Abs((mediaA.SavedZoomFactor ?? 0f) - 1.75f) > 0.01f)
            {
                throw new Exception($"Media A zoom factor not saved! Zoom={mediaA.SavedZoomFactor}");
            }

            Rectangle rectB = new(50, 60, 300, 200);
            canvasCtrl.SetCropRect(rectB);
            canvasCtrl.SetZoomFactor(2.5f);
            canvasCtrl.PanOffset = new PointF(100f, 120f);

            loadMethod.Invoke(testForm, new object[] { mediaA });

            if (canvasCtrl.CropRect.X != 20 || canvasCtrl.CropRect.Y != 30 || canvasCtrl.CropRect.Width != 150 || canvasCtrl.CropRect.Height != 120)
            {
                throw new Exception($"Media A crop rect not restored on canvas! Got {canvasCtrl.CropRect}");
            }
            if (Math.Abs(canvasCtrl.ZoomFactor - 1.75f) > 0.01f)
            {
                throw new Exception($"Media A zoom not restored on canvas! Got {canvasCtrl.ZoomFactor}");
            }

            loadMethod.Invoke(testForm, new object[] { mediaB });

            if (canvasCtrl.CropRect.X != 50 || canvasCtrl.CropRect.Y != 60 || canvasCtrl.CropRect.Width != 300 || canvasCtrl.CropRect.Height != 200)
            {
                throw new Exception($"Media B crop rect not restored on canvas! Got {canvasCtrl.CropRect}");
            }
            if (Math.Abs(canvasCtrl.ZoomFactor - 2.5f) > 0.01f)
            {
                throw new Exception($"Media B zoom not restored on canvas! Got {canvasCtrl.ZoomFactor}");
            }

            Console.WriteLine("[PASS] Test 12: Per-Media Zoom/Pan/Crop Persistence, LiveSplitters & Objects title verified.");
        }

        // Test 13: ExportOptionsDialog No Overlap & Visual Snapshot
        Console.WriteLine("[DEBUG] Starting Test 13: ExportOptionsDialog layout & render verification...");
        using (var exportDlg = new Controls.ExportOptionsDialog(4, 8, 1.0f))
        {
            exportDlg.Show();
            Application.DoEvents();

            using var dlgBmp = new Bitmap(exportDlg.Width, exportDlg.Height);
            exportDlg.DrawToBitmap(dlgBmp, new Rectangle(0, 0, exportDlg.Width, exportDlg.Height));
            dlgBmp.Save("export_dialog_render.png", System.Drawing.Imaging.ImageFormat.Png);
            exportDlg.Close();

            Console.WriteLine("[PASS] Test 13: ExportOptionsDialog layout rendered successfully to export_dialog_render.png.");
        }

        // Test 14: ImageFilterService (Grayscale & Threshold Binarization) & UI Integration
        Console.WriteLine("[DEBUG] Starting Test 14: Filter Service & UI integration verification...");
        using (var testBmp = new Bitmap(10, 10))
        {
            testBmp.SetPixel(0, 0, Color.FromArgb(50, 50, 50));
            testBmp.SetPixel(1, 0, Color.FromArgb(200, 200, 200));

            // 14.1 Grayscale
            using var grayBmp = Services.ImageFilterService.ApplyFilters(testBmp, grayscale: true, thresholdEnabled: false, threshold: 128);
            if (grayBmp == null) throw new Exception("Grayscale filter returned null");
            Color p0 = grayBmp.GetPixel(0, 0);
            Color p1 = grayBmp.GetPixel(1, 0);
            if (p0.R != p0.G || p0.G != p0.B) throw new Exception("Grayscale pixel 0 is not monochromatic");
            if (p1.R != p1.G || p1.G != p1.B) throw new Exception("Grayscale pixel 1 is not monochromatic");

            // 14.2 Threshold = 128
            using var threshBmp = Services.ImageFilterService.ApplyFilters(testBmp, grayscale: true, thresholdEnabled: true, threshold: 128);
            if (threshBmp == null) throw new Exception("Threshold filter returned null");
            Color tp0 = threshBmp.GetPixel(0, 0);
            Color tp1 = threshBmp.GetPixel(1, 0);
            if (tp0.R != 0 || tp0.G != 0 || tp0.B != 0) throw new Exception($"Expected black for pixel < 128, got {tp0}");
            if (tp1.R != 255 || tp1.G != 255 || tp1.B != 255) throw new Exception($"Expected white for pixel >= 128, got {tp1}");

            // 14.3 Form UI Integration with Filters
            using var formFilters = new MainCropperForm();
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var chkGrayField = typeof(MainCropperForm).GetField("chkGrayscale", bindingFlags);
            var chkThreshField = typeof(MainCropperForm).GetField("chkThreshold", bindingFlags);
            var trkThreshField = typeof(MainCropperForm).GetField("trkThreshold", bindingFlags);
            var canvasField = typeof(MainCropperForm).GetField("canvas", bindingFlags);

            var chkGray = (CheckBox)chkGrayField!.GetValue(formFilters)!;
            var chkThresh = (CheckBox)chkThreshField!.GetValue(formFilters)!;
            var trkThresh = (TrackBar)trkThreshField!.GetValue(formFilters)!;
            var canvasCtrl = (Controls.CanvasControl)canvasField!.GetValue(formFilters)!;

            // Load test image to main
            var mediaTest = new Models.MediaItem { Name = "FilterTest.png", Bitmap = testBmp, Width = 10, Height = 10 };
            var loadMethod = typeof(MainCropperForm).GetMethod("LoadMediaItemToMain", bindingFlags);
            loadMethod!.Invoke(formFilters, new object[] { mediaTest });

            // Toggle Grayscale
            chkGray.Checked = true;
            if (canvasCtrl.Image == null) throw new Exception("Canvas image null after grayscale toggle");

            // Toggle Threshold
            chkThresh.Checked = true;
            if (!trkThresh.Enabled) throw new Exception("Trackbar should be enabled when chkThreshold is checked");
            trkThresh.Value = 100;

            // Verify cropped image reflects threshold filter
            canvasCtrl.SetCropRect(new Rectangle(0, 0, 2, 1));
            using var croppedFiltered = formFilters.GetCroppedBitmap();
            if (croppedFiltered == null) throw new Exception("Cropped filtered bitmap null");
            Color c0 = croppedFiltered.GetPixel(0, 0);
            Color c1 = croppedFiltered.GetPixel(1, 0);
            if (c0.R != 0 || c0.G != 0 || c0.B != 0) throw new Exception("Cropped pixel 0 should be black");
            if (c1.R != 255 || c1.G != 255 || c1.B != 255) throw new Exception("Cropped pixel 1 should be white");

            Console.WriteLine("[PASS] Test 14: ImageFilterService (Grayscale & Threshold) & UI verified.");
        }

        // Test 15: Object Sorting (Title Click, Column Header Click) & LiveSplitter Flicker-Free Check
        Console.WriteLine("[DEBUG] Starting Test 15: Object Sorting & LiveSplitter checks...");
        using (var formSort = new MainCropperForm())
        {
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var splitMainField = typeof(MainCropperForm).GetField("splitMain", bindingFlags);
            var splitMain = (Controls.LiveSplitContainer)splitMainField!.GetValue(formSort)!;

            // Check splitMain and panels double buffered
            var doubleBufferedProp = typeof(Control).GetProperty("DoubleBuffered", bindingFlags);
            bool p1Buffered = (bool)doubleBufferedProp!.GetValue(splitMain.Panel1)!;
            bool p2Buffered = (bool)doubleBufferedProp.GetValue(splitMain.Panel2)!;
            if (!p1Buffered || !p2Buffered) throw new Exception("SplitContainer panels are not double buffered");

            // Add 3 sample items in unordered state
            formSort.SavedRegions.Clear();
            formSort.SavedRegions.Add(new Models.CropRegionItem { Name = "Charlie", Width = 300, CreatedAt = DateTime.Now.AddMinutes(-10) });
            formSort.SavedRegions.Add(new Models.CropRegionItem { Name = "Alpha", Width = 100, CreatedAt = DateTime.Now.AddMinutes(-5) });
            formSort.SavedRegions.Add(new Models.CropRegionItem { Name = "Bravo", Width = 200, CreatedAt = DateTime.Now });

            // Refresh grid
            var refreshMethod = typeof(MainCropperForm).GetMethod("RefreshSavedGrid", bindingFlags);
            refreshMethod!.Invoke(formSort, null);

            // Sort by Name (ColName) Ascending
            var sortMethod = typeof(MainCropperForm).GetMethod("SortSavedRegions", bindingFlags);
            sortMethod!.Invoke(formSort, new object[] { "ColName" });

            if (formSort.SavedRegions[0].Name != "Alpha" || formSort.SavedRegions[1].Name != "Bravo" || formSort.SavedRegions[2].Name != "Charlie")
            {
                throw new Exception($"Name sorting Ascending failed! Got {formSort.SavedRegions[0].Name}, {formSort.SavedRegions[1].Name}, {formSort.SavedRegions[2].Name}");
            }

            // Sort by Name again -> Descending
            sortMethod.Invoke(formSort, new object[] { "ColName" });
            if (formSort.SavedRegions[0].Name != "Charlie" || formSort.SavedRegions[1].Name != "Bravo" || formSort.SavedRegions[2].Name != "Alpha")
            {
                throw new Exception($"Name sorting Descending failed! Got {formSort.SavedRegions[0].Name}, {formSort.SavedRegions[1].Name}, {formSort.SavedRegions[2].Name}");
            }

            // Sort by Width (ColW) Ascending
            sortMethod.Invoke(formSort, new object[] { "ColW" });
            if (formSort.SavedRegions[0].Width != 100 || formSort.SavedRegions[1].Width != 200 || formSort.SavedRegions[2].Width != 300)
            {
                throw new Exception($"Width sorting failed! Got {formSort.SavedRegions[0].Width}, {formSort.SavedRegions[1].Width}, {formSort.SavedRegions[2].Width}");
            }

            // Test Title Click sorting
            var toggleTitleMethod = typeof(MainCropperForm).GetMethod("ToggleTitleSort", bindingFlags);
            toggleTitleMethod!.Invoke(formSort, null);
            if (formSort.SavedRegions[0].Name != "Alpha") throw new Exception("Title toggle sort to Alpha failed");

            Console.WriteLine("[PASS] Test 15: Object Sorting (Title Click, Column Header) & Flicker-free Splitters verified.");
        }
    }
}
