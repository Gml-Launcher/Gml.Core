using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gml.Models.System;
using GmlCore.Interfaces.System;
using Newtonsoft.Json;

namespace Gml.Models.Converters;

public class LocalFolderInfoConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(List<IFolderInfo>).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        var fileInfos = serializer.Deserialize<List<LocalFolderInfo>>(reader);

        return fileInfos?.Cast<LocalFolderInfo>() ?? new List<LocalFolderInfo>();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value, typeof(List<LocalFolderInfo>));
    }
}
