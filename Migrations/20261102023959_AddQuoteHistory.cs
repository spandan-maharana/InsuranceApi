using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccuredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteHistories_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteHistories_QuoteId",
                table: "QuoteHistories",
                column: "QuoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteHistories");
        }
    }
}
