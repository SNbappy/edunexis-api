using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    public partial class AddCTRedesign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AverageScriptUrl",
                table: "CTEvents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "AverageStudentId",
                table: "CTEvents",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "BestScriptUrl",
                table: "CTEvents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "BestStudentId",
                table: "CTEvents",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "CTNumber",
                table: "CTEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "HeldOn",
                table: "CTEvents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CTEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WorstScriptUrl",
                table: "CTEvents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "WorstStudentId",
                table: "CTEvents",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsAbsent",         table: "CTSubmissions");
            migrationBuilder.DropColumn(name: "ObtainedMarks",    table: "CTSubmissions");
            migrationBuilder.DropColumn(name: "Remarks",          table: "CTSubmissions");
            migrationBuilder.DropColumn(name: "AverageScriptUrl", table: "CTEvents");
            migrationBuilder.DropColumn(name: "AverageStudentId", table: "CTEvents");
            migrationBuilder.DropColumn(name: "BestScriptUrl",    table: "CTEvents");
            migrationBuilder.DropColumn(name: "BestStudentId",    table: "CTEvents");
            migrationBuilder.DropColumn(name: "CTNumber",         table: "CTEvents");
            migrationBuilder.DropColumn(name: "HeldOn",           table: "CTEvents");
            migrationBuilder.DropColumn(name: "Status",           table: "CTEvents");
            migrationBuilder.DropColumn(name: "WorstScriptUrl",   table: "CTEvents");
            migrationBuilder.DropColumn(name: "WorstStudentId",   table: "CTEvents");
        }
    }
}
