namespace Estreya.BlishHUD.Shared.Contexts
{
    using Blish_HUD;
    using Blish_HUD.Contexts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    public abstract class BaseContext : Context
    {
        protected Logger Logger { get; private set; }
        public BaseContext()
        {
            this.Logger = Logger.GetLogger(this.GetType());
        }

        protected override void Load()
        {
            this.ConfirmReady();
        }

        /// <summary>
        /// Checks if the context is ready.
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws if context is expired or not ready.</exception>
        protected void CheckReady()
        {
            if (this.State == ContextState.Expired) throw new InvalidOperationException("Context has expired.");
            if (this.State != ContextState.Ready) throw new InvalidOperationException("Context is not ready.");
        }

        /// <summary>
        /// Gets the caller of the context method.
        /// </summary>
        /// <returns>The calling type.</returns>
        protected Type GetCaller()
        {
            bool lastFrameWasBaseType = false;
            Type type = null;
            var stackTrace = new StackTrace(false).GetFrames();

            foreach (var frame in stackTrace)
            {
                var methodType = frame.GetMethod().DeclaringType;
                var currentFrameIsBaseType = methodType.BaseType == typeof(BaseContext);

                if (lastFrameWasBaseType&& !currentFrameIsBaseType)
                {
                    type = methodType;
                }

                lastFrameWasBaseType = currentFrameIsBaseType;
            }

            return type.DeclaringType ?? type;
        }
    }
}
