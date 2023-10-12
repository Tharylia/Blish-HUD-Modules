namespace Estreya.BlishHUD.Shared.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ContextEventArgs<T> : EventArgs
    {
        public Type Caller { get; private set; }

        public T Content { get; private set; }

        public ContextEventArgs(Type caller, T content)
        {
            this.Caller = caller;
            this.Content = content;
        }
    }
}
