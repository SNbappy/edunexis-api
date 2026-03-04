using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSocialAndCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverPhotoUrl",
                table: "UserProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "UserProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GitHubUrl",
                table: "UserProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TwitterUrl",
                table: "UserProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "UserProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverPhotoUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "GitHubUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TwitterUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "UserProfiles");
        }
    }
}
