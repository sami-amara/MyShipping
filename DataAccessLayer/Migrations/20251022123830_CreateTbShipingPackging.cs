using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class CreateTbShipingPackging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
              name: "TbShipingPackging",
              columns: table => new
              {
                  Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                  CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                  CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                  CurrentState = table.Column<int>(type: "int", nullable: false),
                  TbShipingPackginAname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                  TbShipingPackginEname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                  UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                  UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
              },
              constraints: table =>
              {
                  table.PrimaryKey("PK_TbShipingPackging", x => x.Id);
              });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
             name: "TbShipingPackging");
        }
    }
}
