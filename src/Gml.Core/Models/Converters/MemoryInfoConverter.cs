using System;
using Gml.Core.Launcher;
using GmlCore.Interfaces.Sentry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gml.Models.Converters;

public class MemoryInfoConverter : JsonConverter<IMemoryInfo>
{
    public override IMemoryInfo ReadJson(JsonReader reader, Type objectType, IMemoryInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var memoryInfo = jsonObject.ToObject<MemoryInfo>();
        return memoryInfo ?? new MemoryInfo();
    }

    public override void WriteJson(JsonWriter writer, IMemoryInfo value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}
