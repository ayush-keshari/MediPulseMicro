using AuditService.DTOs;

namespace AuditService.Services;

public interface IAuditLogService
{
    Task<bool> CreateAsync(CreateAuditLogRequest request);
    Task<PagedResult<AuditLogDto>> QueryAsync(AuditQueryParams query);
    Task<AuditLogDto?> GetByIdAsync(int id);
}
