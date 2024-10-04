using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDalSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps");

            migrationBuilder.AddColumn<int>(
                name: "DalAttestationLag",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalAttestationThreshold",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalShardsPerSlot",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalSlotsPerLevel",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Attested",
                table: "DalPublishCommitmentOps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ShardsAttested",
                table: "DalPublishCommitmentOps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DalAttestations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DalPublishCommitmentOpsId = table.Column<long>(type: "bigint", nullable: false),
                    AttestationId = table.Column<long>(type: "bigint", nullable: false),
                    Attested = table.Column<bool>(type: "boolean", nullable: false),
                    ShardsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DalAttestations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DalAttestations_DalPublishCommitmentOps_DalPublishCommitmen~",
                        column: x => x.DalPublishCommitmentOpsId,
                        principalTable: "DalPublishCommitmentOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DalAttestations_EndorsementOps_AttestationId",
                        column: x => x.AttestationId,
                        principalTable: "EndorsementOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DalRights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    DelegateId = table.Column<int>(type: "integer", nullable: false),
                    Shards = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DalRights", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps",
                column: "TargetId",
                filter: "\"Entrypoint\" = 'transfer'\nAND \"TokenTransfers\" IS NULL\nAND \"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_AttestationId",
                table: "DalAttestations",
                column: "AttestationId");

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_DalPublishCommitmentOpsId",
                table: "DalAttestations",
                column: "DalPublishCommitmentOpsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DalAttestations");

            migrationBuilder.DropTable(
                name: "DalRights");

            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "DalAttestationLag",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "DalAttestationThreshold",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "DalShardsPerSlot",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "DalSlotsPerLevel",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "Attested",
                table: "DalPublishCommitmentOps");

            migrationBuilder.DropColumn(
                name: "ShardsAttested",
                table: "DalPublishCommitmentOps");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps",
                column: "TargetId",
                filter: "\"Entrypoint\" = 'transfer'\r\nAND \"TokenTransfers\" IS NULL\r\nAND \"Status\" = 1");
        }
    }
}
