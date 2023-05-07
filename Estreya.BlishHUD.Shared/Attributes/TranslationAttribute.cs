namespace Estreya.BlishHUD.Shared.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TranslationAttribute : Attribute
    {
        public string TranslationKey { get; }
        public string DefaultValue { get; }

        public TranslationAttribute(string translationKey, string defaultValue)
        {
            this.TranslationKey = translationKey;
            this.DefaultValue = defaultValue;
        }
    }
}
