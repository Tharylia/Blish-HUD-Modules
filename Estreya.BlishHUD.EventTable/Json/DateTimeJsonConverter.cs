namespace Estreya.BlishHUD.EventTable.Json
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
