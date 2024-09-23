using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameDalAttestationStatusToDalAttestation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DalAttestationStatus");

            migrationBuilder.CreateTable(
                name: "DalAttestations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DalCommitmentStatusId = table.Column<int>(type: "integer", nullable: false),
                    AttestationId = table.Column<long>(type: "bigint", nullable: false),
                    Attested = table.Column<bool>(type: "boolean", nullable: false),
                    ShardsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DalAttestations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DalAttestations_DalCommitmentStatus_DalCommitmentStatusId",
                        column: x => x.DalCommitmentStatusId,
                        principalTable: "DalCommitmentStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DalAttestations_EndorsementOps_AttestationId",
                        column: x => x.AttestationId,
                        principalTable: "EndorsementOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_AttestationId",
                table: "DalAttestations",
                column: "AttestationId");

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_DalCommitmentStatusId",
                table: "DalAttestations",
                column: "DalCommitmentStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DalAttestations");

            migrationBuilder.CreateTable(
                name: "DalAttestationStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttestationId = table.Column<long>(type: "bigint", nullable: false),
                    DalCommitmentStatusId = table.Column<int>(type: "integer", nullable: false),
                    Attested = table.Column<bool>(type: "boolean", nullable: false),
                    ShardsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DalAttestationStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DalAttestationStatus_DalCommitmentStatus_DalCommitmentStatu~",
                        column: x => x.DalCommitmentStatusId,
                        principalTable: "DalCommitmentStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DalAttestationStatus_EndorsementOps_AttestationId",
                        column: x => x.AttestationId,
                        principalTable: "EndorsementOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestationStatus_AttestationId",
                table: "DalAttestationStatus",
                column: "AttestationId");

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestationStatus_DalCommitmentStatusId",
                table: "DalAttestationStatus",
                column: "DalCommitmentStatusId");
        }
    }
}
