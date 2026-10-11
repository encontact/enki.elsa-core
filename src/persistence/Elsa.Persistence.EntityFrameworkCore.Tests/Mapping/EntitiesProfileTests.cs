using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Elsa.Persistence.EntityFrameworkCore.Mapping;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;

namespace Elsa.Persistence.EntityFrameworkCore.Tests.Mapping
{
    /// <summary>
    /// Locks in the mapping behavior of <see cref="EntitiesProfile"/> across the AutoMapper version bump
    /// (13.0.1 -> 16.2.0, see GHSA-rvv3-g6hj-g44x). The profiles here are combined exactly as
    /// MapperServiceCollectionExtensions.CreateMapper does at runtime: every registered MappingProfile,
    /// including NodaTimeProfile, in a single MapperConfiguration.
    /// </summary>
    public class EntitiesProfileTests
    {
        private static AutoMapper.IMapper CreateMapper()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntitiesProfile>();
                cfg.AddProfile<NodaTimeProfile>();
            }, NullLoggerFactory.Instance);

            return configuration.CreateMapper();
        }

        [Fact]
        public void Configuration_WithAllRegisteredProfiles_IsValid()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntitiesProfile>();
                cfg.AddProfile<NodaTimeProfile>();
            }, NullLoggerFactory.Instance);

            configuration.AssertConfigurationIsValid();
        }

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
    }
}
