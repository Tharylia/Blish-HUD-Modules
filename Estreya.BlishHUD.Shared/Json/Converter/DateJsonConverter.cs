namespace Estreya.BlishHUD.Shared.Json.Converter
{
    using Newtonsoft.Json.Converters;

    public class DateJsonConverter : IsoDateTimeConverter
    {
        public DateJsonConverter()
        {
            this.DateTimeFormat = "yyyy-MM-dd";
        }
    }
}
