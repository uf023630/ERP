using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SmartIMS.Web.Infrastructure;

public sealed class SqlConnectionFactory
{
    public SqlConnection CreateConnection() => new(BuildConnectionString());

    public SqlConnection CreateMasterConnection() => new(BuildMasterConnectionString());

    public string GetDatabaseName()
    {
        return BuildSettings().Database;
    }

    public string BuildConnectionString()
    {
        var settings = BuildSettings();
        return BuildConnectionString(settings.Server, settings.Database, settings.User, settings.Password, settings.Encrypt, settings.TrustServerCertificate, settings.UseIntegratedSecurity);
    }

    public string BuildMasterConnectionString()
    {
        var settings = BuildSettings();
        return BuildConnectionString(settings.Server, "master", settings.User, settings.Password, settings.Encrypt, settings.TrustServerCertificate, settings.UseIntegratedSecurity);
    }

    private static SqlConnectionSettings BuildSettings()
    {
        var explicitConnection = Environment.GetEnvironmentVariable("MSSQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return NormalizeSqlConnectionString(explicitConnection);
        }

        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "SmartIMS_NewVersion";
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var useIntegratedSecurity = string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password);

        return new SqlConnectionSettings(
            string.Equals(port, "1433", StringComparison.OrdinalIgnoreCase) ? host : $"{host},{port}",
            database,
            user,
            password,
            ParseBoolean(Environment.GetEnvironmentVariable("DB_ENCRYPT"), fallback: false),
            ParseBoolean(Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERT"), fallback: true),
            useIntegratedSecurity);
    }

    private static SqlConnectionSettings NormalizeSqlConnectionString(string connectionString)
    {
        var values = ParseSegments(connectionString);
        var server = GetValue(values, "Server") ?? GetValue(values, "Data Source")
            ?? throw new InvalidOperationException("MSSQL_CONNECTION_STRING is missing Server/Data Source.");
        var database = GetValue(values, "Database") ?? GetValue(values, "Initial Catalog") ?? "SmartIMS_NewVersion";
        var integratedSecurity = ParseBoolean(GetValue(values, "Integrated Security") ?? GetValue(values, "Trusted_Connection"), fallback: false);
        var user = GetValue(values, "User ID") ?? GetValue(values, "Uid");
        var password = GetValue(values, "Password") ?? GetValue(values, "Pwd");
        if (!integratedSecurity && (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password)))
        {
            throw new InvalidOperationException("MSSQL_CONNECTION_STRING must include User ID/Password or Integrated Security.");
        }

        return new SqlConnectionSettings(
            server,
            database,
            user,
            password,
            ParseBoolean(GetValue(values, "Encrypt"), fallback: false),
            ParseBoolean(GetValue(values, "TrustServerCertificate"), fallback: true),
            integratedSecurity);
    }

    private static string BuildConnectionString(string server, string database, string? user, string? password, bool encrypt, bool trustServerCertificate, bool useIntegratedSecurity)
    {
        var builder = new DbConnectionStringBuilder
        {
            ["Server"] = server,
            ["Database"] = database,
            ["Encrypt"] = encrypt,
            ["TrustServerCertificate"] = trustServerCertificate,
            ["MultipleActiveResultSets"] = false,
            ["Connect Timeout"] = 15
        };

        if (useIntegratedSecurity)
        {
            builder["Integrated Security"] = true;
        }
        else
        {
            builder["User ID"] = user;
            builder["Password"] = password;
        }

        return builder.ConnectionString;
    }

    private static Dictionary<string, string> ParseSegments(string connectionString)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = segment.IndexOf('=');
            if (equalsIndex > 0)
            {
                values[segment[..equalsIndex].Trim()] = segment[(equalsIndex + 1)..].Trim();
            }
        }

        return values;
    }

    private static string? GetValue(Dictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : null;
    }

    private static bool ParseBoolean(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SqlConnectionSettings(
        string Server,
        string Database,
        string? User,
        string? Password,
        bool Encrypt,
        bool TrustServerCertificate,
        bool UseIntegratedSecurity);
}
