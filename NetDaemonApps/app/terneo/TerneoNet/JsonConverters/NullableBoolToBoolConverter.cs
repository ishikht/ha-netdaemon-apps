using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerneoIntegration.TerneoNet.JsonConverters;

public class NullableBoolToBoolConverter:JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType != JsonTokenType.Null && reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}