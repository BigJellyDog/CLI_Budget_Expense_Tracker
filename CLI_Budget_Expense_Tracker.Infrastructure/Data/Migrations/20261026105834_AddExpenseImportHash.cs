using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLI_Budget_Expense_Tracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseImportHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportHash",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ImportHash",
                table: "Expenses",
                column: "ImportHash",
                unique: true,
                filter: "\"ImportHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expenses_ImportHash",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ImportHash",
                table: "Expenses");
        }
    }
}
