using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Services.Storage;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Notifications;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Helpers.Notifications;

public class NotificationProcedures(IStorageService storage) : INotificationProcedures
{
    private ISubject<INotification> _notifications = new Subject<INotification>();
    public IObservable<INotification> Notifications => _notifications;
    public IEnumerable<INotification> History => _notificationsHistory;
    private List<Notification> _notificationsHistory = new();

    public async Task SendMessage(string message)
    {
        var notification = new Notification
        {
            Message = message,
            Type = NotificationType.Info,
            Date = DateTimeOffset.Now
        };

        _notificationsHistory.Add(notification);

        await storage.SetAsync(StorageConstants.Notifications, _notificationsHistory);

        _notifications.OnNext(notification);
    }

    public async Task SendMessage(string message, string details, NotificationType type)
    {
        var notification = new Notification
        {
            Message = message,
            Details = details,
            Type = NotificationType.Info,
            Date = DateTimeOffset.Now
        };

        _notificationsHistory.Add(notification);

        await storage.SetAsync(StorageConstants.Notifications, _notificationsHistory);

        _notifications.OnNext(notification);
    }

    public async Task SendMessage(string message, NotificationType type)
    {
        var notification = new Notification
        {
            Message = message,
            Type = type,
            Date = DateTimeOffset.Now
        };

        _notificationsHistory.Add(notification);

        await storage.SetAsync(StorageConstants.Notifications, _notificationsHistory);

        _notifications.OnNext(notification);
    }

    public async Task SendMessage(string message, Exception exception)
    {
        var notification = new Notification
        {
            Message = message,
            Type = NotificationType.Error,
            Details = exception.ToString(),
            Date = DateTimeOffset.Now
        };

        _notificationsHistory.Add(notification);

        await storage.SetAsync(StorageConstants.Notifications, _notificationsHistory);

        _notifications.OnNext(notification);
    }

    public async Task Retore()
    {
        _notificationsHistory = await storage.GetAsync<List<Notification>>(StorageConstants.Notifications) ?? [];
    }

    public async Task Clear()
    {
        _notificationsHistory.Clear();
        await storage.SetAsync(StorageConstants.Notifications, Enumerable.Empty<Notification>());
    }

    public async Task Clear()
    {
        await storage.SetAsync(StorageConstants.Settings, Enumerable.Empty<Notification>());
    }
}
