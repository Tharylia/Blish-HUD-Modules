namespace Estreya.BlishHUD.Shared.Extensions
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class AsyncTexture2DExtensions
    {
        public static Task WaitUntilSwappedAsync(this AsyncTexture2D texture, TimeSpan timeout)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var ct = new CancellationTokenSource((int)timeout.TotalMilliseconds);
            var cancelAction = ct.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

            //if (texture.Texture != ContentService.Textures.TransparentPixel) return Task.CompletedTask;

            texture.TextureSwapped += (s, e) =>
            {

                if (!ct.IsCancellationRequested)
                {
                    cancelAction.Dispose();
                    tcs.SetResult(null);
                }
            };

            return tcs.Task;
        }
    }
}
