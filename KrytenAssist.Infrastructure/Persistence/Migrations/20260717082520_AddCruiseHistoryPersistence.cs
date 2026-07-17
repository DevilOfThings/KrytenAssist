using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrytenAssist.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCruiseHistoryPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CruiseHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NormalizedShipName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: false),
                    RetailSourceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RetailSourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FirstObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    LastSeenAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseHistories", x => x.Id);
                    table.CheckConstraint("CK_CruiseHistories_DurationNights", "\"DurationNights\" > 0");
                    table.CheckConstraint("CK_CruiseHistories_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseHistories_ShipName_Length", "length(\"NormalizedShipName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseHistories_SourceId_Length", "length(\"RetailSourceId\") <= 200");
                    table.CheckConstraint("CK_CruiseHistories_SourceName_Length", "\"RetailSourceName\" IS NULL OR length(\"RetailSourceName\") BETWEEN 1 AND 500");
                });

            migrationBuilder.CreateTable(
                name: "CruiseObservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseHistoryId = table.Column<long>(type: "INTEGER", nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", maxLength: 16000, nullable: false),
                    ProviderOfferId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OperatorName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: false),
                    DeparturePort = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ItinerarySummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    PromotionSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseObservations", x => x.Id);
                    table.CheckConstraint("CK_CruiseObservations_DeparturePort_Length", "\"DeparturePort\" IS NULL OR length(\"DeparturePort\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseObservations_DurationNights", "\"DurationNights\" > 0");
                    table.CheckConstraint("CK_CruiseObservations_Fingerprint_Length", "length(\"Fingerprint\") BETWEEN 1 AND 16000");
                    table.CheckConstraint("CK_CruiseObservations_ItinerarySummary_Length", "\"ItinerarySummary\" IS NULL OR length(\"ItinerarySummary\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseObservations_OperatorName_Length", "length(\"OperatorName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseObservations_PromotionSummary_Length", "\"PromotionSummary\" IS NULL OR length(\"PromotionSummary\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseObservations_ProviderOfferId_Length", "length(\"ProviderOfferId\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseObservations_ShipName_Length", "length(\"ShipName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseObservations_SourceReference_Length", "\"SourceReference\" IS NULL OR length(\"SourceReference\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseObservations_Title_Length", "length(\"Title\") BETWEEN 1 AND 1000");
                    table.ForeignKey(
                        name: "FK_CruiseObservations_CruiseHistories_CruiseHistoryId",
                        column: x => x.CruiseHistoryId,
                        principalTable: "CruiseHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruiseObservationPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CruiseObservationId = table.Column<long>(type: "INTEGER", nullable: false),
                    Amount = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Basis = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseObservationPrices", x => x.Id);
                    table.CheckConstraint("CK_CruiseObservationPrices_Amount", "CAST(\"Amount\" AS REAL) >= 0");
                    table.CheckConstraint("CK_CruiseObservationPrices_Basis_Length", "\"Basis\" IS NULL OR length(\"Basis\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_CruiseObservationPrices_Currency", "length(\"Currency\") = 3 AND \"Currency\" GLOB '[A-Z][A-Z][A-Z]'");
                    table.CheckConstraint("CK_CruiseObservationPrices_DisplayOrder", "\"DisplayOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_CruiseObservationPrices_CruiseObservations_CruiseObservationId",
                        column: x => x.CruiseObservationId,
                        principalTable: "CruiseObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseHistories_Sailing_Source",
                table: "CruiseHistories",
                columns: new[] { "OperatorId", "NormalizedShipName", "DepartureDate", "DurationNights", "RetailSourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruiseObservationPrices_Observation_Order",
                table: "CruiseObservationPrices",
                columns: new[] { "CruiseObservationId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CruiseObservations_History_ObservedAt",
                table: "CruiseObservations",
                columns: new[] { "CruiseHistoryId", "ObservedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseObservations_History_Fingerprint",
                table: "CruiseObservations",
                columns: new[] { "CruiseHistoryId", "Fingerprint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CruiseObservationPrices");

            migrationBuilder.DropTable(
                name: "CruiseObservations");

            migrationBuilder.DropTable(
                name: "CruiseHistories");
        }
    }
}
