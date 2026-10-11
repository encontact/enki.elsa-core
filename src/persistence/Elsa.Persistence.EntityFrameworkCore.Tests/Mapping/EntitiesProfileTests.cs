using System.Collections.Generic;
using System.Linq;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Elsa.Persistence.EntityFrameworkCore.Mapping;
using Elsa.Services;
using NodaTime;
using Xunit;

namespace Elsa.Persistence.EntityFrameworkCore.Tests.Mapping
{
    /// <summary>
    /// Locks in the mapping behavior of <see cref="EntitiesProfile"/> now that it is hand-written C# instead
    /// of AutoMapper configuration (see GHSA-rvv3-g6hj-g44x for why AutoMapper was dropped). The profiles here
    /// are combined exactly as MapperServiceCollectionExtensions.CreateMapper does at runtime: every registered
    /// MappingProfile, including NodaTimeProfile, in a single ManualMapper.
    /// </summary>
    public class EntitiesProfileTests
    {
        private static IMapper CreateMapper() => new ManualMapper(new MappingProfile[] { new EntitiesProfile(), new NodaTimeProfile() });

        [Fact]
        public void Map_WorkflowDefinitionVersionToEntity_MapsIdToVersionIdAndIgnoresEntityId()
        {
            var mapper = CreateMapper();
            var source = new WorkflowDefinitionVersion(
                id: "version-1",
                definitionId: "definition-1",
                version: 2,
                name: "Workflow",
                description: "desc",
                activities: Enumerable.Empty<ActivityDefinition>(),
                connections: Enumerable.Empty<ConnectionDefinition>(),
                isSingleton: true,
                isDisabled: false,
                variables: new Variables());

            var entity = mapper.Map<WorkflowDefinitionVersionEntity>(source);

            Assert.Equal(0, entity.Id);
            Assert.Equal("version-1", entity.VersionId);
            Assert.Equal("definition-1", entity.DefinitionId);
            Assert.Equal(2, entity.Version);
            Assert.Equal("Workflow", entity.Name);
            Assert.Equal("desc", entity.Description);
            Assert.True(entity.IsSingleton);
            Assert.False(entity.IsDisabled);
        }

        [Fact]
        public void Map_WorkflowDefinitionVersionEntityToModel_MapsVersionIdToIdViaConstructorAndMember()
        {
            var mapper = CreateMapper();
            var entity = new WorkflowDefinitionVersionEntity
            {
                Id = 42,
                VersionId = "version-1",
                DefinitionId = "definition-1",
                Version = 2,
                Name = "Workflow",
                Description = "desc",
                Activities = new List<ActivityDefinitionEntity>(),
                Connections = new List<ConnectionDefinitionEntity>()
            };

            var model = mapper.Map<WorkflowDefinitionVersion>(entity);

            Assert.Equal("version-1", model.Id);
            Assert.Equal("definition-1", model.DefinitionId);
            Assert.Equal(2, model.Version);
        }

        [Fact]
        public void MapInto_WorkflowDefinitionVersionIntoExistingEntity_PreservesExistingEntityId()
        {
            var mapper = CreateMapper();
            var existingEntity = new WorkflowDefinitionVersionEntity { Id = 42, VersionId = "stale-id" };
            var source = new WorkflowDefinitionVersion(
                id: "version-1",
                definitionId: "definition-1",
                version: 2,
                name: "Workflow",
                description: "desc",
                activities: Enumerable.Empty<ActivityDefinition>(),
                connections: Enumerable.Empty<ConnectionDefinition>(),
                isSingleton: true,
                isDisabled: false,
                variables: new Variables());

            var result = mapper.Map(source, existingEntity);

            Assert.Same(existingEntity, result);
            Assert.Equal(42, result.Id);
            Assert.Equal("version-1", result.VersionId);
        }

