using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class TokensSupply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                update "Tokens" set "TotalMinted" = ss."Sum" from (select s.* from (select tr."TokenId", sum(tr."Amount"::numeric) as "Sum" from "TokenTransfers" as tr where "FromId" is null group by "TokenId") as s inner join "Tokens" as t on t."Id" = s."TokenId" where s."Sum" != t."TotalMinted"::numeric) as ss where "Id" = ss."TokenId";
                update "Tokens" set "TotalBurned" = ss."Sum" from (select s.* from (select tr."TokenId", sum(tr."Amount"::numeric) as "Sum" from "TokenTransfers" as tr where "ToId" is null group by "TokenId") as s inner join "Tokens" as t on t."Id" = s."TokenId" where s."Sum" != t."TotalBurned"::numeric) as ss where "Id" = ss."TokenId";
                update "Tokens" set "TotalSupply" = "TotalMinted"::numeric - "TotalBurned"::numeric where "TotalMinted"::numeric - "TotalBurned"::numeric != "TotalSupply"::numeric;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do nothing
        }
    }
}
