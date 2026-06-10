using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTbShippmentStatusesNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use safe SQL checks to avoid failing when the constraint/index/column
            // does not exist in the target database (prevents "is not a constraint" errors).
            migrationBuilder.Sql(@"
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TbShippmentStatus_TbCarriers_TbCarrierId')
            BEGIN
                ALTER TABLE [dbo].[TbShippmentStatus] DROP CONSTRAINT [FK_TbShippmentStatus_TbCarriers_TbCarrierId];
            END
            ");

            migrationBuilder.Sql(@"
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TbShippmentStatus_TbCarrierId' AND object_id = OBJECT_ID('dbo.TbShippmentStatus'))
            BEGIN
                DROP INDEX [IX_TbShippmentStatus_TbCarrierId] ON [dbo].[TbShippmentStatus];
            END
            ");

            migrationBuilder.Sql(@"
            IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TbCarrierId' AND Object_ID = Object_ID(N'dbo.TbShippmentStatus'))
            BEGIN
                ALTER TABLE [dbo].[TbShippmentStatus] DROP COLUMN [TbCarrierId];
            END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
