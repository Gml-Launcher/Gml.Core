using GmlCore.Interfaces.Enums;

namespace GmlCore.Interfaces.Notifications;

public interface INotification
{
    string Message { get; set; }
    string Details { get; set; }
    public NotificationType Type { get; set; }
}
