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
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Solicitud> Solicitudes { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<Traza> Trazas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
                        // Índices para buscar rápido las trazas de una petición y las más recientes
            modelBuilder.Entity<Traza>().HasIndex(t => t.Peticion);
            modelBuilder.Entity<Traza>().HasIndex(t => t.Fecha);
            // No pueden existir dos usuarios con el mismo email o nombre
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();

            // Si el admin elimina un usuario, sus reportes se borran con él
            modelBuilder.Entity<Reporte>()
                .HasOne(r => r.UsuarioReportado)
                .WithMany()
                .HasForeignKey(r => r.UsuarioReportadoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Si se borra un usuario, se borran los eventos que organizaba
            modelBuilder.Entity<Evento>()
                .HasOne(e => e.Anfitrion)
                .WithMany()
                .HasForeignKey(e => e.AnfitrionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Si se borra un evento, se borran sus solicitudes
            modelBuilder.Entity<Solicitud>()
                .HasOne(s => s.Evento)
                .WithMany(e => e.Solicitudes)
                .HasForeignKey(s => s.EventoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Si se borra un usuario, se borran sus solicitudes
            modelBuilder.Entity<Solicitud>()
                .HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Una persona solo puede pedir UNA vez entrar al mismo evento
            modelBuilder.Entity<Solicitud>()
                .HasIndex(s => new { s.EventoId, s.UsuarioId })
                .IsUnique();

            // Mensajes: dos claves foráneas a la MISMA tabla (Usuarios), por eso hay que decir cuál es cuál.
            // Si se borra cualquiera de los dos usuarios, sus mensajes se borran.
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Remitente)
                .WithMany()
                .HasForeignKey(m => m.RemitenteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Destinatario)
                .WithMany()
                .HasForeignKey(m => m.DestinatarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Si se borra el evento, el mensaje se queda pero ya no apunta a ningún evento (null)
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Evento)
                .WithMany()
                .HasForeignKey(m => m.EventoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Índice para que buscar "mis mensajes" sea rápido
            modelBuilder.Entity<Mensaje>().HasIndex(m => new { m.DestinatarioId, m.Leido });

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