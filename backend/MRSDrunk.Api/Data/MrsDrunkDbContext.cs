using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Data;

public sealed class MrsDrunkDbContext(DbContextOptions<MrsDrunkDbContext> options) : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<Ventana> Ventanas => Set<Ventana>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UsuarioSesion> UsuarioSesiones => Set<UsuarioSesion>();
    public DbSet<NominaEmpleado> NominaEmpleados => Set<NominaEmpleado>();
    public DbSet<NominaPeriodo> NominaPeriodos => Set<NominaPeriodo>();
    public DbSet<NominaRegistro> NominaRegistros => Set<NominaRegistro>();
    public DbSet<NominaPeriodoDiaNoLaborado> NominaPeriodoDiasNoLaborados => Set<NominaPeriodoDiaNoLaborado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().Property(x => x.UsuarioNombre).HasColumnName("Usuario").HasMaxLength(80);
        modelBuilder.Entity<Usuario>().HasIndex(x => x.UsuarioNombre).IsUnique();
        modelBuilder.Entity<Usuario>().HasIndex(x => x.Correo).IsUnique();
        modelBuilder.Entity<UsuarioSesion>().HasIndex(x => x.SessionId).IsUnique();
        modelBuilder.Entity<Permiso>().HasIndex(x => x.Codigo).IsUnique();
        modelBuilder.Entity<RolPermiso>().HasIndex(x => new { x.RolId, x.PermisoId, x.VentanaId }).IsUnique();

        modelBuilder.Entity<Empresa>().Property(x => x.Nombre).HasMaxLength(160);
        modelBuilder.Entity<Rol>().Property(x => x.Nombre).HasMaxLength(80);
        modelBuilder.Entity<Permiso>().Property(x => x.Codigo).HasMaxLength(120);
        modelBuilder.Entity<Ventana>().Property(x => x.Ruta).HasMaxLength(160);
        modelBuilder.Entity<NominaEmpleado>().Property(x => x.ValorDiaBase).HasPrecision(18, 2);
        modelBuilder.Entity<NominaEmpleado>().Property(x => x.SalarioBase).HasPrecision(18, 2);
        modelBuilder.Entity<NominaRegistro>().Property(x => x.Valor).HasPrecision(18, 2);
        modelBuilder.Entity<NominaRegistro>().Property(x => x.Horas).HasPrecision(18, 2);
        modelBuilder.Entity<NominaRegistro>().Property(x => x.Porcentaje).HasPrecision(18, 4);
        modelBuilder.Entity<NominaRegistro>().Property(x => x.BaseCalculo).HasPrecision(18, 2);
        modelBuilder.Entity<NominaRegistro>().HasIndex(x => new { x.PeriodoId, x.EmpleadoId, x.Fecha, x.Concepto, x.CodigoNovedad }).IsUnique();
        modelBuilder.Entity<NominaPeriodoDiaNoLaborado>().HasIndex(x => new { x.PeriodoId, x.Fecha }).IsUnique();
    }
}
