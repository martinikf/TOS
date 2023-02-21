using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TOS.Migrations
{
    /// <inheritdoc />
    public partial class NotificationSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NameEng",
                table: "Notifications",
                newName: "SubjectEng");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subject",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "SubjectEng",
                table: "Notifications",
                newName: "NameEng");
        }
    }
}
