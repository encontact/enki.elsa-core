using System.Threading.Tasks;
using YesSql;

namespace Elsa.Persistence.YesSql.Schema
{
    public class SchemaVersionStore : ISchemaVersionStore
    {
        private readonly ISession session;

        public SchemaVersionStore(ISession session)
        {
            this.session = session;
        }
        
        public async Task<int> GetVersionAsync()
        {
            var schemaVersion = await session.Query<SchemaVersionDocument>().FirstOrDefaultAsync();

            if (schemaVersion == null)
            {
                schemaVersion = new SchemaVersionDocument {Version = 0};
                await session.SaveAsync(schemaVersion);
                await session.SaveChangesAsync();
            }

            return schemaVersion.Version;
        }

        public async Task SaveVersionAsync(int version)
        {
            var schemaVersion = await session.Query<SchemaVersionDocument>().FirstOrDefaultAsync();
            schemaVersion.Version = version;
            await session.SaveAsync(schemaVersion);
            await session.SaveChangesAsync();
        }
    }
}