using Nhs.Sc.Excel;
using Nhs.Sc.Sqlite;

namespace Nhs.Sc;

class RebuildCatalogCommand
{
    public void Execute(string spreadsheetPath)
    {
        Console.WriteLine("Validating file...");
        SpreadsheetReader.Validate(spreadsheetPath);

        Console.WriteLine("Reading spreadsheet...");
        var table = SpreadsheetReader.Read(spreadsheetPath);
        Console.WriteLine($"Found {table.Rows.Count} rows.");

        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "catalog.db");
        Console.WriteLine($"Opening database: {dbPath}");

        CatalogRepository.Rebuild(dbPath, table);

        Console.WriteLine("Catalog database rebuilt successfully.");
    }
}
