# NpgLiteORM

A lightweight, educational Mini ORM built with C# and PostgreSQL, designed to demonstrate the four core principles of Object-Oriented Programming (Encapsulation, Inheritance, Polymorphism, and Abstraction) through a real-world architecture.

> ⚠️ This project is a work in progress, built as a learning exercise. Not intended for production use yet.

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

## Tech Stack

- **.NET 10**
- **C#**
- **PostgreSQL** (via [Npgsql](https://www.npgsql.org/))
- **Docker** (for local PostgreSQL setup)

## Project Structure

```
NpgLiteORM.Core/              → Attributes, interfaces, abstract base classes
NpgLiteORM.Core/Data/          → Connection factory, pooling, transactions
NpgLiteORM.Core/Mapping/       → Entity mapper, schema builder (reflection-based)
NpgLiteORM.Core/Query/         → Query builder, expression tree translator
NpgLiteORM.Core/Repositories/  → Generic repository, unit of work
NpgLiteORM.Core/Migrations/    → Schema versioning and migration runner
NpgLiteORM.Core/Exceptions/    → Custom exception types
NpgLiteORM.Demo/               → Console app for testing the library
```

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

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
