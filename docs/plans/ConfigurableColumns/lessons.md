# ConfigurableColumns — Lessons

## Developer Lessons

- `RetroReportPanel.vue` (the in-board report panel) was not listed in the plan's explicit file list, but it referenced the old `report.notes.wentWell` shape and failed the TypeScript build. When a plan lists "all report components," verify both the standalone report view components (`SprintReportPanel.vue`) and any in-board report panels that render the same data store.

- TypeScript's `noUncheckedIndexedAccess` mode makes reactive array indexing (`columns.value[i]`) produce `T | undefined`. For small fixed-length reactive collections (e.g., 3 column configs), use separate named `ref` objects instead of a reactive array to avoid non-null assertions throughout the template.

- When widening a prop type from a narrow literal union to `string` in a Vue component, also audit the computed properties that use that prop — the old conditionals keyed on the old literal values become dead code that needs updating.

- EF value object conversions with `HasConversion(c => c.Value, s => ColumnColor.Create(s))` work cleanly, but the class (not record) pattern is required to avoid EF equality tracking issues. Manually overriding `Equals`/`GetHashCode` on the value object is mandatory.

- When a `ColumnType` enum ordinal shifts (ActionItems 2 → 3), existing database rows must be migrated in the same migration via raw SQL (`UPDATE ... SET ColumnType = 3 WHERE ColumnType = 2`) before the new enum value occupies ordinal 2. Add the SQL to `migrationBuilder.Sql(...)` in `Up()` and the reverse in `Down()`.

- Pre-fill logic for sprint creation (copying column config from the most recent sprint) belongs in the endpoint (application layer), not the domain. The domain `Sprint.Create` factory just receives resolved values — it does not query for existing sprints.

- `CreateSprintForm.vue` pre-fills column config from `sprintStore.sprints[0]` on `onMounted`. The store must already be populated (loaded by the parent view) before the form mounts — this is a runtime dependency, not a TypeScript issue.

## Reviewer Lessons

- When `HasDefaultValue` is used alongside `HasConversion` in EF Core configuration, verify the generated migration file directly rather than relying on the config code. The migration is the ground truth for what EF will actually execute — the config may look wrong (passing a CLR object) but the migration generator applies the conversion at generation time and emits the correct stored value.

- Variable names in seed/initializer code (`firstWentWellCard`, `wentWellCards`) can look like leftover semantic references. Check the actual enum value used in the filter (`ColumnType.Column1`) before flagging as a finding. Variable names are cosmetic.

- For inline-edit components, check the early-return guard conditions against the emit call below them. If the guard already ensures a value is truthy, the `value || fallback` pattern in the emit is dead code — not a bug but worth a LOW finding.

- The plan's "no phase restriction" requirement is verified by absence: confirm there is no `if (sprint.State != ...)` guard in the endpoint. Absence of a guard is the correct implementation.

## Architect Lessons

- A 16-step plan spanning domain, persistence, API, and frontend executed cleanly in a single cycle with zero deviations. The key factor was exhaustive codebase reading before planning — every file that would be touched was read first, so file paths, existing patterns, and cascading dependencies were known upfront.

- Explicitly noting "no phase restriction" in the plan (Step 4) prevented the developer from adding a state guard and the reviewer confirmed correctness by verifying its absence. Documenting what should NOT be built is as important as documenting what should.

- The ColumnType enum ordinal shift (ActionItems from 2 to 3) was the highest-risk element. Calling it out in Steps 1 and 9 with explicit SQL gave the developer and reviewer a clear contract to verify against. For future enum ordinal migrations, always specify the exact SQL in the plan rather than leaving it to the developer to figure out.

- Mapping `ColumnColor` value object through EF's `HasConversion` was a judgment call — could have been a plain string property with validation in the domain method only. The value object approach is cleaner (impossible to construct an invalid color) but requires the class-based pattern with manual Equals/GetHashCode. Worth it for this case; for simpler 2-3 value enums, a plain enum would suffice.

- The plan's skill mapping correctly identified that Steps 6, 7, 8 had no applicable skill (modification of existing features with unique logic). This avoided the developer wasting time trying to force-fit a skill pattern where none applied.

## Skill Gaps

- **Missing skill:** No skill covers "rename enum values with EF data migration." Suggested name: `enum-ordinal-migration`. Coverage: renaming enum members whose ordinals back existing database rows, writing Up/Down SQL to shift values before/after structural changes. Reference files: `Reflekt.Domain/Cards/ColumnType.cs`, `Reflekt.Persistence/Migrations/20260510180219_AddConfigurableColumns.cs`.

- **Missing skill:** No skill covers "EF Core value object conversion with `HasConversion`." Suggested name: `ef-value-object-conversion`. Coverage: when to use class vs record for value objects used in EF conversions, `HasConversion` syntax, `Equals`/`GetHashCode` requirements. Reference files: `Reflekt.Domain/Sprints/ColumnColor.cs`, `Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs`.
