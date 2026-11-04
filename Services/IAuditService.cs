namespace Proyecto.Services;

public interface IAuditService
{
    Task LogAsync(string accion, string? entidad = null, int? entidadId = null, int? usuarioId = null, string? detalles = null);
    Task<List<AuditEntry>> GetRecentAsync(int count = 100);
}

public class AuditEntry
{
    public int Id { get; set; }
    public string Accion { get; set; } = "";
    public string? Entidad { get; set; }
    public int? EntidadId { get; set; }
    public int? UsuarioId { get; set; }
    public string? Detalles { get; set; }
    public DateTime Fecha { get; set; }
}
