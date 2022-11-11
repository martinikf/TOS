using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TOS.Migrations
{
    public partial class GroupRenameCreatorMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_User_OwnerId",
                table: "Group");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Group",
                newName: "CreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_Group_OwnerId",
                table: "Group",
                newName: "IX_Group_CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_User_CreatorId",
                table: "Group",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_User_CreatorId",
                table: "Group");

            migrationBuilder.RenameColumn(
                name: "CreatorId",
                table: "Group",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Group_CreatorId",
                table: "Group",
                newName: "IX_Group_OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_User_OwnerId",
                table: "Group",
                column: "OwnerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
