using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.DataStore.SqlServer;

namespace Umea.se.EstateService.Test.DataStore;

public class SqlServerPersistenceTruncationTests
{
    private static ITable GetBuildingsTable()
    {
        DbContextOptions<EstateDbContext> options = new DbContextOptionsBuilder<EstateDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;

        using EstateDbContext context = new(options);
        return context.Model.GetRelationalModel().FindTable("Buildings", null).ShouldNotBeNull();
    }

    private static DataTable CreateBuildingsTable(string? yearOfConstruction, string? zipCode = null)
    {
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("YearOfConstruction", typeof(string));
        table.Columns.Add("AddressZipCode", typeof(string));
        table.Rows.Add(2273, (object?)yearOfConstruction ?? DBNull.Value, (object?)zipCode ?? DBNull.Value);
        return table;
    }

    [Fact]
    public void TruncateOverlongStrings_ValueExceedsMaxLength_TruncatesToMaxLength()
    {
        ITable storeTable = GetBuildingsTable();
        int maxLength = storeTable.FindColumn("YearOfConstruction")!.MaxLength!.Value;
        DataTable table = CreateBuildingsTable(new string('x', maxLength + 10), "903 3456 7890 1234 5678");

        SqlServerPersistence.TruncateOverlongStrings(table, storeTable, NullLogger.Instance);

        ((string)table.Rows[0]["YearOfConstruction"]).Length.ShouldBe(maxLength);
        ((string)table.Rows[0]["AddressZipCode"]).ShouldBe("903 3456 7890 1234 5");
    }

    [Theory]
    [InlineData("ImageIds")]
    [InlineData("WorkOrderTypes")]
    public void TruncateOverlongStrings_ValueConvertedColumnExceedsMaxLength_LeavesValueIntact(string columnName)
    {
        ITable storeTable = GetBuildingsTable();
        int maxLength = storeTable.FindColumn(columnName)!.MaxLength!.Value;
        string json = "[" + string.Join(',', Enumerable.Range(1, maxLength)) + "]";
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add(columnName, typeof(string));
        table.Rows.Add(2273, json);

        SqlServerPersistence.TruncateOverlongStrings(table, storeTable, NullLogger.Instance);

        table.Rows[0][columnName].ShouldBe(json);
    }

    [Theory]
    [InlineData("Byggnad 01: 2002, Byggnad 02: 1966, Byggnad 03: 1965")]
    [InlineData("1973")]
    [InlineData(null)]
    public void TruncateOverlongStrings_ValueWithinMaxLength_LeavesValueUnchanged(string? yearOfConstruction)
    {
        DataTable table = CreateBuildingsTable(yearOfConstruction);

        SqlServerPersistence.TruncateOverlongStrings(table, GetBuildingsTable(), NullLogger.Instance);

        table.Rows[0]["YearOfConstruction"].ShouldBe((object?)yearOfConstruction ?? DBNull.Value);
    }
}
