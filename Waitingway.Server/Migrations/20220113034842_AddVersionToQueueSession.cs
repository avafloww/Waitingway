using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waitingway.Server.Migrations
{
    public partial class AddVersionToQueueSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PluginVersion",
                table: "QueueSessions",
                type: "text",
                nullable: false,
                defaultValue: "1.0.0.0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PluginVersion",
                table: "QueueSessions");
        }
    }
}
