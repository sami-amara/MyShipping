using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodTokenAndConsolidateMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodToken",
                table: "TbPaymentMethods",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TbPaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CurrentState", "PaymentMethodToken" },
                values: new object[] { 0, null });

            migrationBuilder.UpdateData(
                table: "TbPaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "PaymentMethodToken",
                value: null);

            migrationBuilder.UpdateData(
                table: "TbPaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "Commission", "CreatedBy", "CreatedDate", "CurrentState", "MethdAName", "MethodEName", "PaymentMethodToken", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0.0, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "فيزا", "Visa", "pm_card_visa", null, null });

            migrationBuilder.UpdateData(
                table: "TbPaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "Commission", "CreatedBy", "CreatedDate", "CurrentState", "MethdAName", "MethodEName", "PaymentMethodToken", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0.0, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "ماستركارد", "MasterCard", "pm_card_mastercard", null, null });

            migrationBuilder.UpdateData(
                table: "TbPaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "Commission", "CreatedBy", "CreatedDate", "CurrentState", "MethdAName", "MethodEName", "PaymentMethodToken", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0.0, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "أمريكان إكسبريس", "American Express", "pm_card_amex", null, null });

            migrationBuilder.UpdateData(
                table: "TbPaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "Commission", "CreatedBy", "CreatedDate", "CurrentState", "MethdAName", "MethodEName", "PaymentMethodToken", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 0.0, new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "ديسكفر", "Discover", "pm_card_discover", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TbPaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CurrentState",
                value: 1);

            migrationBuilder.DropColumn(
                name: "PaymentMethodToken",
                table: "TbPaymentMethods");
        }
    }
}
