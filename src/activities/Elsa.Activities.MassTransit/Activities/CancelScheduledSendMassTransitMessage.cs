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
        DisplayName = "Cancel scheduled MassTransit Message",
        Description = "Cancel a scheduled message via MassTransit."
    )]
    public class CancelScheduledMassTransitMessage : MassTransitBusActivity
    {
        private readonly IWorkflowExpressionEvaluator evaluator;
        private readonly IMessageScheduler messageScheduler;

        public CancelScheduledMassTransitMessage(IWorkflowExpressionEvaluator evaluator, IBus bus, ConsumeContext consumeContext, IMessageScheduler messageScheduler)
            : base(bus, consumeContext)
        {
            this.evaluator = evaluator;
            this.messageScheduler = messageScheduler;
        }

        [ActivityProperty(Hint = "Expression that returns the tokenId of a scheduled message to cancel.")]
        public WorkflowExpression TokenId
        {
            get => GetState<WorkflowExpression>();
            set => SetState(value);
        }

        // Per MassTransit's IMessageScheduler docs, CancelScheduledSend identifies the scheduled message by
        // (EndpointAddress, TokenId) together, not by TokenId alone. This MUST be the exact EndpointAddress
        // passed to ScheduleSendMassTransitMessage when the message was scheduled. We have not verified what
        // MassTransit does on a mismatch (exception vs. silent no-op) - treat an unverified mismatch here as a
        // risk that the original message still gets delivered as if nothing had been cancelled.
        [ActivityProperty(Hint = "The same endpoint address that was used by the Schedule MassTransit Message activity to schedule this message.")]
        public Uri EndpointAddress
        {
            get
            {
                var endpointAddress = GetState<string>();
                return string.IsNullOrEmpty(endpointAddress) ? null : new Uri(endpointAddress);
            }
            set => SetState(value.ToString());
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return TokenId != null && EndpointAddress != null;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var tokenId = (Guid)await evaluator.EvaluateAsync(TokenId, typeof(Guid), context, cancellationToken);

            await messageScheduler.CancelScheduledSend(EndpointAddress, tokenId, cancellationToken);

            return Done();
        }
    }
}