﻿namespace Estreya.BlishHUD.Shared.Helpers;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public static class VSCodeHelper
{
    public const string SYSTEM_INSTALL_FOLDER = "C:\\Program Files\\Microsoft VS Code";
    public const string USER_INSTALL_FOLDER = "%USERPROFILE%\\AppData\\Local\\Programs\\Microsoft VS Code";
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

            string exePath = GetExePath();

            if (string.IsNullOrWhiteSpace(exePath))
            {
                throw new FileNotFoundException("Could not find VS Code installation.");
            }

            // --wait is important as vs code is started from a cmd window and exits before finishing
            Process vsCodeProcess = Process.Start($"{exePath}", $"--wait --diff \"{filePath1}\" \"{filePath2}\"");

            vsCodeProcess.WaitForExit();
        });
    }

    public static Task EditAsync(string filePath)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException($"Path \"{filePath}\" does not exist.");
            }

            string exePath = GetExePath();

            if (string.IsNullOrWhiteSpace(exePath))
            {
                throw new FileNotFoundException("Could not find VS Code installation.");
            }

            // --wait is important as vs code is started from a cmd window and exits before finishing
            Process vsCodeProcess = Process.Start($"{exePath}", $"--wait \"{filePath}\"");

            vsCodeProcess.WaitForExit();
        });
    }

    public static string GetExePath()
    {
        string userExe = Environment.ExpandEnvironmentVariables(USER_EXE);

        return File.Exists(SYSTEM_EXE) ? SYSTEM_EXE : File.Exists(userExe) ? userExe : null;
    }
}