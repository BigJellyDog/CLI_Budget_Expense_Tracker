using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLI_Budget_Expense_Tracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRemovedImportHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemovedImportHashes",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemovedImportHashes", x => x.Hash);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemovedImportHashes");
        }
    }
}
