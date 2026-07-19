using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrytenAssist.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCruiseCabinPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts");

            migrationBuilder.AddColumn<string>(
                name: "CabinContextFingerprint",
                table: "CruiseSavedCriteriaAlertDetails",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CabinCriterionResult",
                table: "CruiseSavedCriteriaAlertDetails",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CabinEvidenceKey",
                table: "CruiseSavedCriteriaAlertDetails",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CabinEvidenceTime",
                table: "CruiseSavedCriteriaAlertDetails",
                type: "TEXT",
                maxLength: 35,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CabinAvailabilityEnabled",
                table: "CruiseAlertSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "CruiseCabinAvailabilityAlertDetails",
                columns: table => new
                {
                    CruiseAlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CabinType = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousState = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentState = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    ContextFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Coverage = table.Column<int>(type: "INTEGER", nullable: false),
                    StateFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EvidenceTime = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseCabinAvailabilityAlertDetails", x => x.CruiseAlertId);
                    table.CheckConstraint("CK_CruiseCabinAvailabilityDetails_CabinType", "\"CabinType\" BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_CruiseCabinAvailabilityDetails_ContextFingerprint", "length(\"ContextFingerprint\") = 64 AND \"ContextFingerprint\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseCabinAvailabilityDetails_Coverage", "\"Coverage\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_CruiseCabinAvailabilityDetails_EvidenceKey", "length(\"EvidenceKey\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseCabinAvailabilityDetails_StateFingerprint", "length(\"StateFingerprint\") = 64 AND \"StateFingerprint\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseCabinAvailabilityDetails_Transition", "(\"PreviousState\" = 2 AND \"CurrentState\" = 1 AND \"Direction\" = 0) OR (\"PreviousState\" = 1 AND \"CurrentState\" = 2 AND \"Direction\" = 1)");
                    table.ForeignKey(
                        name: "FK_CruiseCabinAvailabilityAlertDetails_CruiseAlerts_CruiseAlertId",
                        column: x => x.CruiseAlertId,
                        principalTable: "CruiseAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseCabinSeries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeriesKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: false),
                    RetailSourceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RetailSourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContextFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AdultCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ChildCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ChildAgesKnown = table.Column<bool>(type: "INTEGER", nullable: false),
                    PackageMode = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartureAirportId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CabinQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    FirstObservedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    LastSeenAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    LatestEvidenceKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LatestSourceReference = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    LatestEvidenceObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    LatestEvidenceObservedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseCabinSeries", x => x.Id);
                    table.CheckConstraint("CK_CruiseCabinSeries_Adults", "\"AdultCount\" IS NULL OR \"AdultCount\" BETWEEN 0 AND 32");
                    table.CheckConstraint("CK_CruiseCabinSeries_CabinQuantity", "\"CabinQuantity\" IS NULL OR \"CabinQuantity\" BETWEEN 1 AND 16");
                    table.CheckConstraint("CK_CruiseCabinSeries_ChildAgesKnown", "\"ChildAgesKnown\" IN (0,1)");
                    table.CheckConstraint("CK_CruiseCabinSeries_Children", "\"ChildCount\" IS NULL OR \"ChildCount\" BETWEEN 0 AND 32");
                    table.CheckConstraint("CK_CruiseCabinSeries_ContextFingerprint", "length(\"ContextFingerprint\") = 64 AND \"ContextFingerprint\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseCabinSeries_DepartureAirportId_Length", "\"DepartureAirportId\" IS NULL OR length(\"DepartureAirportId\") BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_CruiseCabinSeries_Duration", "\"DurationNights\" > 0");
                    table.CheckConstraint("CK_CruiseCabinSeries_LatestEvidenceKey_Length", "length(\"LatestEvidenceKey\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseCabinSeries_LatestSourceReference_Length", "\"LatestSourceReference\" IS NULL OR length(\"LatestSourceReference\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseCabinSeries_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseCabinSeries_PackageMode", "\"PackageMode\" BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_CruiseCabinSeries_RetailSourceId_Length", "length(\"RetailSourceId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseCabinSeries_RetailSourceName_Length", "length(\"RetailSourceName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseCabinSeries_SeriesKey", "length(\"SeriesKey\") = 64 AND \"SeriesKey\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseCabinSeries_ShipName_Length", "length(\"ShipName\") BETWEEN 1 AND 500");
                });

            migrationBuilder.CreateTable(
                name: "CruiseSavedCriteriaAlertCabins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseAlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CabinType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsMatched = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseSavedCriteriaAlertCabins", x => x.Id);
                    table.CheckConstraint("CK_CruiseSavedCriteriaAlertCabins_CabinType", "\"CabinType\" BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_CruiseSavedCriteriaAlertCabins_IsMatched", "\"IsMatched\" IN (0,1)");
                    table.ForeignKey(
                        name: "FK_CruiseSavedCriteriaAlertCabins_CruiseSavedCriteriaAlertDetails_CruiseAlertId",
                        column: x => x.CruiseAlertId,
                        principalTable: "CruiseSavedCriteriaAlertDetails",
                        principalColumn: "CruiseAlertId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseCabinContextChildAges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseCabinSeriesId = table.Column<long>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseCabinContextChildAges", x => x.Id);
                    table.CheckConstraint("CK_CruiseCabinContextChildAges_Age", "\"Age\" BETWEEN 0 AND 17");
                    table.CheckConstraint("CK_CruiseCabinContextChildAges_Order", "\"DisplayOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_CruiseCabinContextChildAges_CruiseCabinSeries_CruiseCabinSeriesId",
                        column: x => x.CruiseCabinSeriesId,
                        principalTable: "CruiseCabinSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseCabinObservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseCabinSeriesId = table.Column<long>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    StateFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Coverage = table.Column<int>(type: "INTEGER", nullable: false),
                    ObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    ObservedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseCabinObservations", x => x.Id);
                    table.CheckConstraint("CK_CruiseCabinObservations_Coverage", "\"Coverage\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_CruiseCabinObservations_EvidenceKey_Length", "length(\"EvidenceKey\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseCabinObservations_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_CruiseCabinObservations_SourceReference_Length", "\"SourceReference\" IS NULL OR length(\"SourceReference\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseCabinObservations_StateFingerprint", "length(\"StateFingerprint\") = 64 AND \"StateFingerprint\" NOT GLOB '*[^0-9a-f]*'");
                    table.ForeignKey(
                        name: "FK_CruiseCabinObservations_CruiseCabinSeries_CruiseCabinSeriesId",
                        column: x => x.CruiseCabinSeriesId,
                        principalTable: "CruiseCabinSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseCabinObservationStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseCabinObservationId = table.Column<long>(type: "INTEGER", nullable: false),
                    CabinType = table.Column<int>(type: "INTEGER", nullable: false),
                    Availability = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseCabinObservationStates", x => x.Id);
                    table.CheckConstraint("CK_CruiseCabinObservationStates_Availability", "\"Availability\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_CruiseCabinObservationStates_CabinType", "\"CabinType\" BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_CruiseCabinObservationStates_CruiseCabinObservations_CruiseCabinObservationId",
                        column: x => x.CruiseCabinObservationId,
                        principalTable: "CruiseCabinObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinContext",
                table: "CruiseSavedCriteriaAlertDetails",
                sql: "\"CabinContextFingerprint\" IS NULL OR (length(\"CabinContextFingerprint\") = 64 AND \"CabinContextFingerprint\" NOT GLOB '*[^0-9a-f]*')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinEvidence",
                table: "CruiseSavedCriteriaAlertDetails",
                sql: "(\"CabinEvidenceKey\" IS NULL AND \"CabinEvidenceTime\" IS NULL) OR (\"CabinEvidenceKey\" IS NOT NULL AND \"CabinEvidenceTime\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinEvidenceKey",
                table: "CruiseSavedCriteriaAlertDetails",
                sql: "\"CabinEvidenceKey\" IS NULL OR length(\"CabinEvidenceKey\") BETWEEN 1 AND 500");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinResult",
                table: "CruiseSavedCriteriaAlertDetails",
                sql: "\"CabinCriterionResult\" BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings",
                sql: "\"PriceDropEnabled\" IN (0,1) AND \"PromotionEnabled\" IN (0,1) AND \"SavedCriteriaEnabled\" IN (0,1) AND \"CabinAvailabilityEnabled\" IN (0,1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts",
                sql: "\"Type\" BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts",
                sql: "(\"Type\" IN (0, 1, 3) AND \"RetailSourceId\" IS NOT NULL) OR (\"Type\" = 2 AND \"RetailSourceId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "UX_CruiseCabinContextChildAges_Series_Order",
                table: "CruiseCabinContextChildAges",
                columns: new[] { "CruiseCabinSeriesId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CruiseCabinObservations_Series_Observed",
                table: "CruiseCabinObservations",
                columns: new[] { "CruiseCabinSeriesId", "ObservedAtUtcTicks", "StateFingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_CruiseCabinObservations_Series_State",
                table: "CruiseCabinObservations",
                columns: new[] { "CruiseCabinSeriesId", "StateFingerprint" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseCabinObservations_Series_Sequence",
                table: "CruiseCabinObservations",
                columns: new[] { "CruiseCabinSeriesId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruiseCabinObservationStates_Observation_Cabin",
                table: "CruiseCabinObservationStates",
                columns: new[] { "CruiseCabinObservationId", "CabinType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CruiseCabinSeries_List",
                table: "CruiseCabinSeries",
                columns: new[] { "DepartureDate", "OperatorId", "ShipName", "RetailSourceId", "ContextFingerprint" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseCabinSeries_SeriesKey",
                table: "CruiseCabinSeries",
                column: "SeriesKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruiseSavedCriteriaAlertCabins_Alert_Cabin",
                table: "CruiseSavedCriteriaAlertCabins",
                columns: new[] { "CruiseAlertId", "CabinType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CruiseCabinAvailabilityAlertDetails");

            migrationBuilder.DropTable(
                name: "CruiseCabinContextChildAges");

            migrationBuilder.DropTable(
                name: "CruiseCabinObservationStates");

            migrationBuilder.DropTable(
                name: "CruiseSavedCriteriaAlertCabins");

            migrationBuilder.DropTable(
                name: "CruiseCabinObservations");

            migrationBuilder.DropTable(
                name: "CruiseCabinSeries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinContext",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinEvidence",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinEvidenceKey",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseSavedCriteriaDetails_CabinResult",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts");

            migrationBuilder.DropColumn(
                name: "CabinContextFingerprint",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropColumn(
                name: "CabinCriterionResult",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropColumn(
                name: "CabinEvidenceKey",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropColumn(
                name: "CabinEvidenceTime",
                table: "CruiseSavedCriteriaAlertDetails");

            migrationBuilder.DropColumn(
                name: "CabinAvailabilityEnabled",
                table: "CruiseAlertSettings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings",
                sql: "\"PriceDropEnabled\" IN (0,1) AND \"PromotionEnabled\" IN (0,1) AND \"SavedCriteriaEnabled\" IN (0,1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts",
                sql: "\"Type\" BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts",
                sql: "(\"Type\" IN (0, 1) AND \"RetailSourceId\" IS NOT NULL) OR (\"Type\" = 2 AND \"RetailSourceId\" IS NULL)");
        }
    }
}
