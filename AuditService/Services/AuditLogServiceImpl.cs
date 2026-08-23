using AuditService.Data;
using AuditService.DTOs;
using AuditService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Services;

public class AuditLogServiceImpl : IAuditLogService
{
    private readonly AuditDbContext _db;

    public AuditLogServiceImpl(AuditDbContext db) => _db = db;

    public async Task<bool> CreateAsync(CreateAuditLogRequest request)
    {
        var log = new AuditLog
        {
            UserId = request.UserId,
            UserName = request.UserName,
            UserRole = request.UserRole,
            HttpMethod = request.HttpMethod,
            Endpoint = request.Endpoint,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            StatusCode = request.StatusCode,
            ServiceName = request.ServiceName,
            Timestamp = DateTime.UtcNow,
            Details = request.Details
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<AuditLogDto>> QueryAsync(AuditQueryParams q)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.UserId))
            query = query.Where(l => l.UserId == q.UserId);

        if (!string.IsNullOrWhiteSpace(q.UserRole))
            query = query.Where(l => l.UserRole == q.UserRole);

        if (!string.IsNullOrWhiteSpace(q.HttpMethod))
            query = query.Where(l => l.HttpMethod == q.HttpMethod.ToUpper());

        if (!string.IsNullOrWhiteSpace(q.EntityType))
            query = query.Where(l => l.EntityType == q.EntityType);

        if (!string.IsNullOrWhiteSpace(q.ServiceName))
            query = query.Where(l => l.ServiceName == q.ServiceName);

        if (q.StatusCode.HasValue)
            query = query.Where(l => l.StatusCode == q.StatusCode.Value);

        if (q.From.HasValue)
            query = query.Where(l => l.Timestamp >= q.From.Value);

        if (q.To.HasValue)
            query = query.Where(l => l.Timestamp <= q.To.Value);

        var total = await query.CountAsync();

        var pageSize = Math.Max(1, q.PageSize);
        var page = Math.Max(1, q.Page);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => ToDto(l))
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AuditLogDto?> GetByIdAsync(int id)
    {
        var log = await _db.AuditLogs.FindAsync(id);
        return log == null ? null : ToDto(log);
    }

    private static AuditLogDto ToDto(AuditLog l) => new()
    {
        AuditLogId = l.AuditLogId,
        UserId = l.UserId,
        UserName = l.UserName,
        UserRole = l.UserRole,
        HttpMethod = l.HttpMethod,
        Endpoint = l.Endpoint,
        EntityType = l.EntityType,
        EntityId = l.EntityId,
        StatusCode = l.StatusCode,
        ServiceName = l.ServiceName,
        // SQL Server stores DateTime without timezone info (Kind=Unspecified).
        // Re-marking as UTC ensures System.Text.Json serialises with a "Z" suffix,
        // so Angular's date pipe converts correctly to the user's local timezone.
        Timestamp = DateTime.SpecifyKind(l.Timestamp, DateTimeKind.Utc),
        Details = l.Details
    };
}
