using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceApi.Migrations
{
    /// <inheritdoc />
    public partial class QuotesAndPoliciesToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Policies");

            migrationBuilder.AddColumn<string>(
                name: "Answers",
                table: "Quotes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PremiumBreakdown",
                table: "Quotes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Quotes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Policies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ProductId",
                table: "Quotes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_ProductId",
                table: "Policies",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Policies_Products_ProductId",
                table: "Policies",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Products_ProductId",
                table: "Quotes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Policies_Products_ProductId",
                table: "Policies");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Products_ProductId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_ProductId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Policies_ProductId",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Answers",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PremiumBreakdown",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Policies");

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Quotes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "Quotes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
