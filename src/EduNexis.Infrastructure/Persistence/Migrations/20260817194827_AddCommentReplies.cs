using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduNexis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentCommentId",
                table: "AssignmentComments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCommentId",
                table: "AnnouncementComments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentComments_ParentCommentId",
                table: "AssignmentComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementComments_ParentCommentId",
                table: "AnnouncementComments",
                column: "ParentCommentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssignmentComments_ParentCommentId",
                table: "AssignmentComments");

            migrationBuilder.DropIndex(
                name: "IX_AnnouncementComments_ParentCommentId",
                table: "AnnouncementComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "AssignmentComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "AnnouncementComments");
        }
    }
}
