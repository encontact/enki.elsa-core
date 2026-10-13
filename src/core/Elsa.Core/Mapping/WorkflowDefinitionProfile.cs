using Elsa.Models;
using Elsa.Services;

namespace Elsa.Mapping
{
    public sealed class WorkflowDefinitionProfile : MappingProfile
    {
        public WorkflowDefinitionProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersion>(Clone);
        }

        private static WorkflowDefinitionVersion Clone(WorkflowDefinitionVersion source) => new WorkflowDefinitionVersion(
            source.Id,
            source.DefinitionId,
            source.Version,
            source.Name,
            source.Description,
            source.Activities,
            source.Connections,
            source.IsSingleton,
            source.IsDisabled,
            source.Variables == null ? null : new Variables(source.Variables))
        {
            IsPublished = source.IsPublished,
            IsLatest = source.IsLatest
        };
    }
}
