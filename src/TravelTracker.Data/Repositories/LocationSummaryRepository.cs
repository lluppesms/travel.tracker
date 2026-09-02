using System.Data;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace TravelTracker.Data.Repositories;

/// <summary>
/// Executes <c>[Travel].[usp_LocationSummary]</c> using the shared <see cref="TravelTrackerDbContext"/>
/// connection and formats the multiple result sets into a compact text block for prompt injection.
/// </summary>
public sealed class LocationSummaryRepository(TravelTrackerDbContext context) : ILocationSummaryRepository
{
    private readonly TravelTrackerDbContext _context = context;

    public async Task<string?> GetLocationSummaryTextAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "[Travel].[usp_LocationSummary]";
            command.CommandTimeout = 30;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@UserName";
            parameter.Value = userName;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var builder = new StringBuilder();
            do
            {
                AppendResultSet(builder, reader);
            }
            while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

            return builder.Length == 0 ? null : builder.ToString();
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static void AppendResultSet(StringBuilder builder, IDataReader reader)
    {
        var tableNameOrdinal = TryGetOrdinal(reader, "TableName");
        var columnNames = GetColumnNames(reader, tableNameOrdinal);
        var sectionHeaderWritten = false;

        while (reader.Read())
        {
            if (!sectionHeaderWritten)
            {
                var sectionName = tableNameOrdinal >= 0
                    ? Convert.ToString(reader.GetValue(tableNameOrdinal))
                    : null;
                builder.AppendLine($"## {(string.IsNullOrWhiteSpace(sectionName) ? "Data" : sectionName)}");
                sectionHeaderWritten = true;
            }

            var fields = columnNames
                .Select(column => $"{column}={FormatValue(reader[column])}");
            builder.AppendLine(string.Join(", ", fields));
        }

        if (sectionHeaderWritten)
        {
            builder.AppendLine();
        }
    }

    private static List<string> GetColumnNames(IDataReader reader, int tableNameOrdinal)
    {
        var columns = new List<string>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (i == tableNameOrdinal)
            {
                continue;
            }

            columns.Add(reader.GetName(i));
        }

        return columns;
    }

    private static int TryGetOrdinal(IDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string FormatValue(object? value) =>
        value is null or DBNull ? string.Empty : Convert.ToString(value) ?? string.Empty;
}
