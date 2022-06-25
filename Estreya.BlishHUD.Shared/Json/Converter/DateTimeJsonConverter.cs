namespace Estreya.BlishHUD.Shared.Json.Converter
{
    using Newtonsoft.Json.Converters;

    public class DateTimeJsonConverter : IsoDateTimeConverter
    {
        public DateTimeJsonConverter()
        {
            this.DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
        }
    }
}
