using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeDalCommitmentStatusInDalPublishCommitmentOps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DalAttestations_DalCommitmentStatus_DalCommitmentStatusId",
                table: "DalAttestations");

            migrationBuilder.DropTable(
                name: "DalCommitmentStatus");

            migrationBuilder.DropIndex(
                name: "IX_DalAttestations_DalCommitmentStatusId",
                table: "DalAttestations");

            migrationBuilder.DropColumn(
                name: "DalCommitmentStatusId",
                table: "DalAttestations");

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

            migrationBuilder.AddColumn<long>(
                name: "DalPublishCommitmentOpsId",
                table: "DalAttestations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_DalPublishCommitmentOpsId",
                table: "DalAttestations",
                column: "DalPublishCommitmentOpsId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DalAttestations_DalPublishCommitmentOps_DalPublishCommitmen~",
                table: "DalAttestations",
                column: "DalPublishCommitmentOpsId",
                principalTable: "DalPublishCommitmentOps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DalAttestations_DalPublishCommitmentOps_DalPublishCommitmen~",
                table: "DalAttestations");

            migrationBuilder.DropIndex(
                name: "IX_DalAttestations_DalPublishCommitmentOpsId",
                table: "DalAttestations");

            migrationBuilder.DropColumn(
                name: "Attested",
                table: "DalPublishCommitmentOps");

            migrationBuilder.DropColumn(
                name: "ShardsAttested",
                table: "DalPublishCommitmentOps");

            migrationBuilder.DropColumn(
                name: "DalPublishCommitmentOpsId",
                table: "DalAttestations");

            migrationBuilder.AddColumn<int>(
                name: "DalCommitmentStatusId",
                table: "DalAttestations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DalCommitmentStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublishmentId = table.Column<long>(type: "bigint", nullable: false),
                    Attested = table.Column<bool>(type: "boolean", nullable: false),
                    ShardsAttested = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DalCommitmentStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DalCommitmentStatus_DalPublishCommitmentOps_PublishmentId",
                        column: x => x.PublishmentId,
                        principalTable: "DalPublishCommitmentOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_DalCommitmentStatusId",
                table: "DalAttestations",
                column: "DalCommitmentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DalCommitmentStatus_PublishmentId",
                table: "DalCommitmentStatus",
                column: "PublishmentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DalAttestations_DalCommitmentStatus_DalCommitmentStatusId",
                table: "DalAttestations",
                column: "DalCommitmentStatusId",
                principalTable: "DalCommitmentStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
