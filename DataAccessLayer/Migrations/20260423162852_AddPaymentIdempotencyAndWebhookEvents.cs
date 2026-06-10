using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentIdempotencyAndWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "TbPaymentTransaction",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderEventId",
                table: "TbPaymentTransaction",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "TbPaymentTransaction",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TbPaymentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderEventId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbPaymentWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TbPaymentTransaction_IdempotencyKey",
                table: "TbPaymentTransaction",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_TbPaymentTransaction_ProviderEventId",
                table: "TbPaymentTransaction",
                column: "ProviderEventId");

            migrationBuilder.CreateIndex(
                name: "IX_TbPaymentWebhookEvents_Provider_EventId",
                table: "TbPaymentWebhookEvents",
                columns: new[] { "ProviderName", "ProviderEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TbPaymentWebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_TbPaymentTransaction_IdempotencyKey",
                table: "TbPaymentTransaction");

            migrationBuilder.DropIndex(
                name: "IX_TbPaymentTransaction_ProviderEventId",
                table: "TbPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "TbPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "ProviderEventId",
                table: "TbPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "TbPaymentTransaction");
        }
    }
}
