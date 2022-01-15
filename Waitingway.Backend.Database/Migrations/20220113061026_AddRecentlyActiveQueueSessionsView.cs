using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waitingway.Backend.Database.Migrations
{
    public partial class AddRecentlyActiveQueueSessionsView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                create view ""RecentlyActiveQueueSessions"" as
                select *
                from (select distinct on (s.""Id"") s.""Id"",
                                                  s.""ClientId"",
                                                  s.""ClientSessionId"",
                                                  s.""DataCenter"",
                                                  s.""SessionType"",
                                                  s.""World"",
                                                  s.""DutyContentId"",
                                                  s.""PluginVersion"",
                                                  d.""Time"",
                                                  d.""Type"",
                                                  d.""QueuePosition"",
                                                  d.""GameTimeEstimate"",
                                                  d.""OurTimeEstimate""
                      from ""QueueSessions"" s,
                           ""QueueSessionData"" d
                      where s.""Id"" = d.""SessionId""
                        and d.""Time"" >= now() - '15 minutes'::interval
                      group by s.""Id"", d.""Time"", d.""Id""
                      order by s.""Id"", d.""Time"" desc) a
                where a.""Type"" < 2;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"drop view ""RecentlyActiveQueueSessions""");
        }
    }
}
