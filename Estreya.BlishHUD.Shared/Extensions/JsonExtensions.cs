namespace Estreya.BlishHUD.Shared.Extensions
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class JsonExtensions
    {
        public static JsonSerializerSettings ToSettings(this JsonSerializer serializer)
        {
            return new JsonSerializerSettings
            {
                Formatting = serializer.Formatting,
                NullValueHandling = serializer.NullValueHandling,
                DefaultValueHandling = serializer.DefaultValueHandling,
                Converters = serializer.Converters.ToList(),
                ContractResolver = serializer.ContractResolver,
                Culture = serializer.Culture,
                DateFormatHandling = serializer.DateFormatHandling,
                DateParseHandling = serializer.DateParseHandling,
                DateTimeZoneHandling = serializer.DateTimeZoneHandling,
                FloatFormatHandling = serializer.FloatFormatHandling,
                FloatParseHandling = serializer.FloatParseHandling,
                MissingMemberHandling = serializer.MissingMemberHandling,
                ObjectCreationHandling = serializer.ObjectCreationHandling,
                PreserveReferencesHandling = serializer.PreserveReferencesHandling,
                ReferenceLoopHandling = serializer.ReferenceLoopHandling,
                StringEscapeHandling = serializer.StringEscapeHandling,
                TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling,
                TypeNameHandling = serializer.TypeNameHandling
            };
        }

        public static string SerializeObject<T>(this JsonSerializer serializer, T obj)
        {
            StringBuilder sb = new StringBuilder(256);
            StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = serializer.Formatting;

                serializer.Serialize(jsonWriter, obj, obj.GetType());
            }

            return sw.ToString();
        }

        public static T DeserializeObject<T>(this JsonSerializer serializer, string value)
        {
            using JsonTextReader reader = new JsonTextReader(new StringReader(value));
            return (T)serializer.Deserialize(reader, typeof(T));
        }
    }
}
