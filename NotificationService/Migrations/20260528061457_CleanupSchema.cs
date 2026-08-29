using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Migrations
{
    /// <inheritdoc />
    public partial class CleanupSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "RelatedEntityId",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "RelatedEntityType",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Notification");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Notification",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityId",
                table: "Notification",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityType",
                table: "Notification",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Notification",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }
    }
}
