using System.Collections.Generic;
using System.Linq;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Services;

namespace Elsa.Persistence.YesSql.Mapping
{
    public class DocumentProfile : MappingProfile
    {
        public DocumentProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionDocument>(ToDocument, MapInto);
            CreateMap<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersion>(ToModel);

            CreateMap<WorkflowInstance, WorkflowInstanceDocument>(ToDocument, MapInto);
            CreateMap<WorkflowInstanceDocument, WorkflowInstance>(ToModel);
        }

        // --- WorkflowDefinitionVersion <-> WorkflowDefinitionVersionDocument ---

        private static WorkflowDefinitionVersionDocument ToDocument(WorkflowDefinitionVersion source) => MapInto(source, new WorkflowDefinitionVersionDocument());

        private static WorkflowDefinitionVersionDocument MapInto(WorkflowDefinitionVersion source, WorkflowDefinitionVersionDocument document)
        {
            document.WorkflowDefinitionVersionId = source.Id;
            document.WorkflowDefinitionId = source.DefinitionId;
            document.Version = source.Version;
            document.Name = source.Name;
            document.Description = source.Description;
            document.Activities = source.Activities == null ? null : new List<ActivityDefinition>(source.Activities);
            document.Connections = source.Connections == null ? null : new List<ConnectionDefinition>(source.Connections);
            document.Variables = source.Variables == null ? null : new Variables(source.Variables);
            document.IsSingleton = source.IsSingleton;
            document.IsDisabled = source.IsDisabled;
            document.IsPublished = source.IsPublished;
            document.IsLatest = source.IsLatest;
            return document;
        }

        private static WorkflowDefinitionVersion ToModel(WorkflowDefinitionVersionDocument source) => new WorkflowDefinitionVersion(
            id: source.WorkflowDefinitionVersionId,
            definitionId: source.WorkflowDefinitionId,
            version: source.Version,
            name: source.Name,
            description: source.Description,
            activities: source.Activities ?? Enumerable.Empty<ActivityDefinition>(),
            connections: source.Connections ?? Enumerable.Empty<ConnectionDefinition>(),
            isSingleton: source.IsSingleton,
            isDisabled: source.IsDisabled,
            variables: source.Variables == null ? null : new Variables(source.Variables))
        {
            IsPublished = source.IsPublished,
            IsLatest = source.IsLatest
        };

        // --- WorkflowInstance <-> WorkflowInstanceDocument ---

        private static WorkflowInstanceDocument ToDocument(WorkflowInstance source) => MapInto(source, new WorkflowInstanceDocument());

        private static WorkflowInstanceDocument MapInto(WorkflowInstance source, WorkflowInstanceDocument document)
        {
            document.WorkflowInstanceId = source.Id;
            document.DefinitionId = source.DefinitionId;
            document.Version = source.Version;
            document.Status = source.Status;
            document.CorrelationId = source.CorrelationId;
            document.CreatedAt = NodaTimeProfile.ToDateTime(source.CreatedAt);
            document.StartedAt = NodaTimeProfile.ToNullableDateTime(source.StartedAt);
            document.FinishedAt = NodaTimeProfile.ToNullableDateTime(source.FinishedAt);
            document.FaultedAt = NodaTimeProfile.ToNullableDateTime(source.FaultedAt);
            document.AbortedAt = NodaTimeProfile.ToNullableDateTime(source.AbortedAt);
            document.Activities = source.Activities == null ? new Dictionary<string, ActivityInstance>() : new Dictionary<string, ActivityInstance>(source.Activities);
            document.Scope = source.Scope;
            document.Input = source.Input == null ? null : new Variables(source.Input);
            document.BlockingActivities = source.BlockingActivities == null ? null : new HashSet<BlockingActivity>(source.BlockingActivities);
            document.ExecutionLog = source.ExecutionLog == null ? null : new List<LogEntry>(source.ExecutionLog);
            document.Fault = source.Fault;
            return document;
        }

        private static WorkflowInstance ToModel(WorkflowInstanceDocument source) => new WorkflowInstance
        {
            Id = source.WorkflowInstanceId,
            DefinitionId = source.DefinitionId,
            Version = source.Version,
            Status = source.Status,
            CorrelationId = source.CorrelationId,
            CreatedAt = NodaTimeProfile.ToInstant(source.CreatedAt),
            StartedAt = NodaTimeProfile.ToNullableInstant(source.StartedAt),
            FinishedAt = NodaTimeProfile.ToNullableInstant(source.FinishedAt),
            FaultedAt = NodaTimeProfile.ToNullableInstant(source.FaultedAt),
            AbortedAt = NodaTimeProfile.ToNullableInstant(source.AbortedAt),
            Activities = source.Activities == null ? new Dictionary<string, ActivityInstance>() : new Dictionary<string, ActivityInstance>(source.Activities),
            Scope = source.Scope,
            Input = source.Input == null ? null : new Variables(source.Input),
            BlockingActivities = source.BlockingActivities == null ? null : new HashSet<BlockingActivity>(source.BlockingActivities),
            ExecutionLog = source.ExecutionLog == null ? null : new List<LogEntry>(source.ExecutionLog),
            Fault = source.Fault
        };
    }
}
