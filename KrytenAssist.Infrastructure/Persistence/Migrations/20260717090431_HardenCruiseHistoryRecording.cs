using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrytenAssist.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCruiseHistoryRecording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CruiseObservations_History_Fingerprint",
                table: "CruiseObservations");

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "CruiseObservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LatestEvidenceObservedAt",
                table: "CruiseHistories",
                type: "TEXT",
                maxLength: 35,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LatestProviderOfferId",
                table: "CruiseHistories",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LatestSourceReference",
                table: "CruiseHistories",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "CruiseObservations" AS "current"
                SET "Sequence" = (
                    SELECT COUNT(*)
                    FROM "CruiseObservations" AS "candidate"
                    WHERE "candidate"."CruiseHistoryId" = "current"."CruiseHistoryId"
                      AND (
                          "candidate"."ObservedAt" < "current"."ObservedAt"
                          OR (
                              "candidate"."ObservedAt" = "current"."ObservedAt"
                              AND "candidate"."Fingerprint" < "current"."Fingerprint"
                          )
                          OR (
                              "candidate"."ObservedAt" = "current"."ObservedAt"
                              AND "candidate"."Fingerprint" = "current"."Fingerprint"
                              AND "candidate"."Id" <= "current"."Id"
                          )
                      )
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE "CruiseHistories"
                SET "LatestProviderOfferId" = (
                        SELECT "ProviderOfferId"
                        FROM "CruiseObservations"
                        WHERE "CruiseHistoryId" = "CruiseHistories"."Id"
                        ORDER BY "ObservedAt" DESC, "Fingerprint" DESC, "Id" DESC
                        LIMIT 1
                    ),
                    "LatestSourceReference" = (
                        SELECT "SourceReference"
                        FROM "CruiseObservations"
                        WHERE "CruiseHistoryId" = "CruiseHistories"."Id"
                        ORDER BY "ObservedAt" DESC, "Fingerprint" DESC, "Id" DESC
                        LIMIT 1
                    ),
                    "LatestEvidenceObservedAt" = (
                        SELECT "ObservedAt"
                        FROM "CruiseObservations"
                        WHERE "CruiseHistoryId" = "CruiseHistories"."Id"
                        ORDER BY "ObservedAt" DESC, "Fingerprint" DESC, "Id" DESC
                        LIMIT 1
                    );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CruiseObservations_History_Fingerprint",
                table: "CruiseObservations",
                columns: new[] { "CruiseHistoryId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "UX_CruiseObservations_History_Sequence",
                table: "CruiseObservations",
                columns: new[] { "CruiseHistoryId", "Sequence" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseObservations_Sequence",
                table: "CruiseObservations",
                sql: "\"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseHistories_LatestProviderOfferId_Length",
                table: "CruiseHistories",
                sql: "length(\"LatestProviderOfferId\") BETWEEN 1 AND 1000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CruiseHistories_LatestSourceReference_Length",
                table: "CruiseHistories",
                sql: "\"LatestSourceReference\" IS NULL OR length(\"LatestSourceReference\") BETWEEN 1 AND 4000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CruiseObservations_History_Fingerprint",
                table: "CruiseObservations");

            migrationBuilder.DropIndex(
                name: "UX_CruiseObservations_History_Sequence",
                table: "CruiseObservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseObservations_Sequence",
                table: "CruiseObservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseHistories_LatestProviderOfferId_Length",
                table: "CruiseHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CruiseHistories_LatestSourceReference_Length",
                table: "CruiseHistories");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "CruiseObservations");

            migrationBuilder.DropColumn(
                name: "LatestEvidenceObservedAt",
                table: "CruiseHistories");

            migrationBuilder.DropColumn(
                name: "LatestProviderOfferId",
                table: "CruiseHistories");

            migrationBuilder.DropColumn(
                name: "LatestSourceReference",
                table: "CruiseHistories");

            migrationBuilder.CreateIndex(
                name: "UX_CruiseObservations_History_Fingerprint",
                table: "CruiseObservations",
                columns: new[] { "CruiseHistoryId", "Fingerprint" },
                unique: true);
        }
    }
}
