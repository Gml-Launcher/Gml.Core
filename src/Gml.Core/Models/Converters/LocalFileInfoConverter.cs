using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gml.Models.System;
using GmlCore.Interfaces.System;

namespace Gml.Models.Converters;

public class LocalFileInfoConverter : JsonConverter<List<IFileInfo>>
{
    public override List<IFileInfo>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var fileInfos = JsonSerializer.Deserialize<List<LocalFileInfo>>(ref reader, options);

        return fileInfos?.Cast<IFileInfo>().ToList() ?? new List<IFileInfo>();
    }

    public override void Write(Utf8JsonWriter writer, List<IFileInfo> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, typeof(List<LocalFileInfo>), options);
    }
}
