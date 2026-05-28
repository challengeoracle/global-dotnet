using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffPay.Infrastructure.Persistence.Oracle.Migrations
{
    /// <inheritdoc />
    public partial class CriarEsquemaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DISPOSITIVO",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    IDENTIFICADOR_PUBLICO = table.Column<string>(type: "VARCHAR2(64)", nullable: false),
                    NOME = table.Column<string>(type: "VARCHAR2(120)", nullable: false),
                    COMERCIANTE_ID = table.Column<string>(type: "VARCHAR2(64)", nullable: false),
                    CHAVE_PUBLICA_PEM = table.Column<string>(type: "CLOB", nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", nullable: false),
                    DATA_REGISTRO = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    DATA_BLOQUEIO = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    MOTIVO_BLOQUEIO = table.Column<string>(type: "VARCHAR2(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DISPOSITIVO", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LOG_AUDITORIA",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    DISPOSITIVO_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    LOTE_ID = table.Column<string>(type: "VARCHAR2(64)", nullable: false),
                    TRANSACAO_ID = table.Column<string>(type: "VARCHAR2(64)", nullable: false),
                    TIMESTAMP_TRANSACAO = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TIMESTAMP_RECEBIMENTO = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    STATUS_VALIDACAO = table.Column<string>(type: "VARCHAR2(30)", nullable: false),
                    HASH_TRANSACAO = table.Column<string>(type: "VARCHAR2(64)", nullable: false),
                    HASH_ANTERIOR = table.Column<string>(type: "VARCHAR2(64)", nullable: false),
                    OBSERVACAO = table.Column<string>(type: "VARCHAR2(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOG_AUDITORIA", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LOG_AUDITORIA_DISPOSITIVO_DISPOSITIVO_ID",
                        column: x => x.DISPOSITIVO_ID,
                        principalTable: "DISPOSITIVO",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DISP_COMERCIANTE_STATUS",
                table: "DISPOSITIVO",
                columns: new[] { "COMERCIANTE_ID", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_DISP_IDENT_PUBLICO",
                table: "DISPOSITIVO",
                column: "IDENTIFICADOR_PUBLICO",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LOG_DISP_TIMESTAMP",
                table: "LOG_AUDITORIA",
                columns: new[] { "DISPOSITIVO_ID", "TIMESTAMP_TRANSACAO" });

            migrationBuilder.CreateIndex(
                name: "IX_LOG_LOTE_ID",
                table: "LOG_AUDITORIA",
                column: "LOTE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LOG_AUDITORIA");

            migrationBuilder.DropTable(
                name: "DISPOSITIVO");
        }
    }
}
