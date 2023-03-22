
namespace Estreya.BlishHUD.Shared.Security;

using Blish_HUD;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class PasswordManager
{
    private static readonly Logger Logger = Logger.GetLogger<PasswordManager>();

    private byte[] _passwordEntroy;
    private readonly string _directoryPath;

    public PasswordManager(string directoryPath)
    {
        this._directoryPath = Path.Combine(directoryPath, "credentials");
    }

    public void InitializeEntropy(byte[] data)
    {
        _passwordEntroy = data;
    }

    public async Task Save(string key, byte[] data, bool silent = false)
    {
        var protectedData = this.EncryptData(data, silent);

        if (protectedData != null)
        {
            await this.WritePasswordFile(key, protectedData);
        }
    }

    public void Delete(string key)
    {
        var filePath = Path.Combine(this._directoryPath, $"{key}.pwd");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public async Task<byte[]> Retrive(string key, bool silent = false)
    {
        var protectedData = await this.ReadPasswordFile(key);

        return this.DecryptData(protectedData, silent);
    }

    private async Task WritePasswordFile(string key, byte[] data)
    {
        var dataString = Convert.ToBase64String(data);

        _ = Directory.CreateDirectory(this._directoryPath);

        var filePath = Path.Combine(this._directoryPath, $"{key}.pwd");

        await FileUtil.WriteStringAsync(filePath, dataString);
    }

    private async Task<byte[]> ReadPasswordFile(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Key can't be null.");
        }

        var filePath = Path.Combine(this._directoryPath, $"{key}.pwd");

        if (!File.Exists(filePath))
        {
            return null;
        }

        var fileContent = await FileUtil.ReadStringAsync(filePath);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return null;
        }

        var data = Convert.FromBase64String(fileContent);

        return data;
    }

    private byte[] EncryptData(byte[] data, bool silent = false)
    {
        if (_passwordEntroy == null)
        {
            throw new ArgumentException("Entroy was not initialized.");
        }

        if (data == null)
        {
            return null;
        }

        try
        {
            byte[] protectedData = ProtectedData.Protect(data, _passwordEntroy, DataProtectionScope.CurrentUser);

            return protectedData;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to encrypt data:");
            if (!silent)
            {
                throw;
            }
        }

        return null;
    }

    private byte[] DecryptData(byte[] data, bool silent = false)
    {
        if (_passwordEntroy == null)
        {
            throw new ArgumentException("Entroy was not initialized.");
        }

        if (data == null)
        {
            return null;
        }

        try
        {
            byte[] clearData = ProtectedData.Unprotect(data, _passwordEntroy, DataProtectionScope.CurrentUser);
            return clearData;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to decrypt data:");
            // The entropy is different from the one used for encryption
            // The data was not encrypted by the current user (scope == CurrentUser)
            // The data was not encrypted on this machine (scope == LocalMachine)

            if (!silent)
            {
                throw;
            }
        }

        return null;
    }
}
