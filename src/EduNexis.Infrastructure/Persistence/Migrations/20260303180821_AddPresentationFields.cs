using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAbsent",
                table: "PresentationMarks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "PresentationMarks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PresentationEvents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DurationPerGroupMinutes",
                table: "PresentationEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "PresentationEvents",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "PresentationEvents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PresentationEvents",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "TopicsAllowed",
                table: "PresentationEvents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Venue",
                table: "PresentationEvents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAbsent",
                table: "PresentationMarks");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "PresentationMarks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "DurationPerGroupMinutes",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "TopicsAllowed",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "Venue",
                table: "PresentationEvents");
        }
    }
}
