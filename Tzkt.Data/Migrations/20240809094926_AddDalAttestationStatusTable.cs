using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDalAttestationStatusTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DalAttestationStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DalCommitmentStatusId = table.Column<int>(type: "integer", nullable: false),
                    AttestationId = table.Column<long>(type: "bigint", nullable: false),
                    Attested = table.Column<bool>(type: "boolean", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DalAttestationStatus");
        }
    }
}
