using Microsoft.EntityFrameworkCore.Migrations;

namespace GwendolineBot.Migrations
{
    public partial class RepeatingReminders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRepeating",
                table: "Reminders",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRepeating",
                table: "Reminders");
        }
    }
}
