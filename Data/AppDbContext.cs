using Microsoft.EntityFrameworkCore;
using SecretParty.Models;

namespace SecretParty.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Esta será la tabla "Usuarios" en la base de datos
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // No pueden existir dos usuarios con el mismo email o nombre
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        }
    }
}