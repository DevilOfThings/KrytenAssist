using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrytenAssist.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCruiseAlertPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CruiseAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: false),
                    RetailSourceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RetailSourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EventTime = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    EventTimeUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    CreatedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseAlerts", x => x.Id);
                    table.CheckConstraint("CK_CruiseAlerts_Duration", "\"DurationNights\" > 0");
                    table.CheckConstraint("CK_CruiseAlerts_EventKey", "length(\"EventKey\") = 64 AND \"EventKey\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseAlerts_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseAlerts_RetailSourceId_Length", "\"RetailSourceId\" IS NULL OR length(\"RetailSourceId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseAlerts_RetailSourceName_Length", "\"RetailSourceName\" IS NULL OR length(\"RetailSourceName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseAlerts_ShipName_Length", "length(\"ShipName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseAlerts_SourcePair", "(\"RetailSourceId\" IS NULL AND \"RetailSourceName\" IS NULL) OR (\"RetailSourceId\" IS NOT NULL AND \"RetailSourceName\" IS NOT NULL)");
                    table.CheckConstraint("CK_CruiseAlerts_Status", "\"Status\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_CruiseAlerts_Type", "\"Type\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_CruiseAlerts_TypeSource", "(\"Type\" IN (0, 1) AND \"RetailSourceId\" IS NOT NULL) OR (\"Type\" = 2 AND \"RetailSourceId\" IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "CruiseAlertSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PriceDropEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PromotionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SavedCriteriaEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumPriceDropPercentage = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseAlertSettings", x => x.Id);
                    table.CheckConstraint("CK_CruiseAlertSettings_Booleans", "\"PriceDropEnabled\" IN (0,1) AND \"PromotionEnabled\" IN (0,1) AND \"SavedCriteriaEnabled\" IN (0,1)");
                    table.CheckConstraint("CK_CruiseAlertSettings_Percentage", "CAST(\"MinimumPriceDropPercentage\" AS REAL) BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_CruiseAlertSettings_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "SavedCruiseCriteriaEvaluationStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: false),
                    CriteriaFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    EvidenceTime = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    EvidenceTimeUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCruiseCriteriaEvaluationStates", x => x.Id);
                    table.CheckConstraint("CK_SavedCriteriaStates_Duration", "\"DurationNights\" > 0");
                    table.CheckConstraint("CK_SavedCriteriaStates_Required", "length(\"OperatorId\") BETWEEN 1 AND 200 AND length(\"ShipName\") BETWEEN 1 AND 500 AND length(\"CriteriaFingerprint\") BETWEEN 1 AND 128 AND length(\"EvidenceKey\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_SavedCriteriaStates_Result", "\"Result\" BETWEEN 0 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "CruisePriceDropAlertDetails",
                columns: table => new
                {
                    CruiseAlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreviousAmount = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PreviousCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PreviousBasis = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CurrentAmount = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CurrentCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CurrentBasis = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Reduction = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PercentageReduction = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruisePriceDropAlertDetails", x => x.CruiseAlertId);
                    table.CheckConstraint("CK_CruisePriceDropDetails_Amounts", "CAST(\"PreviousAmount\" AS REAL) >= 0 AND CAST(\"CurrentAmount\" AS REAL) >= 0 AND CAST(\"Reduction\" AS REAL) > 0");
                    table.CheckConstraint("CK_CruisePriceDropDetails_CurrentBasis_Length", "\"CurrentBasis\" IS NULL OR length(\"CurrentBasis\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruisePriceDropDetails_CurrentCurrency", "length(\"CurrentCurrency\") = 3 AND \"CurrentCurrency\" GLOB '[A-Z][A-Z][A-Z]'");
                    table.CheckConstraint("CK_CruisePriceDropDetails_EvidenceKey_Length", "length(\"EvidenceKey\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruisePriceDropDetails_Percentage", "CAST(\"PercentageReduction\" AS REAL) BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_CruisePriceDropDetails_PreviousBasis_Length", "\"PreviousBasis\" IS NULL OR length(\"PreviousBasis\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruisePriceDropDetails_PreviousCurrency", "length(\"PreviousCurrency\") = 3 AND \"PreviousCurrency\" GLOB '[A-Z][A-Z][A-Z]'");
                    table.ForeignKey(
                        name: "FK_CruisePriceDropAlertDetails_CruiseAlerts_CruiseAlertId",
                        column: x => x.CruiseAlertId,
                        principalTable: "CruiseAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruisePromotionAlertDetails",
                columns: table => new
                {
                    CruiseAlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreviousSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CurrentSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruisePromotionAlertDetails", x => x.CruiseAlertId);
                    table.CheckConstraint("CK_CruisePromotionDetails_CurrentSummary_Length", "length(\"CurrentSummary\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruisePromotionDetails_EvidenceKey_Length", "length(\"EvidenceKey\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruisePromotionDetails_PreviousSummary_Length", "\"PreviousSummary\" IS NULL OR length(\"PreviousSummary\") BETWEEN 1 AND 4000");
                    table.ForeignKey(
                        name: "FK_CruisePromotionAlertDetails_CruiseAlerts_CruiseAlertId",
                        column: x => x.CruiseAlertId,
                        principalTable: "CruiseAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseSavedCriteriaAlertDetails",
                columns: table => new
                {
                    CruiseAlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonthConfiguredAndMatched = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfiguredBudgetAmount = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ConfiguredBudgetCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    ConfiguredBudgetBasis = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchedPriceAmount = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    MatchedPriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    MatchedPriceBasis = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CriteriaFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EvidenceOrigin = table.Column<int>(type: "INTEGER", nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    EvidenceTime = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    CabinPreferencesUnavailable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseSavedCriteriaAlertDetails", x => x.CruiseAlertId);
                    table.CheckConstraint("CK_CruiseSavedCriteriaDetails_Amounts", "(\"ConfiguredBudgetAmount\" IS NULL OR CAST(\"ConfiguredBudgetAmount\" AS REAL) >= 0) AND (\"MatchedPriceAmount\" IS NULL OR CAST(\"MatchedPriceAmount\" AS REAL) >= 0)");
                    table.CheckConstraint("CK_CruiseSavedCriteriaDetails_Booleans", "\"MonthConfiguredAndMatched\" IN (0,1) AND \"CabinPreferencesUnavailable\" IN (0,1)");
                    table.CheckConstraint("CK_CruiseSavedCriteriaDetails_BudgetBasis", "\"ConfiguredBudgetBasis\" IS NULL OR \"ConfiguredBudgetBasis\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_CruiseSavedCriteriaDetails_BudgetTuple", "(\"ConfiguredBudgetAmount\" IS NULL AND \"ConfiguredBudgetCurrency\" IS NULL AND \"ConfiguredBudgetBasis\" IS NULL AND \"MatchedPriceAmount\" IS NULL AND \"MatchedPriceCurrency\" IS NULL AND \"MatchedPriceBasis\" IS NULL) OR (\"ConfiguredBudgetAmount\" IS NOT NULL AND \"ConfiguredBudgetCurrency\" IS NOT NULL AND \"ConfiguredBudgetBasis\" IS NOT NULL AND \"MatchedPriceAmount\" IS NOT NULL AND \"MatchedPriceCurrency\" IS NOT NULL)");
                    table.CheckConstraint("CK_CruiseSavedCriteriaDetails_Currencies", "(\"ConfiguredBudgetCurrency\" IS NULL OR (length(\"ConfiguredBudgetCurrency\") = 3 AND \"ConfiguredBudgetCurrency\" GLOB '[A-Z][A-Z][A-Z]')) AND (\"MatchedPriceCurrency\" IS NULL OR (length(\"MatchedPriceCurrency\") = 3 AND \"MatchedPriceCurrency\" GLOB '[A-Z][A-Z][A-Z]'))");
                    table.CheckConstraint("CK_CruiseSavedCriteriaDetails_Origin", "\"EvidenceOrigin\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_CruiseSavedCriteriaDetails_Required", "length(\"CriteriaFingerprint\") BETWEEN 1 AND 128 AND length(\"EvidenceKey\") BETWEEN 1 AND 4000");
                    table.ForeignKey(
                        name: "FK_CruiseSavedCriteriaAlertDetails_CruiseAlerts_CruiseAlertId",
                        column: x => x.CruiseAlertId,
                        principalTable: "CruiseAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CruiseAlerts_Newest",
                table: "CruiseAlerts",
                columns: new[] { "EventTimeUtcTicks", "CreatedAtUtcTicks", "EventKey" });

            migrationBuilder.CreateIndex(
                name: "IX_CruiseAlerts_Status_Type_Event",
                table: "CruiseAlerts",
                columns: new[] { "Status", "Type", "EventTimeUtcTicks" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseAlerts_EventKey",
                table: "CruiseAlerts",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SavedCriteriaStates_Sailing_Fingerprint",
                table: "SavedCruiseCriteriaEvaluationStates",
                columns: new[] { "OperatorId", "ShipName", "DepartureDate", "DurationNights", "CriteriaFingerprint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CruiseAlertSettings");

            migrationBuilder.DropTable(
                name: "CruisePriceDropAlertDetails");

            migrationBuilder.DropTable(
                name: "CruisePromotionAlertDetails");

            migrationBuilder.DropTable(
                name: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropTable(
                name: "SavedCruiseCriteriaEvaluationStates");

            migrationBuilder.DropTable(
                name: "CruiseAlerts");
        }
    }
}
