using System.Linq.Expressions;
using Newtonsoft.Json;

namespace vali_flow_test.JsonConvert;

public class ExpressionConverter : JsonConverter<LambdaExpression>
{
    public override void WriteJson(JsonWriter writer, LambdaExpression? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Expression");
        writer.WriteValue(value?.ToString()); // Serializa la expresión como string legible
        writer.WriteEndObject();
    }

    public override LambdaExpression? ReadJson(JsonReader reader, Type objectType, LambdaExpression? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException("Deserialization not supported");
    }
}