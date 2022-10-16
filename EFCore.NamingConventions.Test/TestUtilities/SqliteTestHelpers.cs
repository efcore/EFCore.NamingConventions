using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Sqlite.Diagnostics.Internal;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class SqliteTestHelpers : TestHelpers
{
    protected SqliteTestHelpers()
    {
    }

    public static SqliteTestHelpers Instance { get; } = new();

    public override IServiceCollection AddProviderServices(IServiceCollection services)
        => services.AddEntityFrameworkSqlite();

    public override DbContextOptionsBuilder UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite(new SqliteConnection("Data Source=:memory:"));

#pragma warning disable EF1001
    public override LoggingDefinitions LoggingDefinitions { get; } = new SqliteLoggingDefinitions();
#pragma warning restore EF1001
}
