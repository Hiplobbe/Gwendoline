using Microsoft.EntityFrameworkCore.Migrations;

namespace GwendolineBot.Migrations
{
    public partial class ReminderText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Reminders",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message",
                table: "Reminders");
        }
    }
}
