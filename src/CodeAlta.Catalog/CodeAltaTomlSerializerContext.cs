using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

namespace CodeAlta.Catalog;

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull)]
[TomlSerializable(typeof(CodeAltaConfigDocument))]
internal partial class CodeAltaTomlSerializerContext : TomlSerializerContext
{
}
