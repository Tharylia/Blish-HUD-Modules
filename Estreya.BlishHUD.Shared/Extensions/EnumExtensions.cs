namespace Estreya.BlishHUD.Shared.Extensions;

using Attributes;
using Humanizer;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class EnumExtensions
{
    public static IEnumerable<Enum> GetFlags(this Enum e)
    {
        return Enum.GetValues(e.GetType()).Cast<Enum>().Where(e.HasFlag);
    }

    /// <summary>
    ///     Gets an attribute on an enum field value
    /// </summary>
    /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
    /// <param name="enumVal">The enum value</param>
    /// <returns>The attribute of type T that exists on the enum value</returns>
    /// <example><![CDATA[string desc = myEnumVariable.GetAttributeOfType<DescriptionAttribute>().Description;]]></example>
    public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        Type type = enumVal.GetType();
        MemberInfo[] memInfo = type.GetMember(enumVal.ToString());
        object[] attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0 ? (T)attributes[0] : null;
    }

    public static string GetTranslatedValue(this Enum enumVal, TranslationService translationService)
    {
        return GetTranslatedValue(enumVal, translationService, LetterCasing.Title);
    }

    public static string GetTranslatedValue(this Enum enumVal, TranslationService translationService, LetterCasing fallbackCasing)
    {
        TranslationAttribute translationsAttribute = enumVal.GetAttributeOfType<TranslationAttribute>();
        return translationsAttribute != null
            ? translationService.GetTranslation(translationsAttribute.TranslationKey, translationsAttribute.DefaultValue)
            : enumVal.Humanize(fallbackCasing);
    }
}