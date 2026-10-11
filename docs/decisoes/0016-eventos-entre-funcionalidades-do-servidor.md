# 0016. Eventos síncronos entre funcionalidades do servidor

**Data:** 2026-10-11
**Status:** proposta

## Contexto

A [arquitetura](../arquitetura.md#regras-de-dependência) proíbe uma funcionalidade de referenciar outra e manda conversar por eventos, mas o `ServerContext` só oferecia o tempo real. Sem um caminho, o combate não consegue ler a posição (que é do Mundo), o chat não sabe quem está na cidade e a esquiva é recusada pelo validador de movimento (issues #61, #62, #121).

Opções avaliadas:

- **Eventos síncronos em memória**, entregues na thread de quem publica.
- **Fila assíncrona** (canal com um consumidor): desacopla o tempo, mas o estado muda depois que o manipulador voltou, e a ordem entre mensagens de rede e eventos deixa de ser óbvia.
- **Biblioteca pronta** (MediatR e parecidas): resolve mais do que precisamos e traz dependência e reflexão.
- **Referência direta por interface na DI**: serve para perguntas ("onde está o personagem?"), não para avisos com vários interessados.

## Decisão

- O Core oferece **`ServerContext.Eventos`** (`EventosDoServidor`): `Assinar<T>(Action<T>)` e `Publicar<T>(T)`.
- A entrega é **síncrona**, na thread de quem publica, na **ordem de inscrição**. Publique do laço de rede (manipuladores de mensagem e eventos do tempo real já rodam nele); o manipulador não bloqueia, igual aos de mensagem.
- A inscrição vale para o **tipo exato** do evento. Assine no `Inicializar` do módulo.
- Um evento publicado **de dentro de um manipulador** só é entregue depois que o evento atual chegou a todos. Assim todos os assinantes veem os eventos na mesma ordem.
- Exceção num assinante **não impede os outros**. No fim, as falhas saem juntas numa `AggregateException` para quem publicou (no laço de rede, ela é registrada em log como qualquer falha de manipulador).
- Evento sem assinante não é erro.
- O **tipo do evento é definido por quem publica**, como diz a arquitetura. Onde o arquivo mora (shared ou uma pasta só do servidor) fica em aberto, abaixo.
- **Perguntas** entre funcionalidades (ler o estado de outra) não usam eventos: usam uma interface registrada na DI por quem é o dono, no molde do `IVerificadorPersonagem`.

## Consequências

- Combate, chat, grupo e esquiva deixam de depender de referência direta. A fila da fatia jogável destrava.
- O fluxo fica mais difícil de seguir pelo código: quem publica não mostra quem reage. A doc de cada funcionalidade lista o que ela publica em "Expõe" e o que escuta em "Depende de".
- Síncrono significa que um assinante lento atrasa o laço de rede. Trabalho com banco continua indo para uma `Task`.
- Não há garantia de entrega se o servidor cair no meio, nem eventos entre processos. Quando houver mais de um processo de servidor, esta decisão é revista.

## Em aberto

- Onde ficam os tipos de evento e as interfaces de consulta que só o servidor usa: em `shared/Package/Runtime/Features/<dono>/` (regra atual, mas o client passa a enxergar tipos que não usa, e cada arquivo novo precisa do `.meta` do Unity) ou numa pasta só do servidor, por exemplo `server/src/JogoDoGrupo.Server/Contratos/<Dono>/`. A decisão vem com o primeiro evento de verdade (Mundo, issue #119).
