using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodTokenColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            IF COL_LENGTH('dbo.TbPaymentMethods', 'PaymentMethodToken') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TbPaymentMethods]
                ADD [PaymentMethodToken] nvarchar(200) NULL;
            END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.TbPaymentMethods', 'PaymentMethodToken') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[TbPaymentMethods]
                    DROP COLUMN [PaymentMethodToken];
                END
                ");
        }
    }
}
