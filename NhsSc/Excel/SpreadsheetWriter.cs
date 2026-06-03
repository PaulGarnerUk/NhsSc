using ClosedXML.Excel;

namespace Nhs.Sc.Excel;

static class SpreadsheetWriter
{
    static readonly XLColor HeaderBackground = XLColor.FromArgb(0xD9, 0xD9, 0xD9);
    static readonly XLColor CheapestBackground = XLColor.FromArgb(0xC6, 0xEF, 0xCE);

    public static void WriteDedupeReport(string outputPath, List<List<CatalogRow>> groups)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Duplicates");

        WriteHeaders(sheet);

        int rowNum = 2;
        foreach (var group in groups)
        {
            var pricesInGroup = group
                .Where(r => r.IndividualPrice.HasValue)
                .Select(r => r.IndividualPrice!.Value)
                .ToList();

            double? cheapestPrice = pricesInGroup.Count > 0 ? pricesInGroup.Min() : null;
            double? mostExpensivePrice = pricesInGroup.Count > 0 ? pricesInGroup.Max() : null;

            foreach (var item in group)
            {
                bool isCheapest = item.IndividualPrice == cheapestPrice;

                double? pctSavingVsMostExpensive = null;
                if (item.IndividualPrice.HasValue && mostExpensivePrice is double mp && mp > 0)
                    pctSavingVsMostExpensive = (mp - item.IndividualPrice.Value) / mp * 100.0;

                double? pctMoreExpensiveThanCheapest = null;
                if (!isCheapest && item.IndividualPrice.HasValue && cheapestPrice is double cp && cp > 0)
                    pctMoreExpensiveThanCheapest = (item.IndividualPrice.Value - cp) / cp * 100.0;

                WriteDataRow(sheet, rowNum, item, pctSavingVsMostExpensive, pctMoreExpensiveThanCheapest);

                if (isCheapest)
                    sheet.Row(rowNum).Style.Fill.BackgroundColor = CheapestBackground;

                rowNum++;
            }

            rowNum++; // blank row between groups
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }

    static void WriteHeaders(IXLWorksheet sheet)
    {
        string[] headers =
        [
            "MPC", "NPC", "Base Description", "Secondary Description",
            "UOI", "List Price", "Individual Price",
            "% Saving vs Most Expensive", "% More Expensive than Cheapest"
        ];
        for (int c = 0; c < headers.Length; c++)
            sheet.Cell(1, c + 1).Value = headers[c];

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = HeaderBackground;
    }

    static void WriteDataRow(IXLWorksheet sheet, int rowNum, CatalogRow item,
        double? pctSavingVsMostExpensive, double? pctMoreExpensiveThanCheapest)
    {
        sheet.Cell(rowNum, 1).Value = item.Mpc ?? "";
        sheet.Cell(rowNum, 2).Value = item.Npc ?? "";
        sheet.Cell(rowNum, 3).Value = item.BaseDescription ?? "";
        sheet.Cell(rowNum, 4).Value = item.SecondaryDescription ?? "";
        sheet.Cell(rowNum, 5).Value = item.Uoi ?? "";

        if (item.ListPrice.HasValue)
        {
            sheet.Cell(rowNum, 6).Value = item.ListPrice.Value;
            sheet.Cell(rowNum, 6).Style.NumberFormat.Format = "#,##0.00";
        }

        if (item.IndividualPrice.HasValue)
        {
            sheet.Cell(rowNum, 7).Value = item.IndividualPrice.Value;
            sheet.Cell(rowNum, 7).Style.NumberFormat.Format = "#,##0.0000";
        }

        if (pctSavingVsMostExpensive.HasValue)
        {
            sheet.Cell(rowNum, 8).Value = Math.Round(pctSavingVsMostExpensive.Value, 1);
            sheet.Cell(rowNum, 8).Style.NumberFormat.Format = "0.0\"%\"";
        }

        if (pctMoreExpensiveThanCheapest.HasValue)
        {
            sheet.Cell(rowNum, 9).Value = Math.Round(pctMoreExpensiveThanCheapest.Value, 1);
            sheet.Cell(rowNum, 9).Style.NumberFormat.Format = "0.0\"%\"";
        }
    }
}
