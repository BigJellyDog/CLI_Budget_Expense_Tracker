using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLI_Budget_Expense_Tracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "RecurringExpenses",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "RecurringExpenses");
        }
    }
}
