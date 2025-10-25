using System;

namespace Gml.Dto.Player;

public record ServerJoinHistoryDto
{
    public string ServerUuid { get; set; }
    public DateTime Date { get; set; }
}
