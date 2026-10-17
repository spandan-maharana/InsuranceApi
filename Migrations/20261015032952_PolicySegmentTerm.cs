using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceApi.Migrations
{
    /// <inheritdoc />
    public partial class PolicySegmentTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies");

            migrationBuilder.AddColumn<int>(
                name: "Segment",
                table: "Policies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Term",
                table: "Policies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies",
                column: "QuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_Segment_Term",
                table: "Policies",
                columns: new[] { "Segment", "Term" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_Segment_Term",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Segment",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Term",
                table: "Policies");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies",
                column: "QuoteId");
        }
    }
}
