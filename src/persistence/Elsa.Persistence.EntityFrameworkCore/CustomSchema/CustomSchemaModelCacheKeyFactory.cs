using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFrameworkCore.CustomSchema
{
    public class CustomSchemaModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public CustomSchemaModelCacheKeyFactory()
        {
        }

        // designTime parameter was ignored in this implementation 
        // after EF lib upgrade.
        // To More: https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.infrastructure.imodelcachekeyfactory?view=efcore-8.0
        public object Create(DbContext context, bool designTime)
        {
            string schema = null;
            if (context is ElsaContext)
            {
                var dbContextCustomSchema = ((ElsaContext)context).DbContextCustomSchema;
                if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
                {
                    schema = dbContextCustomSchema.Schema;
                }
            }

            return new
            {
                Type = context.GetType(),
                Schema = schema
            };
        }
    }
}