using System.Data;
using Microsoft.Data.Sqlite;

namespace Nhs.Sc.Sqlite;

static class CatalogRepository
{
    const string CreateTableSql = """
        CREATE TABLE Catalog (
            id                   INTEGER PRIMARY KEY AUTOINCREMENT,
            NPC                  TEXT,
            EClass               TEXT,
            Section              TEXT,
            BaseDescription      TEXT,
            SecondaryDescription TEXT,
            Supplier             TEXT,
            Brand                TEXT,
            MPC                  TEXT,
            UOI                  TEXT,
            Unit                 NUMERIC,
            B1Price              NUMERIC,
            IndividualPrice      NUMERIC
        )
        """;

    public static void Rebuild(string dbPath, DataTable table)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        Execute(connection, "DROP TABLE IF EXISTS Catalog");
        Execute(connection, CreateTableSql);

        using var transaction = connection.BeginTransaction();

        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            INSERT INTO Catalog
                (NPC, EClass, Section, BaseDescription, SecondaryDescription, Supplier, Brand, MPC, UOI, Unit, B1Price, IndividualPrice)
            VALUES
                (@NPC, @EClass, @Section, @BaseDescription, @SecondaryDescription, @Supplier, @Brand, @MPC, @UOI, @Unit, @B1Price, @IndividualPrice)
            """;

        var pNpc = cmd.Parameters.Add("@NPC", SqliteType.Text);
        var pEClass = cmd.Parameters.Add("@EClass", SqliteType.Text);
        var pSection = cmd.Parameters.Add("@Section", SqliteType.Text);
        var pBaseDesc = cmd.Parameters.Add("@BaseDescription", SqliteType.Text);
        var pSecDesc = cmd.Parameters.Add("@SecondaryDescription", SqliteType.Text);
        var pSupplier = cmd.Parameters.Add("@Supplier", SqliteType.Text);
        var pBrand = cmd.Parameters.Add("@Brand", SqliteType.Text);
        var pMpc = cmd.Parameters.Add("@MPC", SqliteType.Text);
        var pUoi = cmd.Parameters.Add("@UOI", SqliteType.Text);
        var pUnit = cmd.Parameters.Add("@Unit", SqliteType.Real);
        var pB1Price = cmd.Parameters.Add("@B1Price", SqliteType.Real);
        var pIndividualPrice = cmd.Parameters.Add("@IndividualPrice", SqliteType.Real);

        foreach (DataRow row in table.Rows)
        {
            pNpc.Value = TextOrNull(row["NPC"]);
            pEClass.Value = TextOrNull(row["EClass"]);
            pSection.Value = TextOrNull(row["Section"]);
            pBaseDesc.Value = TextOrNull(row["BaseDescription"]);
            pSecDesc.Value = TextOrNull(row["SecondaryDescription"]);
            pSupplier.Value = TextOrNull(row["Supplier"]);
            pBrand.Value = TextOrNull(row["Brand"]);
            pMpc.Value = TextOrNull(row["MPC"]);
            pUoi.Value = TextOrNull(row["UOI"]);

            var unit = NumericOrNull(row["Units"]);
            var b1Price = NumericOrNull(row["B1_Price"]);
            double? individualPrice = (unit is > 0 && b1Price is not null)
                ? b1Price.Value / unit.Value
                : null;

            pUnit.Value = unit.HasValue ? unit.Value : DBNull.Value;
            pB1Price.Value = b1Price.HasValue ? b1Price.Value : DBNull.Value;
            pIndividualPrice.Value = individualPrice.HasValue ? individualPrice.Value : DBNull.Value;

            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static List<List<CatalogRow>> GetDuplicateMpcGroups(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT NPC, MPC, BaseDescription, SecondaryDescription, UOI, B1Price, IndividualPrice
            FROM Catalog
            WHERE MPC IS NOT NULL AND MPC != ''
              AND MPC IN (
                  SELECT MPC FROM Catalog
                  WHERE MPC IS NOT NULL AND MPC != ''
                  GROUP BY MPC HAVING COUNT(*) > 1
              )
            ORDER BY MPC,
                     CASE WHEN IndividualPrice IS NULL THEN 1 ELSE 0 END,
                     IndividualPrice ASC
            """;

        var groups = new Dictionary<string, List<CatalogRow>>(StringComparer.Ordinal);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var mpc = reader.GetString(1);
            var row = new CatalogRow(
                Npc: reader.IsDBNull(0) ? null : reader.GetString(0),
                Mpc: mpc,
                BaseDescription: reader.IsDBNull(2) ? null : reader.GetString(2),
                SecondaryDescription: reader.IsDBNull(3) ? null : reader.GetString(3),
                Uoi: reader.IsDBNull(4) ? null : reader.GetString(4),
                ListPrice: reader.IsDBNull(5) ? null : reader.GetDouble(5),
                IndividualPrice: reader.IsDBNull(6) ? null : reader.GetDouble(6)
            );

            if (!groups.TryGetValue(mpc, out var list))
            {
                list = [];
                groups[mpc] = list;
            }
            list.Add(row);
        }

        return [.. groups.Values];
    }

    static void Execute(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    static object TextOrNull(object value) =>
        value is DBNull || value is null || string.IsNullOrWhiteSpace(value.ToString())
            ? DBNull.Value
            : (object)value.ToString()!;

    static double? NumericOrNull(object value)
    {
        if (value is DBNull || value is null) return null;
        var str = value.ToString();
        if (string.IsNullOrWhiteSpace(str)) return null;
        return double.TryParse(str, out var d) ? d : null;
    }
}
