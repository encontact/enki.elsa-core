using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Messaging;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace Elsa.Scripting.JavaScript.Tests
{
    /// <summary>
    /// End-to-end tests of the JavaScript evaluator, covering the built-in engine handlers and the extension point used by
    /// library consumers (custom <see cref="INotificationHandler{TNotification}"/> for <see cref="EvaluatingJavaScriptExpression"/>).
    /// </summary>
    public class JavaScriptExpressionEvaluatorTests
    {
        private static readonly Instant Now = Instant.FromUtc(2030, 6, 15, 12, 0);

        private class FixedClock : IClock
        {
            public Instant GetCurrentInstant() => Now;
        }

        public class EvaluationCounter
        {
            public int Count { get; set; }
        }

        public class CustomFunctionHandler : INotificationHandler<EvaluatingJavaScriptExpression>
        {
            public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
            {
                notification.Engine.SetValue("double", (Func<int, int>)(x => x * 2));
                return Task.CompletedTask;
            }
        }

        public class OverridingHandler : INotificationHandler<EvaluatingJavaScriptExpression>
        {
            public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
            {
                notification.Engine.SetValue("newGuid", (Func<string>)(() => "overridden"));
                return Task.CompletedTask;
            }
        }

        public class CountingHandler : INotificationHandler<EvaluatingJavaScriptExpression>
        {
            private readonly EvaluationCounter counter;

            public CountingHandler(EvaluationCounter counter)
            {
                this.counter = counter;
            }

            public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
            {
                counter.Count++;
                return Task.CompletedTask;
            }
        }

        // Throws only when requested, since assembly-scanning tests register every handler of this assembly.
        public class ThrowingHandler : INotificationHandler<EvaluatingJavaScriptExpression>
        {
            public const string FailFlag = "failHandler";

            public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
            {
                if (notification.WorkflowExecutionContext.TransientState.ContainsKey(FailFlag))
                    throw new InvalidOperationException("consumer handler failure");

                return Task.CompletedTask;
            }
        }

        private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection()
                .AddSingleton<IClock>(new FixedClock())
                .AddSingleton<EvaluationCounter>();

            services.AddElsaCore().AddJavaScriptExpressionEvaluator();
            configure?.Invoke(services);
            return services.BuildServiceProvider();
        }

        private static async Task<object> EvaluateAsync(IServiceProvider provider, string expression, Type type, Action<WorkflowExecutionContext>? setup = null)
        {
            using var scope = provider.CreateScope();
            var evaluator = scope.ServiceProvider.GetServices<IExpressionEvaluator>().Single(x => x.Syntax == JavaScriptExpressionEvaluator.SyntaxName);
            var workflow = new Workflow { Input = new Variables() };
            var context = new WorkflowExecutionContext(workflow, scope.ServiceProvider.GetRequiredService<IClock>(), scope.ServiceProvider);
            setup?.Invoke(context);
            return await evaluator.EvaluateAsync(expression, type, context, CancellationToken.None);
        }

        [Fact]
        public async Task EvaluateAsync_WithBuiltInHandler_ExposesWorkflowVariables()
        {
            using var provider = BuildProvider();

            var result = await EvaluateAsync(provider, "variable('name') + '!'", typeof(string), context => context.CurrentScope.SetVariable("name", "Elsa"));

            Assert.Equal("Elsa!", result);
        }

        [Fact]
        public async Task EvaluateAsync_WithBuiltInHandler_ExposesNewGuidFunction()
        {
            using var provider = BuildProvider();

            var result = await EvaluateAsync(provider, "newGuid()", typeof(string));

            Assert.True(Guid.TryParse((string)result, out _));
        }

        [Fact]
        public async Task EvaluateAsync_WithDateTimeHandler_ResolvesHandlerDependenciesFromContainer()
        {
            using var provider = BuildProvider();

            var result = await EvaluateAsync(provider, "currentYear()", typeof(int));

            Assert.Equal(2030, result);
        }

        [Fact]
        public async Task EvaluateAsync_WithConsumerHandlerRegisteredByAssemblyScan_ExposesCustomFunction()
        {
            using var provider = BuildProvider(services => services.AddNotificationHandlers(typeof(CustomFunctionHandler)));

            var result = await EvaluateAsync(provider, "double(21)", typeof(int));

            Assert.Equal(42, result);
        }

        [Fact]
        public async Task EvaluateAsync_WithConsumerHandlerRegisteredExplicitly_ExposesCustomFunction()
        {
            using var provider = BuildProvider(services => services.AddNotificationHandler<EvaluatingJavaScriptExpression, CustomFunctionHandler>());

            var result = await EvaluateAsync(provider, "double(5)", typeof(int));

            Assert.Equal(10, result);
        }

        [Fact]
        public async Task EvaluateAsync_WithConsumerHandlerRegisteredAfterBuiltIns_CanOverrideBuiltInFunction()
        {
            using var provider = BuildProvider(services => services.AddNotificationHandler<EvaluatingJavaScriptExpression, OverridingHandler>());

            var result = await EvaluateAsync(provider, "newGuid()", typeof(string));

            Assert.Equal("overridden", result);
        }

        [Fact]
        public async Task EvaluateAsync_WithEvaluatorRegisteredTwice_InvokesEachHandlerOncePerEvaluation()
        {
            using var provider = BuildProvider(services => services
                .AddJavaScriptExpressionEvaluator()
                .AddNotificationHandler<EvaluatingJavaScriptExpression, CountingHandler>()
                .AddNotificationHandler<EvaluatingJavaScriptExpression, CountingHandler>());

            await EvaluateAsync(provider, "1", typeof(int));

            Assert.Equal(1, provider.GetRequiredService<EvaluationCounter>().Count);
        }

        [Fact]
        public async Task EvaluateAsync_WhenConsumerHandlerThrows_PropagatesException()
        {
            using var provider = BuildProvider(services => services.AddNotificationHandler<EvaluatingJavaScriptExpression, ThrowingHandler>());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => EvaluateAsync(provider, "1", typeof(int), context => context.TransientState.SetVariable(ThrowingHandler.FailFlag, true)));

            Assert.Equal("consumer handler failure", exception.Message);
        }
    }
}
