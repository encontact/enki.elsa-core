using Elsa.Services.Models;
using Jint;
using Elsa.Messaging;

namespace Elsa.Scripting.JavaScript.Messages
{
    public class EvaluatingJavaScriptExpression : INotification
    {
        public EvaluatingJavaScriptExpression(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            Engine = engine;
            WorkflowExecutionContext = workflowExecutionContext;
        }

        public Engine Engine { get; }
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
    }
}