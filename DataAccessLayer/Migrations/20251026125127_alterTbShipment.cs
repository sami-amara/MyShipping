using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class alterTbShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
             name: "CarrierId",
             table: "TbShippments",
             type: "uniqueidentifier",
             nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DelivryDate",
                table: "TbShippments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2000, 1, 1)); // choose an appropriate default

            migrationBuilder.AddColumn<Guid>(
                name: "ShipingPackgingId",
                table: "TbShippments",
                type: "uniqueidentifier",
                nullable: true);

            // Optionally re-create indexes / FKs if required by your model
            migrationBuilder.CreateIndex(
                name: "IX_TbShippments_CarrierId",
                table: "TbShippments",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_TbShippments_ShipingPackgingId",
                table: "TbShippments",
                column: "ShipingPackgingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_TbShippments_CarrierId", table: "TbShippments");
            migrationBuilder.DropIndex(name: "IX_TbShippments_ShipingPackgingId", table: "TbShippments");

            migrationBuilder.DropColumn(name: "CarrierId", table: "TbShippments");
            migrationBuilder.DropColumn(name: "DelivryDate", table: "TbShippments");
            migrationBuilder.DropColumn(name: "ShipingPackgingId", table: "TbShippments");
        }
    }
}
