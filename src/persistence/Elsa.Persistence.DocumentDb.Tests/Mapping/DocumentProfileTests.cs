using System.Collections.Generic;
using System.Linq;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Mapping;
using Elsa.Services;
using Moq;
using NodaTime;
using Xunit;

namespace Elsa.Persistence.DocumentDb.Tests.Mapping
{
    public class DocumentProfileTests
    {
        private static IMapper CreateMapper(string tenantId = "tenant-1")
        {
            var tenantProvider = new Mock<ITenantProvider>();
            tenantProvider.Setup(x => x.GetTenantId<WorkflowDefinitionVersionDocument>()).Returns(tenantId);
            tenantProvider.Setup(x => x.GetTenantId<WorkflowInstanceDocument>()).Returns(tenantId);

            return new ManualMapper(new MappingProfile[] { new DocumentProfile(tenantProvider.Object), new NodaTimeProfile() });
        }

        [Fact]
        public void Map_WorkflowDefinitionVersionToDocument_MapsIdDirectlyAndSetsTenantId()
        {
            var mapper = CreateMapper("tenant-42");
            var source = new WorkflowDefinitionVersion(
                id: "version-1",
                definitionId: "definition-1",
                version: 2,
                name: "Workflow",
                description: "desc",
                activities: new[] { new ActivityDefinition("a1", "Console", new Newtonsoft.Json.Linq.JObject()) },
                connections: new[] { new ConnectionDefinition("a1", "a2", "Done") },
                isSingleton: true,
                isDisabled: false,
                variables: new Variables());

            var document = mapper.Map<WorkflowDefinitionVersionDocument>(source);

            Assert.Equal("version-1", document.Id);
            Assert.Equal("definition-1", document.DefinitionId);
            Assert.Equal(2, document.Version);
            Assert.Equal("tenant-42", document.TenantId);
            Assert.Single(document.Activities);
            Assert.Single(document.Connections);
        }

        [Fact]
        public void Map_WorkflowDefinitionVersionDocumentToModel_MapsIdDirectly()
        {
            var mapper = CreateMapper();
            var document = new WorkflowDefinitionVersionDocument
            {
                Id = "version-1",
                DefinitionId = "definition-1",
                Version = 2,
                Name = "Workflow",
                Activities = new List<ActivityDefinition>(),
                Connections = new List<ConnectionDefinition>()
            };

            var model = mapper.Map<WorkflowDefinitionVersion>(document);

            Assert.Equal("version-1", model.Id);
            Assert.Equal("definition-1", model.DefinitionId);
            Assert.Equal(2, model.Version);
        }

        [Fact]
        public void Map_WorkflowInstanceToDocument_ConvertsInstantsAndSetsTenantId()
        {
            var mapper = CreateMapper("tenant-7");
            var source = new WorkflowInstance
            {
                Id = "instance-1",
                CreatedAt = Instant.FromUtc(2024, 3, 15, 10, 0, 0)
            };

            var document = mapper.Map<WorkflowInstanceDocument>(source);

            Assert.Equal("instance-1", document.Id);
            Assert.Equal(new System.DateTime(2024, 3, 15, 10, 0, 0, System.DateTimeKind.Utc), document.CreatedAt);
            Assert.Equal("tenant-7", document.TenantId);
        }

        [Fact]
        public void Map_WorkflowInstanceDocumentToModel_ConvertsDateTimesToInstants()
        {
            var mapper = CreateMapper();
            var document = new WorkflowInstanceDocument
            {
                Id = "instance-1",
                CreatedAt = new System.DateTime(2024, 3, 15, 10, 0, 0, System.DateTimeKind.Utc)
            };

            var model = mapper.Map<WorkflowInstance>(document);

            Assert.Equal("instance-1", model.Id);
            Assert.Equal(Instant.FromUtc(2024, 3, 15, 10, 0, 0), model.CreatedAt);
        }

        [Fact]
        public void Map_CollectionOfDocuments_MapsEachElement()
        {
            var mapper = CreateMapper();
            var documents = new List<WorkflowInstanceDocument>
            {
                new WorkflowInstanceDocument { Id = "i1" },
                new WorkflowInstanceDocument { Id = "i2" }
            };

            var models = mapper.Map<IEnumerable<WorkflowInstance>>(documents);

            Assert.Equal(new[] { "i1", "i2" }, models.Select(x => x.Id));
        }
    }
}
