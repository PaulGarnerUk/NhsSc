using Nhs.Sc.Excel;
using Nhs.Sc.Sqlite;

namespace Nhs.Sc;

class DedupeCommand
{
    public void Execute(string spreadsheetPath)
    {
        Console.WriteLine("Validating file...");
        SpreadsheetReader.Validate(spreadsheetPath);

        Console.WriteLine("Reading spreadsheet...");
        var table = SpreadsheetReader.Read(spreadsheetPath);
        Console.WriteLine($"Found {table.Rows.Count} rows.");

        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "catalog.db");
        Console.WriteLine($"Rebuilding database: {dbPath}");
        CatalogRepository.Rebuild(dbPath, table);

        Console.WriteLine("Finding duplicate MPC codes...");
        var groups = CatalogRepository.GetDuplicateMpcGroups(dbPath);
        Console.WriteLine($"Found {groups.Count} MPC codes with duplicates ({groups.Sum(g => g.Count)} rows total).");

        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "output.xlsx");
        Console.WriteLine($"Writing report: {outputPath}");
        SpreadsheetWriter.WriteDedupeReport(outputPath, groups);

        Console.WriteLine("Done.");
    }
}
