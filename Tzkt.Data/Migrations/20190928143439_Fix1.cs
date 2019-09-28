using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class Fix1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetAllocated",
                table: "TransactionOps");

            migrationBuilder.AddColumn<byte>(
                name: "InternalOperations",
                table: "TransactionOps",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Nonce",
                table: "TransactionOps",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Nonce",
                table: "OriginationOps",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Nonce",
                table: "DelegationOps",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternalOperations",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "Nonce",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "Nonce",
                table: "OriginationOps");

            migrationBuilder.DropColumn(
                name: "Nonce",
                table: "DelegationOps");

            migrationBuilder.AddColumn<bool>(
                name: "TargetAllocated",
                table: "TransactionOps",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
