using System.Data;
using System.Text;
using ExcelDataReader;

namespace Nhs.Sc.Excel;

static class SpreadsheetReader
{
	static SpreadsheetReader()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}

	static readonly string[] RequiredColumns =
	[
		"NPC", "EClass", "Section", "BaseDescription", "SecondaryDescription", "Supplier", "Brand", "MPC", "UOI", "Units", "B1_Price"
	];

	public static void Validate(string path)
	{
		if (!File.Exists(path))
		{
			throw new Exception($"Error - File not found: {path}");
		}

		var ext = Path.GetExtension(path).ToLowerInvariant();
		if (ext != ".xls" && ext != ".xlsx")
		{
			throw new Exception($"Error - File is not an Excel spreadsheet (.xls or .xlsx): {path}");
		}

		using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var reader = ExcelReaderFactory.CreateReader(stream);

		var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
		{
			ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
		});

		var actual = dataSet.Tables[0].Columns.Cast<DataColumn>()
			.Select(c => c.ColumnName.ToLowerInvariant())
			.ToHashSet();

		var missing = RequiredColumns
			.Where(col => !actual.Contains(col.ToLowerInvariant()))
			.ToList();

		if (missing.Count > 0)
		{
			throw new Exception($"Error - Catalog file has missing columns: {string.Join(", ", missing)}");
		}
	}

	public static DataTable Read(string path)
	{
		using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var reader = ExcelReaderFactory.CreateReader(stream);

		var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
		{
			ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
		});

		return dataSet.Tables[0];
	}
}
