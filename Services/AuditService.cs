using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;

namespace Proyecto.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string accion, string? entidad = null, int? entidadId = null, int? usuarioId = null, string? detalles = null)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        var log = new AuditLog
        {
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            UsuarioId = usuarioId,
            Detalles = detalles,
            IpAddress = ip,
            Fecha = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditEntry>> GetRecentAsync(int count = 100)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.Fecha)
            .Take(count)
            .Select(a => new AuditEntry
            {
                Id = a.Id,
                Accion = a.Accion,
                Entidad = a.Entidad,
                EntidadId = a.EntidadId,
                UsuarioId = a.UsuarioId,
                Detalles = a.Detalles,
                Fecha = a.Fecha
            })
            .ToListAsync();
    }
}
