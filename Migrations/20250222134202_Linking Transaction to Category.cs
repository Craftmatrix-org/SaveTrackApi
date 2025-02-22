using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveTrackApi.Migrations
{
    /// <inheritdoc />
    public partial class LinkingTransactiontoCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TransactionDto_CategoryID",
                table: "TransactionDto",
                column: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDto_CategoryDto_CategoryID",
                table: "TransactionDto",
                column: "CategoryID",
                principalTable: "CategoryDto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDto_CategoryDto_CategoryID",
                table: "TransactionDto");

            migrationBuilder.DropIndex(
                name: "IX_TransactionDto_CategoryID",
                table: "TransactionDto");
        }
    }
}
