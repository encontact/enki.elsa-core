using System.Linq;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Services;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Elsa.Core.Tests.Mapping
{
    public class WorkflowDefinitionProfileTests
    {
        private static IMapper CreateMapper() => new ManualMapper(new MappingProfile[] { new WorkflowDefinitionProfile() });

        [Fact]
        public void Map_WorkflowDefinitionVersionToItself_CopiesAllScalarFields()
        {
            var mapper = CreateMapper();
            var source = new WorkflowDefinitionVersion(
                id: "version-1",
                definitionId: "definition-1",
                version: 3,
                name: "My Workflow",
                description: "A sample workflow",
                activities: new[] { new ActivityDefinition("activity-1", "Console", new JObject()) },
                connections: new[] { new ConnectionDefinition("activity-1", "activity-2", "Done") },
                isSingleton: true,
                isDisabled: false,
                variables: new Variables());

            var result = mapper.Map<WorkflowDefinitionVersion>(source);

            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.DefinitionId, result.DefinitionId);
            Assert.Equal(source.Version, result.Version);
            Assert.Equal(source.Name, result.Name);
            Assert.Equal(source.Description, result.Description);
            Assert.Equal(source.IsSingleton, result.IsSingleton);
            Assert.Equal(source.IsDisabled, result.IsDisabled);
            Assert.Single(result.Activities);
            Assert.Single(result.Connections);
        }

        [Fact]
        public void Map_WorkflowDefinitionVersionToItself_CopiesActivitiesListButKeepsElementReferences()
        {
            // WorkflowDefinitionProfile.Clone() passes the Activities enumerable straight into the
            // WorkflowDefinitionVersion constructor, which only materializes a new List via .ToList();
            // it does not clone the ActivityDefinition elements themselves.
            var mapper = CreateMapper();
            var activity = new ActivityDefinition("activity-1", "Console", new JObject());
            var source = new WorkflowDefinitionVersion(
                id: "version-1",
                definitionId: "definition-1",
                version: 1,
                name: "My Workflow",
                description: "",
                activities: new[] { activity },
                connections: new ConnectionDefinition[0],
                isSingleton: false,
                isDisabled: false,
                variables: new Variables());

            var result = mapper.Map<WorkflowDefinitionVersion>(source);

            Assert.NotSame(source.Activities, result.Activities);
            Assert.Same(activity, result.Activities.First());
        }
    }
}
