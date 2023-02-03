namespace Estreya.BlishHUD.Shared.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    public static class StreamerUtils
    {
        public static bool IsStreaming()
        {
            return IsOBSOpen();
        }

        public static bool IsOBSOpen()
        {
            try
            {
                return Process.GetProcessesByName("obs64").Any();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