        [Fact]
        public void Map_WorkflowInstanceToEntity_ConvertsActivitiesDictionaryToCollectionAndMapsId()
        {
            var mapper = CreateMapper();
            var source = new WorkflowInstance
            {
                Id = "instance-1",
                DefinitionId = "definition-1",
                Version = 1,
                CreatedAt = Instant.FromUtc(2024, 3, 15, 10, 0, 0),
                Activities = new Dictionary<string, ActivityInstance>
                {
                    ["activity-1"] = new ActivityInstance { Id = "activity-1", Type = "Console" }
                }
            };

            var entity = mapper.Map<WorkflowInstanceEntity>(source);

            Assert.Equal(0, entity.Id);
            Assert.Equal("instance-1", entity.InstanceId);
            Assert.Equal(new System.DateTime(2024, 3, 15, 10, 0, 0, System.DateTimeKind.Utc), entity.CreatedAt);
            Assert.Single(entity.Activities);
            Assert.Equal("activity-1", entity.Activities.First().ActivityId);
        }

        [Fact]
        public void Map_WorkflowInstanceEntityToModel_ConvertsActivitiesCollectionToDictionaryAndMapsId()
        {
            var mapper = CreateMapper();
            var entity = new WorkflowInstanceEntity
            {
                Id = 7,
                InstanceId = "instance-1",
                DefinitionId = "definition-1",
                Version = 1,
                CreatedAt = new System.DateTime(2024, 3, 15, 10, 0, 0, System.DateTimeKind.Utc),
                Activities = new List<ActivityInstanceEntity>
                {
                    new ActivityInstanceEntity { ActivityId = "activity-1", Type = "Console" }
                },
                BlockingActivities = new List<BlockingActivityEntity>(),
                ExecutionLog = new List<LogEntry>()
            };

            var model = mapper.Map<WorkflowInstance>(entity);

            Assert.Equal("instance-1", model.Id);
            Assert.Equal(Instant.FromUtc(2024, 3, 15, 10, 0, 0), model.CreatedAt);
            Assert.True(model.Activities.ContainsKey("activity-1"));
            Assert.Equal("Console", model.Activities["activity-1"].Type);
        }

        [Fact]
        public void MapInto_WorkflowInstanceIntoExistingEntity_ReusesExistingTrackedCollections()
        {
            // EF Core's stores clear WorkflowInstanceEntity.Activities/BlockingActivities in place (so the
            // change tracker still owns those exact List instances) and then call mapper.Map(instance, entity).
            // The mapping must refill those same collections rather than replacing them with new ones.
            var mapper = CreateMapper();
            var existingEntity = new WorkflowInstanceEntity
            {
                Id = 5,
                Activities = new List<ActivityInstanceEntity>(),
                BlockingActivities = new List<BlockingActivityEntity>(),
                ExecutionLog = new List<LogEntry>()
            };
            var originalActivities = existingEntity.Activities;
            var originalBlockingActivities = existingEntity.BlockingActivities;

            var source = new WorkflowInstance
            {
                Id = "instance-1",
                Activities = new Dictionary<string, ActivityInstance> { ["a1"] = new ActivityInstance { Id = "a1", Type = "Console" } },
                BlockingActivities = new HashSet<BlockingActivity> { new BlockingActivity("a1", "UserTask") }
            };

            var result = mapper.Map(source, existingEntity);

            Assert.Same(existingEntity, result);
            Assert.Equal(5, result.Id);
            Assert.Same(originalActivities, result.Activities);
            Assert.Same(originalBlockingActivities, result.BlockingActivities);
            Assert.Single(result.Activities);
            Assert.Single(result.BlockingActivities);
        }

        [Fact]
        public void Map_ActivityDefinitionToEntity_MapsIdToActivityIdAndIgnoresNavigationProperty()
        {
            var mapper = CreateMapper();
            var source = new ActivityDefinition("activity-1", "Console", new Newtonsoft.Json.Linq.JObject(), left: 10, top: 20);

            var entity = mapper.Map<ActivityDefinitionEntity>(source);

            Assert.Equal(0, entity.Id);
            Assert.Equal("activity-1", entity.ActivityId);
            Assert.Equal("Console", entity.Type);
            Assert.Equal(10, entity.Left);
            Assert.Equal(20, entity.Top);
            Assert.Null(entity.WorkflowDefinitionVersion);
        }

