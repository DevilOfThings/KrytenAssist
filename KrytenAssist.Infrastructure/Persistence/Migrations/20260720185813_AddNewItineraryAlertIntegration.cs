using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrytenAssist.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewItineraryAlertIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_Duration",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_OperatorId_Length",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_ShipName_Length",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts");

            migrationBuilder.AddColumn<bool>(
                name: "NewItineraryEnabled",
                table: "CruiseAlertSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShipName",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "OperatorId",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "DurationNights",
                table: "CruiseAlerts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "DepartureDate",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "ItineraryOperatorId",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderItineraryId",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CruiseNewItineraryAlertDetails",
                columns: table => new
                {
                    CruiseAlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperatorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProviderItineraryId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ScopeFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CheckEvidenceKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OccurrenceFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ProviderEvidenceKey = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    FirstObservedEventKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FirstObservedAt = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ShipName = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DepartureDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    DurationNights = table.Column<int>(type: "INTEGER", nullable: true),
                    DeparturePort = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ItinerarySummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CruiseNewItineraryAlertDetails", x => x.CruiseAlertId);
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_DeparturePort_Length", "\"DeparturePort\" IS NULL OR length(\"DeparturePort\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_Duration", "\"DurationNights\" IS NULL OR \"DurationNights\" BETWEEN 1 AND 365");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_Hashes", "length(\"ScopeFingerprint\") = 64 AND \"ScopeFingerprint\" NOT GLOB '*[^0-9a-f]*' AND length(\"CheckEvidenceKey\") = 64 AND \"CheckEvidenceKey\" NOT GLOB '*[^0-9a-f]*' AND length(\"OccurrenceFingerprint\") = 64 AND \"OccurrenceFingerprint\" NOT GLOB '*[^0-9a-f]*' AND length(\"FirstObservedEventKey\") = 64 AND \"FirstObservedEventKey\" NOT GLOB '*[^0-9a-f]*'");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_ItinerarySummary_Length", "\"ItinerarySummary\" IS NULL OR length(\"ItinerarySummary\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_ProviderEvidenceKey_Length", "length(\"ProviderEvidenceKey\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_ProviderItineraryId_Length", "length(\"ProviderItineraryId\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_ShipName_Length", "\"ShipName\" IS NULL OR length(\"ShipName\") BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_SourceReference_Length", "\"SourceReference\" IS NULL OR length(\"SourceReference\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_CruiseNewItineraryDetails_Title_Length", "\"Title\" IS NULL OR length(\"Title\") BETWEEN 1 AND 1000");
                    table.ForeignKey(
                        name: "FK_CruiseNewItineraryAlertDetails_CruiseAlerts_CruiseAlertId",
                        column: x => x.CruiseAlertId,
                        principalTable: "CruiseAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings",
                sql: "\"PriceDropEnabled\" IN (0,1) AND \"PromotionEnabled\" IN (0,1) AND \"SavedCriteriaEnabled\" IN (0,1) AND \"CabinAvailabilityEnabled\" IN (0,1) AND \"NewItineraryEnabled\" IN (0,1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_ItineraryOperatorId_Length",
                table: "CruiseAlerts",
                sql: "\"ItineraryOperatorId\" IS NULL OR length(\"ItineraryOperatorId\") BETWEEN 1 AND 200");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_OperatorId_Length",
                table: "CruiseAlerts",
                sql: "\"OperatorId\" IS NULL OR length(\"OperatorId\") BETWEEN 1 AND 200");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_ProviderItineraryId_Length",
                table: "CruiseAlerts",
                sql: "\"ProviderItineraryId\" IS NULL OR length(\"ProviderItineraryId\") BETWEEN 1 AND 1000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_ShipName_Length",
                table: "CruiseAlerts",
                sql: "\"ShipName\" IS NULL OR length(\"ShipName\") BETWEEN 1 AND 500");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_Subject",
                table: "CruiseAlerts",
                sql: "(\"Type\" BETWEEN 0 AND 3 AND \"OperatorId\" IS NOT NULL AND \"ShipName\" IS NOT NULL AND \"DepartureDate\" IS NOT NULL AND \"DurationNights\" > 0 AND \"ItineraryOperatorId\" IS NULL AND \"ProviderItineraryId\" IS NULL) OR (\"Type\" = 4 AND \"OperatorId\" IS NULL AND \"ShipName\" IS NULL AND \"DepartureDate\" IS NULL AND \"DurationNights\" IS NULL AND \"ItineraryOperatorId\" IS NOT NULL AND \"ProviderItineraryId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts",
                sql: "\"Type\" BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts",
                sql: "(\"Type\" IN (0, 1, 3, 4) AND \"RetailSourceId\" IS NOT NULL) OR (\"Type\" = 2 AND \"RetailSourceId\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CruiseNewItineraryAlertDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_ItineraryOperatorId_Length",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_OperatorId_Length",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_ProviderItineraryId_Length",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_ShipName_Length",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_Subject",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts");

            migrationBuilder.DropColumn(
                name: "NewItineraryEnabled",
                table: "CruiseAlertSettings");

            migrationBuilder.DropColumn(
                name: "ItineraryOperatorId",
                table: "CruiseAlerts");

            migrationBuilder.DropColumn(
                name: "ProviderItineraryId",
                table: "CruiseAlerts");

            migrationBuilder.AlterColumn<string>(
                name: "ShipName",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OperatorId",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DurationNights",
                table: "CruiseAlerts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DepartureDate",
                table: "CruiseAlerts",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlertSettings_Booleans",
                table: "CruiseAlertSettings",
                sql: "\"PriceDropEnabled\" IN (0,1) AND \"PromotionEnabled\" IN (0,1) AND \"SavedCriteriaEnabled\" IN (0,1) AND \"CabinAvailabilityEnabled\" IN (0,1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_Duration",
                table: "CruiseAlerts",
                sql: "\"DurationNights\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_OperatorId_Length",
                table: "CruiseAlerts",
                sql: "length(\"OperatorId\") BETWEEN 1 AND 200");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_ShipName_Length",
                table: "CruiseAlerts",
                sql: "length(\"ShipName\") BETWEEN 1 AND 500");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_Type",
                table: "CruiseAlerts",
                sql: "\"Type\" BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseAlerts_TypeSource",
                table: "CruiseAlerts",
                sql: "(\"Type\" IN (0, 1, 3) AND \"RetailSourceId\" IS NOT NULL) OR (\"Type\" = 2 AND \"RetailSourceId\" IS NULL)");
        }
    }
}
