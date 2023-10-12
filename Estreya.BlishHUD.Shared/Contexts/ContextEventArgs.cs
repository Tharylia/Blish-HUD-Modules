namespace Estreya.BlishHUD.Shared.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ContextEventArgs : EventArgs
    {
        public Type Caller { get; private set; }

        public ContextEventArgs(Type caller)
        {
            this.Caller = caller;
        }
    }
}
