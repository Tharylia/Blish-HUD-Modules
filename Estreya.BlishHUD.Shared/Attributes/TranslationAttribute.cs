namespace Estreya.BlishHUD.Shared.Attributes;

using System;

[AttributeUsage(AttributeTargets.Field)]
public class TranslationAttribute : Attribute
{
    public TranslationAttribute(string translationKey, string defaultValue)
    {
        this.TranslationKey = translationKey;
        this.DefaultValue = defaultValue;
    }

    public string TranslationKey { get; }
    public string DefaultValue { get; }
}