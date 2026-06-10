using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "TbRefreshTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "TbRefreshTokens",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "TbRefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedReason",
                table: "TbRefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TbRefreshTokens_Token",
                table: "TbRefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TbRefreshTokens_UserId",
                table: "TbRefreshTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TbRefreshTokens_AspNetUsers_UserId",
                table: "TbRefreshTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TbRefreshTokens_AspNetUsers_UserId",
                table: "TbRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_TbRefreshTokens_Token",
                table: "TbRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_TbRefreshTokens_UserId",
                table: "TbRefreshTokens");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "TbRefreshTokens");

            migrationBuilder.DropColumn(
                name: "RevokedReason",
                table: "TbRefreshTokens");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "TbRefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "TbRefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);
        }
    }
}
