using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Balance = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppState",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    Protocol = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Kind = table.Column<int>(nullable: false),
                    Address = table.Column<string>(fixedLength: true, maxLength: 36, nullable: false),
                    PublicKey = table.Column<string>(maxLength: 65, nullable: true),
                    DelegateId = table.Column<int>(nullable: true),
                    ManagerId = table.Column<int>(nullable: true),
                    Delegatable = table.Column<bool>(nullable: false),
                    Spendable = table.Column<bool>(nullable: false),
                    Staked = table.Column<bool>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    Counter = table.Column<long>(nullable: false),
                    Frozen = table.Column<long>(nullable: false),
                    StakingBalance = table.Column<long>(nullable: false),
                    DelegatorsCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Contracts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Contracts_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CycleStats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Cycle = table.Column<int>(nullable: false),
                    Snapshot = table.Column<int>(nullable: false),
                    ActiveBakers = table.Column<int>(nullable: false),
                    ActiveDelegators = table.Column<int>(nullable: false),
                    TotalBalances = table.Column<int>(nullable: false),
                    TotalRolls = table.Column<int>(nullable: false),
                    Transactions = table.Column<int>(nullable: false),
                    TransactionsVolume = table.Column<int>(nullable: false),
                    CreatedContracts = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Protocols",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Blocks = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BakerStats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Cycle = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    StakingBalance = table.Column<long>(nullable: false),
                    Blocks = table.Column<int>(nullable: false),
                    BlocksMissed = table.Column<int>(nullable: false),
                    BlocksExtra = table.Column<int>(nullable: false),
                    Endorsements = table.Column<int>(nullable: false),
                    EndorsementsMissed = table.Column<int>(nullable: false),
                    BlocksReward = table.Column<long>(nullable: false),
                    EndorsementsReward = table.Column<long>(nullable: false),
                    FeesReward = table.Column<long>(nullable: false),
                    AccusationReward = table.Column<long>(nullable: false),
                    AccusationLostDeposit = table.Column<long>(nullable: false),
                    AccusationLostReward = table.Column<long>(nullable: false),
                    AccusationLostFee = table.Column<long>(nullable: false),
                    RevelationReward = table.Column<long>(nullable: false),
                    RevelationLostReward = table.Column<long>(nullable: false),
                    RevelationLostFee = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakerStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BakerStats_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BakingRights",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Priority = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakingRights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BakingRights_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BalanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    ContractId = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BalanceSnapshots_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BalanceSnapshots_Contracts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DelegationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    Fee = table.Column<long>(nullable: false),
                    Applied = table.Column<bool>(nullable: false),
                    Internal = table.Column<bool>(nullable: false),
                    DelegateId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Contracts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Contracts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DelegatorStats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Cycle = table.Column<int>(nullable: false),
                    DelegatorId = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegatorStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegatorStats_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DelegatorStats_Contracts_DelegatorId",
                        column: x => x.DelegatorId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoubleBakingOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    BakerId = table.Column<int>(nullable: false),
                    AccusedLevel = table.Column<int>(nullable: false),
                    AccuserId = table.Column<int>(nullable: false),
                    Reward = table.Column<long>(nullable: false),
                    OffenderId = table.Column<int>(nullable: false),
                    Burned = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleBakingOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Contracts_AccuserId",
                        column: x => x.AccuserId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Contracts_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoubleEndorsingOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    BakerId = table.Column<int>(nullable: false),
                    AccusedLevel = table.Column<int>(nullable: false),
                    AccuserId = table.Column<int>(nullable: false),
                    Reward = table.Column<long>(nullable: false),
                    OffenderId = table.Column<int>(nullable: false),
                    Burned = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleEndorsingOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Contracts_AccuserId",
                        column: x => x.AccuserId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Contracts_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EndorsementOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    DelegateId = table.Column<int>(nullable: false),
                    SlotsCount = table.Column<int>(nullable: false),
                    Reward = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsementOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EndorsementOps_Contracts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EndorsingRights",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Slots = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsingRights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EndorsingRights_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NonceRevelationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    BakerId = table.Column<int>(nullable: false),
                    NonceLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonceRevelationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonceRevelationOps_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OriginationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    Fee = table.Column<long>(nullable: false),
                    Applied = table.Column<bool>(nullable: false),
                    Internal = table.Column<bool>(nullable: false),
                    ContractId = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: true),
                    ManagerId = table.Column<int>(nullable: false),
                    Delegatable = table.Column<bool>(nullable: false),
                    Spendable = table.Column<bool>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OriginationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Contracts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Contracts_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Contracts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevealOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    Fee = table.Column<long>(nullable: false),
                    Applied = table.Column<bool>(nullable: false),
                    Internal = table.Column<bool>(nullable: false),
                    PublicKey = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevealOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevealOps_Contracts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    Fee = table.Column<long>(nullable: false),
                    Applied = table.Column<bool>(nullable: false),
                    Internal = table.Column<bool>(nullable: false),
                    TargetId = table.Column<int>(nullable: false),
                    TargetAllocated = table.Column<bool>(nullable: false),
                    Amount = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Contracts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Contracts_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposalOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(nullable: true),
                    Period = table.Column<int>(nullable: false),
                    ProposalId = table.Column<int>(nullable: false),
                    SenderId = table.Column<int>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    Vote = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Contracts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Level = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(nullable: true),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    ProtocolId = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: true),
                    Priority = table.Column<int>(nullable: false),
                    Validations = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blocks_Contracts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Blocks_Protocols_ProtocolId",
                        column: x => x.ProtocolId,
                        principalTable: "Protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppState",
                columns: new[] { "Id", "Hash", "Level", "Protocol", "Timestamp" },
                values: new object[] { -1, "", -1, "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_BakerStats_BakerId",
                table: "BakerStats",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_BakingRights_BakerId",
                table: "BakingRights",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSnapshots_ContractId",
                table: "BalanceSnapshots",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSnapshots_DelegateId",
                table: "BalanceSnapshots",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_BakerId",
                table: "Blocks",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ProtocolId",
                table: "Blocks",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_Address",
                table: "Contracts",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_DelegateId",
                table: "Contracts",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ManagerId",
                table: "Contracts",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_DelegateId",
                table: "DelegationOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_SenderId",
                table: "DelegationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorStats_BakerId",
                table: "DelegatorStats",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorStats_DelegatorId",
                table: "DelegatorStats",
                column: "DelegatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleBakingOps_AccuserId",
                table: "DoubleBakingOps",
                column: "AccuserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleBakingOps_BakerId",
                table: "DoubleBakingOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleBakingOps_OffenderId",
                table: "DoubleBakingOps",
                column: "OffenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleEndorsingOps_AccuserId",
                table: "DoubleEndorsingOps",
                column: "AccuserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleEndorsingOps_BakerId",
                table: "DoubleEndorsingOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleEndorsingOps_OffenderId",
                table: "DoubleEndorsingOps",
                column: "OffenderId");

            migrationBuilder.CreateIndex(
                name: "IX_EndorsementOps_DelegateId",
                table: "EndorsementOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_EndorsingRights_BakerId",
                table: "EndorsingRights",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_NonceRevelationOps_BakerId",
                table: "NonceRevelationOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ContractId",
                table: "OriginationOps",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_DelegateId",
                table: "OriginationOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ManagerId",
                table: "OriginationOps",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_SenderId",
                table: "OriginationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_ProposalId",
                table: "ProposalOps",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_SenderId",
                table: "ProposalOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_RevealOps_SenderId",
                table: "RevealOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_SenderId",
                table: "TransactionOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId",
                table: "TransactionOps",
                column: "TargetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationOps");

            migrationBuilder.DropTable(
                name: "AppState");

            migrationBuilder.DropTable(
                name: "BakerStats");

            migrationBuilder.DropTable(
                name: "BakingRights");

            migrationBuilder.DropTable(
                name: "BalanceSnapshots");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "CycleStats");

            migrationBuilder.DropTable(
                name: "DelegationOps");

            migrationBuilder.DropTable(
                name: "DelegatorStats");

            migrationBuilder.DropTable(
                name: "DoubleBakingOps");

            migrationBuilder.DropTable(
                name: "DoubleEndorsingOps");

            migrationBuilder.DropTable(
                name: "EndorsementOps");

            migrationBuilder.DropTable(
                name: "EndorsingRights");

            migrationBuilder.DropTable(
                name: "NonceRevelationOps");

            migrationBuilder.DropTable(
                name: "OriginationOps");

            migrationBuilder.DropTable(
                name: "ProposalOps");

            migrationBuilder.DropTable(
                name: "RevealOps");

            migrationBuilder.DropTable(
                name: "TransactionOps");

            migrationBuilder.DropTable(
                name: "Protocols");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "Contracts");
        }
    }
}
