using System.Linq;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Elsa.Persistence.EntityFrameworkCore.Tests.DbContexts
{
    /// <summary>
    /// Guards the MySQL/MariaDB provider (Pomelo) against EF Core versions it is not binary compatible with:
    /// Pomelo 9.0.0 on EF Core 10 throws MissingMethodException as soon as UseMySql is called. None of these
    /// tests open a database connection.
    /// </summary>
    public class MySqlContextTests
    {
        private static MySqlContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MySqlContext>()
                .UseMySql("Server=localhost;Database=elsa;User=elsa;Password=elsa", MariaDbServerVersion.LatestSupportedServerVersion)
                .Options;

            return new MySqlContext(options);
        }

        [Fact]
        public void UseMySql_WithMariaDbServerVersion_BuildsModel()
        {
            using var context = CreateContext();

            Assert.NotNull(context.Model.FindEntityType(typeof(WorkflowDefinitionVersionEntity)));
            Assert.NotNull(context.Model.FindEntityType(typeof(WorkflowInstanceEntity)));
        }

        [Fact]
        public void UseMySql_WithMariaDbServerVersion_FindsMySqlMigrations()
        {
            using var context = CreateContext();

            Assert.Contains(context.Database.GetMigrations(), x => x.EndsWith("_InitialCreate"));
            Assert.Equal(3, context.Database.GetMigrations().Count());
        }
    }
}
