namespace Estreya.BlishHUD.Shared.Security;

using Blish_HUD;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Utils;

public class PasswordManager
{
    private static readonly Logger Logger = Logger.GetLogger<PasswordManager>();
    private readonly string _directoryPath;

    private byte[] _passwordEntroy;

    public PasswordManager(string directoryPath)
    {
        this._directoryPath = Path.Combine(directoryPath, "credentials");
    }

    public void InitializeEntropy(byte[] data)
    {
        this._passwordEntroy = data;
    }

    public async Task Save(string key, byte[] data, bool silent = false)
    {
        byte[] protectedData = this.EncryptData(data, silent);

        if (protectedData != null)
        {
            await this.WritePasswordFile(key, protectedData);
        }
    }

    public void Delete(string key)
    {
        string filePath = Path.Combine(this._directoryPath, $"{key}.pwd");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public async Task<byte[]> Retrive(string key, bool silent = false)
    {
        byte[] protectedData = await this.ReadPasswordFile(key);

        return this.DecryptData(protectedData, silent);
    }

    private async Task WritePasswordFile(string key, byte[] data)
    {
        string dataString = Convert.ToBase64String(data);

        _ = Directory.CreateDirectory(this._directoryPath);

        string filePath = Path.Combine(this._directoryPath, $"{key}.pwd");

        await FileUtil.WriteStringAsync(filePath, dataString);
    }

    private async Task<byte[]> ReadPasswordFile(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Key can't be null.");
        }

        string filePath = Path.Combine(this._directoryPath, $"{key}.pwd");

        if (!File.Exists(filePath))
        {
            return null;
        }

        string fileContent = await FileUtil.ReadStringAsync(filePath);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return null;
        }

        byte[] data = Convert.FromBase64String(fileContent);

        return data;
    }

    private byte[] EncryptData(byte[] data, bool silent = false)
    {
        if (this._passwordEntroy == null)
        {
            throw new ArgumentException("Entroy was not initialized.");
        }

        if (data == null)
        {
            return null;
        }

        try
        {
            byte[] protectedData = ProtectedData.Protect(data, this._passwordEntroy, DataProtectionScope.CurrentUser);

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
        if (this._passwordEntroy == null)
        {
            throw new ArgumentException("Entroy was not initialized.");
        }

        if (data == null)
        {
            return null;
        }

        try
        {
            byte[] clearData = ProtectedData.Unprotect(data, this._passwordEntroy, DataProtectionScope.CurrentUser);
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