using System;
using GmlCore.Interfaces.User;

namespace Gml.Models.Sessions;

public class GameSession : ISession
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset EndDate { get; set; }

    public GameSession()
    {
        Start = DateTimeOffset.Now;
    }

}
