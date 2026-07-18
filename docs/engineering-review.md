# Engineering review of the legacy Java application

Scope: every tracked meaningful file under `D:\Workspace\repos\SqlScriptGen` was read on 2026-07-18: root metadata, all nine production Java files (including discovered GUI files), and the test file. The source repository was treated as read-only.

## Current functionality and architecture

`Main.main` selects console or JavaFX, while `runConsoleApplication` collects a dialect, table, raw column map, and constraint strings. `DatabaseScriptGenerator` exposes create-table/database methods; `ScriptGeneratorFactory.getScriptGenerator(int)` returns PostgreSQL or MySQL implementations. `UserInteraction` performs prompting and partial validation. `GUIInteraction.updateSQLTextArea` independently assembles SQL, so generation logic is duplicated and the GUI path is incomplete. `Enums` is a large container for dialects, type catalogs, and executable constraint behavior.

The usable product is a single-table DDL text generator. `generateCreateDatabaseScript` exists but is not reachable from the application. The GUI can collect columns, but `setupConstraintsUI` computes and discards constraint SQL. No database connection or execution exists.

## Prioritized findings

| Severity | Finding | Evidence and impact |
|---|---|---|
| High | SQL is built from unvalidated raw input | `PostgreSQLScriptGenerator.generateCreateTableScript`, `MySQLScriptGenerator.generateCreateTableScript`, and `GUIInteraction.updateSQLTextArea` concatenate names/types/constraints. Malformed or hostile DDL can be generated. |
| High | MySQL constraints are nonfunctional | `Enums.MySQLConstraintType.getConstraintSQL` returns `null`; callers append it to generated text. |
| High | Unsupported MongoDB can be selected | `Enums.DatabaseType` and `UserInteraction.getDatabaseType/getDataTypesForDatabase` expose MongoDB, but `ScriptGeneratorFactory` has no implementation, producing an exception. |
| High | PostgreSQL foreign keys are malformed | `PostgreSQLConstraintType.getConstraintSQL` emits `REFERENCES (` + input + `)`, while `UserInteraction.getConstraint` supplies `table(column)`, yielding extra parentheses. |
| High | Empty MySQL content can crash/corrupt output | `MySQLScriptGenerator.generateCreateTableScript` unconditionally removes two trailing characters without proving a delimiter exists. |
| Medium | Column order is not guaranteed | `UserInteraction.getColumns` and `JavaFXApp.start` use `HashMap`; both renderers iterate the map, so entry order can differ from user order. |
| Medium | Ordinals are persistence/factory identifiers | `DatabaseType.getValue` uses `ordinal()+1`; `ScriptGeneratorFactory.getScriptGenerator(int)` switches on 1/2. Enum reordering silently changes behavior. Other `fromInt` methods index `values()` without bounds checks. |
| Medium | Input recovery is fragile | `getDatabaseType`, `getColumns`, and `addSQLConstraints` recursively retry; repeated bad input grows the stack. `promptForDataType` mixes `nextInt()` and `nextLine()`, and `Main.main` does the same. EOF and out-of-range indexes are not handled consistently. |
| Medium | PostgreSQL exclusion is advertised but absent | `PostgreSQLConstraintType.EXCLUSION` appears in menus, but its renderer is commented out and falls to `IllegalArgumentException`. |
| Medium | CLI, domain, and rendering are tightly coupled | `Main.runConsoleApplication` wires concrete console interaction directly to factory/rendering; `UserInteraction` owns a real `Scanner`; GUI maintains a second renderer. This limits reuse and testability. |
| Medium | Tests provide no protection | `UserInteractionTest` has four empty tests and a larger test commented out. There are no renderer, constraint, validation, or packaging tests. |
| Medium | Distribution instructions do not match the build | `README.md` tells users to run `SqlScriptGen.jar`; `pom.xml` has no jar manifest/main-class or shade/assembly plugin and installation uses a placeholder clone URL. |
| Low | Error reporting is inconsistent | Broad `catch (Exception)` blocks print messages or stack traces; `getDatabaseType` can return `null`; no stable exit codes exist. |
| Low | Maintainability/documentation gaps | TODOs dominate generator classes, `Enums` mixes unrelated concerns, raw `Map<String,String>`/`List<String>` erase semantics, and README claims customizable constraints more strongly than implementation supports. |

No Critical finding is warranted because the tool does not execute SQL or handle protected data; the primary risks are incorrect or unsafe generated artifacts.

## Quality evaluation

1. **Functionality:** basic `CREATE TABLE` and library-only `CREATE DATABASE`; console/partial GUI; PostgreSQL is the least incomplete path.
2. **Separation:** interface/renderers are a useful start, but UI, selection, raw domain data, constraint behavior, and SQL assembly remain entangled.
3. **Correctness:** malformed foreign keys, null MySQL constraints, ordering instability, empty-output trimming, and selectable exclusion/MongoDB paths are confirmed.
4. **Completeness:** GUI constraints are discarded; database creation is unreachable; many advertised types accept no structured arguments.
5. **Validation:** names, duplicates, empty tables, type bounds, referenced columns, and expressions are not validated; numeric indices may fail.
6. **SQL safety:** raw concatenation is universal. DDL identifiers cannot use query parameters; structured validation and dialect quoting are required.
7. **Errors:** recursive retries and broad catches obscure causes; no cancellation or error-code contract.
8. **Testability:** hard-coded `System.in`, concrete construction, static factory, and raw strings impede isolated tests.
9. **Maintainability:** mega-enum, duplicated renderers, TODO code, magic numbers, and string fragments make extension risky.
10. **Packaging:** Maven compiles Java 11 and pulls JavaFX 17 but does not configure an executable/fat JAR.
11. **UX:** menus expose broken choices, input recovery is inconsistent, output cannot be saved directly, and no repeatable file workflow exists.
12. **Documentation:** brief README has an invalid/placeholder clone command, mismatched JAR instructions, no format contract, and no safety warning.
13. **Future development:** first stabilize typed definitions, validation, dialect renderers, deterministic JSON/CLI workflows, tests, and packaging; then add indexes/multi-table ordering and new SQL dialects. MongoDB belongs in a separate document-schema product, not the SQL dialect registry.

## File notes

- `README.md`: useful intent and example, inaccurate installation/distribution.
- `pom.xml`: Java 11, JUnit/Mockito/JavaFX dependencies; no test/plugin or executable packaging configuration.
- `Main.java`: interface selection plus concrete orchestration and mixed scanner calls.
- `helpers/Enums.java`: all catalogs and constraints; confirms MongoDB, ordinals, null MySQL behavior, malformed FK, and incomplete exclusion.
- `interfaces/*.java`: minimal abstractions, but raw maps/lists discard structure.
- `service/*.java`: duplicated concatenation, no validation/quoting, magic-number factory, MySQL trimming defect.
- `userinteraction/UserInteraction.java`: recursion, mixed scanner APIs, unsupported choices, index-based selections.
- `userinteraction/GUIInteraction.java` and `gui/JavaFXApp.java`: discovered incomplete alternative UI with duplicate SQL generation and `HashMap` ordering.
- `UserInteractionTest.java`: empty/commented tests only.
- `.gitignore` and `LICENSE`: conventional, though legacy license names “Preston Stewart Butler II” while the migration requires “Preston S. Butler II”.
