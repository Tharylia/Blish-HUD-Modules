namespace Estreya.BlishHUD.Shared.Utils;

using Blish_HUD;
using Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public static class WindowsUtil
{
    private static readonly Logger Logger = Logger.GetLogger(typeof(WindowsUtil));

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }


    public static async Task<string> GetDxDiagInformation()
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        if (IntPtr.Size == 4 && Environment.Is64BitOperatingSystem)
        {
            // Need to run the 64-bit version
            psi.FileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "sysnative\\dxdiag.exe");
        }
        else
        {
            // Okay with the native version
            psi.FileName = Path.Combine(
                Environment.SystemDirectory,
                "dxdiag.exe");
        }

        string path = Path.GetTempFileName();

        try
        {
            psi.Arguments = "/x " + path;
            using (Process prc = Process.Start(psi))
            {
                await prc.WaitForExitAsync();

                if (prc.ExitCode != 0)
                {
                    Logger.Warn($"DXDIAG failed with exit code {prc.ExitCode}");
                    return null;
                }
            }

            StringBuilder stringBuilder = new StringBuilder();

            string dxDiagContent = File.ReadAllText(path);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(dxDiagContent);

            stringBuilder.AppendLine("**---System Information---**");
            stringBuilder.AppendLine();

            XmlNode systemInformationNode = doc.DocumentElement.SelectSingleNode("/DxDiag/SystemInformation");
            foreach (XmlNode systemInformationChildNode in systemInformationNode.ChildNodes)
            {
                stringBuilder.AppendLine($"**{systemInformationChildNode.Name}**: {systemInformationChildNode.InnerText}");
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("**---Display Devices---**");
            stringBuilder.AppendLine();

            XmlNodeList displayDeviceNodes = doc.DocumentElement.SelectNodes("/DxDiag/DisplayDevices/DisplayDevice");
            for (int i = 0; i < displayDeviceNodes.Count; i++)
            {
                XmlNode displayDeviceNode = displayDeviceNodes.Item(i);

                foreach (XmlNode displayDeviceChildNode in displayDeviceNode.ChildNodes)
                {
                    stringBuilder.AppendLine($"**{displayDeviceChildNode.Name}**: {displayDeviceChildNode.InnerText}");
                }

                if (i < displayDeviceNodes.Count - 1)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("**----------------**");
                    stringBuilder.AppendLine();
                }
            }

            return stringBuilder.ToString();
        }
        finally
        {
            File.Delete(path);
        }
    }
}