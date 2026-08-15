using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotaGrantLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "TeacherQuotas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "TeacherQuotas",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "TeacherQuotas");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "TeacherQuotas");
        }
    }
}
