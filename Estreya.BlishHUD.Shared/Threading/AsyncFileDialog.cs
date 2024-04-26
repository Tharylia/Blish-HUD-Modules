namespace Estreya.BlishHUD.Shared.Threading
{
    using Blish_HUD;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Windows.Interop;

    public class AsyncFileDialog<T> where T : FileDialog
    {
        public T Dialog { get; }

        private readonly TaskCompletionSource<DialogResult> _result;
        private readonly Thread _thread;

        public AsyncFileDialog(T dialog)
        {
            this.Dialog = dialog;

            this._result = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            this._thread = new Thread(() =>
            {
                void abort(object sender, EventArgs e)
                {
                    this.AbortThread();
                }

                try
                {
                    GameService.GameIntegration.Gw2Instance.Gw2AcquiredFocus += abort;
                    var result = this.Dialog.ShowDialog();
                    this._result.SetResult(result);
                }
                catch (ThreadAbortException)
                {
                    this._result.SetResult(DialogResult.Cancel);
                }
                catch (Exception e)
                {
                    this._result.SetException(e);
                }
                finally
                {
                    GameService.GameIntegration.Gw2Instance.Gw2AcquiredFocus -= abort;
                }
            });

            this._thread.SetApartmentState(ApartmentState.STA);
        }

        private void AbortThread()
        {
            try
            {
                // Do not use, crashes blish
                //this._thread.Abort();
            }
            catch (ThreadStateException)
            {
            }
        }

        public Task<DialogResult> ShowAsync()
        {
            this._thread.Start();
            return this._result.Task;
        }
    }
}
