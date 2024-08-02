using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDalAttestation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps");

            migrationBuilder.AddColumn<BigInteger>(
                name: "DalAttestation",
                table: "EndorsementOps",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps",
                column: "TargetId",
                filter: "\"Entrypoint\" = 'transfer'\nAND \"TokenTransfers\" IS NULL\nAND \"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "DalAttestation",
                table: "EndorsementOps");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps",
                column: "TargetId",
                filter: "\"Entrypoint\" = 'transfer'\r\nAND \"TokenTransfers\" IS NULL\r\nAND \"Status\" = 1");
        }
    }
}
