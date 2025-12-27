using System;
using System.Linq;
using Gml.Core.Launcher;
using GmlCore.Interfaces.Sentry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gml.Models.Converters;

public class ExceptionReportConverter : JsonConverter<IExceptionReport>
{
    public override IExceptionReport ReadJson(JsonReader reader, Type objectType, IExceptionReport? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var exceptionReport = jsonObject.ToObject<ExceptionReport>(new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All,
            Converters = { new StackTraceConverter() }
        });
        return exceptionReport ?? new ExceptionReport();
    }

    public override void WriteJson(JsonWriter writer, IExceptionReport? value, JsonSerializer serializer)
    {
        if (value == null) return;

        var t = JToken.FromObject(value);

        if (t.Type != JTokenType.Object)
        {
            t.WriteTo(writer);
        }
        else
        {
            var o = (JObject)t;
            var propertyNames = o.Properties().Select(p => p.Name).ToList();

            o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));

            o.WriteTo(writer);
        }
    }
}
