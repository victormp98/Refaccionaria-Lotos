using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RefaccionariaWeb.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaScrap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compatibilidades_Productos_ProductoId",
                table: "Compatibilidades");

            migrationBuilder.DropForeignKey(
                name: "FK_Compatibilidades_Vehiculos_VehiculoId",
                table: "Compatibilidades");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoId",
                table: "DetallesPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Productos_ProductoId",
                table: "DetallesPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_AspNetUsers_ClienteId",
                table: "Pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehiculos",
                table: "Vehiculos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Productos",
                table: "Productos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pedidos",
                table: "Pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DetallesPedido",
                table: "DetallesPedido");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Compatibilidades",
                table: "Compatibilidades");

            migrationBuilder.RenameTable(
                name: "Vehiculos",
                newName: "vehiculos");

            migrationBuilder.RenameTable(
                name: "Productos",
                newName: "productos");

            migrationBuilder.RenameTable(
                name: "Pedidos",
                newName: "pedidos");

            migrationBuilder.RenameTable(
                name: "DetallesPedido",
                newName: "detallespedido");

            migrationBuilder.RenameTable(
                name: "Compatibilidades",
                newName: "compatibilidades");

            migrationBuilder.RenameIndex(
                name: "IX_Pedidos_ClienteId",
                table: "pedidos",
                newName: "IX_pedidos_ClienteId");

            migrationBuilder.RenameIndex(
                name: "IX_DetallesPedido_ProductoId",
                table: "detallespedido",
                newName: "IX_detallespedido_ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_DetallesPedido_PedidoId",
                table: "detallespedido",
                newName: "IX_detallespedido_PedidoId");

            migrationBuilder.RenameIndex(
                name: "IX_Compatibilidades_VehiculoId",
                table: "compatibilidades",
                newName: "IX_compatibilidades_VehiculoId");

            migrationBuilder.RenameIndex(
                name: "IX_Compatibilidades_ProductoId",
                table: "compatibilidades",
                newName: "IX_compatibilidades_ProductoId");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEnvio",
                table: "pedidos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroGuia",
                table: "pedidos",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Paqueteria",
                table: "pedidos",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TipoEntrega",
                table: "pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_vehiculos",
                table: "vehiculos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_productos",
                table: "productos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_pedidos",
                table: "pedidos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_detallespedido",
                table: "detallespedido",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_compatibilidades",
                table: "compatibilidades",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "scraps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaRegistro = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scraps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scraps_productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SucursalConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NombreTienda = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direccion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ciudad = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CP = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SucursalConfig", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_scraps_ProductoId",
                table: "scraps",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_compatibilidades_productos_ProductoId",
                table: "compatibilidades",
                column: "ProductoId",
                principalTable: "productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_compatibilidades_vehiculos_VehiculoId",
                table: "compatibilidades",
                column: "VehiculoId",
                principalTable: "vehiculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_detallespedido_pedidos_PedidoId",
                table: "detallespedido",
                column: "PedidoId",
                principalTable: "pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_detallespedido_productos_ProductoId",
                table: "detallespedido",
                column: "ProductoId",
                principalTable: "productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pedidos_AspNetUsers_ClienteId",
                table: "pedidos",
                column: "ClienteId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compatibilidades_productos_ProductoId",
                table: "compatibilidades");

            migrationBuilder.DropForeignKey(
                name: "FK_compatibilidades_vehiculos_VehiculoId",
                table: "compatibilidades");

            migrationBuilder.DropForeignKey(
                name: "FK_detallespedido_pedidos_PedidoId",
                table: "detallespedido");

            migrationBuilder.DropForeignKey(
                name: "FK_detallespedido_productos_ProductoId",
                table: "detallespedido");

            migrationBuilder.DropForeignKey(
                name: "FK_pedidos_AspNetUsers_ClienteId",
                table: "pedidos");

            migrationBuilder.DropTable(
                name: "scraps");

            migrationBuilder.DropTable(
                name: "SucursalConfig");

            migrationBuilder.DropPrimaryKey(
                name: "PK_vehiculos",
                table: "vehiculos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_productos",
                table: "productos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_pedidos",
                table: "pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_detallespedido",
                table: "detallespedido");

            migrationBuilder.DropPrimaryKey(
                name: "PK_compatibilidades",
                table: "compatibilidades");

            migrationBuilder.DropColumn(
                name: "FechaEnvio",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "NumeroGuia",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "Paqueteria",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "TipoEntrega",
                table: "pedidos");

            migrationBuilder.RenameTable(
                name: "vehiculos",
                newName: "Vehiculos");

            migrationBuilder.RenameTable(
                name: "productos",
                newName: "Productos");

            migrationBuilder.RenameTable(
                name: "pedidos",
                newName: "Pedidos");

            migrationBuilder.RenameTable(
                name: "detallespedido",
                newName: "DetallesPedido");

            migrationBuilder.RenameTable(
                name: "compatibilidades",
                newName: "Compatibilidades");

            migrationBuilder.RenameIndex(
                name: "IX_pedidos_ClienteId",
                table: "Pedidos",
                newName: "IX_Pedidos_ClienteId");

            migrationBuilder.RenameIndex(
                name: "IX_detallespedido_ProductoId",
                table: "DetallesPedido",
                newName: "IX_DetallesPedido_ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_detallespedido_PedidoId",
                table: "DetallesPedido",
                newName: "IX_DetallesPedido_PedidoId");

            migrationBuilder.RenameIndex(
                name: "IX_compatibilidades_VehiculoId",
                table: "Compatibilidades",
                newName: "IX_Compatibilidades_VehiculoId");

            migrationBuilder.RenameIndex(
                name: "IX_compatibilidades_ProductoId",
                table: "Compatibilidades",
                newName: "IX_Compatibilidades_ProductoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehiculos",
                table: "Vehiculos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Productos",
                table: "Productos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pedidos",
                table: "Pedidos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DetallesPedido",
                table: "DetallesPedido",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Compatibilidades",
                table: "Compatibilidades",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Compatibilidades_Productos_ProductoId",
                table: "Compatibilidades",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Compatibilidades_Vehiculos_VehiculoId",
                table: "Compatibilidades",
                column: "VehiculoId",
                principalTable: "Vehiculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoId",
                table: "DetallesPedido",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Productos_ProductoId",
                table: "DetallesPedido",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_AspNetUsers_ClienteId",
                table: "Pedidos",
                column: "ClienteId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
