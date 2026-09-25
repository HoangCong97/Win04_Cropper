using System;
using System.IO;
using System.Windows.Forms;

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
                return Tests.SelfDiagnosticTests.Run();
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
}