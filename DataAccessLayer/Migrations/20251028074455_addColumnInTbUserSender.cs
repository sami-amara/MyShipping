using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class addColumnInTbUserSender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TbShippments_TbUserSebders",
                table: "TbShippments");

            migrationBuilder.DropForeignKey(
                name: "FK_TbUserSubscriptions_TbSubscriptionPackages",
                table: "TbUserSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TbUserSebders",
                table: "TbUserSenders");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                table: "TbUserSubscriptions",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MethodEName",
                table: "TbPaymentMethods",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TbUserSenders",
                table: "TbUserSenders",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TbShippments_TbUserSenders",
                table: "TbShippments",
                column: "SenderId",
                principalTable: "TbUserSenders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TbUserSubscriptions_TbSubscriptionPackage_PackageId",
                table: "TbUserSubscriptions",
                column: "PackageId",
                principalTable: "TbSubscriptionPackage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TbShippments_TbUserSenders",
                table: "TbShippments");

            migrationBuilder.DropForeignKey(
                name: "FK_TbUserSubscriptions_TbSubscriptionPackage_PackageId",
                table: "TbUserSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TbUserSenders",
                table: "TbUserSenders");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                table: "TbUserSubscriptions",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "MethodEName",
                table: "TbPaymentMethods",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TbUserSebders",
                table: "TbUserSenders",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TbShippments_TbUserSebders",
                table: "TbShippments",
                column: "SenderId",
                principalTable: "TbUserSenders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TbUserSubscriptions_TbSubscriptionPackages",
                table: "TbUserSubscriptions",
                column: "PackageId",
                principalTable: "TbSubscriptionPackage",
                principalColumn: "Id");
        }
    }
}
