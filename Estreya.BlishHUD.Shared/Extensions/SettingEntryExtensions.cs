namespace Estreya.BlishHUD.Shared.Extensions
{
    using Blish_HUD.Settings;
    using System.Collections.Generic;
    using System.Linq;

    public static class SettingEntryExtensions
    {

        public static float GetValue(this SettingEntry<float> settingEntry)
        {
            if (settingEntry == null)
            {
                return 0;
            }

            (float Min, float Max)? range = GetRange(settingEntry);

            if (!range.HasValue)
            {
                return settingEntry.Value;
            }

            if (settingEntry.Value > range.Value.Max)
            {
                return range.Value.Max;
            }

            if (settingEntry.Value < range.Value.Min)
            {
                return range.Value.Min;
            }

            return settingEntry.Value;
        }

        public static int GetValue(this SettingEntry<int> settingEntry)
        {
            if (settingEntry == null)
            {
                return 0;
            }

            (float Min, float Max)? range = GetRange(settingEntry);

            if (!range.HasValue)
            {
                return settingEntry.Value;
            }

            if (settingEntry.Value > range.Value.Max)
            {
                return (int)range.Value.Max;
            }

            if (settingEntry.Value < range.Value.Min)
            {
                return (int)range.Value.Min;
            }

            return settingEntry.Value;
        }

        public static (float Min, float Max)? GetRange<T>(this SettingEntry<T> settingEntry)
        {
            if (settingEntry == null)
            {
                return null;
            }

            List<IComplianceRequisite> intRangeList = settingEntry.GetComplianceRequisite().Where(cr => cr is IntRangeRangeComplianceRequisite).ToList();

            if (intRangeList.Count > 0)
            {
                IntRangeRangeComplianceRequisite intRangeCr = (IntRangeRangeComplianceRequisite)intRangeList[0];
                return (intRangeCr.MinValue, intRangeCr.MaxValue);
            }

            List<IComplianceRequisite> floatList = settingEntry.GetComplianceRequisite().Where(cr => cr is FloatRangeRangeComplianceRequisite).ToList();

            if (floatList.Count > 0)
            {
                FloatRangeRangeComplianceRequisite floatRangeCr = (FloatRangeRangeComplianceRequisite)floatList[0];
                return (floatRangeCr.MinValue, floatRangeCr.MaxValue);
            }

            return null;
        }

        public static bool IsDisabled(this SettingEntry settingEntry)
        {
            return settingEntry.GetComplianceRequisite()?.Any(cr => cr is SettingDisabledComplianceRequisite) ?? false;
        }

        public static bool HasValidation<T>(this SettingEntry<T> settingEntry)
        {
            return settingEntry.GetComplianceRequisite()?.Any(cr => cr is SettingValidationComplianceRequisite<T>) ?? false;
        }

        public static SettingValidationComplianceRequisite<T> GetValidation<T>(this SettingEntry<T> settingEntry)
        {
            return (SettingValidationComplianceRequisite<T>)settingEntry.GetComplianceRequisite()?.First(cr => cr is SettingValidationComplianceRequisite<T>);
        }

        public static SettingValidationResult CheckValidation<T>(this SettingEntry<T> settingEntry, T value)
        {
            if (settingEntry == null)
            {
                return new SettingValidationResult(true);
            }

            if (!settingEntry.HasValidation())
            {
                return new SettingValidationResult(true);
            }

            SettingValidationComplianceRequisite<T> validation = settingEntry.GetValidation();

            SettingValidationResult result = validation.ValidationFunc?.Invoke(value) ?? new SettingValidationResult(false);

            return result;
        }
    }
}
