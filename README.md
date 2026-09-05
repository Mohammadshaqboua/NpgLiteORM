# NpgLiteORM — Architecture & Design Documentation

![Tests](https://github.com/Mohammadshaqboua/NpgLiteORM/actions/workflows/tests.yml/badge.svg)

> A lightweight, educational **Object-Relational Mapper (ORM)** for PostgreSQL, built in C# / .NET 10 on top of Npgsql.
> This document provides an in-depth technical walkthrough of the codebase: its architecture, design patterns, class relationships, and the object-oriented principles it demonstrates.

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker (for running PostgreSQL locally)

### Run PostgreSQL locally

```bash
docker compose up -d
```

### Run the demo project

```bash
cd NpgLiteORM.Demo
dotnet run
```


---
## Overview

NpgLiteORM lets you map C# classes to PostgreSQL tables using attributes, then interact with your database through objects instead of writing raw SQL manually.

```csharp
[Table("users")]
public class User : EntityBase
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Column("full_name"), NotNull, MaxLength(100)]
    public string Name { get; set; }

    [Unique]
    public string Email { get; set; }
}
```

---

## 1. Executive Summary

**NpgLiteORM** is a hand-rolled micro-ORM that lets developers map plain C# classes to PostgreSQL tables using attributes (`[Table]`, `[Column]`, `[PrimaryKey]`, `[ForeignKey]`, etc.) and interact with the database through strongly-typed repositories and a fluent, LINQ-style query builder — instead of writing raw SQL by hand.

What makes this project noteworthy from an engineering standpoint is **not** the size of the codebase, but the **discipline of its design**. It is deliberately structured as a teaching artifact for the four pillars of OOP, and it shows a working knowledge of patterns that real-world ORMs (Entity Framework, Dapper, NHibernate) use internally:

| Pillar / Pattern | Where it appears |
|---|---|
| **Abstraction** | `RepositoryBase<T>`, `DbConnectionBase`, `EntityBase` hide *how* persistence happens behind a stable contract |
| **Encapsulation** | Reflection, SQL string building, and parameter binding are hidden inside `EntityMapper`, `SchemaBuilder`, `SqlGenerator` |
| **Inheritance** | `Repository<T> : RepositoryBase<T>`, `PostgresConnectionFactory : DbConnectionBase`, all entities `: EntityBase` |
| **Polymorphism** | `IDbConnectionFactory`, `IRepository<T>`, `IQueryBuilder<T>`, `IUnitOfWork` — swappable implementations behind interfaces |
| **Repository Pattern** | `Repository<T>` / `IRepository<T>` |
| **Unit of Work Pattern** | `UnitOfWork` — coordinates repositories + transactions over one shared connection |
| **Builder / Fluent Interface** | `QueryBuilder<T>` — `Where().OrderBy().Take().Skip().ExecuteAsync()` |
| **Factory Pattern** | `IDbConnectionFactory` / `PostgresConnectionFactory` |
| **Strategy-like Translation** | `ExpressionTranslator<T>` converts LINQ expression trees into SQL fragments |
| **Data Mapper Pattern** | `EntityMapper<T>` — converts between POCOs and `IDataRecord` rows |
| **Custom Exception Hierarchy** | `ConnectionException`, `EntityNotFoundException`, `SchemaValidationException` |
| **Attribute-Driven Metadata** | `TableAttribute`, `ColumnAttribute`, `PrimaryKeyAttribute`, `ForeignKeyAttribute`, `UniqueAttribute`, `NotNullAttribute`, `MaxLengthAttribute`, `AutoIncrementAttribute` |

The result reads like a **miniature, correctly-layered version of Entity Framework Core** — connection factory → schema/mapping layer → query layer → repository/unit-of-work layer — which is a genuinely difficult architecture to get right, especially the interplay between reflection-based mapping and expression-tree parsing.

---

## 2. Tech Stack

- **.NET 10** / **C# 12**
- **PostgreSQL**, accessed through **Npgsql**
- **Docker Compose** for local database provisioning
- Layered solution: `NpgLiteORM.Core` (library), `NpgLiteORM.Demo` (console showcase), `NpgLiteORM.Tests` (unit tests for mapping, schema, expression translation, repositories)

---

## 3. Project / Solution Structure

```
NpgLiteORM.Core/
├── Abstract/           EntityBase, RepositoryBase<T>, DbConnectionBase        (abstraction layer)
├── Attributes/          Table, Column, PrimaryKey, ForeignKey, Unique,
                          NotNull, MaxLength, AutoIncrement                     (declarative metadata)
├── Data/                PostgresConnectionFactory                             (connection creation)
├── Enums/                SortDirection, JoinType
├── Exceptions/           ConnectionException, EntityNotFoundException,
                          SchemaValidationException
├── Interfaces/           IDbConnectionFactory, IRepository<T>,
                          IQueryBuilder<T>, IUnitOfWork
├── Mapping/              EntityMapper<T>, SchemaBuilder                       (reflection engine)
├── Migrations/           IMigration, MigrationRunner                         (schema versioning)
├── Query/                QueryBuilder<T>, ExpressionTranslator<T>,
                          SqlGenerator                                         (fluent LINQ→SQL)
└── AttributeHelper.cs    Shared reflection helper

NpgLiteORM.Demo/
├── Models/               User, Order, Role                                    (sample entities)
└── Migrations/           001_CreateUsersTable, 002_CreateOrdersTable

NpgLiteORM.Tests/
├── Mapping/               EntityMapperTests, SchemaBuilderTests, FakeDataRecord
├── Query/                 ExpressionTranslatorTests
└── Repositories/          RepositoryTests
```

The layering is a clean **onion / clean-architecture shape**: `Attributes` and `Abstract` sit at the core with zero outward dependencies; `Mapping` and `Query` build on top of them via reflection; `Repositories` and `Migrations` orchestrate everything for the consumer; `Demo` and `Tests` depend on `Core` but never the reverse.

---

## 4. How It Works, End to End

1. **Define an entity** as a plain class inheriting `EntityBase`, decorated with `[Table]`, `[Column]`, `[PrimaryKey]`, `[NotNull]`, `[MaxLength]`, `[Unique]`, `[ForeignKey]`.
2. **`SchemaBuilder`** reflects over the type at runtime and generates the `CREATE TABLE` DDL, mapping C# types (`int`, `string`, `bool`, `DateTime`, `decimal`, `double`, `Guid`) to PostgreSQL column types, including `SERIAL`/`BIGSERIAL` for auto-increment keys — and throwing a typed **`SchemaValidationException`** (carrying the offending entity `Type` and property name) when an unsupported .NET type is encountered, instead of a generic `NotSupportedException`.
3. **`IMigration`** implementations (e.g. `CreateUsersTable`) use `SchemaBuilder` to apply/roll back that DDL; **`MigrationRunner`** applies all migrations in version order.
4. **`PostgresConnectionFactory`** (via `DbConnectionBase` → `IDbConnectionFactory`) is the single seam through which every other component obtains an `IDbConnection`, so the persistence provider could be swapped without touching business logic.
5. **`Repository<T>`** (implements `IRepository<T>`, extends `RepositoryBase<T>`) provides `AddAsync`, `GetByIdAsync`, `GetAllAsync`, `UpdateAsync`, `DeleteAsync`. It uses:
   - **`EntityMapper<T>`** to turn an entity into a column/value dictionary (`MapToRow`) and an `IDataRecord` back into an entity (`MapToEntity`), driven entirely by attribute reflection.
   - **`SchemaBuilder.GetTableName<T>()`** to resolve the physical table name.
   - Parameterized ADO.NET commands (`DbParameter`) everywhere — meaning **the library is SQL-injection-safe by construction** for all repository operations.
6. **`QueryBuilder<T>`** (implements `IQueryBuilder<T>`) offers a fluent LINQ-like API — `Where(x => x.Age > 18).OrderByDescending(x => x.CreatedAt).Take(10).ExecuteAsync()`. Internally:
   - **`ExpressionTranslator<T>`** walks the `Expression<Func<T,bool>>` tree, extracts the member/operator/constant, and turns it into a parameterized `WHERE` fragment (`age > @p0`).
   - **`SqlGenerator`** assembles the final `SELECT` statement (`WHERE`, `ORDER BY`, `LIMIT`, `OFFSET`) from those fragments — a clean separation between *"what to filter"* (translation) and *"how to write SQL"* (generation).
7. **`UnitOfWork`** (implements `IUnitOfWork`) opens one shared `DbConnection`, lazily creates/caches a `Repository<T>` per entity type via reflection (`Repository<>.MakeGenericType`), and coordinates `BeginTransaction` / `Commit` / `Rollback` across all of them — the classic Unit-of-Work pattern used by EF Core's `DbContext.SaveChanges()`.
8. **Domain-specific exceptions** (`EntityNotFoundException`, `ConnectionException`, `SchemaValidationException`) replace generic exceptions with typed, information-rich failures (e.g. `EntityNotFoundException` carries the entity `Type` and the missing `Id`; `SchemaValidationException` carries the entity `Type` and the offending property name).

---

## 5. Class Diagram — Core Domain & Abstractions

```mermaid
classDiagram
    direction TB

    class EntityBase {
        <<abstract>>
        +int Id
        +DateTime CreatedAt
        +DateTime UpdatedAt
    }

    class DbConnectionBase {
        <<abstract>>
        #string connectionString
        +CreateConnection() IDbConnection*
    }

    class RepositoryBase~T~ {
        <<abstract>>
        #IDbConnectionFactory connectionFactory
        +Validate(T entity) bool
    }

    class IDbConnectionFactory {
        <<interface>>
        +CreateConnection() IDbConnection
    }

    class IRepository~T~ {
        <<interface>>
        +GetByIdAsync(int id) Task~T~
        +GetAllAsync() Task~IEnumerable~T~~
        +AddAsync(T entity) Task
        +UpdateAsync(T entity) Task
        +DeleteAsync(int id) Task
    }

    class IQueryBuilder~T~ {
        <<interface>>
        +Where(predicate) IQueryBuilder~T~
        +OrderBy(keySelector) IQueryBuilder~T~
        +OrderByDescending(keySelector) IQueryBuilder~T~
        +Take(count) IQueryBuilder~T~
        +Skip(count) IQueryBuilder~T~
        +ExecuteAsync() Task~IEnumerable~T~~
    }

    class IUnitOfWork {
        <<interface>>
        +Repository~T~() IRepository~T~
        +Transaction() Task
        +SaveChangesAsync() Task
    }

    class PostgresConnectionFactory {
        +CreateConnection() IDbConnection
    }

    class Repository~T~ {
        -SchemaBuilder SchemaBuilder
        -EntityMapper~T~ Mapper
        -DbConnection sharedConnection
        +AddAsync(T entity) Task
        +GetAllAsync() Task~IEnumerable~T~~
        +GetByIdAsync(int id) Task~T~
        +UpdateAsync(T entity) Task
        +DeleteAsync(int id) Task
    }

    class QueryBuilder~T~ {
        -IDbConnectionFactory _connectionFactory
        -List~Expression~ _whereConditions
        -SchemaBuilder _schemaBuilder
        -EntityMapper~T~ _mapper
        -ExpressionTranslator~T~ _translator
        -SqlGenerator _sqlGenerator
        +Where(predicate) IQueryBuilder~T~
        +OrderBy(expr) IQueryBuilder~T~
        +OrderByDescending(expr) IQueryBuilder~T~
        +Take(count) IQueryBuilder~T~
        +Skip(count) IQueryBuilder~T~
        +ExecuteAsync() Task~IEnumerable~T~~
    }

    class UnitOfWork {
        -IDbConnectionFactory _connectionFactory
        -DbConnection _connection
        -DbTransaction _transaction
        -Dictionary~Type,object~ _repositories
        +InitializeAsync() Task
        +Repository~T~() IRepository~T~
        +Transaction() Task
        +SaveChangesAsync() Task
        +RollbackAsync() Task
    }

    class EntityMapper~T~ {
        +MapToRow(T entity) Dictionary~string,object~
        +MapToEntity(IDataRecord row) T
    }

    class SchemaBuilder {
        +BuildCreateTableSql~T~() string
        +GetTableName~T~() string
        -BuildColumnDefinition(property) string
        -MapCSharpTypeToSql(property) string
    }

    class ExpressionTranslator~T~ {
        +GetSqlOperator(nodeType) string
        +TranslateExpression(expr, paramName) (string, object)
    }

    class SqlGenerator {
        +BuildSelectQuery(table, where, orderBy, dir, take, skip) string
    }

    class AttributeHelper {
        <<static>>
        +GetColumnName(property) string
    }

    class IMigration {
        <<interface>>
        +Version int
        +Name string
        +UpAsync(connection) Task
        +DownAsync(connection) Task
    }

    class MigrationRunner {
        -DbConnection _connection
        +RunAsync(migrations) Task
    }

    %% Inheritance
    DbConnectionBase ..|> IDbConnectionFactory
    PostgresConnectionFactory --|> DbConnectionBase
    RepositoryBase~T~ ..|> IRepository~T~ : (contract fulfilled by Repository~T~)
    Repository~T~ --|> RepositoryBase~T~
    Repository~T~ ..|> IRepository~T~
    QueryBuilder~T~ ..|> IQueryBuilder~T~
    UnitOfWork ..|> IUnitOfWork

    %% Composition / usage
    RepositoryBase~T~ o-- IDbConnectionFactory : uses
    Repository~T~ *-- SchemaBuilder
    Repository~T~ *-- EntityMapper~T~
    QueryBuilder~T~ *-- SchemaBuilder
    QueryBuilder~T~ *-- EntityMapper~T~
    QueryBuilder~T~ *-- ExpressionTranslator~T~
    QueryBuilder~T~ *-- SqlGenerator
    QueryBuilder~T~ o-- IDbConnectionFactory : uses
    UnitOfWork o-- IDbConnectionFactory : uses
    UnitOfWork ..> Repository~T~ : creates via reflection
    EntityMapper~T~ ..> AttributeHelper : uses
    SchemaBuilder ..> AttributeHelper : uses
    ExpressionTranslator~T~ ..> AttributeHelper : uses
    MigrationRunner o-- IMigration : runs
    IMigration ..> SchemaBuilder : uses (in Demo migrations)

    %% Entities
    Repository~T~ ..> EntityBase : constrained to
    QueryBuilder~T~ ..> EntityBase : constrained to
    EntityBase <|-- User
    EntityBase <|-- Order

    class User {
        +string Name
        +string Email
    }
    class Order {
        +int UserId
        +decimal Total
    }
```

---

## 6. Class Diagram — Attributes & Exceptions (Metadata Layer)

```mermaid
classDiagram
    direction LR

    class Attribute {
        <<.NET base>>
    }

    class TableAttribute {
        +string Name
    }
    class ColumnAttribute {
        +string Name
    }
    class PrimaryKeyAttribute
    class AutoIncrementAttribute
    class NotNullAttribute
    class UniqueAttribute
    class MaxLengthAttribute {
        +int Length
    }
    class ForeignKeyAttribute {
        +Type ReferencedType
        +string ReferencedColumn
    }

    Attribute <|-- TableAttribute
    Attribute <|-- ColumnAttribute
    Attribute <|-- PrimaryKeyAttribute
    Attribute <|-- AutoIncrementAttribute
    Attribute <|-- NotNullAttribute
    Attribute <|-- UniqueAttribute
    Attribute <|-- MaxLengthAttribute
    Attribute <|-- ForeignKeyAttribute

    class Exception {
        <<.NET base>>
    }
    class ConnectionException {
        +string DatabaseName
        +string Host
    }
    class EntityNotFoundException {
        +Type EntityType
        +object EntityId
    }
    class SchemaValidationException {
        +Type EntityType
        +string PropertyName
    }

    Exception <|-- ConnectionException
    Exception <|-- EntityNotFoundException
    Exception <|-- SchemaValidationException

    class SortDirection {
        <<enumeration>>
        Ascending
        Descending
    }
    class JoinType {
        <<enumeration>>
        Inner
        Left
        Right
        Full
    }
```

---

## 7. Sequence Diagram — `Repository<T>.AddAsync()`

Shows how attribute-driven reflection, the connection factory, and parameterized ADO.NET commands cooperate to insert a row safely.

```mermaid
sequenceDiagram
    participant Client
    participant Repo as Repository<T>
    participant Mapper as EntityMapper<T>
    participant Schema as SchemaBuilder
    participant Factory as IDbConnectionFactory
    participant DB as PostgreSQL

    Client->>Repo: AddAsync(entity)
    Repo->>Factory: CreateConnection()
    Factory-->>Repo: IDbConnection
    Repo->>DB: OpenAsync()
    Repo->>Mapper: MapToRow(entity)
    Mapper-->>Repo: Dictionary<column, value>
    Repo->>Schema: GetTableName<T>()
    Schema-->>Repo: "users"
    Repo->>Repo: build parameterized INSERT ... RETURNING id
    Repo->>DB: ExecuteScalarAsync(sql, params)
    DB-->>Repo: new id
    Repo->>Client: entity.Id = newId
```

---

## 8. Sequence Diagram — `QueryBuilder<T>.Where(...).OrderBy(...).ExecuteAsync()`

Shows how a LINQ expression becomes safe, parameterized SQL — the most technically interesting flow in the codebase.

```mermaid
sequenceDiagram
    participant Client
    participant QB as QueryBuilder<T>
    participant Translator as ExpressionTranslator<T>
    participant SqlGen as SqlGenerator
    participant Factory as IDbConnectionFactory
    participant DB as PostgreSQL
    participant Mapper as EntityMapper<T>

    Client->>QB: Where(x => x.Age > 18)
    Client->>QB: OrderByDescending(x => x.CreatedAt)
    Client->>QB: Take(10)
    Client->>QB: ExecuteAsync()

    loop for each Where() condition
        QB->>Translator: TranslateExpression(predicate, "p0")
        Translator-->>QB: ("age > @p0", 18)
    end

    QB->>SqlGen: BuildSelectQuery(table, whereClauses, orderBy, dir, take, skip)
    SqlGen-->>QB: "SELECT * FROM users WHERE age > @p0 ORDER BY created_at DESC LIMIT 10"

    QB->>Factory: CreateConnection()
    Factory-->>QB: IDbConnection
    QB->>DB: OpenAsync() + ExecuteReaderAsync(sql, params)
    DB-->>QB: rows

    loop for each row
        QB->>Mapper: MapToEntity(row)
        Mapper-->>QB: T entity
    end

    QB-->>Client: IEnumerable<T>
```

---

## 9. Sequence Diagram — `UnitOfWork` Transaction Flow

```mermaid
sequenceDiagram
    participant Client
    participant UoW as UnitOfWork
    participant Factory as IDbConnectionFactory
    participant Repo1 as Repository<User>
    participant Repo2 as Repository<Order>
    participant DB as PostgreSQL

    Client->>UoW: InitializeAsync()
    UoW->>Factory: CreateConnection()
    Factory-->>UoW: DbConnection (opened, shared)

    Client->>UoW: Transaction()
    UoW->>DB: BeginTransactionAsync()

    Client->>UoW: Repository<User>()
    UoW->>Repo1: new Repository<User>(sharedConnection)
    Client->>Repo1: AddAsync(user)
    Repo1->>DB: INSERT INTO users ...

    Client->>UoW: Repository<Order>()
    UoW->>Repo2: new Repository<Order>(sharedConnection)
    Client->>Repo2: AddAsync(order)
    Repo2->>DB: INSERT INTO orders ...

    Client->>UoW: SaveChangesAsync()
    UoW->>DB: CommitAsync()
```

---

## 10. Entity-Relationship View (Demo Domain)

Derived from the `[ForeignKey]` metadata on `Order.UserId` referencing `User`.

```mermaid
erDiagram
    USERS ||--o{ ORDERS : places
    USERS {
        int id PK
        string full_name
        string email UK
        timestamp created_at
        timestamp updated_at
    }
    ORDERS {
        int id PK
        int user_id FK
        numeric total
        timestamp created_at
        timestamp updated_at
    }
```

---

## 11. Migration Flow

```mermaid
flowchart LR
    A[MigrationRunner.RunAsync] --> B{Order migrations\nby Version}
    B --> C["001_CreateUsersTable.UpAsync()"]
    C --> D["SchemaBuilder.BuildCreateTableSql&lt;User&gt;()"]
    D --> E[(PostgreSQL: CREATE TABLE users)]
    B --> F["002_CreateOrdersTable.UpAsync()"]
    F --> G["SchemaBuilder.BuildCreateTableSql&lt;Order&gt;()"]
    G --> H[(PostgreSQL: CREATE TABLE orders,\nwith FK to users)]
```

---

## 12. Design Highlights — What This Codebase Demonstrates Well

- **Single Responsibility, taken seriously.** Each class does exactly one thing: `SchemaBuilder` only generates DDL, `SqlGenerator` only assembles `SELECT` statements, `ExpressionTranslator` only parses expression trees, `EntityMapper` only converts between objects and rows. None of these responsibilities bleed into each other.
- **Program to interfaces, not implementations.** `IDbConnectionFactory`, `IRepository<T>`, `IQueryBuilder<T>`, and `IUnitOfWork` mean the concrete Postgres/Npgsql dependency is isolated to a single class (`PostgresConnectionFactory`), making the design provider-agnostic in principle and easy to unit-test with fakes (see `NpgLiteORM.Tests/Mapping/FakeDataRecord.cs`).
- **Generics used correctly, not decoratively.** `RepositoryBase<T>`, `Repository<T>`, `QueryBuilder<T>`, `EntityMapper<T>`, `ExpressionTranslator<T>` all constrain `T` appropriately (`where T : EntityBase, new()`), giving compile-time safety without sacrificing reuse across every entity type.
- **Security by construction.** Every SQL statement across `Repository<T>` and `QueryBuilder<T>` is built with `DbParameter` bindings — there is no string-concatenated user input anywhere in the query path, which avoids SQL injection by design rather than by convention.
- **Reflection is centralized, not scattered.** All attribute lookups funnel through `AttributeHelper.GetColumnName()`, so column-naming behavior can change in one place.
- **Typed schema failures instead of generic ones.** Unsupported .NET-to-SQL type mappings raise a dedicated `SchemaValidationException` carrying the offending entity type and property name, rather than a bare `NotSupportedException` string.
- **Realistic pattern combination.** Repository + Unit of Work + Fluent Query Builder + Data Mapper is exactly the layering real ORMs use; combining them correctly (especially `UnitOfWork` creating typed repositories via `MakeGenericType` over one shared transaction) requires a solid grasp of generics, reflection, and ADO.NET transaction semantics simultaneously.
- **Typed, informative exceptions** over generic `Exception`/`InvalidOperationException` misuse — `EntityNotFoundException` and `ConnectionException` carry structured diagnostic data instead of just a message string.
- **Test coverage targets the hard parts.** Tests focus on `EntityMapper`, `SchemaBuilder`, and `ExpressionTranslator` — precisely the reflection- and expression-tree-heavy code that is easiest to get subtly wrong, showing an accurate sense of where the real risk lives.
- **Willingness to remove code, not just add it.** An earlier `ComparisonOperator` enum was deliberately removed once it became clear it duplicated .NET's own `ExpressionType` with no added value — a small but telling sign of engineering judgment over sunk-cost attachment to code already written.

## 13. Known Limitations (as of this snapshot)

- `ExpressionTranslator<T>` currently supports only a single top-level `BinaryExpression` (e.g. `x => x.Age > 18`) — compound predicates (`&&` / `||`), non-constant right-hand values (closures over local variables), and `.Contains()`/`LIKE`-style string matching are not yet handled.
- `JoinType` is defined but not yet wired into `QueryBuilder<T>` / `SqlGenerator` — the groundwork for joins is laid but not yet consumed.
- `Role.cs` in the Demo project is currently an empty placeholder entity.
- No connection pooling/retry policy is implemented beyond what Npgsql provides by default.

These are natural, well-scoped next steps rather than design flaws — the seams needed to add them (`JoinType`, `IQueryBuilder<T>`) are already in place.

---

## 14. License

MIT License — see `LICENSE`.
