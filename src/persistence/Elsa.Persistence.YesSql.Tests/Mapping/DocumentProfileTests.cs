using System.Collections.Generic;
using System.Linq;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Mapping;
using Elsa.Services;
using NodaTime;
using Xunit;

namespace Elsa.Persistence.YesSql.Tests.Mapping
{
    public class DocumentProfileTests
    {
        private static IMapper CreateMapper() => new ManualMapper(new MappingProfile[] { new DocumentProfile(), new NodaTimeProfile() });

        [Fact]
        public void Map_WorkflowDefinitionVersionToDocument_MapsIdsAndIgnoresSurrogateId()
        {
            var mapper = CreateMapper();
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

            Assert.Equal(0, document.Id);
            Assert.Equal("version-1", document.WorkflowDefinitionVersionId);
            Assert.Equal("definition-1", document.WorkflowDefinitionId);
            Assert.Equal(2, document.Version);
            Assert.Equal("Workflow", document.Name);
            Assert.True(document.IsSingleton);
            Assert.Single(document.Activities);
            Assert.Single(document.Connections);
        }

        [Fact]
        public void Map_WorkflowDefinitionVersionDocumentToModel_MapsIdsBack()
        {
            var mapper = CreateMapper();
            var document = new WorkflowDefinitionVersionDocument
            {
                Id = 99,
                WorkflowDefinitionVersionId = "version-1",
                WorkflowDefinitionId = "definition-1",
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
        public void MapInto_WorkflowDefinitionVersionIntoExistingDocument_PreservesSurrogateId()
        {
            var mapper = CreateMapper();
            var existingDocument = new WorkflowDefinitionVersionDocument { Id = 7 };
            var source = new WorkflowDefinitionVersion(
                id: "version-1",
                definitionId: "definition-1",
                version: 2,
                name: "Workflow",
                description: "desc",
                activities: Enumerable.Empty<ActivityDefinition>(),
                connections: Enumerable.Empty<ConnectionDefinition>(),
                isSingleton: false,
                isDisabled: false,
                variables: new Variables());

            var result = mapper.Map(source, existingDocument);

            Assert.Same(existingDocument, result);
            Assert.Equal(7, result.Id);
            Assert.Equal("version-1", result.WorkflowDefinitionVersionId);
        }

        [Fact]
        public void Map_WorkflowInstanceToDocument_ConvertsInstantsToUtcDateTimes()
        {
            var mapper = CreateMapper();
            var source = new WorkflowInstance
            {
                Id = "instance-1",
                CreatedAt = Instant.FromUtc(2024, 3, 15, 10, 0, 0),
                StartedAt = Instant.FromUtc(2024, 3, 15, 10, 5, 0)
            };

            var document = mapper.Map<WorkflowInstanceDocument>(source);

            Assert.Equal("instance-1", document.WorkflowInstanceId);
            Assert.Equal(new System.DateTime(2024, 3, 15, 10, 0, 0, System.DateTimeKind.Utc), document.CreatedAt);
            Assert.Equal(new System.DateTime(2024, 3, 15, 10, 5, 0, System.DateTimeKind.Utc), document.StartedAt);
        }

        [Fact]
        public void Map_WorkflowInstanceDocumentToModel_ConvertsUtcDateTimesToInstants()
        {
            var mapper = CreateMapper();
            var document = new WorkflowInstanceDocument
            {
                WorkflowInstanceId = "instance-1",
                CreatedAt = new System.DateTime(2024, 3, 15, 10, 0, 0, System.DateTimeKind.Utc),
                FinishedAt = null
            };

            var model = mapper.Map<WorkflowInstance>(document);

            Assert.Equal("instance-1", model.Id);
            Assert.Equal(Instant.FromUtc(2024, 3, 15, 10, 0, 0), model.CreatedAt);
            Assert.Null(model.FinishedAt);
        }

        [Fact]
        public void MapInto_WorkflowInstanceIntoExistingDocument_PreservesSurrogateId()
        {
            var mapper = CreateMapper();
            var existingDocument = new WorkflowInstanceDocument { Id = 3 };
            var source = new WorkflowInstance { Id = "instance-1" };

            var result = mapper.Map(source, existingDocument);

            Assert.Same(existingDocument, result);
            Assert.Equal(3, result.Id);
            Assert.Equal("instance-1", result.WorkflowInstanceId);
        }
    }
}
