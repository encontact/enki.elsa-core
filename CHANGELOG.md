# Changelog

Mudanças relevantes dos pacotes `Enki.Elsa.*` (fork do [Elsa Workflows 1.x](https://github.com/elsa-workflows/elsa-core)).

## [3.0.0] - não publicado

### Alterado (quebra de compatibilidade)

- **As DLLs passam a ter o nome do pacote.** Até a 2.1.1, cada pacote `Enki.Elsa.*` trazia uma DLL com o nome do
  projeto original (`Elsa.dll`, `Elsa.Core.dll`, `Elsa.Abstractions.dll`, ...), o mesmo das DLLs do Elsa original.
  Agora o nome da DLL é igual ao do pacote (`Enki.Elsa.dll`, `Enki.Elsa.Core.dll`, `Enki.Elsa.Abstractions.dll`, ...).
  Os namespaces não mudaram, então quem consome os pacotes só precisa recompilar.
- Dados gravados que guardem o nome de um tipo junto com o nome antigo de uma DLL do Elsa (por exemplo, uma variável
  com valor do tipo `Elsa.Models.ConnectionDefinition, Elsa.Abstractions`) deixam de ser carregados. Valores simples
  (números, textos) e tipos da própria aplicação não são afetados. Antes de atualizar, confirme que as colunas JSON das
  tabelas `WorkflowInstances`, `ActivityInstances`, `WorkflowDefinitionVersions` e `ActivityDefinitions` não têm
  nomes de tipo que terminem em `, Elsa` ou `, Elsa.<pacote>`.

### Removido

- `Enki.Elsa.Persistence.YesSql`. O provider não funcionava desde a atualização para o YesSql 6 (1.6.3): o YesSql 6
  exige inicializar o store, o provider não fazia isso e a aplicação falhava ao iniciar.
- `Enki.Elsa.Dashboard` e `Enki.Elsa.WorkflowDesigner`. Os pacotes eram publicados sem os arquivos estáticos (CSS, JS e
  imagens), que dependiam de um build de front-end que não roda no CI, e as páginas apontavam para
  `_content/Elsa.Dashboard`, enquanto o caminho publicado era `_content/Enki.Elsa.Dashboard`.
- Junto com eles saíram o app `Elsa.Dashboard.Web`, os samples 10, 16 e 19, o `Dockerfile` e o `docker-compose.yaml`
  (que só rodavam o dashboard) e o `appveyor.yml` (CI antigo do Elsa original).
- As versões já publicadas desses pacotes continuam no NuGet, mas não recebem versões novas.

### Segurança

- `System.Security.Cryptography.Xml` e `System.Security.Cryptography.Pkcs` 10.0.13, que corrigem vulnerabilidades de
  negação de serviço: CVE-2026-96958, CVE-2026-96959 e CVE-2026-96960 (Xml), CVE-2026-84008 e CVE-2026-70162 (Pkcs).
  - `Enki.Elsa.Activities.Http` (e, por dependência, `Enki.Elsa`): `Microsoft.AspNetCore.DataProtection` 10.0.12 → 10.0.13.
  - `Enki.Elsa.Activities.Email` e `Enki.Elsa.Persistence.EntityFrameworkCore`: referência direta ao
    `System.Security.Cryptography.Pkcs` 10.0.13, porque o MimeKit e o Microsoft.Data.SqlClient traziam a 10.0.0 e a 9.0.11.
- Em aplicações ASP.NET Core, o `System.Security.Cryptography.Xml` faz parte do runtime (`Microsoft.AspNetCore.App`) e
  é sempre a cópia do runtime que é usada: a correção dele depende de rodar o runtime 10.0.13 ou superior. Já o
  `System.Security.Cryptography.Pkcs` 10.0.13 vai junto com a aplicação e é usado mesmo com o runtime 10.0.12.
- Quem referencia o `System.Security.Cryptography.Xml` diretamente numa versão anterior à 10.0.13 precisa subir para a
  10.0.13; senão o restore falha com NU1605 (downgrade).

## [2.1.1] - 2026-10-14

### Corrigido

- **Salvar um fluxo que já existe em MySQL/MariaDB (`Enki.Elsa.Persistence.EntityFrameworkCore`).** Ao atualizar
  uma definição, o store apaga as atividades e conexões antigas com `ExecuteDelete`, filtrando pela navegação
  `WorkflowDefinitionVersion.VersionId`. Com o EF Core 10 e o `Microting.EntityFrameworkCore.MySql`, isso passou a
  gerar `DELETE ... WHERE Id IN (SELECT ... FROM <mesma tabela>)`, que o MariaDB recusa (erro 1093, "Table 'a' is
  specified twice"). Na 1.6.3, o Pomelo gerava um `DELETE ... JOIN`, que funcionava. O filtro agora usa a própria
  chave estrangeira (`WorkflowDefinitionVersionId`), o que gera um `DELETE` simples numa tabela só.
- Novos testes em `Elsa.Persistence.EntityFrameworkCore.Tests` salvam e atualizam um fluxo existente contra um
  MySQL/MariaDB real. Eles só rodam com a variável `ELSA_TEST_MYSQL_CONNECTION` definida (connection string) e são
  pulados sem ela.

## [2.1.0] - 2026-10-13

### Alterado

- **Provider MySQL/MariaDB trocado para `Microting.EntityFrameworkCore.MySql` e EF Core de volta para a 10
  (`Enki.Elsa.Persistence.EntityFrameworkCore`).** O Pomelo parou no EF Core 9 (última versão em agosto de 2025, sem
  suporte ao EF Core 10), e o EF Core 9 perde suporte da Microsoft em 10/11/2026. O `Microting.EntityFrameworkCore.MySql`
  é um fork do Pomelo (MIT), mantido, com suporte ao EF Core 10 e testado com MariaDB. A API usada (`UseMySql`,
  `MariaDbServerVersion`, `MySqlServerVersion`) e as migrations são as mesmas, então não há mudança de código para quem
  consome o pacote.
  - `Pomelo.EntityFrameworkCore.MySql` 9.0.0 → `Microting.EntityFrameworkCore.MySql` 10.0.12
  - `Microsoft.EntityFrameworkCore.*` 9.0.20 → 10.0.12
  - `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 → 10.0.3
  - `FlexLabs.EntityFrameworkCore.Upsert` 9.0.0 → 10.1.0
- Quem referencia o EF Core diretamente deve usar a 10.x. Bancos criados pelas versões anteriores continuam
  funcionando sem migration nova.

## [2.0.1] - 2026-10-13

### Corrigido

- **Persistência em MySQL/MariaDB (`Enki.Elsa.Persistence.EntityFrameworkCore`).** A 2.0.0 usava EF Core 10 com o
  Pomelo 9.0.0, que só é compatível com o EF Core 9. Ao configurar o contexto com `UseMySql`, a aplicação falhava com
  `MissingMethodException` (`AbstractionsStrings.ArgumentIsEmpty`). Como ainda não existe Pomelo para o EF Core 10, os
  pacotes do EF voltaram para a linha 9: `Microsoft.EntityFrameworkCore.*` 9.0.20, `Npgsql.EntityFrameworkCore.PostgreSQL`
  9.0.4 e `FlexLabs.EntityFrameworkCore.Upsert` 9.0.0. Quem usa o pacote e referencia o EF Core diretamente deve usar a
  9.0.x. **Não use a 2.0.0 com MySQL/MariaDB.**
- Novos testes em `Elsa.Persistence.EntityFrameworkCore.Tests` configuram o `MySqlContext` com `UseMySql` para pegar
  essa incompatibilidade no build.

## [2.0.0] - 2026-10-13

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
- o publisher é **Transient** e resolve os handlers no escopo de quem o recebe. Handlers com dependências Scoped (um
  `DbContext`, por exemplo) recebem a instância do escopo da requisição. Com os stores em memória, que são Singleton, o
  decorator que publica `WorkflowDefinitionStoreUpdated` também é Singleton, então os handlers dessa notificação são
  resolvidos a partir do container raiz;
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
