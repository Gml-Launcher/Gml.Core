using System;
using System.Collections.Generic;
using System.Linq;
using Gml.Core.Launcher;
using GmlCore.Interfaces.Sentry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gml.Models.Converters;

public class StackTraceConverter : JsonConverter<IStackTrace>
{
    public override IStackTrace ReadJson(JsonReader reader, Type objectType, IStackTrace? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var stackTrace = jsonObject.ToObject<StackTrace>();
        return stackTrace ?? new StackTrace();
        // switch (reader.TokenType)
        // {
        //     case JsonToken.StartArray:
        //         var jsonArray = JArray.Load(reader);
        //         var stackTraceList = jsonArray.ToObject<IEnumerable<StackTrace>>();
        //         return stackTraceList.FirstOrDefault() ?? new StackTrace();
        //     case JsonToken.StartObject:
        //         // Handle object case
        //         var jsonObject = JObject.Load(reader);
        //         var stackTraceInfo = jsonObject.ToObject<StackTrace>();
        //         return stackTraceInfo ?? new StackTrace();
        //     default:
        //         throw new JsonReaderException($"Unexpected token when parsing StackTrace: {reader.TokenType}");
        // }
    }

    public override void WriteJson(JsonWriter writer, IStackTrace? value, JsonSerializer serializer)
    {
        if (value is null) return;

        var t = JToken.FromObject(value);

        if (t.Type is not JTokenType.Object)
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
