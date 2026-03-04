using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Materials",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Materials",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Materials");
        }
    }
}
