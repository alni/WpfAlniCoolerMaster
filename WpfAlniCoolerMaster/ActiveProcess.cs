using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WpfAlniCoolerMaster
{
    /// <summary>
    /// https://stackoverflow.com/a/6569555
    /// </summary>
    class ActiveProcess
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static string GetActiveProcessFileName()
        {
            IntPtr hwnd = GetForegroundWindow();
            // Declare _out_ variable inlined (see: https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7#out-variables)
            GetWindowThreadProcessId(hwnd, out uint pid); 
            Process p = Process.GetProcessById((int)pid);
            return p.ProcessName;
            //p.MainModule.FileName.Dump();
        }
    }
}
