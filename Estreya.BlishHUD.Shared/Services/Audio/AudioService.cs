namespace Estreya.BlishHUD.Shared.Services.Audio
{
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Audio;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class AudioService : ManagedService
    {
        private AsyncLock _audioLock = new AsyncLock();
        private const string SOUND_EFFECT_FILE_EXTENSION = ".wav";
        private const string AUDIO_FOLDER_NAME = "audio";
        private const string SOUND_FILE_INDEX_SEPARATOR = "_";
        private readonly string _rootPath;

        private List<string> _registeredSubfolders;

        private int _playRemainingAttempts = 3;

        private string FullPath => Path.Combine(this._rootPath, AUDIO_FOLDER_NAME);

        public AudioService(ServiceConfiguration configuration, string rootPath) : base(configuration)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentNullException(nameof(rootPath));

            this._rootPath = rootPath;
        }

        protected override Task Initialize()
        {
            if (!Directory.Exists(this.FullPath))
            {
                Directory.CreateDirectory(this.FullPath);
            }

            this._registeredSubfolders = new List<string>();

            return Task.CompletedTask;
        }

        protected override void InternalUnload()
        {
            this._registeredSubfolders?.Clear();
            this._registeredSubfolders = null;
        }

        protected override void InternalUpdate(GameTime gameTime) { }

        protected override Task Load() => Task.CompletedTask;

        public async Task<AudioPlaybackResult> PlaySoundFromFile(string soundName, string subfolder = null, bool failSilent = false)
        {
            if (subfolder is not null && !this._registeredSubfolders.Contains(subfolder)) throw new DirectoryNotFoundException($"The directory \"{subfolder}\" is not registered.");

            var files = await this.GetSoundFiles(soundName, subfolder ?? string.Empty);
            if (files.Length == 0) return AudioPlaybackResult.NotFound;

            var file = files.PickRandom();

            return await this.PlaySoundFromPath(file, failSilent);
        }

        public async Task<AudioPlaybackResult> PlaySoundFromPath(string filePath, bool failSilent = false)
        {
            if (_playRemainingAttempts <= 0)
            {
                // We keep failing to play sound effects - don't even bother
                return AudioPlaybackResult.Failed;
            }

            if (GameService.GameIntegration.Audio.AudioDevice == null)
            {
                // No device is set yet or there isn't one to use
                return AudioPlaybackResult.Failed;
            }

            if (Path.GetExtension(filePath) != SOUND_EFFECT_FILE_EXTENSION)
            {
                if (failSilent)
                {
                    return AudioPlaybackResult.Failed;
                }
                else
                {
                    throw new ArgumentException(nameof(filePath), $"Sound file does not has the required format \"{SOUND_EFFECT_FILE_EXTENSION}\".");
                }
            }

            if (!File.Exists(filePath))
            {
                if (failSilent)
                {
                    return AudioPlaybackResult.Failed;
                }
                else
                {
                    throw new FileNotFoundException("Soundfile does not exist.");
                }
            }

            try
            {
                if (this._audioLock.IsFree())
                {
                    using (await this._audioLock.LockAsync())
                    {
                        Logger.Debug("AudioService started playing sound...");

                        using var stream = FileUtil.ReadStream(filePath);
                        var se = SoundEffect.FromStream(stream);
                        var sei = se.CreateInstance();
                        sei.Volume = GameService.GameIntegration.Audio.Volume;
                        sei.Play();

                        try
                        {
                            await AsyncHelper.WaitUntil(() => sei.State == SoundState.Stopped, TimeSpan.FromSeconds(30), 500);
                            Logger.Debug("AudioService finished playing sound.");

                            _playRemainingAttempts = 3;
                            return AudioPlaybackResult.Success;
                        }
                        catch (TimeoutException)
                        {
                            Logger.Debug("AudioService could not finish playing sound in allocated timeout. This could be the cause of long sound files.");
                            return AudioPlaybackResult.Failed;
                        }
                    }
                }
                else
                {
                    Logger.Debug("AudioService is currently busy playing. Skipping");
                    return AudioPlaybackResult.Busy;
                }

            }
            catch (Exception ex)
            {
                _playRemainingAttempts--;
                Logger.Warn(ex, "Failed to play sound effect.");
                return AudioPlaybackResult.Failed;
            }
        }

        public void PlaySoundFromRef(string soundName)
        {
            GameService.Content.PlaySoundEffectByName(soundName);
        }

        public Task RegisterSubfolder(params string[] subfolders)
        {
            foreach (var subfolder in subfolders)
            {
                Directory.CreateDirectory(Path.Combine(this.FullPath, subfolder));
                this._registeredSubfolders.Add(subfolder);
            }

            return Task.CompletedTask;
        }

        public Task<string[]> GetSoundFiles(string soundName, string subfolder = null)
        {
            if (subfolder is not null && !this._registeredSubfolders.Contains(subfolder)) throw new DirectoryNotFoundException($"The directory \"{subfolder}\" is not registered.");

            var files = new List<string>();
            var mainFileName = Path.Combine(this.FullPath, subfolder ?? string.Empty, soundName + SOUND_EFFECT_FILE_EXTENSION);
            if (File.Exists(mainFileName))
            {
                files.Add(mainFileName);
            }

            var dir = Path.Combine(this.FullPath, subfolder ?? string.Empty);
            if (Directory.Exists(dir))
            {
                files.AddRange(Directory.GetFiles(dir, $"{soundName}{SOUND_FILE_INDEX_SEPARATOR}*{SOUND_EFFECT_FILE_EXTENSION}"));
            }

            return Task.FromResult(files.ToArray());
        }

        private async Task<int> GetNextSoundFileIndex(string soundName, string subfolder = null)
        {
            var files = await this.GetSoundFiles(soundName, subfolder);

            var indexes = files
                .Select(f => Path.GetFileNameWithoutExtension(f)
                .Split(new string[] { SOUND_FILE_INDEX_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries))
                .Select(split =>
                {
                    return split.Length < 2 ? new string[] { split[0], "0" } : split;
                })
                .Select(split =>
                {
                    if (!int.TryParse(split[split.Length - 1], out var _))
                    {
                        var newSplit = split.ToList();
                        newSplit.Add("0");
                        return  newSplit.ToArray();
                    }

                    return split;
                })
                .Select(split => split[split.Length - 1])
                .Select(i => Convert.ToInt32(i))
                .OrderBy(i => i)
                .ToList();

            return indexes.Count == 0 ? -1 : indexes.LastOrDefault() + 1;
        }

        public async Task UploadFile(string sourceFilePath, string destinationFileName, string subfolder = null)
        {
            if (Path.GetExtension(sourceFilePath) != SOUND_EFFECT_FILE_EXTENSION) throw new NotSupportedException($"The source file is not in the {SOUND_EFFECT_FILE_EXTENSION} format.");
            if (subfolder is not null && !this._registeredSubfolders.Contains(subfolder)) throw new DirectoryNotFoundException($"The directory \"{subfolder}\" is not registered.");

            var newIndex = await this.GetNextSoundFileIndex(Path.GetFileNameWithoutExtension(destinationFileName), subfolder);
            if (newIndex != -1)
            {
                destinationFileName = $"{destinationFileName}{SOUND_FILE_INDEX_SEPARATOR}{newIndex}";
            }

            File.Copy(sourceFilePath, Path.Combine(this.FullPath, subfolder ?? string.Empty, destinationFileName + SOUND_EFFECT_FILE_EXTENSION), true);
        }

        public enum AudioPlaybackResult
        {
            Success,
            NotFound,
            Busy,
            Failed
        }
    }
}
