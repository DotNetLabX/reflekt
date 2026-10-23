using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftKickParticipants : Migration
    {
        // Repairs existing duplicates before the unique index is created. Within a sprint, active names that
        // match case-insensitively keep the lowest-id participant's name; each later duplicate, in id order,
        // takes the first suffix " 2", " 3", ... that is free against every active name in the sprint and
        // every name assigned before it in that sprint, with the base trimmed so the result fits in 50
        // characters. No row is deleted. SQLite has no loops, so the recursive CTE handles one duplicate per
        // step and carries the names assigned so far as a JSON array of "{SprintId}:{lower(name)}" entries.
        // A free suffix always exists in 2 .. 2 * (largest sprint) + 2: each active name and each earlier
        // assignment in the sprint blocks at most one suffix.
        private const string BuildDuplicateNameRenamesSql = """
            CREATE TEMP TABLE "ParticipantRenames" AS
            WITH RECURSIVE
            "Duplicates" AS (
                SELECT p."Id", p."SprintId", p."DisplayName", ROW_NUMBER() OVER (ORDER BY p."Id") AS "Rn"
                FROM "Participants" AS p
                WHERE p."RemovedAt" IS NULL
                  AND EXISTS (
                      SELECT 1 FROM "Participants" AS o
                      WHERE o."SprintId" = p."SprintId"
                        AND o."RemovedAt" IS NULL
                        AND o."Id" < p."Id"
                        AND o."DisplayName" = p."DisplayName" COLLATE NOCASE)
            ),
            "Suffixes" AS (
                SELECT 2 AS "N"
                UNION ALL
                SELECT "N" + 1 FROM "Suffixes"
                WHERE "N" < (
                    SELECT MAX("Size") * 2 + 2
                    FROM (SELECT COUNT(*) AS "Size" FROM "Participants" GROUP BY "SprintId"))
            ),
            "Candidates" AS (
                SELECT d."Id", s."N",
                       substr(d."DisplayName", 1, 50 - length(' ' || s."N")) || ' ' || s."N" AS "Name"
                FROM "Duplicates" AS d
                CROSS JOIN "Suffixes" AS s
            ),
            "Renames" AS (
                SELECT 0 AS "Rn", NULL AS "Id", NULL AS "SprintId", NULL AS "NewName", json('[]') AS "Taken"
                UNION ALL
                SELECT d."Rn", d."Id", d."SprintId",
                       (SELECT c."Name"
                        FROM "Candidates" AS c
                        WHERE c."Id" = d."Id"
                          AND NOT EXISTS (
                              SELECT 1 FROM "Participants" AS a
                              WHERE a."SprintId" = d."SprintId"
                                AND a."RemovedAt" IS NULL
                                AND a."DisplayName" = c."Name" COLLATE NOCASE)
                          AND NOT EXISTS (
                              SELECT 1
                              FROM json_each(CASE WHEN r."NewName" IS NULL THEN r."Taken"
                                                  ELSE json_insert(r."Taken", '$[#]', r."SprintId" || ':' || lower(r."NewName")) END) AS t
                              WHERE t."value" = d."SprintId" || ':' || lower(c."Name"))
                        ORDER BY c."N"
                        LIMIT 1),
                       CASE WHEN r."NewName" IS NULL THEN r."Taken"
                            ELSE json_insert(r."Taken", '$[#]', r."SprintId" || ':' || lower(r."NewName")) END
                FROM "Renames" AS r
                JOIN "Duplicates" AS d ON d."Rn" = r."Rn" + 1
            )
            SELECT "Id", "NewName" FROM "Renames" WHERE "Rn" > 0;
            """;

        private const string ApplyDuplicateNameRenamesSql = """
            UPDATE "Participants"
            SET "DisplayName" = (SELECT r."NewName" FROM temp."ParticipantRenames" AS r WHERE r."Id" = "Participants"."Id")
            WHERE "Id" IN (SELECT "Id" FROM temp."ParticipantRenames");
            """;

        private const string DropDuplicateNameRenamesSql = """
            DROP TABLE temp."ParticipantRenames";
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAt",
                table: "Participants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(BuildDuplicateNameRenamesSql);
            migrationBuilder.Sql(ApplyDuplicateNameRenamesSql);
            migrationBuilder.Sql(DropDuplicateNameRenamesSql);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Participants",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Participants_SprintId_DisplayName",
                table: "Participants",
                columns: new[] { "SprintId", "DisplayName" },
                unique: true,
                filter: "\"RemovedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participants_SprintId_DisplayName",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                table: "Participants");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Participants",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldCollation: "NOCASE");
        }
    }
}
