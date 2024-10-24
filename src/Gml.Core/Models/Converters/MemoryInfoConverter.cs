using System;
using System.Collections.Generic;
using System.Linq;
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
        var t = JToken.FromObject(value);

        if (t.Type != JTokenType.Object)
        {
            t.WriteTo(writer);
        }
        else
        {
            var o = (JObject)t;
            IList<string> propertyNames = o.Properties().Select(p => p.Name).ToList();

            o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));

            o.WriteTo(writer);
        }
    }
}
