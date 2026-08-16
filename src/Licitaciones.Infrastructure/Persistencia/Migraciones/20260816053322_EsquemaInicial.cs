using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Licitaciones.Infrastructure.Persistencia.Migraciones;

/// <inheritdoc />
public partial class EsquemaInicial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "licitaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                codigo_normalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                estado = table.Column<int>(type: "integer", nullable: false),
                presupuesto_estimado_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                fecha_cierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_licitaciones", x => x.id);
                table.CheckConstraint("ck_licitaciones_presupuesto_positivo", "presupuesto_estimado_crc > 0");
            });

        migrationBuilder.CreateTable(
            name: "niveles_aprobacion",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                monto_minimo_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                monto_maximo_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                aprobador = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_niveles_aprobacion", x => x.id);
                table.CheckConstraint("ck_niveles_rango_valido", "monto_minimo_crc > 0 AND (monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc)");
            });

        migrationBuilder.CreateTable(
            name: "proveedores",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                nombre_normalizado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_proveedores", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tipos_cambio",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                crc_por_usd = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                fecha_vigencia = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                activo = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tipos_cambio", x => x.id);
                table.CheckConstraint("ck_tipos_cambio_tasa_positiva", "crc_por_usd > 0");
            });

        migrationBuilder.CreateTable(
            name: "ofertas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                licitacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                monto_ofertado_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                fecha_registro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ofertas", x => x.id);
                table.CheckConstraint("ck_ofertas_monto_positivo", "monto_ofertado_crc > 0");
                table.ForeignKey(
                    name: "FK_ofertas_licitaciones_licitacion_id",
                    column: x => x.licitacion_id,
                    principalTable: "licitaciones",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ofertas_proveedores_proveedor_id",
                    column: x => x.proveedor_id,
                    principalTable: "proveedores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "niveles_aprobacion",
            columns: new[] { "id", "aprobador", "created_at", "monto_maximo_crc", "monto_minimo_crc", "updated_at" },
            values: new object[,]
            {
                { new Guid("6f2b1c4a-0000-4000-8000-000000000001"), "Encargado de área", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 999999.99m, 0.01m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                { new Guid("6f2b1c4a-0000-4000-8000-000000000002"), "Gerencia", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 9999999.99m, 1000000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                { new Guid("6f2b1c4a-0000-4000-8000-000000000003"), "Junta Directiva", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10000000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
            });

        migrationBuilder.InsertData(
            table: "tipos_cambio",
            columns: new[] { "id", "activo", "crc_por_usd", "created_at", "fecha_vigencia", "updated_at" },
            values: new object[] { new Guid("7a3c2d5b-0000-4000-8000-000000000001"), true, 512.0000m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

        migrationBuilder.CreateIndex(
            name: "ix_licitaciones_codigo_normalizado",
            table: "licitaciones",
            column: "codigo_normalizado",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_licitaciones_estado_fecha_cierre",
            table: "licitaciones",
            columns: new[] { "estado", "fecha_cierre" });

        migrationBuilder.CreateIndex(
            name: "ix_niveles_aprobacion_monto_minimo",
            table: "niveles_aprobacion",
            column: "monto_minimo_crc");

        migrationBuilder.CreateIndex(
            name: "ix_ofertas_licitacion_proveedor",
            table: "ofertas",
            columns: new[] { "licitacion_id", "proveedor_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_ofertas_mejor_oferta",
            table: "ofertas",
            columns: new[] { "licitacion_id", "monto_ofertado_crc", "fecha_registro" });

        migrationBuilder.CreateIndex(
            name: "IX_ofertas_proveedor_id",
            table: "ofertas",
            column: "proveedor_id");

        migrationBuilder.CreateIndex(
            name: "ix_proveedores_nombre_normalizado",
            table: "proveedores",
            column: "nombre_normalizado",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_tipos_cambio_unico_activo",
            table: "tipos_cambio",
            column: "activo",
            unique: true,
            filter: "activo = true");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "niveles_aprobacion");

        migrationBuilder.DropTable(
            name: "ofertas");

        migrationBuilder.DropTable(
            name: "tipos_cambio");

        migrationBuilder.DropTable(
            name: "licitaciones");

        migrationBuilder.DropTable(
            name: "proveedores");
    }
}
