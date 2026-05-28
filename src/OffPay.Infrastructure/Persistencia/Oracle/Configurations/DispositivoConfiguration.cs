using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OffPay.Domain.Entidades;
using OffPay.Domain.Enums;

namespace OffPay.Infrastructure.Persistencia.Oracle.Configurations;

public class DispositivoConfiguration : IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> builder)
    {
        builder.ToTable("DISPOSITIVO");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("ID")
            .HasColumnType("NUMBER(19)")
            .UseIdentityColumn();

        builder.Property(d => d.IdentificadorPublico)
            .HasColumnName("IDENTIFICADOR_PUBLICO")
            .HasColumnType("VARCHAR2(64)")
            .IsRequired();

        builder.Property(d => d.Nome)
            .HasColumnName("NOME")
            .HasColumnType("VARCHAR2(120)")
            .IsRequired();

        builder.Property(d => d.ComercianteId)
            .HasColumnName("COMERCIANTE_ID")
            .HasColumnType("VARCHAR2(64)")
            .IsRequired();

        builder.Property(d => d.ChavePublicaPem)
            .HasColumnName("CHAVE_PUBLICA_PEM")
            .HasColumnType("CLOB")
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(20)")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.DataRegistro)
            .HasColumnName("DATA_REGISTRO")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(d => d.DataBloqueio)
            .HasColumnName("DATA_BLOQUEIO")
            .HasColumnType("TIMESTAMP");

        builder.Property(d => d.MotivoBloqueio)
            .HasColumnName("MOTIVO_BLOQUEIO")
            .HasColumnType("VARCHAR2(500)");

        // Coleção de logs mapeada pelo campo privado _logs
        builder.HasMany(d => d.Logs)
            .WithOne(l => l.Dispositivo)
            .HasForeignKey(l => l.DispositivoId);

        builder.Navigation(d => d.Logs).HasField("_logs");

        builder.HasIndex(d => d.IdentificadorPublico)
            .IsUnique()
            .HasDatabaseName("IX_DISP_IDENT_PUBLICO");

        builder.HasIndex(d => new { d.ComercianteId, d.Status })
            .HasDatabaseName("IX_DISP_COMERCIANTE_STATUS");
    }
}
