using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class alterTbUserSender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
              name: "Contact",
              table: "TbUserSenders",
              type: "nvarchar(max)",
              nullable: false,
              defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "TbUserSenders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherAddress",
                table: "TbUserSenders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "TbUserSenders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
               name: "Contact",
               table: "TbUserSenders");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "TbUserSenders");

            migrationBuilder.DropColumn(
                name: "OtherAddress",
                table: "TbUserSenders");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "TbUserSenders");
        }
    }
}
