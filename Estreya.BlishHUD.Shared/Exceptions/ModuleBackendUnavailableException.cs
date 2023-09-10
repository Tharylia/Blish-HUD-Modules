namespace Estreya.BlishHUD.Shared.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ModuleBackendUnavailableException : Exception
    {
        public ModuleBackendUnavailableException() : this(null) { }
        public ModuleBackendUnavailableException(string message) : base(!string.IsNullOrWhiteSpace(message) ? message : "The backend of this module is currently unavailable. The module can't be used without.") { }
    }
}
