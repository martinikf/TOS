using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TOS.Migrations
{
    /// <inheritdoc />
    public partial class TopicGroupRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Topic_Group_GroupId",
                table: "Topic");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "Topic",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Topic_Group_GroupId",
                table: "Topic",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Topic_Group_GroupId",
                table: "Topic");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "Topic",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Topic_Group_GroupId",
                table: "Topic",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "GroupId");
        }
    }
}
