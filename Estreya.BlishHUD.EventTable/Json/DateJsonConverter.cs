namespace Estreya.BlishHUD.EventTable.Json
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
