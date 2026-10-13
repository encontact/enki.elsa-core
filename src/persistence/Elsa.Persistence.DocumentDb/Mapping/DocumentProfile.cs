using System.Collections.Generic;
using System.Linq;
using Elsa.Mapping;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Services;

namespace Elsa.Persistence.DocumentDb.Mapping
{
    public sealed class DocumentProfile : MappingProfile
    {
        public DocumentProfile(ITenantProvider tenantProvider)
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionDocument>(source => ToDocument(source, tenantProvider));
            CreateMap<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersion>(ToModel);

            CreateMap<WorkflowInstance, WorkflowInstanceDocument>(source => ToDocument(source, tenantProvider));
            CreateMap<WorkflowInstanceDocument, WorkflowInstance>(ToModel);
        }

        // --- WorkflowDefinitionVersion <-> WorkflowDefinitionVersionDocument ---

        private static WorkflowDefinitionVersionDocument ToDocument(WorkflowDefinitionVersion source, ITenantProvider tenantProvider) => new WorkflowDefinitionVersionDocument
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            Version = source.Version,
            Name = source.Name,
            Description = source.Description,
            Activities = source.Activities == null ? null : new List<ActivityDefinition>(source.Activities),
            Connections = source.Connections == null ? null : new List<ConnectionDefinition>(source.Connections),
            Variables = source.Variables == null ? null : new Variables(source.Variables),
            IsSingleton = source.IsSingleton,
            IsDisabled = source.IsDisabled,
            IsPublished = source.IsPublished,
            IsLatest = source.IsLatest,
            TenantId = tenantProvider.GetTenantId<WorkflowDefinitionVersionDocument>()
        };

        private static WorkflowDefinitionVersion ToModel(WorkflowDefinitionVersionDocument source) => new WorkflowDefinitionVersion(
            id: source.Id,
            definitionId: source.DefinitionId,
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

        private static WorkflowInstanceDocument ToDocument(WorkflowInstance source, ITenantProvider tenantProvider) => new WorkflowInstanceDocument
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            Version = source.Version,
            Status = source.Status,
            CorrelationId = source.CorrelationId,
            CreatedAt = NodaTimeProfile.ToDateTime(source.CreatedAt),
            StartedAt = NodaTimeProfile.ToNullableDateTime(source.StartedAt),
            FinishedAt = NodaTimeProfile.ToNullableDateTime(source.FinishedAt),
            FaultedAt = NodaTimeProfile.ToNullableDateTime(source.FaultedAt),
            AbortedAt = NodaTimeProfile.ToNullableDateTime(source.AbortedAt),
            Activities = source.Activities == null ? new Dictionary<string, ActivityInstance>() : new Dictionary<string, ActivityInstance>(source.Activities),
            Scope = source.Scope,
            Input = source.Input == null ? null : new Variables(source.Input),
            BlockingActivities = source.BlockingActivities == null ? null : new HashSet<BlockingActivity>(source.BlockingActivities),
            ExecutionLog = source.ExecutionLog == null ? null : new List<LogEntry>(source.ExecutionLog),
            Fault = source.Fault,
            TenantId = tenantProvider.GetTenantId<WorkflowInstanceDocument>()
        };

        private static WorkflowInstance ToModel(WorkflowInstanceDocument source) => new WorkflowInstance
        {
            Id = source.Id,
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
