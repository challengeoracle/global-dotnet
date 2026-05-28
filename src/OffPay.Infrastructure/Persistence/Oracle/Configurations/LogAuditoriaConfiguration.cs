using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OffPay.Domain.Entities;

namespace OffPay.Infrastructure.Persistence.Oracle.Configurations;

public class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> builder)
    {
        builder.ToTable("LOG_AUDITORIA");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("ID")
            .HasColumnType("NUMBER(19)")
            .UseIdentityColumn();

        builder.Property(l => l.DispositivoId)
            .HasColumnName("DISPOSITIVO_ID")
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        builder.Property(l => l.LoteId)
            .HasColumnName("LOTE_ID")
            .HasColumnType("VARCHAR2(64)")
            .IsRequired();

        builder.Property(l => l.TransacaoId)
            .HasColumnName("TRANSACAO_ID")
            .HasColumnType("VARCHAR2(64)")
            .IsRequired();

        builder.Property(l => l.TimestampTransacao)
            .HasColumnName("TIMESTAMP_TRANSACAO")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(l => l.TimestampRecebimento)
            .HasColumnName("TIMESTAMP_RECEBIMENTO")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(l => l.StatusValidacao)
            .HasColumnName("STATUS_VALIDACAO")
            .HasColumnType("VARCHAR2(30)")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(l => l.HashTransacao)
            .HasColumnName("HASH_TRANSACAO")
            .HasColumnType("VARCHAR2(64)")
            .IsRequired();

        builder.Property(l => l.HashAnterior)
            .HasColumnName("HASH_ANTERIOR")
            .HasColumnType("VARCHAR2(64)")
            .IsRequired();

        builder.Property(l => l.Observacao)
            .HasColumnName("OBSERVACAO")
            .HasColumnType("VARCHAR2(500)");

        // FK configurada na entidade Dispositivo — apenas navigation aqui
        builder.HasOne(l => l.Dispositivo)
            .WithMany(d => d.Logs)
            .HasForeignKey(l => l.DispositivoId);

        builder.HasIndex(l => new { l.DispositivoId, l.TimestampTransacao })
            .HasDatabaseName("IX_LOG_DISP_TIMESTAMP");

        builder.HasIndex(l => l.LoteId)
            .HasDatabaseName("IX_LOG_LOTE_ID");
    }
}
