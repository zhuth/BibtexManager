using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace BibtexManager
{
    static class Program
    {
        public static string[] args;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            args = argv;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }


        public static class ProcessEx
        {
            private static class NativeMethods
            {
                internal const uint GW_OWNER = 4;

                internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

                [DllImport("User32.dll", CharSet = CharSet.Auto)]
                internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

                [DllImport("User32.dll", CharSet = CharSet.Auto)]
                internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

                [DllImport("User32.dll", CharSet = CharSet.Auto)]
                internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

                [DllImport("User32.dll", CharSet = CharSet.Auto)]
                internal static extern bool IsWindowVisible(IntPtr hWnd);

                [DllImport("user32.dll")]
                internal static extern IntPtr SetActiveWindow(IntPtr hWnd);
             
                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                internal static extern bool SetForegroundWindow(IntPtr hWnd);

                [DllImport("user32.dll")]
                internal static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
            }
            
            public static IntPtr GetMainWindowHandle(string processName)
            {
                Process[] processes = Process.GetProcessesByName(processName); 
                if (processes.Length <= 0) return IntPtr.Zero;
                return processes[0].MainWindowHandle;
            }

            public static void SetActiveWindow(IntPtr hWnd)
            {
                NativeMethods.SetActiveWindow(hWnd);
                NativeMethods.SetForegroundWindow(hWnd);
            }

            public static void SendChars(IntPtr hWnd, string englishChars)
            {
                byte[] chars = System.Text.Encoding.ASCII.GetBytes(englishChars);
                foreach (byte ch in chars)
                    NativeMethods.SendMessage(hWnd, 0x0102 /* WM_CHAR */, ch, 0);
            }
        }
    }
}
