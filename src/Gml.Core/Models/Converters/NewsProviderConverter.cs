using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gml.Core.Integrations;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Integrations;

namespace Gml.Models.Converters;

public class NewsProviderConverter(GmlManager gmlManager) : JsonConverter<INewsProvider>
{
    private static readonly Dictionary<NewsListenerType, Type> _typeMapping = new()
    {
        { NewsListenerType.UnicoreCMS, typeof(UnicoreNewsProvider) },
        { NewsListenerType.Telegram, typeof(TelegramNewsProvider) },
        { NewsListenerType.Azuriom, typeof(AzuriomNewsProvider) },
        { NewsListenerType.VK, typeof(VkNewsProvider) },
        { NewsListenerType.Custom, typeof(CustomNewsProvider) }
    };

    public override void Write(Utf8JsonWriter writer, INewsProvider value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

    public override INewsProvider Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var optionsWithFields = new JsonSerializerOptions(options)
        {
            IncludeFields = true
        };
        var root = document.RootElement;

        if (!root.TryGetProperty("Type", out var typeProperty))
            throw new JsonException($"Поле 'type' не найдено для десериализации {nameof(INewsProvider)}.");

        if (!Enum.TryParse<NewsListenerType>(typeProperty.ToString(), out var listenerType))
            throw new JsonException($"Неизвестный тип {typeProperty} для десериализации {nameof(INewsProvider)}.");

        var targetType = _typeMapping[listenerType];

        var provider = (INewsProvider)JsonSerializer.Deserialize(root.GetRawText(), targetType, optionsWithFields)!;

        provider.SetManager(gmlManager);

        return provider;
    }

    private static NewsListenerType GetDiscriminatorByType(Type type)
    {
        foreach (var kvp in _typeMapping)
            if (kvp.Value == type)
                return kvp.Key;

        throw new JsonException($"Не удалось найти дискриминатор для типа {type.FullName}");
    }
}
