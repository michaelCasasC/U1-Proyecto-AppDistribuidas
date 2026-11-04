using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPasswordHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Usuarios"
                SET "PasswordHash" = 'AQAAAAIAAYagAAAAEEGTYww6WARVHq8F/18DwXxkon95/jpLdSIWqfyroFYN7x1j+KvPOhmUrefJZwkhLw==',
                    "EsAdministrador" = TRUE
                WHERE "Email" IN ('admin@emprendemarket.com', 'admin@market.com');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
