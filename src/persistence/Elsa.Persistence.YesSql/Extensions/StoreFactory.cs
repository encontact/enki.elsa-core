using System;
using Elsa.Persistence.YesSql.Serialization;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class StoreFactory
    {
        internal static IStore CreateStore(IServiceProvider services, Action<IConfiguration> configure)
        {
            IConfiguration configuration = new Configuration();

            configuration.ContentSerializer = new YesSqlJsonSerializer();

            configure(configuration);
            var store = global::YesSql.StoreFactory.Create(configuration);
            var indexes = services.GetServices<IIndexProvider>();

            store.RegisterIndexes(indexes);
            return store;
        }
    }
}