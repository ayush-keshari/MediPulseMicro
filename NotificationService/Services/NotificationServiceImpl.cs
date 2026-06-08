using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.DTOs;
using NotificationService.Models;

namespace NotificationService.Services;

public class NotificationServiceImpl : INotificationService
{
    private readonly NotificationDbContext _db;

    public NotificationServiceImpl(NotificationDbContext db) => _db = db;

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(NotificationQueryParams q)
    {
        var query = _db.Notifications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.UserId))
            query = query.Where(n => n.UserId == q.UserId);

        if (!string.IsNullOrWhiteSpace(q.Category))
            query = query.Where(n => n.Category == q.Category);

        if (q.IsRead.HasValue)
            query = query.Where(n => n.IsRead == q.IsRead.Value);

        var pageSize = Math.Max(1, q.PageSize);
        var page     = Math.Max(1, q.Page);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => ToDto(n))
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
        => await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

    public async Task<bool> CreateAsync(CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            UserId    = request.UserId,
            Category  = request.Category,
            Title     = request.Title,
            Message   = request.Message,
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkReadAsync(int id, string callerUserId, bool isAdmin)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null) return false;

        if (!isAdmin && notification.UserId != callerUserId) return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id, string callerUserId, bool isAdmin)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null) return false;

        if (!isAdmin && notification.UserId != callerUserId) return false;

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        return true;
    }

    private static NotificationDto ToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        UserId         = n.UserId,
        Category       = n.Category,
        Title          = n.Title,
        Message        = n.Message,
        IsRead         = n.IsRead,
        CreatedAt      = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
    };
}