        [Fact]
        public void Map_ActivityDefinitionEntityToModel_MapsActivityIdToIdViaConstructorAndMember()
        {
            var mapper = CreateMapper();
            var entity = new ActivityDefinitionEntity
            {
                Id = 1,
                ActivityId = "activity-1",
                Type = "Console",
                State = new Newtonsoft.Json.Linq.JObject(),
                Left = 10,
                Top = 20
            };

            var model = mapper.Map<ActivityDefinition>(entity);

            Assert.Equal("activity-1", model.Id);
            Assert.Equal("Console", model.Type);
            Assert.Equal(10, model.Left);
            Assert.Equal(20, model.Top);
        }

        [Fact]
        public void Map_ActivityInstanceToEntity_MapsIdToActivityIdAndIgnoresNavigationProperty()
        {
            var mapper = CreateMapper();
            var source = new ActivityInstance { Id = "activity-1", Type = "Console" };

            var entity = mapper.Map<ActivityInstanceEntity>(source);

            Assert.Equal(0, entity.Id);
            Assert.Equal("activity-1", entity.ActivityId);
            Assert.Null(entity.WorkflowInstance);
        }

        [Fact]
        public void Map_ActivityInstanceEntityToModel_MapsActivityIdToId()
        {
            var mapper = CreateMapper();
            var entity = new ActivityInstanceEntity { ActivityId = "activity-1", Type = "Console" };

            var model = mapper.Map<ActivityInstance>(entity);

            Assert.Equal("activity-1", model.Id);
        }

        [Fact]
        public void Map_BlockingActivity_RoundTripsThroughEntityIgnoringNavigationAndId()
        {
            var mapper = CreateMapper();
            var source = new BlockingActivity("activity-1", "UserTask");

            var entity = mapper.Map<BlockingActivityEntity>(source);
            var roundTripped = mapper.Map<BlockingActivity>(entity);

            Assert.Equal(0, entity.Id);
            Assert.Null(entity.WorkflowInstance);
            Assert.Equal("activity-1", entity.ActivityId);
            Assert.Equal("UserTask", entity.ActivityType);
            Assert.Equal(source.ActivityId, roundTripped.ActivityId);
            Assert.Equal(source.ActivityType, roundTripped.ActivityType);
        }

        [Fact]
        public void Map_ConnectionDefinition_RoundTripsThroughEntityIgnoringNavigationAndId()
        {
            var mapper = CreateMapper();
            var source = new ConnectionDefinition("activity-1", "activity-2", "Done");

            var entity = mapper.Map<ConnectionDefinitionEntity>(source);
            var roundTripped = mapper.Map<ConnectionDefinition>(entity);

            Assert.Equal(0, entity.Id);
            Assert.Null(entity.WorkflowDefinitionVersion);
            Assert.Equal(source.SourceActivityId, roundTripped.SourceActivityId);
            Assert.Equal(source.DestinationActivityId, roundTripped.DestinationActivityId);
            Assert.Equal(source.Outcome, roundTripped.Outcome);
        }

        [Fact]
        public void Map_CollectionOfEntities_MapsEachElement()
        {
            var mapper = CreateMapper();
            var entities = new List<WorkflowDefinitionVersionEntity>
            {
                new WorkflowDefinitionVersionEntity { VersionId = "v1", Activities = new List<ActivityDefinitionEntity>(), Connections = new List<ConnectionDefinitionEntity>() },
                new WorkflowDefinitionVersionEntity { VersionId = "v2", Activities = new List<ActivityDefinitionEntity>(), Connections = new List<ConnectionDefinitionEntity>() }
            };

            var models = mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(entities);

            Assert.Equal(new[] { "v1", "v2" }, models.Select(x => x.Id));
        }
    }
}
