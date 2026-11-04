using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto.Migrations
{
    /// <inheritdoc />
    public partial class AddPayPhoneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayPhoneClientTransactionId",
                table: "Pagos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPhonePaymentUrl",
                table: "Pagos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPhoneTransactionId",
                table: "Pagos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayPhoneClientTransactionId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "PayPhonePaymentUrl",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "PayPhoneTransactionId",
                table: "Pagos");
        }
    }
}
