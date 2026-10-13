using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Messaging;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services;
using Elsa.Services.Models;
using Fluid.Values;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace Elsa.Scripting.Liquid.Tests
{
    /// <summary>
    /// End-to-end tests of the Liquid evaluator, covering the built-in context handler and the extension point used by
    /// library consumers (custom <see cref="INotificationHandler{TNotification}"/> for <see cref="EvaluatingLiquidExpression"/>).
    /// </summary>
    public class LiquidExpressionEvaluatorTests
    {
        public class GreetingHandler : INotificationHandler<EvaluatingLiquidExpression>
        {
            public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
            {
                notification.TemplateContext.SetValue("greeting", new StringValue("Hello"));
                return Task.CompletedTask;
            }
        }

        private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddElsaCore().AddLiquidExpressionEvaluator();
            configure?.Invoke(services);
            return services.BuildServiceProvider();
        }

        private static async Task<object> EvaluateAsync(IServiceProvider provider, string expression)
        {
            using var scope = provider.CreateScope();
            var evaluator = scope.ServiceProvider.GetServices<IExpressionEvaluator>().Single(x => x.Syntax == LiquidExpressionEvaluator.SyntaxName);
            var workflow = new Workflow { Input = new Variables() };
            var context = new WorkflowExecutionContext(workflow, scope.ServiceProvider.GetRequiredService<IClock>(), scope.ServiceProvider);
            context.CurrentScope.SetVariable("name", "Elsa");
            return await evaluator.EvaluateAsync(expression, typeof(string), context, CancellationToken.None);
        }

        [Fact]
        public async Task EvaluateAsync_WithBuiltInHandler_ExposesWorkflowVariables()
        {
            using var provider = BuildProvider();

            var result = await EvaluateAsync(provider, "{{ Variables.name }}");

            Assert.Equal("Elsa", result);
        }

        [Fact]
        public async Task EvaluateAsync_WithConsumerHandlerRegisteredByAssemblyScan_ExposesCustomValue()
        {
            using var provider = BuildProvider(services => services.AddNotificationHandlers(typeof(GreetingHandler)));

            var result = await EvaluateAsync(provider, "{{ greeting }}, {{ Variables.name }}");

            Assert.Equal("Hello, Elsa", result);
        }

        [Fact]
        public async Task EvaluateAsync_WithConsumerHandlerRegisteredExplicitly_ExposesCustomValue()
        {
            using var provider = BuildProvider(services => services.AddNotificationHandler<EvaluatingLiquidExpression, GreetingHandler>());

            var result = await EvaluateAsync(provider, "{{ greeting }}");

            Assert.Equal("Hello", result);
        }
    }
}
