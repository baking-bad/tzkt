using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDalAttestationRelationFromDalPublishCommitmentOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DalAttestations_DalPublishCommitmentOpsId",
                table: "DalAttestations");

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_DalPublishCommitmentOpsId",
                table: "DalAttestations",
                column: "DalPublishCommitmentOpsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DalAttestations_DalPublishCommitmentOpsId",
                table: "DalAttestations");

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestations_DalPublishCommitmentOpsId",
                table: "DalAttestations",
                column: "DalPublishCommitmentOpsId",
                unique: true);
        }
    }
}
