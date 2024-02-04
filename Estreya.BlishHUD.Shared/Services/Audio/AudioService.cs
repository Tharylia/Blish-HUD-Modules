namespace Estreya.BlishHUD.Shared.Services.Audio
{
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Audio;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class AudioService : ManagedService
    {
        private const string SOUND_EFFECT_FILE_EXTENSION = ".wav";
        private const string AUDIO_FOLDER_NAME = "audio";
        private readonly string _rootPath;
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

            return Task.CompletedTask;
        }

        protected override void InternalUnload() { }

        protected override void InternalUpdate(GameTime gameTime) { }

        protected override Task Load() => Task.CompletedTask;

        public void PlaySoundFromFile(string soundName, bool silent = false)
        {
            var filePath = Path.Combine(this.FullPath, soundName + SOUND_EFFECT_FILE_EXTENSION);

            this.PlaySoundFromPath(filePath, silent);
        }

        public void PlaySoundFromPath(string filePath, bool silent = false)
        {
            if (_playRemainingAttempts <= 0)
            {
                // We keep failing to play sound effects - don't even bother
                return;
            }

            if (GameService.GameIntegration.Audio.AudioDevice == null)
            {
                // No device is set yet or there isn't one to use
                return;
            }

            if (Path.GetExtension(filePath) != SOUND_EFFECT_FILE_EXTENSION)
            {
                if (!silent)
                {
                    throw new ArgumentException(nameof(filePath), $"Sound file does not has the required format \"{SOUND_EFFECT_FILE_EXTENSION}\".");
                }
            }

            if (!File.Exists(filePath))
            {
                if (!silent)
                {
                    throw new FileNotFoundException("Soundfile does not exist.");
                }
            }

            try
            {
                using var stream = FileUtil.ReadStream(filePath);
                _ = SoundEffect.FromStream(stream).Play(GameService.GameIntegration.Audio.Volume, 0, 0);

                _playRemainingAttempts = 3;
            }
            catch (Exception ex)
            {
                _playRemainingAttempts--;
                Logger.Warn(ex, "Failed to play sound effect.");
            }
        }

        public void PlaySoundFromRef(string soundName)
        {
            GameService.Content.PlaySoundEffectByName(soundName);
        }
    }
}
