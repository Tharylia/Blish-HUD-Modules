namespace Estreya.BlishHUD.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class VSCodeHelper
{
    public const string SYSTEM_INSTALL_FOLDER = "C:\\Program Files\\Microsoft VS Code";
    public const string USER_INSTALL_FOLDER = "%HOMEPATH%\\AppData\\Local\\Programs\\Microsoft VS Code";
    public const string EXE_NAME = "code.exe";
    public const string SYSTEM_EXE = $"{SYSTEM_INSTALL_FOLDER}\\{EXE_NAME}";
    public const string USER_EXE = $"{USER_INSTALL_FOLDER}\\{EXE_NAME}";


    public static Task Diff(string filePath1, string filePath2)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrEmpty(filePath1) || !File.Exists(filePath1))
            {
                throw new FileNotFoundException($"Path \"{filePath1}\" does not exist.");
            }

            if (string.IsNullOrEmpty(filePath2) || !File.Exists(filePath2))
            {
                throw new FileNotFoundException($"Path \"{filePath2}\" does not exist.");
            }

            var exePath = GetExePath();

            if (string.IsNullOrWhiteSpace(exePath))
            {
                throw new FileNotFoundException("Could not find VS Code installation.");
            }

            // --wait is important as vs code is started from a cmd window and exits before finishing
            var vsCodeProcess = Process.Start($"{exePath}", $"--wait --diff \"{filePath1}\" \"{filePath2}\"");

            vsCodeProcess.WaitForExit();
        });
    }

    public static string GetExePath()
    {
        return File.Exists(SYSTEM_EXE) ? SYSTEM_EXE : File.Exists(USER_EXE) ? USER_EXE : null;
    }
}
