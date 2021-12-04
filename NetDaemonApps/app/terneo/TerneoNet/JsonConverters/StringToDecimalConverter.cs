using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerneoIntegration.TerneoNet.JsonConverters
{
    public class StringToDecimalConverter:JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return decimal.Parse(reader.GetString(), CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}