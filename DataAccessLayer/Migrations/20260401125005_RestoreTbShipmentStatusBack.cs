using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RestoreTbShipmentStatusBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TbCarrierId",
                table: "TbShippmentStatus",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TbShippmentStatus_TbCarrierId",
                table: "TbShippmentStatus",
                column: "TbCarrierId");

            migrationBuilder.AddForeignKey(
                name: "FK_TbShippmentStatus_TbCarriers_TbCarrierId",
                table: "TbShippmentStatus",
                column: "TbCarrierId",
                principalTable: "TbCarriers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TbShippmentStatus_TbCarriers_TbCarrierId",
                table: "TbShippmentStatus");

            migrationBuilder.DropIndex(
                name: "IX_TbShippmentStatus_TbCarrierId",
                table: "TbShippmentStatus");

            migrationBuilder.DropColumn(
                name: "TbCarrierId",
                table: "TbShippmentStatus");
        }
    }
}
