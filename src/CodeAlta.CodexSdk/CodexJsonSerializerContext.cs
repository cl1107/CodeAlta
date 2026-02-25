using System.Text.Json.Serialization;

namespace CodeAlta.CodexSdk;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class CodexJsonSerializerContext : JsonSerializerContext
{
}
