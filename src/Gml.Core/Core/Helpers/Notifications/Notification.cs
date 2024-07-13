using System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Notifications;

namespace Gml.Core.Helpers.Notifications;

public record Notification : INotification
{
    public string Message { get; set; }
    public string Details { get; set; }
    public NotificationType Type { get; set; }
    public DateTimeOffset Date { get; set; }
}
