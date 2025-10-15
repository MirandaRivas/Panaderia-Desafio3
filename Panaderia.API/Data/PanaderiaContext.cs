using Microsoft.EntityFrameworkCore;
using Panaderia.API.Models;

namespace Panaderia.API.Data
{
    public class PanaderiaContext : DbContext
    {
        public PanaderiaContext(DbContextOptions<PanaderiaContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar relaciones
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.UsuarioId);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Venta)
                .WithMany(v => v.DetallesVenta)
                .HasForeignKey(d => d.VentaId);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesVenta)
                .HasForeignKey(d => d.ProductoId);

            // Datos de prueba
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario { Id = 1, Email = "admin@panaderia.com", Password = "admin123", Rol = "Admin" },
                new Usuario { Id = 2, Email = "vendedor@panaderia.com", Password = "vendedor123", Rol = "Vendedor" }
            );

            modelBuilder.Entity<Producto>().HasData(
                new Producto { Id = 1, Nombre = "Pan Francés", Precio = 0.25m, Stock = 100, Categoria = "Pan" },
                new Producto { Id = 2, Nombre = "Pan Dulce", Precio = 0.50m, Stock = 50, Categoria = "Pan" },
                new Producto { Id = 3, Nombre = "Pastel de Chocolate", Precio = 15.00m, Stock = 10, Categoria = "Pasteles" }
            );
        }
    }
}