using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TOS.Migrations
{
    public partial class TopicDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Topic",
                newName: "DescriptionLong");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTime",
                table: "UserInterestedTopic",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DescriptionShort",
                table: "Topic",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateTime",
                table: "UserInterestedTopic");

            migrationBuilder.DropColumn(
                name: "DescriptionShort",
                table: "Topic");

            migrationBuilder.RenameColumn(
                name: "DescriptionLong",
                table: "Topic",
                newName: "Description");
        }
    }
}
