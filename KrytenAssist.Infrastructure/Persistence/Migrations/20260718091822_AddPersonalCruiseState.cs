using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrytenAssist.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalCruiseState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CruisePreferenceProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaximumBudgetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MaximumBudgetCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    MaximumBudgetBasis = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruisePreferenceProfiles", x => x.Id);
                    table.CheckConstraint("CK_CruisePreferenceProfiles_BudgetAmount", "\"MaximumBudgetAmount\" IS NULL OR CAST(\"MaximumBudgetAmount\" AS REAL) >= 0");
                    table.CheckConstraint("CK_CruisePreferenceProfiles_BudgetBasis", "\"MaximumBudgetBasis\" IS NULL OR \"MaximumBudgetBasis\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_CruisePreferenceProfiles_BudgetCurrency", "\"MaximumBudgetCurrency\" IS NULL OR (length(\"MaximumBudgetCurrency\") = 3 AND \"MaximumBudgetCurrency\" GLOB '[A-Z][A-Z][A-Z]')");
                    table.CheckConstraint("CK_CruisePreferenceProfiles_BudgetTuple", "(\"MaximumBudgetAmount\" IS NULL AND \"MaximumBudgetCurrency\" IS NULL AND \"MaximumBudgetBasis\" IS NULL) OR (\"MaximumBudgetAmount\" IS NOT NULL AND \"MaximumBudgetCurrency\" IS NOT NULL AND \"MaximumBudgetBasis\" IS NOT NULL)");
                    table.CheckConstraint("CK_CruisePreferenceProfiles_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "FavouriteCruiseShips",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavouriteCruiseShips", x => x.Id);
                    table.CheckConstraint("CK_FavouriteCruiseShips_Operator_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_FavouriteCruiseShips_Ship_Length", "length(\"ShipName\") BETWEEN 1 AND 500");
                });

            migrationBuilder.CreateTable(
                name: "SavedCruises",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OperatorName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DeparturePort = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ItinerarySummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DisplayedPriceAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DisplayedPriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DisplayedPriceBasis = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RetailSourceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RetailSourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SavedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    OverallRating = table.Column<int>(type: "INTEGER", nullable: true),
                    ItineraryRating = table.Column<int>(type: "INTEGER", nullable: true),
                    ShipRating = table.Column<int>(type: "INTEGER", nullable: true),
                    ValueRating = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsFavourite = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCruises", x => x.Id);
                    table.CheckConstraint("CK_SavedCruises_Currency", "length(\"DisplayedPriceCurrency\") = 3 AND \"DisplayedPriceCurrency\" GLOB '[A-Z][A-Z][A-Z]'");
                    table.CheckConstraint("CK_SavedCruises_DeparturePort_Length", "\"DeparturePort\" IS NULL OR length(\"DeparturePort\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_SavedCruises_DisplayedPriceBasis_Length", "\"DisplayedPriceBasis\" IS NULL OR length(\"DisplayedPriceBasis\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_SavedCruises_Duration", "\"DurationNights\" > 0");
                    table.CheckConstraint("CK_SavedCruises_Interest", "\"InterestLevel\" IS NULL OR \"InterestLevel\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_SavedCruises_ItineraryRating", "\"ItineraryRating\" IS NULL OR \"ItineraryRating\" BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_SavedCruises_ItinerarySummary_Length", "\"ItinerarySummary\" IS NULL OR length(\"ItinerarySummary\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_SavedCruises_Notes_Length", "\"Notes\" IS NULL OR length(\"Notes\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_SavedCruises_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_SavedCruises_OperatorName_Length", "length(\"OperatorName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_SavedCruises_OverallRating", "\"OverallRating\" IS NULL OR \"OverallRating\" BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_SavedCruises_Price", "CAST(\"DisplayedPriceAmount\" AS REAL) >= 0");
                    table.CheckConstraint("CK_SavedCruises_RetailSourceId_Length", "\"RetailSourceId\" IS NULL OR length(\"RetailSourceId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_SavedCruises_RetailSourceName_Length", "\"RetailSourceName\" IS NULL OR length(\"RetailSourceName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_SavedCruises_ShipName_Length", "length(\"ShipName\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_SavedCruises_ShipRating", "\"ShipRating\" IS NULL OR \"ShipRating\" BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_SavedCruises_SourcePair", "(\"RetailSourceId\" IS NULL AND \"RetailSourceName\" IS NULL) OR (\"RetailSourceId\" IS NOT NULL AND \"RetailSourceName\" IS NOT NULL)");
                    table.CheckConstraint("CK_SavedCruises_SourceReference_Length", "\"SourceReference\" IS NULL OR length(\"SourceReference\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_SavedCruises_Status", "\"Status\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_SavedCruises_Title_Length", "length(\"Title\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_SavedCruises_ValueRating", "\"ValueRating\" IS NULL OR \"ValueRating\" BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "CruisePreferenceCabins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cabin = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruisePreferenceCabins", x => x.Id);
                    table.CheckConstraint("CK_CruisePreferenceCabins_Cabin", "\"Cabin\" BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_CruisePreferenceCabins_CruisePreferenceProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CruisePreferenceProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CruisePreferenceMonths",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruisePreferenceMonths", x => x.Id);
                    table.CheckConstraint("CK_CruisePreferenceMonths_Month", "\"Month\" BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_CruisePreferenceMonths_CruisePreferenceProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CruisePreferenceProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CruisePreferenceCabins_Profile_Cabin",
                table: "CruisePreferenceCabins",
                columns: new[] { "ProfileId", "Cabin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CruisePreferenceMonths_Profile_Month",
                table: "CruisePreferenceMonths",
                columns: new[] { "ProfileId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FavouriteCruiseShips_Operator_Ship",
                table: "FavouriteCruiseShips",
                columns: new[] { "OperatorId", "ShipName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SavedCruises_Sailing",
                table: "SavedCruises",
                columns: new[] { "OperatorId", "ShipName", "DepartureDate", "DurationNights" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CruisePreferenceCabins");

            migrationBuilder.DropTable(
                name: "CruisePreferenceMonths");

            migrationBuilder.DropTable(
                name: "FavouriteCruiseShips");

            migrationBuilder.DropTable(
                name: "SavedCruises");

            migrationBuilder.DropTable(
                name: "CruisePreferenceProfiles");
        }
    }
}
