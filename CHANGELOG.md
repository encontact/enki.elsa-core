# Changelog

Mudanças relevantes dos pacotes `Enki.Elsa.*` (fork do [Elsa Workflows 1.x](https://github.com/elsa-workflows/elsa-core)).

## [2.0.0] - não publicado

### Removido

- **Dependência do MediatR.** A partir da versão 13, o MediatR passou a ter licença comercial. A biblioteca usava
  apenas a publicação de notificações (`INotification` / `INotificationHandler<T>` / `IMediator.Publish`). Agora essa
  publicação é feita por uma implementação própria, sem dependência externa.

### Adicionado

- Namespace `Elsa.Messaging` (pacote `Enki.Elsa.Abstractions`) com os contratos:
  - `INotification`: interface marcadora das notificações;
  - `INotificationHandler<TNotification>`: mesma assinatura do MediatR
    (`Task Handle(TNotification notification, CancellationToken cancellationToken)`);
  - `INotificationPublisher`: substitui o `IMediator` para publicar notificações.
- `NotificationPublisher` (pacote `Enki.Elsa.Core`), a implementação padrão do `INotificationPublisher`.
- `services.AddNotificationPublisher()`. Registra o publisher apenas se nenhum outro já estiver registrado, então dá
  para substituí-lo por uma implementação própria.
- Projetos de teste `Elsa.Scripting.JavaScript.Tests` e `Elsa.Scripting.Liquid.Tests`, além de novos testes no
  `Elsa.Core.Tests`, cobrindo a publicação, o registro e a extensão dos motores de script por handlers de consumidores.

### Alterado (quebra de compatibilidade)

| Antes (MediatR) | Agora |
|---|---|
| `using MediatR;` | `using Elsa.Messaging;` |
| `MediatR.INotification` | `Elsa.Messaging.INotification` |
| `MediatR.INotificationHandler<T>` | `Elsa.Messaging.INotificationHandler<T>` |
| `IMediator` / `IPublisher` injetado para `Publish` | `INotificationPublisher` |
| `services.AddMediatR(typeof(Program))` para registrar handlers de notificações do Elsa | `services.AddNotificationHandlers(typeof(Program))` (`using Elsa.Extensions;`) |

As notificações publicadas pela biblioteca agora implementam `Elsa.Messaging.INotification`:

- `Elsa.Messages.WorkflowDefinitionStoreUpdated` (Elsa.Core)
- `Elsa.Scripting.JavaScript.Messages.EvaluatingJavaScriptExpression` (Elsa.Scripting.JavaScript)
- `Elsa.Scripting.Liquid.Messages.EvaluatingLiquidExpression` (Elsa.Scripting.Liquid)

Os construtores públicos de `PublishingWorkflowDefinitionStore`, `JavaScriptExpressionEvaluator` e
`LiquidExpressionEvaluator` passaram a receber `INotificationPublisher` no lugar de `IMediator`.

### Comportamento preservado

O publisher reproduz a semântica observada no MediatR 14.2.0, que era a versão em uso:

- os handlers são executados **em sequência, na ordem de registro**, e cada um é aguardado antes do próximo;
- a **primeira exceção** lançada por um handler interrompe a publicação e é propagada para quem chamou;
- se a mesma classe de handler foi registrada mais de uma vez (por varredura e explicitamente, por exemplo), ela é
  executada **uma única vez** por publicação;
- publicar `null` lança `ArgumentNullException`, e publicar sem nenhum handler registrado não faz nada;
- o publisher é **Singleton** e resolve os handlers a partir do container raiz, que é o mesmo comportamento do
  `IMediator`;
- os handlers registrados por `AddNotificationHandlers` / `AddNotificationHandler` são **Transient**.

Diferenças em relação ao MediatR:

- `AddNotificationHandlers` não registra o mesmo par serviço/handler duas vezes (usa `TryAddEnumerable`), mesmo que
  seja chamado várias vezes com o mesmo assembly;
- não existe despacho polimórfico. Um handler de uma classe base (ou de `INotification`) não recebe notificações de
  classes derivadas. Nenhuma notificação da biblioteca tem subclasses, então isso não afeta o uso atual.

### Guia de migração para consumidores

1. **Handlers que estendem os motores de script ou reagem a eventos do Elsa.** Troque `using MediatR;` por
   `using Elsa.Messaging;`. A assinatura do `Handle` é a mesma. Se o `using MediatR;` continuar no arquivo, o
   compilador acusa o erro (o tipo da notificação não satisfaz mais a restrição do MediatR), então nenhum handler deixa
   de ser executado sem aviso.

   ```csharp
   using Elsa.Messaging;
   using Elsa.Scripting.JavaScript.Messages;

   public class ScriptEngineConfigurator : INotificationHandler<EvaluatingJavaScriptExpression>
   {
       public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
       {
           notification.Engine.SetValue("double", (Func<int, int>)(x => x * 2));
           return Task.CompletedTask;
       }
   }
   ```

2. **Registro dos handlers.** Troque `services.AddMediatR(typeof(Program))` por
   `services.AddNotificationHandlers(typeof(Program))` ou, para um handler específico, por
   `services.AddNotificationHandler<EvaluatingJavaScriptExpression, ScriptEngineConfigurator>()`. Registre os handlers
   **depois** de `AddElsa()` / `AddElsaCore()` para que rodem depois dos handlers da biblioteca e possam sobrescrever
   funções padrão.

3. **Aplicações que usam o MediatR para os próprios comandos e queries.** Antes, o `AddElsaCore()` registrava o
   `IMediator` no container. **Isso não acontece mais.** Uma aplicação que injetava `IMediator` sem chamar
   `services.AddMediatR(...)` por conta própria vai falhar ao resolver a dependência na inicialização. Nesse caso,
   registre o MediatR explicitamente na aplicação, avaliando a licença, ou migre para outra solução. Os handlers do
   MediatR da aplicação não interferem nos do Elsa, porque as interfaces ficam em namespaces diferentes.

4. **Arquivos que usam o MediatR e o Elsa ao mesmo tempo.** Se um arquivo importar `MediatR` e `Elsa.Messaging`, os
   nomes `INotification` e `INotificationHandler<T>` ficam ambíguos. Use um alias, por exemplo
   `using ElsaMessaging = Elsa.Messaging;`, ou separe os handlers em arquivos diferentes.
