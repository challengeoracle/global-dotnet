using Microsoft.EntityFrameworkCore;
using OffPay.Domain.Entidades;

namespace OffPay.Infrastructure.Persistencia.Oracle;

public class OffPayDbContext : DbContext
{
    public OffPayDbContext(DbContextOptions<OffPayDbContext> options) : base(options) { }

    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OffPayDbContext).Assembly);
    }
}
