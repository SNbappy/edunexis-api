using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionTurnInAndAutoZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoZero",
                table: "AssignmentSubmissions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTurnedIn",
                table: "AssignmentSubmissions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TurnedInAt",
                table: "AssignmentSubmissions",
                type: "datetime(6)",
                nullable: true);

            // Everything already in the table was submitted under the old model,
            // where attaching work *was* handing it in. Leaving these at the
            // column default of 0 would hide every existing submission from its
            // teacher the moment this ships.
            migrationBuilder.Sql(
                "UPDATE AssignmentSubmissions " +
                "SET IsTurnedIn = 1, TurnedInAt = SubmittedAt " +
                "WHERE IsTurnedIn = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoZero",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "IsTurnedIn",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "TurnedInAt",
                table: "AssignmentSubmissions");
        }
    }
}
