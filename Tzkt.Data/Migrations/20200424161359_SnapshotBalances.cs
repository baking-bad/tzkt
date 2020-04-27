using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class SnapshotBalances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SnapshotBalances",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    AccountId = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotBalances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotBalances_Level",
                table: "SnapshotBalances",
                column: "Level");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SnapshotBalances");
        }
    }
}
