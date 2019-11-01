using Microsoft.EntityFrameworkCore.Migrations;

namespace HeatApp.Migrations
{
    public partial class AddLocked : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Locked",
                table: "Valves",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Timers_TimeTableId",
                table: "Timers",
                column: "TimeTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timers_Timetables_TimeTableId",
                table: "Timers",
                column: "TimeTableId",
                principalTable: "Timetables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timers_Timetables_TimeTableId",
                table: "Timers");

            migrationBuilder.DropIndex(
                name: "IX_Timers_TimeTableId",
                table: "Timers");

            migrationBuilder.DropColumn(
                name: "Locked",
                table: "Valves");
        }
    }
}
