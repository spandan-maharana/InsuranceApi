using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverageEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverageBreakdown",
                table: "QuoteRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCoverage",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CoverageStep",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CoverageRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    MatchValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ToValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Multiplier = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverageRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoverageRules_ProductQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "ProductQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoverageRules_QuestionId",
                table: "CoverageRules",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoverageRules");

            migrationBuilder.DropColumn(
                name: "CoverageBreakdown",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "BaseCoverage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CoverageStep",
                table: "Products");
        }
    }
}
