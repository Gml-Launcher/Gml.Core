using System;

namespace Gml.Models.User;

public class ServerJoinHistory(string serverUuid, DateTime date)
{
    public string ServerUuid { get; } = serverUuid;
    public DateTime Date { get; } = date;
}
