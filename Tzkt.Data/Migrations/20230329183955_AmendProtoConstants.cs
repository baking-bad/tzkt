using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class AmendProtoConstants : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Protocols"
                SET "MaxBakingReward" = "BlockReward0" + "EndorsersPerBlock" / 3 * "BlockReward1"
                WHERE "Hash" = 'PtMumbai2TmsJHNGRkD8v8YDbtao7BLUC3wjASn1inAKLFCjaH1';
                
                UPDATE "Protocols"
                SET "MaxEndorsingReward" = "EndorsersPerBlock" * "EndorsementReward0"
                WHERE "Hash" = 'PtMumbai2TmsJHNGRkD8v8YDbtao7BLUC3wjASn1inAKLFCjaH1';

                UPDATE "BakerCycles"
                SET "FutureBlockRewards" = updates.value
                FROM (
                    SELECT
                        bc."Id" AS id,
                        bc."FutureBlocks" * p."MaxBakingReward" AS value
                    FROM "BakerCycles" AS bc
                    INNER JOIN "Protocols" AS p
                    ON p."Hash" = 'PtMumbai2TmsJHNGRkD8v8YDbtao7BLUC3wjASn1inAKLFCjaH1'
                    WHERE bc."Cycle" >= p."FirstCycle"
                ) updates
                WHERE "Id" = updates.id;
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
