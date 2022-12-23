using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TOS.Migrations
{
    /// <inheritdoc />
    public partial class EngFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionLongEng",
                table: "Topic",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionShortEng",
                table: "Topic",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEng",
                table: "Topic",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEng",
                table: "Programme",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEng",
                table: "Group",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEng",
                table: "Attachment",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionLongEng",
                table: "Topic");

            migrationBuilder.DropColumn(
                name: "DescriptionShortEng",
                table: "Topic");

            migrationBuilder.DropColumn(
                name: "NameEng",
                table: "Topic");

            migrationBuilder.DropColumn(
                name: "NameEng",
                table: "Programme");

            migrationBuilder.DropColumn(
                name: "NameEng",
                table: "Group");

            migrationBuilder.DropColumn(
                name: "NameEng",
                table: "Attachment");
        }
    }
}
