using Microsoft.EntityFrameworkCore;
using SecretParty.Models;

namespace SecretParty.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Esta será la tabla "Usuarios" en la base de datos
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<CategoriaReporte> CategoriasReporte { get; set; }
        public DbSet<MotivoReporte> MotivosReporte { get; set; }
        public DbSet<Reporte> Reportes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // No pueden existir dos usuarios con el mismo email o nombre
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();

            // Si el admin elimina un usuario, sus reportes se borran con él
            modelBuilder.Entity<Reporte>()
                .HasOne(r => r.UsuarioReportado)
                .WithMany()
                .HasForeignKey(r => r.UsuarioReportadoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Datos iniciales: la migración los inserta en la base
            modelBuilder.Entity<CategoriaReporte>().HasData(
                new CategoriaReporte { Id = 1, Nombre = "Perfil falso" },
                new CategoriaReporte { Id = 2, Nombre = "Comportamiento" },
                new CategoriaReporte { Id = 3, Nombre = "Contenido inapropiado" }
            );
            modelBuilder.Entity<MotivoReporte>().HasData(
                new MotivoReporte { Id = 1, Nombre = "Fotos robadas", CategoriaReporteId = 1 },
                new MotivoReporte { Id = 2, Nombre = "Se hace pasar por otra persona", CategoriaReporteId = 1 },
                new MotivoReporte { Id = 3, Nombre = "Parece menor de edad", CategoriaReporteId = 1 },
                new MotivoReporte { Id = 4, Nombre = "Acoso", CategoriaReporteId = 2 },
                new MotivoReporte { Id = 5, Nombre = "Lenguaje ofensivo", CategoriaReporteId = 2 },
                new MotivoReporte { Id = 6, Nombre = "No se presentó al evento", CategoriaReporteId = 2 },
                new MotivoReporte { Id = 7, Nombre = "Fotos explícitas", CategoriaReporteId = 3 },
                new MotivoReporte { Id = 8, Nombre = "Spam o publicidad", CategoriaReporteId = 3 },
                new MotivoReporte { Id = 9, Nombre = "Venta de sustancias", CategoriaReporteId = 3 }
            );
        }
    }
}