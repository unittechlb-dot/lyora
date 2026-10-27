using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>View Notifications use case (customers and administrators).</summary>
public class NotificationService
{
    private readonly NotificationRepository _notifications;

    public NotificationService(NotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public List<Notification> GetForUser(int userId) => _notifications.GetForUser(userId);

    public int CountUnread(int userId) => _notifications.CountUnread(userId);

    /// <summary>Unread low-stock alerts of an administrator (shown on the dashboard).</summary>
    public int CountUnreadLowStockAlerts(int adminId) => _notifications.CountUnread(adminId, NotificationType.LowStock);

    public void MarkRead(int userId, int notificationId) => _notifications.MarkRead(userId, notificationId);

    public void MarkAllRead(int userId) => _notifications.MarkAllRead(userId);
}
