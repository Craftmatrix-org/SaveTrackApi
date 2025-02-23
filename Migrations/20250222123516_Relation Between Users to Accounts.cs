using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveTrackApi.Migrations
{
    /// <inheritdoc />
    public partial class RelationBetweenUserstoAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Uid",
                table: "Accounts",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_Uid",
                table: "Accounts",
                column: "Uid",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_Uid",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Uid",
                table: "Accounts");
        }
    }
}
