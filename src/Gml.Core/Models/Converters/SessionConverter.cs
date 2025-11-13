using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gml.Models.Sessions;
using GmlCore.Interfaces.User;

namespace Gml.Models.Converters;

public class SessionConverter : JsonConverter<List<ISession>>
{
    public override List<ISession> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = JsonSerializer.Deserialize<List<GameSession>>(ref reader, options);
        return result.Cast<ISession>().ToList();
    }

    public override void Write(Utf8JsonWriter writer, List<ISession> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Cast<GameSession>().ToList(), options);
    }
}
