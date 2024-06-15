using System;
using System.Collections.Generic;
using System.Linq;
using Gml.Models.Servers;
using GmlCore.Interfaces.System;
using Newtonsoft.Json;

namespace Gml.Models.Converters
{
    public class ServerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(List<IFileInfo>).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var fileInfos = serializer.Deserialize<List<MinecraftServer>>(reader);

            return fileInfos?.Cast<MinecraftServer>() ?? new List<MinecraftServer>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(List<MinecraftServer>));
        }
    }
}
