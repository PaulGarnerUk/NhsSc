# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A C# CLI tool that imports NHS Supply Chain product catalogue data from an Excel spreadsheet into a SQLite database and produces a deduplication report identifying products with matching MPC (Manufacturer Product Code) values.

## Commands

```bash
# Build
dotnet build

# Run – validate, import spreadsheet, and write output.xlsx in one step
dotnet run --project NhsSc -- --dedupe "C:\path\to\catalogue.xlsx"

# Publish as self-contained single-file Windows executable
dotnet publish NhsSc -c Release
```

Reads the input spreadsheet from the path provided; writes `catalog.db` and `output.xlsx` to the current working directory. No test project or linter is configured.

## Architecture

Single pipeline dispatched from `Program.cs`:

`Program.cs` → `DedupeCommand` → `Excel/SpreadsheetReader` → `Sqlite/CatalogRepository.Rebuild()` → `Sqlite/CatalogRepository.GetDuplicateMpcGroups()` → `Excel/SpreadsheetWriter`

**`Excel/SpreadsheetReader`** — wraps `ExcelDataReader`. `Validate()` checks file existence, extension, and that all required column names are present (case-insensitive). `Read()` returns a raw `DataTable`. Registers `CodePagesEncodingProvider` to support legacy `.xls` format.

**`Sqlite/CatalogRepository`** — `Rebuild()` drops and recreates the `Catalog` table and bulk-inserts all rows in a single transaction, deriving `IndividualPrice = B1_Price / Units`. `GetDuplicateMpcGroups()` returns rows grouped by MPC where count > 1, ordered by MPC then `IndividualPrice ASC NULLS LAST`.

**`Excel/SpreadsheetWriter`** — wraps `ClosedXML`. Writes one sheet with all duplicate groups, a blank row between groups, and the cheapest row in each group highlighted green. Per-group price calculations:
- `% Saving vs Most Expensive`: `(maxPrice − thisPrice) / maxPrice × 100` — populated for all rows (0% on the most expensive).
- `% More Expensive than Cheapest`: `(thisPrice − minPrice) / minPrice × 100` — populated only on non-cheapest rows.

**`CatalogRow`** — shared record used as the hand-off type between `CatalogRepository` and `SpreadsheetWriter`.

## Key Details

- Required spreadsheet columns: `NPC`, `EClass`, `Section`, `BaseDescription`, `SecondaryDescription`, `Supplier`, `Brand`, `MPC`, `UOI`, `Units`, `B1_Price`.
- `catalog.db` and `output.xlsx` are always written to the current working directory.
- Solution uses the modern `.slnx` format (VS 2022+) and centralized NuGet versioning via `Directory.Packages.props`.
- Target framework is `net10.0`; nullable reference types and implicit usings are enabled project-wide via `Directory.Build.props`.
- NuGet packages: `ExcelDataReader` + `ExcelDataReader.DataSet` (reading), `Microsoft.Data.Sqlite` (SQLite), `ClosedXML` (writing `.xlsx`).
