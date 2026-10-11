using System.Collections.Generic;
using System.Linq;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Elsa.Services;
using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.EntityFrameworkCore.Mapping
{
    public class EntitiesProfile : MappingProfile
    {
        public EntitiesProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionEntity>(ToEntity, MapInto);
            CreateMap<WorkflowDefinitionVersionEntity, WorkflowDefinitionVersion>(ToModel);

            CreateMap<WorkflowInstance, WorkflowInstanceEntity>(ToEntity, MapInto);
            CreateMap<WorkflowInstanceEntity, WorkflowInstance>(ToModel);

            CreateMap<ActivityDefinition, ActivityDefinitionEntity>(ToEntity);
            CreateMap<ActivityDefinitionEntity, ActivityDefinition>(ToModel);

            CreateMap<ActivityInstance, ActivityInstanceEntity>(ToEntity);
            CreateMap<ActivityInstanceEntity, ActivityInstance>(ToModel);

            CreateMap<BlockingActivity, BlockingActivityEntity>(ToEntity);
            CreateMap<BlockingActivityEntity, BlockingActivity>(ToModel);

            CreateMap<ConnectionDefinition, ConnectionDefinitionEntity>(ToEntity);
            CreateMap<ConnectionDefinitionEntity, ConnectionDefinition>(ToModel);
        }

        // --- WorkflowDefinitionVersion <-> WorkflowDefinitionVersionEntity ---

        private static WorkflowDefinitionVersionEntity ToEntity(WorkflowDefinitionVersion source) => MapInto(source, new WorkflowDefinitionVersionEntity());

        private static WorkflowDefinitionVersionEntity MapInto(WorkflowDefinitionVersion source, WorkflowDefinitionVersionEntity entity)
        {
            entity.VersionId = source.Id;
            entity.DefinitionId = source.DefinitionId;
            entity.Version = source.Version;
            entity.Name = source.Name;
            entity.Description = source.Description;
            entity.Variables = CloneVariables(source.Variables);
            entity.IsSingleton = source.IsSingleton;
            entity.IsDisabled = source.IsDisabled;
            entity.IsPublished = source.IsPublished;
            entity.IsLatest = source.IsLatest;
            entity.Activities = source.Activities?.Select(ToEntity).ToList() ?? new List<ActivityDefinitionEntity>();
            entity.Connections = source.Connections?.Select(ToEntity).ToList() ?? new List<ConnectionDefinitionEntity>();
            return entity;
        }

        private static WorkflowDefinitionVersion ToModel(WorkflowDefinitionVersionEntity source) => new WorkflowDefinitionVersion(
            id: source.VersionId,
            definitionId: source.DefinitionId,
            version: source.Version,
            name: source.Name,
            description: source.Description,
            activities: source.Activities?.Select(ToModel) ?? Enumerable.Empty<ActivityDefinition>(),
            connections: source.Connections?.Select(ToModel) ?? Enumerable.Empty<ConnectionDefinition>(),
            isSingleton: source.IsSingleton,
            isDisabled: source.IsDisabled,
            variables: CloneVariables(source.Variables))
        {
            IsPublished = source.IsPublished,
            IsLatest = source.IsLatest
        };

        // --- WorkflowInstance <-> WorkflowInstanceEntity ---

        private static WorkflowInstanceEntity ToEntity(WorkflowInstance source) => MapInto(source, new WorkflowInstanceEntity());

        private static WorkflowInstanceEntity MapInto(WorkflowInstance source, WorkflowInstanceEntity entity)
        {
            entity.InstanceId = source.Id;
            entity.DefinitionId = source.DefinitionId;
            entity.Version = source.Version;
            entity.Status = source.Status;
            entity.CorrelationId = source.CorrelationId;
            entity.CreatedAt = NodaTimeProfile.ToDateTime(source.CreatedAt);
            entity.StartedAt = NodaTimeProfile.ToNullableDateTime(source.StartedAt);
            entity.FinishedAt = NodaTimeProfile.ToNullableDateTime(source.FinishedAt);
            entity.FaultedAt = NodaTimeProfile.ToNullableDateTime(source.FaultedAt);
            entity.AbortedAt = NodaTimeProfile.ToNullableDateTime(source.AbortedAt);
            entity.Scope = source.Scope;
            entity.Input = CloneVariables(source.Input);
            entity.Fault = source.Fault;

            entity.Activities ??= new List<ActivityInstanceEntity>();
            ReplaceCollection(entity.Activities, (source.Activities ?? new Dictionary<string, ActivityInstance>()).Values.Select(ToEntity));

            entity.BlockingActivities ??= new List<BlockingActivityEntity>();
            ReplaceCollection(entity.BlockingActivities, (source.BlockingActivities ?? Enumerable.Empty<BlockingActivity>()).Select(ToEntity));

            entity.ExecutionLog ??= new List<LogEntry>();
            ReplaceCollection(entity.ExecutionLog, source.ExecutionLog ?? Enumerable.Empty<LogEntry>());

            return entity;
        }

        private static WorkflowInstance ToModel(WorkflowInstanceEntity source) => new WorkflowInstance
        {
            Id = source.InstanceId,
            DefinitionId = source.DefinitionId,
            Version = source.Version,
            Status = source.Status,
            CorrelationId = source.CorrelationId,
            CreatedAt = NodaTimeProfile.ToInstant(source.CreatedAt),
            StartedAt = NodaTimeProfile.ToNullableInstant(source.StartedAt),
            FinishedAt = NodaTimeProfile.ToNullableInstant(source.FinishedAt),
            FaultedAt = NodaTimeProfile.ToNullableInstant(source.FaultedAt),
            AbortedAt = NodaTimeProfile.ToNullableInstant(source.AbortedAt),
            Activities = (source.Activities ?? new List<ActivityInstanceEntity>()).ToDictionary(x => x.ActivityId, ToModel),
            Scope = source.Scope,
            Input = CloneVariables(source.Input),
            BlockingActivities = new HashSet<BlockingActivity>((source.BlockingActivities ?? new List<BlockingActivityEntity>()).Select(ToModel)),
            ExecutionLog = source.ExecutionLog == null ? new List<LogEntry>() : new List<LogEntry>(source.ExecutionLog),
            Fault = source.Fault
        };

        // --- ActivityDefinition <-> ActivityDefinitionEntity ---

        private static ActivityDefinitionEntity ToEntity(ActivityDefinition source) => new ActivityDefinitionEntity
        {
            ActivityId = source.Id,
            Type = source.Type,
            Name = source.Name,
            DisplayName = source.DisplayName,
            Description = source.Description,
            Left = source.Left,
            Top = source.Top,
            State = CloneJObject(source.State)
        };

        private static ActivityDefinition ToModel(ActivityDefinitionEntity source)
        {
            var model = new ActivityDefinition(source.ActivityId, source.Type, CloneJObject(source.State) ?? new JObject(), source.Left, source.Top)
            {
                DisplayName = source.DisplayName,
                Description = source.Description
            };

            // Name is a computed property backed by State["name"]; re-assign to mirror the entity's own Name field.
            model.Name = source.Name;

            return model;
        }

        // --- ActivityInstance <-> ActivityInstanceEntity ---

        private static ActivityInstanceEntity ToEntity(ActivityInstance source) => new ActivityInstanceEntity
        {
            ActivityId = source.Id,
            Type = source.Type,
            State = CloneJObject(source.State),
            Output = CloneJObject(source.Output)
        };

        private static ActivityInstance ToModel(ActivityInstanceEntity source) => new ActivityInstance
        {
            Id = source.ActivityId,
            Type = source.Type,
            State = CloneJObject(source.State),
            Output = CloneJObject(source.Output)
        };

        // --- BlockingActivity <-> BlockingActivityEntity ---

        private static BlockingActivityEntity ToEntity(BlockingActivity source) => new BlockingActivityEntity
        {
            ActivityId = source.ActivityId,
            ActivityType = source.ActivityType
        };

        private static BlockingActivity ToModel(BlockingActivityEntity source) => new BlockingActivity(source.ActivityId, source.ActivityType);

        // --- ConnectionDefinition <-> ConnectionDefinitionEntity ---

        private static ConnectionDefinitionEntity ToEntity(ConnectionDefinition source) => new ConnectionDefinitionEntity
        {
            SourceActivityId = source.SourceActivityId,
            DestinationActivityId = source.DestinationActivityId,
            Outcome = source.Outcome
        };

        private static ConnectionDefinition ToModel(ConnectionDefinitionEntity source) => new ConnectionDefinition(source.SourceActivityId, source.DestinationActivityId, source.Outcome);

        // --- shared helpers ---

        private static Variables CloneVariables(Variables source) => source == null ? null : new Variables(source);

        private static JObject CloneJObject(JObject source) => source == null ? null : new JObject(source);

        private static void ReplaceCollection<T>(ICollection<T> destination, IEnumerable<T> items)
        {
            destination.Clear();

            foreach (var item in items)
                destination.Add(item);
        }
    }
}
