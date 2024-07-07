using System;
using System.Threading.Tasks;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Notifications;

namespace GmlCore.Interfaces.Procedures;

public interface INotificationProcedures
{
    IObservable<INotification> Notifications { get; }
    Task SendMessage(string message);
    Task SendMessage(string message, string details, NotificationType type);
    Task SendMessage(string message, NotificationType type);
    Task SendMessage(string message, Exception exception);
}
