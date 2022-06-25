namespace Estreya.BlishHUD.Shared.Utils;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public static class FileUtil
{
    private static async Task<byte[]> ReadBytesAsync(Stream stream)
    {
        if (stream == null)
        {
            return null;
        }

        try
        {
            byte[] result = new byte[stream.Length];
            var read = await stream.ReadAsync(result, 0, (int)stream.Length);

            return result;
        }
        catch (Exception ex)
        {
            Blish_HUD.Debug.Contingency.NotifyFileSaveAccessDenied("stream", ex?.Message ?? "Failed to read stream.");
        }

        return new byte[0];
    }

    public static async Task<byte[]> ReadBytesAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) && !File.Exists(path))
        {
            return null;
        }

        try
        {
            using FileStream fileStream = new FileStream(path, FileMode.Open);
            byte[] result = new byte[fileStream.Length];
            _ = await fileStream.ReadAsync(result, 0, (int)fileStream.Length);

            return result;
        }
        catch (Exception ex)
        {
            Blish_HUD.Debug.Contingency.NotifyFileSaveAccessDenied(path, ex?.Message ?? "Failed to read file.");
        }

        return new byte[0];
    }

    public static async Task<string> ReadStringAsync(string path)
    {
        return string.IsNullOrWhiteSpace(path) && !File.Exists(path) ? null : Encoding.UTF8.GetString(await ReadBytesAsync(path));
    }

    public static async Task<string> ReadStringAsync(Stream stream)
    {
        return stream == null ? null : Encoding.UTF8.GetString(await ReadBytesAsync(stream));
    }

    public static async Task<string[]> ReadLinesAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) && !File.Exists(path))
        {
            return null;
        }

        string text = await ReadStringAsync(path);

        return string.IsNullOrWhiteSpace(text) ? new string[0] : text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static async Task WriteBytesAsync(string path, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        try
        {
            using FileStream SourceStream = new FileStream(path, FileMode.Create);

            await SourceStream.WriteAsync(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Blish_HUD.Debug.Contingency.NotifyFileSaveAccessDenied(path, ex?.Message ?? "Failed to write file.");
        }
    }

    public static async Task WriteStringAsync(string path, string data)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        byte[] byteData = Encoding.UTF8.GetBytes(data);

        await WriteBytesAsync(path, byteData);
    }

    public static async Task WriteLinesAsync(string path, string[] data)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string stringData = string.Join("\r\n", data);

        await WriteStringAsync(path, stringData);
    }

    public static string CreateTempFile(string extension)
    {
        string initialTempFileName = Path.GetTempFileName();
        string tempFileNameWithExtension = Path.ChangeExtension(initialTempFileName, extension);
        File.Move(initialTempFileName, tempFileNameWithExtension);

        return tempFileNameWithExtension;
    }

    public static string SanitizeFileName(string fileName)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(fileName, invalidRegStr, "_");
    }
}
