using System;
using Gml.Core.Launcher;
using GmlCore.Interfaces.Sentry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gml.Models.Converters;

public class ExceptionReportConverter : JsonConverter<IExceptionReport>
{
    public override IExceptionReport ReadJson(JsonReader reader, Type objectType, IExceptionReport existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var memoryInfo = jsonObject.ToObject<ExceptionReport>();
        return memoryInfo ?? new ExceptionReport();
    }

    public override void WriteJson(JsonWriter writer, IExceptionReport value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value, typeof(ExceptionReport));
    }
}
