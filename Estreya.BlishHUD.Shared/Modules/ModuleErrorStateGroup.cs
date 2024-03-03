namespace Estreya.BlishHUD.Shared.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ModuleErrorStateGroup
    {
        private string _group;

        protected ModuleErrorStateGroup(string group)
        {
            this._group = group;
        }

        public override string ToString()
        {
            return this._group;
        }

        public static ModuleErrorStateGroup BACKEND_UNAVAILABLE = new ModuleErrorStateGroup("backend-unavailable");
        public static ModuleErrorStateGroup MODULE_VALIDATION = new ModuleErrorStateGroup("module-validation");
    }
}
