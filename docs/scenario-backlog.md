# Scenario backlog

Candidate scenarios, mapped onto this repo's categories and approach vocabulary. Each entry becomes a folder under `Scenarios/<Category>/<Name>/` with a spec when it is picked up. New approach names go into `Approaches.cs` only when a scenario needs them.

Engine-specific techniques (SQL Server `SqlBulkCopy`, TVPs, `MERGE`, `UPDLOCK`/`READPAST`, row-level security, PostgreSQL `COPY`, `ON CONFLICT`, `SKIP LOCKED`) will be approaches that declare which engines they support (not built yet: the first such scenario adds an `[Engines(...)]` attribute that the runner filters on). Each engine runs its own native equivalent where one exists.

| Category | Scenario | Question | Notes |
|---|---|---|---|
| Querying | ~~Fortunes~~ | Fixed per-request overhead | **Done** |
| Querying | ~~SingleById~~ | Point lookup cost | **Done** |
| Querying | NRandomRows | N round trips vs one `IN` query vs batched commands | Params N = 1, 20, 100, 500 |
| Querying | LargeInLists | How `Contains` translates at K = 10 to 10,000 ids, and plan-cache impact | Inline constants, TVP, temp table, chunking |
| Querying | Paging | Offset vs keyset paging at depth | Page 1 to 10,000, with and without total count |
| Querying | Projection | Cost of loading unneeded wide columns | Wide product table, DTO vs full entity |
| Querying | LargeResultSets | Buffering vs streaming 10k to 1M rows | Also time-to-first-row and peak memory |
| Querying | Aggregation | LINQ `GroupBy` vs raw SQL vs precomputed rollups | Events per type per day |
| Querying | TextSearch | `LIKE` vs prefix index vs full-text | Results differ by design; report counts |
| Querying | MultiTenancy | Explicit filter vs global query filter vs RLS | Skewed tenants; plan reuse |
| Querying | FlexibleSchema | JSON column vs EAV vs typed columns | Filter, read and update an attribute |
| Loading | ~~OrdersWithLines~~ | One-to-many graph loading | **Done** |
| Loading | ObjectGraphs | Include vs split vs projection vs N+1 at 3 levels | Also rows transferred |
| Writes | ReadModifyWrite | Tracked `SaveChanges` vs `ExecuteUpdate` vs set-based update | Params N |
| Writes | Upsert | Read-then-write vs catch-duplicate vs `MERGE`/`ON CONFLICT` | Batch size and existing % |
| Writes | ChangeTracker | Change detection cost at 1k to 100k tracked entities | Manual `DetectChanges`, `Attach` |
| Bulk | BulkInsert | `AddRange` vs chunked `SaveChanges` vs native bulk copy | Rows = 100 to 100,000 |
| Bulk | SetBasedUpdateDelete | Load-and-save vs `ExecuteUpdate`/`ExecuteDelete` vs chunked SQL | Also blocking of readers |
| Concurrency | OptimisticConcurrency | Row-version retry vs pessimistic lock vs atomic update | Lost updates, retries |
| Concurrency | StreamingIngest | Per-event insert vs buffered bulk writes under producers | Custom measures: backlog, end-to-end latency |
| Concurrency | Outbox | Outbox polling strategies and dispatch lag | Custom measures |
| Concurrency | CacheAside | DB only vs memory vs distributed vs HybridCache | Needs a Redis profile |
| Concurrency | ColdStart | Startup and first-query cost; compiled models | Fresh process per iteration |

The last five need measures beyond time-per-operation and throughput (backlog, lag, lost updates, cache hit ratio, process start). Each will get a small workload runner beside `LoadRunner` when it is first built.
