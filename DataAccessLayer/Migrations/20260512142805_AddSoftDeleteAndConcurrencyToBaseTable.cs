using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndConcurrencyToBaseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbUserSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbUserSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbUserSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbUserSubscriptions",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbUserSenders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbUserSenders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbUserSenders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbUserSenders",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbUserReceivers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbUserReceivers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbUserReceivers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbUserReceivers",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbSubscriptionPackage",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbSubscriptionPackage",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbSubscriptionPackage",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbSubscriptionPackage",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbShippmentStatus",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbShippmentStatus",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbShippmentStatus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbShippmentStatus",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbShippments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbShippments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbShippments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbShippments",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbShippingTypes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbShippingTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbShippingTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbShippingTypes",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbShipingPackging",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbShipingPackging",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbShipingPackging",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbShipingPackging",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbSetting",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbSetting",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbSetting",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbSetting",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbRefreshTokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbRefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbRefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbRefreshTokens",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbPaymentWebhookEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbPaymentWebhookEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbPaymentWebhookEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbPaymentWebhookEvents",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbPaymentTransaction",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbPaymentTransaction",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbPaymentTransaction",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbPaymentTransaction",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbPaymentMethods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbPaymentMethods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbPaymentMethods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbPaymentMethods",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbCountries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbCountries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbCountries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbCountries",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbCities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbCities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbCities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbCities",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TbCarriers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TbCarriers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TbCarriers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TbCarriers",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbUserSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbUserSubscriptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbUserSubscriptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbUserSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbUserSenders");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbUserSenders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbUserSenders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbUserSenders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbUserReceivers");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbUserReceivers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbUserReceivers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbUserReceivers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbSubscriptionPackage");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbSubscriptionPackage");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbSubscriptionPackage");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbSubscriptionPackage");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbShippmentStatus");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbShippmentStatus");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbShippmentStatus");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbShippmentStatus");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbShippments");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbShippments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbShippments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbShippments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbShippingTypes");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbShippingTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbShippingTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbShippingTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbShipingPackging");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbShipingPackging");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbShipingPackging");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbShipingPackging");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbSetting");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbSetting");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbSetting");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbSetting");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbRefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbRefreshTokens");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbRefreshTokens");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbRefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbPaymentWebhookEvents");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbPaymentWebhookEvents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbPaymentWebhookEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbPaymentWebhookEvents");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbPaymentMethods");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbPaymentMethods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbPaymentMethods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbPaymentMethods");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbCountries");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbCountries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbCountries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbCountries");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbCities");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbCities");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbCities");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbCities");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TbCarriers");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TbCarriers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TbCarriers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TbCarriers");
        }
    }
}
