using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gml.Models.System;
using GmlCore.Interfaces.System;

namespace Gml.Models.Converters;

public class LocalFolderInfoConverter : JsonConverter<List<IFolderInfo>>
{
    public override List<IFolderInfo>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var folderInfos = JsonSerializer.Deserialize<List<LocalFolderInfo>>(ref reader, options);
        return folderInfos?.Cast<IFolderInfo>().ToList();
    }

    public override void Write(Utf8JsonWriter writer, List<IFolderInfo> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, typeof(List<LocalFolderInfo>), options);
    }
}
