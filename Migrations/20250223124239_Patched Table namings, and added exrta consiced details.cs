using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveTrackApi.Migrations
{
    /// <inheritdoc />
    public partial class PatchedTablenamingsandaddedexrtaconsiceddetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetItemDto_Budgets_BudgetID",
                table: "BudgetItemDto");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetItemDto_Users_UserID",
                table: "BudgetItemDto");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryDto_Users_UserID",
                table: "CategoryDto");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDto_Accounts_AccountID",
                table: "TransactionDto");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDto_CategoryDto_CategoryID",
                table: "TransactionDto");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDto_Users_UserID",
                table: "TransactionDto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionDto",
                table: "TransactionDto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryDto",
                table: "CategoryDto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BudgetItemDto",
                table: "BudgetItemDto");

            migrationBuilder.RenameTable(
                name: "TransactionDto",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "CategoryDto",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "BudgetItemDto",
                newName: "BudgetItems");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionDto_UserID",
                table: "Transactions",
                newName: "IX_Transactions_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionDto_CategoryID",
                table: "Transactions",
                newName: "IX_Transactions_CategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionDto_AccountID",
                table: "Transactions",
                newName: "IX_Transactions_AccountID");

            migrationBuilder.RenameIndex(
                name: "IX_CategoryDto_UserID",
                table: "Categories",
                newName: "IX_Categories_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetItemDto_UserID",
                table: "BudgetItems",
                newName: "IX_BudgetItems_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetItemDto_BudgetID",
                table: "BudgetItems",
                newName: "IX_BudgetItems_BudgetID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BudgetItems",
                table: "BudgetItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetItems_Budgets_BudgetID",
                table: "BudgetItems",
                column: "BudgetID",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetItems_Users_UserID",
                table: "BudgetItems",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_UserID",
                table: "Categories",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Accounts_AccountID",
                table: "Transactions",
                column: "AccountID",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Categories_CategoryID",
                table: "Transactions",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserID",
                table: "Transactions",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetItems_Budgets_BudgetID",
                table: "BudgetItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetItems_Users_UserID",
                table: "BudgetItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Users_UserID",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Accounts_AccountID",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Categories_CategoryID",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserID",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BudgetItems",
                table: "BudgetItems");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "TransactionDto");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "CategoryDto");

            migrationBuilder.RenameTable(
                name: "BudgetItems",
                newName: "BudgetItemDto");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_UserID",
                table: "TransactionDto",
                newName: "IX_TransactionDto_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_CategoryID",
                table: "TransactionDto",
                newName: "IX_TransactionDto_CategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_AccountID",
                table: "TransactionDto",
                newName: "IX_TransactionDto_AccountID");

            migrationBuilder.RenameIndex(
                name: "IX_Categories_UserID",
                table: "CategoryDto",
                newName: "IX_CategoryDto_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetItems_UserID",
                table: "BudgetItemDto",
                newName: "IX_BudgetItemDto_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetItems_BudgetID",
                table: "BudgetItemDto",
                newName: "IX_BudgetItemDto_BudgetID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionDto",
                table: "TransactionDto",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryDto",
                table: "CategoryDto",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BudgetItemDto",
                table: "BudgetItemDto",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetItemDto_Budgets_BudgetID",
                table: "BudgetItemDto",
                column: "BudgetID",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetItemDto_Users_UserID",
                table: "BudgetItemDto",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryDto_Users_UserID",
                table: "CategoryDto",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDto_Accounts_AccountID",
                table: "TransactionDto",
                column: "AccountID",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDto_CategoryDto_CategoryID",
                table: "TransactionDto",
                column: "CategoryID",
                principalTable: "CategoryDto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDto_Users_UserID",
                table: "TransactionDto",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
