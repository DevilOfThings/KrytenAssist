using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrytenAssist.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCruiseDiscoveryPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CruiseDiscoveryScopes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScopeFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RetailSourceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RetailSourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Surface = table.Column<int>(type: "INTEGER", nullable: false),
                    CaptureContractVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstCheckedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    FirstCheckedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    LastCheckedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    LastCheckedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseDiscoveryScopes", x => x.Id);
                    table.CheckConstraint("CK_CruiseDiscoveryScopeEntity_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseDiscoveryScopeEntity_RetailSourceId_Length", "length(\"RetailSourceId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseDiscoveryScopeEntity_RetailSourceName_Length", "length(\"RetailSourceName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseDiscoveryScopeEntity_ScopeFingerprint", "length(\"ScopeFingerprint\") = 64 AND \"ScopeFingerprint\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseDiscoveryScopes_Surface", "\"Surface\" = 0");
                    table.CheckConstraint("CK_CruiseDiscoveryScopes_Time", "\"FirstCheckedAtUtcTicks\" <= \"LastCheckedAtUtcTicks\"");
                    table.CheckConstraint("CK_CruiseDiscoveryScopes_Version", "\"CaptureContractVersion\" BETWEEN 1 AND 1000");
                });

            migrationBuilder.CreateTable(
                name: "CruiseDiscoveryChecks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseDiscoveryScopeId = table.Column<long>(type: "INTEGER", nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    ObservedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    WasTruncated = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcceptedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseDiscoveryChecks", x => x.Id);
                    table.CheckConstraint("CK_CruiseDiscoveryCheckEntity_EvidenceKey", "length(\"EvidenceKey\") = 64 AND \"EvidenceKey\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseDiscoveryChecks_Accepted", "\"AcceptedCount\" BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_CruiseDiscoveryChecks_Rejected", "\"RejectedCount\" BETWEEN 0 AND 10");
                    table.CheckConstraint("CK_CruiseDiscoveryChecks_Truncated", "\"WasTruncated\" IN (0,1)");
                    table.ForeignKey(
                        name: "FK_CruiseDiscoveryChecks_CruiseDiscoveryScopes_CruiseDiscoveryScopeId",
                        column: x => x.CruiseDiscoveryScopeId,
                        principalTable: "CruiseDiscoveryScopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseDiscoveryScopeCriteria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseDiscoveryScopeId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseDiscoveryScopeCriteria", x => x.Id);
                    table.CheckConstraint("CK_CruiseDiscoveryScopeCriteria_State", "\"State\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_CruiseDiscoveryScopeCriterionEntity_Name_Length", "length(\"Name\") BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_CruiseDiscoveryScopeCriteria_CruiseDiscoveryScopes_CruiseDiscoveryScopeId",
                        column: x => x.CruiseDiscoveryScopeId,
                        principalTable: "CruiseDiscoveryScopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseDiscoveryOccurrences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseDiscoveryCheckId = table.Column<long>(type: "INTEGER", nullable: false),
                    CatalogueKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OccurrenceFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProviderItineraryId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RetailSourceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RetailSourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: true),
                    DeparturePort = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ItinerarySummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ProviderOfferId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    ObservedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    EvidenceKey = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseDiscoveryOccurrences", x => x.Id);
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_CatalogueKey", "length(\"CatalogueKey\") = 64 AND \"CatalogueKey\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_DeparturePort_Length", "\"DeparturePort\" IS NULL OR length(\"DeparturePort\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_EvidenceKey_Length", "length(\"EvidenceKey\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_ItinerarySummary_Length", "\"ItinerarySummary\" IS NULL OR length(\"ItinerarySummary\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_OccurrenceFingerprint", "length(\"OccurrenceFingerprint\") = 64 AND \"OccurrenceFingerprint\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_ProviderItineraryId_Length", "length(\"ProviderItineraryId\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_ProviderOfferId_Length", "\"ProviderOfferId\" IS NULL OR length(\"ProviderOfferId\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_RetailSourceId_Length", "length(\"RetailSourceId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_RetailSourceName_Length", "length(\"RetailSourceName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_ShipName_Length", "\"ShipName\" IS NULL OR length(\"ShipName\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_SourceReference_Length", "\"SourceReference\" IS NULL OR length(\"SourceReference\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrenceEntity_Title_Length", "\"Title\" IS NULL OR length(\"Title\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseDiscoveryOccurrences_Duration", "\"DurationNights\" IS NULL OR \"DurationNights\" BETWEEN 1 AND 365");
                    table.ForeignKey(
                        name: "FK_CruiseDiscoveryOccurrences_CruiseDiscoveryChecks_CruiseDiscoveryCheckId",
                        column: x => x.CruiseDiscoveryCheckId,
                        principalTable: "CruiseDiscoveryChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseDiscoveryRejections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseDiscoveryCheckId = table.Column<long>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CandidateKey = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseDiscoveryRejections", x => x.Id);
                    table.CheckConstraint("CK_CruiseDiscoveryRejectionEntity_CandidateKey_Length", "length(\"CandidateKey\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseDiscoveryRejectionEntity_Reason_Length", "length(\"Reason\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseDiscoveryRejections_Order", "\"DisplayOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_CruiseDiscoveryRejections_CruiseDiscoveryChecks_CruiseDiscoveryCheckId",
                        column: x => x.CruiseDiscoveryCheckId,
                        principalTable: "CruiseDiscoveryChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseDiscoveryScopeCriterionValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseDiscoveryScopeCriterionId = table.Column<long>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseDiscoveryScopeCriterionValues", x => x.Id);
                    table.CheckConstraint("CK_CruiseDiscoveryScopeCriterionValueEntity_Value_Length", "length(\"Value\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseDiscoveryScopeCriterionValues_Order", "\"DisplayOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_CruiseDiscoveryScopeCriterionValues_CruiseDiscoveryScopeCriteria_CruiseDiscoveryScopeCriterionId",
                        column: x => x.CruiseDiscoveryScopeCriterionId,
                        principalTable: "CruiseDiscoveryScopeCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseItineraryCatalogue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CatalogueKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RetailSourceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RetailSourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProviderItineraryId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FirstOccurrenceId = table.Column<long>(type: "INTEGER", nullable: false),
                    LatestOccurrenceId = table.Column<long>(type: "INTEGER", nullable: false),
                    FirstSeenAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    FirstSeenAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    LastSeenAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    FirstObservedEventKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseItineraryCatalogue", x => x.Id);
                    table.CheckConstraint("CK_CruiseItineraryCatalogue_EventKey", "\"FirstObservedEventKey\" IS NULL OR (length(\"FirstObservedEventKey\") = 64 AND \"FirstObservedEventKey\" NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_CruiseItineraryCatalogue_Time", "\"FirstSeenAtUtcTicks\" <= \"LastSeenAtUtcTicks\"");
                    table.CheckConstraint("CK_CruiseItineraryCatalogueEntity_CatalogueKey", "length(\"CatalogueKey\") = 64 AND \"CatalogueKey\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseItineraryCatalogueEntity_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseItineraryCatalogueEntity_ProviderItineraryId_Length", "length(\"ProviderItineraryId\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseItineraryCatalogueEntity_RetailSourceId_Length", "length(\"RetailSourceId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseItineraryCatalogueEntity_RetailSourceName_Length", "length(\"RetailSourceName\") BETWEEN 1 AND 500");
                    table.ForeignKey(
                        name: "FK_CruiseItineraryCatalogue_CruiseDiscoveryOccurrences_FirstOccurrenceId",
                        column: x => x.FirstOccurrenceId,
                        principalTable: "CruiseDiscoveryOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CruiseItineraryCatalogue_CruiseDiscoveryOccurrences_LatestOccurrenceId",
                        column: x => x.LatestOccurrenceId,
                        principalTable: "CruiseDiscoveryOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CruiseDiscoveryChecks_CruiseDiscoveryScopeId",
                table: "CruiseDiscoveryChecks",
                column: "CruiseDiscoveryScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_CruiseDiscoveryChecks_Observed",
                table: "CruiseDiscoveryChecks",
                columns: new[] { "ObservedAtUtcTicks", "EvidenceKey" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseDiscoveryChecks_EvidenceKey",
                table: "CruiseDiscoveryChecks",
                column: "EvidenceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CruiseDiscoveryOccurrences_Catalogue_Observed",
                table: "CruiseDiscoveryOccurrences",
                columns: new[] { "CatalogueKey", "ObservedAtUtcTicks", "OccurrenceFingerprint" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseDiscoveryOccurrences_Check_Catalogue",
                table: "CruiseDiscoveryOccurrences",
                columns: new[] { "CruiseDiscoveryCheckId", "CatalogueKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruiseDiscoveryRejections_Check_Order",
                table: "CruiseDiscoveryRejections",
                columns: new[] { "CruiseDiscoveryCheckId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruiseDiscoveryScopeCriteria_Scope_Name",
                table: "CruiseDiscoveryScopeCriteria",
                columns: new[] { "CruiseDiscoveryScopeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruiseDiscoveryScopeCriterionValues_Criterion_Order",
                table: "CruiseDiscoveryScopeCriterionValues",
                columns: new[] { "CruiseDiscoveryScopeCriterionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruiseDiscoveryScopeCriterionValues_Criterion_Value",
                table: "CruiseDiscoveryScopeCriterionValues",
                columns: new[] { "CruiseDiscoveryScopeCriterionId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CruiseDiscoveryScopes_LastCheck",
                table: "CruiseDiscoveryScopes",
                columns: new[] { "LastCheckedAtUtcTicks", "ScopeFingerprint" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseDiscoveryScopes_Fingerprint",
                table: "CruiseDiscoveryScopes",
                column: "ScopeFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CruiseItineraryCatalogue_FirstOccurrenceId",
                table: "CruiseItineraryCatalogue",
                column: "FirstOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CruiseItineraryCatalogue_FirstSeen",
                table: "CruiseItineraryCatalogue",
                columns: new[] { "FirstSeenAtUtcTicks", "CatalogueKey" });

            migrationBuilder.CreateIndex(
                name: "IX_CruiseItineraryCatalogue_LatestOccurrenceId",
                table: "CruiseItineraryCatalogue",
                column: "LatestOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "UX_CruiseItineraryCatalogue_EventKey",
                table: "CruiseItineraryCatalogue",
                column: "FirstObservedEventKey",
                unique: true,
                filter: "\"FirstObservedEventKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_CruiseItineraryCatalogue_Key",
                table: "CruiseItineraryCatalogue",
                column: "CatalogueKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CruiseDiscoveryRejections");

            migrationBuilder.DropTable(
                name: "CruiseDiscoveryScopeCriterionValues");

            migrationBuilder.DropTable(
                name: "CruiseItineraryCatalogue");

            migrationBuilder.DropTable(
                name: "CruiseDiscoveryScopeCriteria");

            migrationBuilder.DropTable(
                name: "CruiseDiscoveryOccurrences");

            migrationBuilder.DropTable(
                name: "CruiseDiscoveryChecks");

            migrationBuilder.DropTable(
                name: "CruiseDiscoveryScopes");
        }
    }
}
