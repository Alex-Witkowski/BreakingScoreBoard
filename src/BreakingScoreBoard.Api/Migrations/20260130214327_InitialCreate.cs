using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakingScoreBoard.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BattleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    JudgeCount = table.Column<int>(type: "integer", nullable: false),
                    AdminPinHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JudgePinHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RegistrationOpen = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Breakers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Breakers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxAge = table.Column<int>(type: "integer", nullable: true),
                    BracketSize = table.Column<int>(type: "integer", nullable: false),
                    CurrentPhase = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgeCategories_BattleEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "BattleEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Battles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BracketLevel = table.Column<int>(type: "integer", nullable: false),
                    BracketPosition = table.Column<int>(type: "integer", nullable: false),
                    Breaker1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Breaker2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsReBattle = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalBattleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Battles_AgeCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Battles_Battles_OriginalBattleId",
                        column: x => x.OriginalBattleId,
                        principalTable: "Battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Battles_Breakers_Breaker1Id",
                        column: x => x.Breaker1Id,
                        principalTable: "Breakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Battles_Breakers_Breaker2Id",
                        column: x => x.Breaker2Id,
                        principalTable: "Breakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Battles_Breakers_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "Breakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BreakerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_AgeCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Registrations_Breakers_BreakerId",
                        column: x => x.BreakerId,
                        principalTable: "Breakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JudgeScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleId = table.Column<Guid>(type: "uuid", nullable: false),
                    BreakerId = table.Column<Guid>(type: "uuid", nullable: false),
                    JudgeIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JudgeScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JudgeScores_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JudgeScores_Breakers_BreakerId",
                        column: x => x.BreakerId,
                        principalTable: "Breakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgeCategories_EventId",
                table: "AgeCategories",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "UX_AgeCategories_EventId_Name",
                table: "AgeCategories",
                columns: new[] { "EventId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Battles_Breaker1Id",
                table: "Battles",
                column: "Breaker1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_Breaker2Id",
                table: "Battles",
                column: "Breaker2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_CategoryId",
                table: "Battles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_CategoryId_BracketLevel",
                table: "Battles",
                columns: new[] { "CategoryId", "BracketLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_Battles_OriginalBattleId",
                table: "Battles",
                column: "OriginalBattleId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_WinnerId",
                table: "Battles",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeScores_BattleId",
                table: "JudgeScores",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeScores_BattleId_JudgeIdentifier_BreakerId",
                table: "JudgeScores",
                columns: new[] { "BattleId", "JudgeIdentifier", "BreakerId" });

            migrationBuilder.CreateIndex(
                name: "IX_JudgeScores_BreakerId",
                table: "JudgeScores",
                column: "BreakerId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_CategoryId",
                table: "Registrations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "UX_Registrations_BreakerId_CategoryId",
                table: "Registrations",
                columns: new[] { "BreakerId", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JudgeScores");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "Battles");

            migrationBuilder.DropTable(
                name: "AgeCategories");

            migrationBuilder.DropTable(
                name: "Breakers");

            migrationBuilder.DropTable(
                name: "BattleEvents");
        }
    }
}
