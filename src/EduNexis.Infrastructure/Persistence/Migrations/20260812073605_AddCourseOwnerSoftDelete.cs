using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseOwnerSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPdfPublic",
                table: "UserPublications",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PdfPublicId",
                table: "UserPublications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "PdfSizeBytes",
                table: "UserPublications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PdfUploadedAt",
                table: "UserPublications",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfUrl",
                table: "UserPublications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublicProfile",
                table: "UserProfiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PublicSlug",
                table: "UserProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "PresentationEvents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastPublishedMarksHash",
                table: "PresentationEvents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "PresentationEvents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedByOwnerAt",
                table: "Courses",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletedByOwner",
                table: "Courses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPdfPublic",
                table: "UserPublications");

            migrationBuilder.DropColumn(
                name: "PdfPublicId",
                table: "UserPublications");

            migrationBuilder.DropColumn(
                name: "PdfSizeBytes",
                table: "UserPublications");

            migrationBuilder.DropColumn(
                name: "PdfUploadedAt",
                table: "UserPublications");

            migrationBuilder.DropColumn(
                name: "PdfUrl",
                table: "UserPublications");

            migrationBuilder.DropColumn(
                name: "IsPublicProfile",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "PublicSlug",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "LastPublishedMarksHash",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "PresentationEvents");

            migrationBuilder.DropColumn(
                name: "DeletedByOwnerAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsDeletedByOwner",
                table: "Courses");
        }
    }
}
