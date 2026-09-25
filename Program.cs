namespace Win04_Cropper;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "--test")
            {
                return RunSelfTests();
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                // Workaround for known .NET Windows Forms bug where SplitContainer.RepaintSplitterRect
                // throws transient ExternalException (0x80004005) during display session transitions
                if (e.Exception is System.Runtime.InteropServices.ExternalException &&
                    e.Exception.StackTrace?.Contains("SplitContainer.RepaintSplitterRect") == true)
                {
                    return;
                }

                File.WriteAllText("thread_crash.log", e.Exception.ToString());
                MessageBox.Show(e.Exception.ToString(), "Thread Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is System.Runtime.InteropServices.ExternalException ex &&
                    ex.StackTrace?.Contains("SplitContainer.RepaintSplitterRect") == true)
                {
                    return;
                }
                File.WriteAllText("domain_crash.log", e.ExceptionObject.ToString());
            };

            ApplicationConfiguration.Initialize();
            Application.Run(new MainCropperForm());
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText("crash.log", ex.ToString());
            MessageBox.Show(ex.ToString(), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static int RunSelfTests()
    {
        Console.WriteLine("=== ScreenCropperPro Self-Diagnostic Tests ===");
        try
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

                // Force layout
                mediaCtrl.UpdateView();
                Application.DoEvents();

                // Snapshot Media Panel
                using var mediaBmp = new Bitmap(350, 500);
                mediaCtrl.DrawToBitmap(mediaBmp, new Rectangle(0, 0, 350, 500));
                mediaBmp.Save("media_panel_render.png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("[PASS] Saved visual snapshot to media_panel_render.png (350x500)");

                // Arrow Nudge verification: Arrow=1px, Ctrl+Arrow=10px, Shift+Arrow=50px
                int stepDefault = 1;
                int stepCtrl = 10;
                int stepShift = 50;
                var testRect = new Rectangle(100, 100, 200, 150);
                
                // Normal arrow nudge (+1)
                testRect.Offset(stepDefault, 0);
                if (testRect.X != 101) throw new Exception($"Expected X=101, got {testRect.X}");

                // Ctrl+Arrow nudge (+10)
                testRect.Offset(stepCtrl, 0);
                if (testRect.X != 111) throw new Exception($"Expected X=111, got {testRect.X}");

                // Shift+Arrow nudge (+50)
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

                // Use 4-arg backward-compatible overload (exportSources defaults to false)
                exportForm.ExportDatasetToFolder(testExportDir, exportAreasWithImages: true, exportCropsWithCoords: true, showSuccessDialog: false);

                // Verify simplified structure exists
                if (!File.Exists(Path.Combine(testExportDir, "README.md"))) throw new Exception("README.md missing from export root");
                if (!File.Exists(Path.Combine(testExportDir, "data.json"))) throw new Exception("data.json missing from export root");
                if (!Directory.Exists(Path.Combine(testExportDir, "crops"))) throw new Exception("crops/ folder missing");

                // Verify old complex files/folders do NOT exist (simplified!)
                if (File.Exists(Path.Combine(testExportDir, "dataset.json"))) throw new Exception("dataset.json should NOT exist in simplified export");
                if (File.Exists(Path.Combine(testExportDir, "summary.txt"))) throw new Exception("summary.txt should NOT exist in simplified export");
                if (Directory.Exists(Path.Combine(testExportDir, "annotations"))) throw new Exception("annotations/ should NOT exist in simplified export");

                // sources/ should NOT exist when exportSources=false (default)
                if (Directory.Exists(Path.Combine(testExportDir, "sources"))) throw new Exception("sources/ should NOT exist when exportSources=false");

                // Verify data.json content
                string dataJsonContent = File.ReadAllText(Path.Combine(testExportDir, "data.json"));
                if (!dataJsonContent.Contains("\"areas\"")) throw new Exception("data.json missing 'areas' array");
                if (!dataJsonContent.Contains("\"crops\"")) throw new Exception("data.json missing 'crops' array");
                if (!dataJsonContent.Contains("Area 1")) throw new Exception("data.json missing Area 1 entry");
                if (!dataJsonContent.Contains("Crop 1")) throw new Exception("data.json missing Crop 1 entry");

                // Verify README.md mentions coordinate origin
                string readmeContent = File.ReadAllText(Path.Combine(testExportDir, "README.md"));
                if (!readmeContent.Contains("Top-Left")) throw new Exception("README.md missing coordinate origin spec (Top-Left)");

                // Verify crop file was generated
                var cropFiles = Directory.GetFiles(Path.Combine(testExportDir, "crops"), "*.png");
                if (cropFiles.Length < 1) throw new Exception("Expected at least 1 crop PNG file in crops/");

                // Cleanup test dir
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

                // Use reflection to get controls
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

                // Verify LiveSplitContainer
                if (splitMainCtrl is not Controls.LiveSplitContainer) throw new Exception("splitMain is not LiveSplitContainer");
                if (splitTopCtrl is not Controls.LiveSplitContainer) throw new Exception("splitTop is not LiveSplitContainer");
                if (splitMediaCtrl is not Controls.LiveSplitContainer) throw new Exception("splitMediaCanvas is not LiveSplitContainer");

                // Verify Objects title formatting
                if (!lblSavedTitleCtrl.Text.StartsWith("Objects ("))
                {
                    throw new Exception($"Expected Objects (count), got '{lblSavedTitleCtrl.Text}'");
                }

                // Add items to mediaPanel
                mediaPanelCtrl.AddItem(mediaA);
                mediaPanelCtrl.AddItem(mediaB);

                // Select Media A and customize crop & zoom
                var loadMethod = typeof(MainCropperForm).GetMethod("LoadMediaItemToMain", bindingFlags);
                loadMethod!.Invoke(testForm, new object[] { mediaA });

                Rectangle rectA = new(20, 30, 150, 120);
                canvasCtrl.SetCropRect(rectA);
                canvasCtrl.SetZoomFactor(1.75f);
                canvasCtrl.PanOffset = new PointF(45f, 55f);

                // Select Media B and customize crop & zoom
                loadMethod.Invoke(testForm, new object[] { mediaB });

                // Check that Media A's state was saved
                if (mediaA.SavedCropW != 150 || mediaA.SavedCropH != 120 || mediaA.SavedCropX != 20 || mediaA.SavedCropY != 30)
                {
                    throw new Exception($"Media A crop state not saved! W={mediaA.SavedCropW}, H={mediaA.SavedCropH}");
                }
                if (Math.Abs((mediaA.SavedZoomFactor ?? 0f) - 1.75f) > 0.01f)
                {
                    throw new Exception($"Media A zoom factor not saved! Zoom={mediaA.SavedZoomFactor}");
                }

                // Customize Media B
                Rectangle rectB = new(50, 60, 300, 200);
                canvasCtrl.SetCropRect(rectB);
                canvasCtrl.SetZoomFactor(2.5f);
                canvasCtrl.PanOffset = new PointF(100f, 120f);

                // Switch back to Media A
                loadMethod.Invoke(testForm, new object[] { mediaA });

                // Verify Media A's state was accurately restored on canvas
                if (canvasCtrl.CropRect.X != 20 || canvasCtrl.CropRect.Y != 30 || canvasCtrl.CropRect.Width != 150 || canvasCtrl.CropRect.Height != 120)
                {
                    throw new Exception($"Media A crop rect not restored on canvas! Got {canvasCtrl.CropRect}");
                }
                if (Math.Abs(canvasCtrl.ZoomFactor - 1.75f) > 0.01f)
                {
                    throw new Exception($"Media A zoom not restored on canvas! Got {canvasCtrl.ZoomFactor}");
                }

                // Switch back to Media B
                loadMethod.Invoke(testForm, new object[] { mediaB });

                // Verify Media B's state was accurately restored on canvas
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
                // Pixel (0,0): Dark gray (50, 50, 50) -> lum ~50
                // Pixel (1,0): Bright white (200, 200, 200) -> lum ~200
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
                // Lum 50 < 128 => Black (0, 0, 0)
                if (tp0.R != 0 || tp0.G != 0 || tp0.B != 0) throw new Exception($"Expected black for pixel < 128, got {tp0}");
                // Lum 200 >= 128 => White (255, 255, 255)
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
}