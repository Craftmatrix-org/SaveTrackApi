using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveTrackApi.Migrations
{
    /// <inheritdoc />
    public partial class LinkingTransactiontoAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TransactionDto_AccountID",
                table: "TransactionDto",
                column: "AccountID");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDto_Accounts_AccountID",
                table: "TransactionDto",
                column: "AccountID",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDto_Accounts_AccountID",
                table: "TransactionDto");

            migrationBuilder.DropIndex(
                name: "IX_TransactionDto_AccountID",
                table: "TransactionDto");
        }
    }
}
