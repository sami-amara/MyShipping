using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCarrierAsForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defensive removal: drop any FK and the CarrierId column if present
            migrationBuilder.Sql(@"
            IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TbShippmentStatus_TbCarriers_CarrierId')
                ALTER TABLE [dbo].[TbShippmentStatus] DROP CONSTRAINT [FK_TbShippmentStatus_TbCarriers_CarrierId];
            IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TbShippmentStatus_TbCarriers_TbCarrierId')
                ALTER TABLE [dbo].[TbShippmentStatus] DROP CONSTRAINT [FK_TbShippmentStatus_TbCarriers_TbCarrierId];

            IF COL_LENGTH('dbo.TbShippmentStatus','CarrierId') IS NOT NULL
            BEGIN
                IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_TbShippmentStatus_CarrierId')
                    DROP INDEX IX_TbShippmentStatus_CarrierId ON dbo.TbShippmentStatus;
                ALTER TABLE dbo.TbShippmentStatus DROP COLUMN CarrierId;
            END

            IF COL_LENGTH('dbo.TbShippmentStatus','TbCarrierId') IS NOT NULL
            BEGIN
                IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_TbShippmentStatus_TbCarrierId')
                    DROP INDEX IX_TbShippmentStatus_TbCarrierId ON dbo.TbShippmentStatus;
                ALTER TABLE dbo.TbShippmentStatus DROP COLUMN TbCarrierId;
            END
            " );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: drop new FK if exists
            migrationBuilder.Sql(@"
            IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TbShippmentStatus_TbCarriers_TbCarrierId')
                ALTER TABLE [dbo].[TbShippmentStatus] DROP CONSTRAINT [FK_TbShippmentStatus_TbCarriers_TbCarrierId];
            " );

            // Rename column TbCarrierId -> CarrierId only if TbCarrierId exists and CarrierId does not
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.TbShippmentStatus','TbCarrierId') IS NOT NULL AND COL_LENGTH('dbo.TbShippmentStatus','CarrierId') IS NULL
                BEGIN
                    EXEC sp_rename 'dbo.TbShippmentStatus.TbCarrierId', 'CarrierId', 'COLUMN';
                END
                " );

            // Rename index back if exists
            migrationBuilder.Sql(@"
            IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_TbShippmentStatus_TbCarrierId')
            BEGIN
                EXEC sp_rename 'IX_TbShippmentStatus_TbCarrierId','IX_TbShippmentStatus_CarrierId','INDEX';
            END
            " );

            // Recreate old FK if column exists and old fk not present
            migrationBuilder.Sql(@"
            IF COL_LENGTH('dbo.TbShippmentStatus','CarrierId') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TbShippmentStatus_TbCarriers_CarrierId')
            BEGIN
                ALTER TABLE [dbo].[TbShippmentStatus]
                ADD CONSTRAINT [FK_TbShippmentStatus_TbCarriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [dbo].[TbCarriers]([Id]);
            END
            " );
        }
    }
}
