namespace Estreya.BlishHUD.Shared.Utils;

using Blish_HUD;
using Estreya.BlishHUD.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

public static class WindowsUtil
{
    private static readonly Logger Logger = Logger.GetLogger(typeof(WindowsUtil));

    public static async Task<string> GetDxDiagInformation()
    {
        var psi = new ProcessStartInfo();
        if (IntPtr.Size == 4 && Environment.Is64BitOperatingSystem)
        {
            // Need to run the 64-bit version
            psi.FileName = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "sysnative\\dxdiag.exe");
        }
        else
        {
            // Okay with the native version
            psi.FileName = System.IO.Path.Combine(
                Environment.SystemDirectory,
                "dxdiag.exe");
        }

        string path = System.IO.Path.GetTempFileName();

        try
        {
            psi.Arguments = "/x " + path;
            using (var prc = Process.Start(psi))
            {
                await prc.WaitForExitAsync();

                if (prc.ExitCode != 0)
                {
                    Logger.Warn($"DXDIAG failed with exit code {prc.ExitCode}");
                    return null;
                }
            }

            StringBuilder stringBuilder = new StringBuilder();

            var dxDiagContent = System.IO.File.ReadAllText(path);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(dxDiagContent);

            stringBuilder.AppendLine("**---System Information---**");
            stringBuilder.AppendLine();

            var systemInformationNode = doc.DocumentElement.SelectSingleNode("/DxDiag/SystemInformation");
            foreach (XmlNode systemInformationChildNode in systemInformationNode.ChildNodes)
            {
                stringBuilder.AppendLine($"**{systemInformationChildNode.Name}**: {systemInformationChildNode.InnerText}");
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("**---Display Devices---**");
            stringBuilder.AppendLine();

            var displayDeviceNodes = doc.DocumentElement.SelectNodes("/DxDiag/DisplayDevices/DisplayDevice");
            for (int i = 0; i < displayDeviceNodes.Count; i++)
            {
                var displayDeviceNode = displayDeviceNodes.Item(i);

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
            System.IO.File.Delete(path);
        }
    }
}
