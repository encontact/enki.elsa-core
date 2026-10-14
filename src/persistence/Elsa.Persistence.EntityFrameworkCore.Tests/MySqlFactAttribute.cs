using System;
using Xunit;

namespace Elsa.Persistence.EntityFrameworkCore.Tests
{
    /// <summary>
    /// A fact that runs against a real MySQL/MariaDB server, whose connection string is read from
    /// <see cref="ConnectionStringVariable"/>. It is skipped when the variable is not set, because the server-side
    /// restrictions these tests guard against (such as MariaDB error 1093) cannot be reproduced without a real server.
    /// </summary>
    public sealed class MySqlFactAttribute : FactAttribute
    {
        public const string ConnectionStringVariable = "ELSA_TEST_MYSQL_CONNECTION";

        public MySqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                Skip = $"Set {ConnectionStringVariable} to run this test against MySQL/MariaDB.";
        }

        public static string? ConnectionString => Environment.GetEnvironmentVariable(ConnectionStringVariable);
    }
}
