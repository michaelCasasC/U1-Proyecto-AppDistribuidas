using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;

namespace Proyecto.Data
{
    /// <summary>
    /// DbContext de la aplicación.  Hereda de IdentityDbContext con clave primaria int
    /// para que EF Core cree las tablas estándar de ASP.NET Core Identity.
    /// </summary>
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Las tablas de Identity (AspNetUsers, AspNetRoles, etc.) las genera la clase base.
        // Aquí solo exponemos las entidades propias de negocio.
        public DbSet<Emprendimiento> Emprendimientos { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<DetallePedido> DetallesPedido { get; set; } = null!;
        public DbSet<Pago> Pagos { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<EmailLog> EmailLogs { get; set; } = null!;
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Debe llamarse primero para que Identity configure sus entidades.
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Emprendimientos)
                .WithOne(e => e.Usuario)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Pedidos)
                .WithOne(p => p.Usuario)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Emprendimiento>()
                .HasMany(e => e.Productos)
                .WithOne(p => p.Emprendimiento)
                .HasForeignKey(p => p.EmprendimientoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.Productos)
                .WithOne(p => p.Categoria)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Detalles)
                .WithOne(d => d.Pedido)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Producto>()
                .HasMany(p => p.DetallesPedido)
                .WithOne(d => d.Producto)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Nombre);

            modelBuilder.Entity<Categoria>()
                .HasIndex(c => c.Nombre);

            // Global query filters: excluir registros eliminados (soft delete)
            modelBuilder.Entity<ApplicationUser>().HasQueryFilter(e => !e.FechaEliminacion.HasValue);
            modelBuilder.Entity<Categoria>().HasQueryFilter(e => !e.FechaEliminacion.HasValue);
            modelBuilder.Entity<DetallePedido>().HasQueryFilter(e => !e.FechaEliminacion.HasValue);
            modelBuilder.Entity<Emprendimiento>().HasQueryFilter(e => !e.FechaEliminacion.HasValue);
            modelBuilder.Entity<Pedido>().HasQueryFilter(e => !e.FechaEliminacion.HasValue);
            modelBuilder.Entity<Producto>().HasQueryFilter(e => !e.FechaEliminacion.HasValue);

            modelBuilder.Entity<Pago>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Monto).HasColumnType("decimal(18,2)");
                entity.Property(p => p.PayPalOrderId).HasMaxLength(100);
                entity.Property(p => p.Estado).HasMaxLength(50);
                entity.Property(p => p.Moneda).HasMaxLength(10);
                entity.Property(p => p.PayPalPayerId).HasMaxLength(500);
                entity.Property(p => p.PayPalPaymentId).HasMaxLength(1000);
                entity.Property(p => p.DetallesJson).HasMaxLength(2000);
                entity.HasOne(p => p.Pedido)
                    .WithMany(ped => ped.Pagos)
                    .HasForeignKey(p => p.PedidoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // =========================
        // SEED DATA + GENERADOR MASIVO
        // =========================
        public static async Task SeedData(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            // Si ya hay pedidos, los eliminamos para regenerar el millón de registros.
            if (await context.Pedidos.AnyAsync())
            {
                context.DetallesPedido.RemoveRange(context.DetallesPedido);
                context.Pedidos.RemoveRange(context.Pedidos);
                await context.SaveChangesAsync();
            }

            // =====================
            // ROLES
            // =====================
            string[] roleNames = { "Administrador", "Emprendedor", "Cliente" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // =====================
            // USUARIOS
            // =====================
            async Task<ApplicationUser> CreateUserIfNotExists(
                string email, string nombre, string password,
                bool esAdmin, bool esEmprendedor, bool esCliente,
                string[] roles)
            {
                var existing = await userManager.FindByEmailAsync(email);
                if (existing != null) return existing;

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    Nombre = nombre,
                    EsCliente = esCliente,
                    EsEmprendedor = esEmprendedor,
                    EsAdministrador = esAdmin,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new Exception($"Error creando usuario {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRolesAsync(user, roles);
                return user;
            }

            var admin = await CreateUserIfNotExists(
                "admin@emprendemarket.com", "Admin", "Admin123!",
                esAdmin: true, esEmprendedor: true, esCliente: true,
                new[] { "Administrador", "Emprendedor", "Cliente" });

            var juan = await CreateUserIfNotExists(
                "juan@gmail.com", "Juan Perez", "Juan123!",
                esAdmin: false, esEmprendedor: false, esCliente: true,
                new[] { "Cliente" });

            var maria = await CreateUserIfNotExists(
                "maria@emprendedora.com", "Maria Garcia", "Maria123!",
                esAdmin: false, esEmprendedor: true, esCliente: true,
                new[] { "Emprendedor", "Cliente" });

            // =====================
            // CATEGORÍAS
            // =====================
            var cat1 = new Categoria { Nombre = "Artesanías", Activo = true };
            var cat2 = new Categoria { Nombre = "Comida", Activo = true };
            var cat3 = new Categoria { Nombre = "Ropa", Activo = true };

            await context.Categorias.AddRangeAsync(cat1, cat2, cat3);
            await context.SaveChangesAsync();

            // =====================
            // EMPRENDIMIENTOS
            // =====================
            var emp1 = new Emprendimiento
            {
                Nombre = "Tejidos Maria",
                UsuarioId = maria.Id,
                Activo = true
            };

            var emp2 = new Emprendimiento
            {
                Nombre = "Dulces Tentaciones",
                UsuarioId = admin.Id,
                Activo = true
            };

            await context.Emprendimientos.AddRangeAsync(emp1, emp2);
            await context.SaveChangesAsync();

            // =====================
            // PRODUCTOS
            // =====================
            var p1 = new Producto
            {
                Nombre = "Saco Lana",
                Precio = 25.50m,
                Stock = 10,
                CategoriaId = cat3.Id,
                EmprendimientoId = emp1.Id,
                Activo = true
            };

            var p2 = new Producto
            {
                Nombre = "Bufanda",
                Precio = 12.00m,
                Stock = 20,
                CategoriaId = cat3.Id,
                EmprendimientoId = emp1.Id,
                Activo = true
            };

            var p3 = new Producto
            {
                Nombre = "Torta Chocolate",
                Precio = 18.00m,
                Stock = 5,
                CategoriaId = cat2.Id,
                EmprendimientoId = emp2.Id,
                Activo = true
            };

            await context.Productos.AddRangeAsync(p1, p2, p3);
            await context.SaveChangesAsync();
        }
    }
}
