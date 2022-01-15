using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Waitingway.Backend.Database.Migrations
{
    public partial class InitialStructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueueSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientSessionId = table.Column<string>(type: "text", nullable: false),
                    DataCenter = table.Column<int>(type: "integer", nullable: false),
                    SessionType = table.Column<int>(type: "integer", nullable: false),
                    World = table.Column<int>(type: "integer", nullable: true),
                    DutyContentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueueSessionData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    EndReason = table.Column<int>(type: "integer", nullable: true),
                    QueuePosition = table.Column<long>(type: "bigint", nullable: true),
                    GameTimeEstimate = table.Column<TimeSpan>(type: "interval", nullable: true),
                    OurTimeEstimate = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueSessionData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueSessionData_QueueSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "QueueSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueSessionData_SessionId",
                table: "QueueSessionData",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueSessionData_Time_Type",
                table: "QueueSessionData",
                columns: new[] { "Time", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueSessions_DataCenter_World",
                table: "QueueSessions",
                columns: new[] { "DataCenter", "World" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueSessionData");

            migrationBuilder.DropTable(
                name: "QueueSessions");
        }
    }
}
