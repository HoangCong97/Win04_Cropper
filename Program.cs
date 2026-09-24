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
                File.WriteAllText("thread_crash.log", e.Exception.ToString());
                MessageBox.Show(e.Exception.ToString(), "Thread Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
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
                if (grid.Columns["ColEditBtn"].Width > 50) throw new Exception($"ColEditBtn width expected compact <= 50, got {grid.Columns["ColEditBtn"].Width}");
                if (grid.Columns["ColDeleteBtn"].Width > 50) throw new Exception($"ColDeleteBtn width expected compact <= 50, got {grid.Columns["ColDeleteBtn"].Width}");
                Console.WriteLine($"[PASS] Grid verified: Bounds={grid.Bounds}, Columns={grid.Columns.Count}, HeadersVisible={grid.ColumnHeadersVisible}");
                timer.Stop();
                timer.Dispose();
                form.Close();
            };
            timer.Start();
            Application.Run(form);
            Console.WriteLine("[PASS] Test 5: Application.Run lifecycle completed normally.");

            Console.WriteLine(">>> ALL 5 SELF-DIAGNOSTIC TESTS PASSED SUCCESSFULLY! <<<");
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