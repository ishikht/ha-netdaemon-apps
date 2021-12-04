using System;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet.JsonConverters
{
    public class RawTemperatureToDecimalConverter:JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var parseResult = Int32.TryParse(reader.Value as string, out var intValue);
            if (parseResult)
            {
                return  Convert.ToDecimal(intValue / 16.0);
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal);
        }
    }
}