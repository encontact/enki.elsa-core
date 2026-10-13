using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;
using Elsa.Messaging;
using Newtonsoft.Json.Linq;

namespace Elsa.Scripting.Liquid.Handlers
{
    public class CommonLiquidContextHandler : INotificationHandler<EvaluatingLiquidExpression>
    {
        internal static readonly TemplateOptions Options = new TemplateContext().Options;

        static CommonLiquidContextHandler()
        {
            // Fluid resolves these per-TemplateContext via an implicit, process-wide default
            // TemplateOptions instance, so these converters must be registered exactly once.
            Options.ValueConverters.Add(x => x is ExpandoObject expando ? new ObjectValue(expando) : null);
            Options.ValueConverters.Add(x => x is JObject jObject ? new ObjectValue(jObject) : null);
            Options.ValueConverters.Add(x => x is JValue jValue ? jValue.Value : null);
        }

        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;
            var options = context.Options;

            options.MemberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
            options.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Input", x => new LiquidPropertyAccessor(name => ToFluidValue(x.Workflow.Input, name, options)));
            options.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Output", x => new LiquidPropertyAccessor(name => ToFluidValue(x.Workflow.Output, name, options)));
            options.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => ToFluidValue(x.GetVariables(), name, options)));
            options.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("TransientState", x => new LiquidPropertyAccessor(name => ToFluidValue(x.TransientState, name, options)));
            options.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidObjectAccessor<IActivity>>("Activities", x => new LiquidObjectAccessor<IActivity>(name => GetActivityAsync(x, name)));
            options.MemberAccessStrategy.Register<LiquidObjectAccessor<IActivity>, LiquidObjectAccessor<object>>((x, activityName) => new LiquidObjectAccessor<object>(outputKey => GetActivityOutput(x, activityName, outputKey)));
            options.MemberAccessStrategy.Register<LiquidObjectAccessor<object>, object>((x, name) => x.GetValueAsync(name));
            options.MemberAccessStrategy.Register<ExpandoObject, object>((x, name) => ((IDictionary<string, object>)x)[name]);
            options.MemberAccessStrategy.Register<JObject, object>((source, name) => source[name]);

            return Task.CompletedTask;
        }

        private Task<FluidValue> ToFluidValue(IDictionary<string, Variable> dictionary, string key, TemplateOptions options)
            => Task.FromResult(!dictionary.TryGetValue(key, out var variable) ? default : FluidValue.Create(variable.Value, options));

        private async Task<object> GetActivityOutput(LiquidObjectAccessor<IActivity> accessor, string activityName, string outputKey)
        {
            var activity = await accessor.GetValueAsync(activityName);
            var output = activity.Output;
            return output.GetVariable(outputKey);
        }

        private Task<IActivity> GetActivityAsync(WorkflowExecutionContext executionContext, string name) 
            => Task.FromResult(executionContext.Workflow.Activities.FirstOrDefault(x => x.Name == name));
    }
}