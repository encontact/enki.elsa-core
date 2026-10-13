using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using MassTransit;

namespace Elsa.Activities.MassTransit.Activities
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Schedule MassTransit Message",
        Description = "Schedule a message via MassTransit."
    )]
    public class ScheduleSendMassTransitMessage : MassTransitBusActivity
    {
        private readonly IWorkflowExpressionEvaluator evaluator;
        private readonly IMessageScheduler messageScheduler;

        public ScheduleSendMassTransitMessage(IWorkflowExpressionEvaluator evaluator, IBus bus, ConsumeContext consumeContext, IMessageScheduler messageScheduler)
        : base(bus, consumeContext)
        {
            this.evaluator = evaluator;
            this.messageScheduler = messageScheduler;
        }

        [ActivityProperty(Hint = "The assembly-qualified type name of the message to send.")]
        public Type MessageType
        {
            get
            {
                var typeName = GetState<string>();
                return string.IsNullOrWhiteSpace(typeName) ? null : System.Type.GetType(typeName);
            }
            set => SetState(value.AssemblyQualifiedName);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the message to be delivered.")]
        public WorkflowExpression Message
        {
            get => GetState<WorkflowExpression>();
            set => SetState(value);
        }

        // CancelScheduledMassTransitMessage.EndpointAddress must be set to this exact value to cancel this
        // message later - see the note on that property for why a mismatch is a silent-failure risk.
        [ActivityProperty(Hint = "The address of a specific endpoint to deliver the message to.")]
        public Uri EndpointAddress
        {
            get
            {
                var endpointAddress = GetState<string>();
                return string.IsNullOrEmpty(endpointAddress) ? null : new Uri(endpointAddress);
            }
            set => SetState(value.ToString());
        }

        [ActivityProperty(Hint = "An expression that evaluates to the date and time to deliver the message.")]
        public WorkflowExpression<DateTime> ScheduledTime
        {
            get => GetState<WorkflowExpression<DateTime>>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return MessageType != null;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var message = await evaluator.EvaluateAsync(Message, MessageType, context, cancellationToken);
            var scheduledTime = (DateTime)await evaluator.EvaluateAsync(ScheduledTime, typeof(DateTime), context, cancellationToken);

            var scheduledMessage = await messageScheduler.ScheduleSend(EndpointAddress, scheduledTime, message, cancellationToken);

            context.SetLastResult(Output.SetVariable("TokenId", scheduledMessage.TokenId));

            return Done();
        }
    }
}