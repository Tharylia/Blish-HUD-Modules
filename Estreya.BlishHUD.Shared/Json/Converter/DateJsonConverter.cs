namespace Estreya.BlishHUD.Shared.Json.Converter;

using Newtonsoft.Json.Converters;
using System.Globalization;

public class DateJsonConverter : IsoDateTimeConverter
{
    public DateJsonConverter()
    {
        this.DateTimeFormat = "yyyy-MM-dd";
        this.Culture = CultureInfo.InvariantCulture;
    }
}