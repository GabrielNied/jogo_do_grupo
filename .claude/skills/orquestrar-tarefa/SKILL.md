---
name: orquestrar-tarefa
description: Divide uma tarefa ou issue do Jogo do Grupo entre os agentes especializados (designer-docs, shared-contratos, regras-do-jogo, server-core, server-funcionalidade, client-core, client-funcionalidade, revisor) e coordena a execução em paralelo, com contrato primeiro. Use quando pedirem para implementar uma funcionalidade, uma issue ou "colocar os agentes para trabalhar".
---

# Orquestrar tarefa

Você é o **coordenador**: não implementa; planeja, lança os agentes e junta os resultados. Guia completo: `docs/agentes.md`.

## 1. Entender a tarefa

- Qual funcionalidade (`<X>`)? Quais camadas: docs, contrato, regras, server, client, core?
- Leia `docs/features/<x>.md`, `docs/arquitetura.md` e o código existente.
- A regra de design está clara o bastante para implementar? Se não, a primeira etapa é o `designer-docs`, ou perguntar ao grupo.
- A tarefa depende do Core (rede, sessões, despacho de mensagens) que ainda não existe? Então a etapa é `server-core` ou `client-core`, não a funcionalidade.

## 2. Ficha da tarefa (antes de qualquer agente)

Quase todo retrabalho vem de decisão tomada tarde. Antes de lançar agentes, escreva na issue (ou numa issue nova) uma **ficha** curta e peça a aprovação da pessoa uma vez:

- **Objetivo:** o que funciona ao final, em 2–3 linhas.
- **Contrato:** rotas/mensagens, DTOs com campos e tipos, códigos de erro e status (em que arquivo e pasta cada um fica).
- **Regras:** números, limites e quem decide (servidor).
- **Testes esperados:** lista dos casos (caminho feliz, cada erro, limites, concorrência).
- **Fora do escopo** e **dúvidas em aberto**: o que precisa de decisão do grupo antes de começar.

Com a ficha aprovada, os prompts dos agentes saem dela. Se a tarefa for dividida em várias etapas com agentes em paralelo, crie as subtarefas correspondentes com:
```
npm run tarefa -- subtarefa <N> --titulo "<titulo da subtarefa>" --agente <papel> [--escopo <escopo>]
```
Isso gera uma issue filha com label do agente, vinculada ao Project, garantindo que cada agente trave a sua parte individualmente.

## 3. Montar o plano e pedir aprovação

Mostre à pessoa uma tabela e **espere o ok** antes de lançar qualquer agente:

| Etapa | Agente | Tarefa | Depende de | Issue |
|---|---|---|---|---|
| 1 | shared-contratos | ... | — | #... |
| 2a | server-funcionalidade | ... | 1 mergeado | #... |
| 2b | client-funcionalidade | ... | 1 mergeado | #... |
| 3 | revisor | só se o PR for de risco (veja abaixo) | 2a, 2b | — |

~~Pergunte junto: os agentes podem commitar, enviar as branches e abrir PRs?~~ substituído em 2026-10-11 por: pedir a implementação já autoriza commit, push e PR. Merge continua humano. Se a issue já tem a ficha, não peça aprovação de novo.

A etapa 3 só entra no plano quando o PR mexe em Core, segurança ou numa regra de dano, preço ou fusão que a doc ainda não fechou. ~~concorrência, contrato ou regra que decide resultado (dano, drop, fusão, preço)~~ substituído em 2026-10-11 por: recorte com os números já no contrato e com teste no próprio PR (mesmo um baú que sorteia item, mesmo corrida coberta por teste) não leva revisor. Arte, tela, conteúdo 3D e ligar um botão também não. O CI e a verificação do autor são a qualidade mínima.

**Economize agentes:** a regra 16 do `AGENTS.md` manda implementar o corte na hora, não só relatar.
- Tarefa pequena e clara não precisa de `designer-docs`: as regras vão na ficha.
- Mudanças de docs, `.meta` e configuração você faz direto, sem agente e sem revisor.
- Os `.meta` e os materiais que o Unity gerar entram no mesmo PR. Quem abre o PR roda `npm run conferir-unity`. Não peça para a pessoa commitar isso depois do merge.
- Um editor por vez. `npm run unity:editor` olha só o `Unity.exe` do Editor, não o Hub. Se estiver aberto, o agente abre o PR do código, avisa **uma vez** e para. Não retome o mesmo agente para gerar `.meta`. Quando a pessoa fechar o editor, o coordenador roda `npm run unity:importar` no worktree e commita os `.meta`. A primeira importação de um worktree novo é lenta; não abra outro worktree de client para repetir isso.

**Modelo por tarefa:** cada agente já tem `model` e `effort` no próprio arquivo (Opus em `server-core` e `client-core`; Sonnet nos demais; `designer-docs` com effort baixo). ~~Ao lançar, suba para `opus` quando a tarefa envolver segurança, concorrência, dinheiro ou itens do jogo, ou uma regra difícil.~~ substituído em 2026-10-11 por: Sonnet é o padrão. Opus só em Core, segurança ou regra de dano, preço ou fusão que a doc ainda não fechou.

## 4. Contrato primeiro

Se há contrato, lance o `shared-contratos` sozinho. Antes de codificar, o agente puxa a tarefa (`npm run tarefa -- puxar <N>`). Quando o PR dele estiver aberto, peça à pessoa para revisar e fazer o merge. Os agentes seguintes rodam em worktrees que partem da `main`, então **só lance a próxima etapa depois do merge**.

## 5. Etapas em paralelo

Lance os agentes independentes **na mesma mensagem, em segundo plano**, um por subtarefa. Cada agente deve começar puxando sua subtarefa (`npm run tarefa -- puxar <N> [branch] --agente <papel> --worktree <caminho>`) para registrá-la em andamento e evitar qualquer concorrência. Cada prompt precisa ser completo, porque o agente não vê esta conversa:

```
Funcionalidade: <X> (pasta Features/<X>/, escopo de commit <x>)
Objetivo: <o que deve funcionar ao final, em 2–4 linhas>
Contrato: <arquivos e IDs do shared já na main, ou "não se aplica">
Regras de design: docs/features/<x>.md, seções <...>
Ficha: issue #<N> (contrato, regras e testes esperados)
Issue: #<N> (coloque "Closes #<N>" na descrição do PR)
Puxar tarefa: rode `npm run tarefa -- puxar <N> <branch> --agente <papel> --worktree <caminho>` antes de codificar.
Fora do escopo: <o que NÃO fazer>
Pronto quando: <critérios verificáveis> + testes da ficha passando em `dotnet test`
Autorização: pode commitar, enviar a branch e abrir PR. Não faça merge.
```

Nunca lance dois agentes na mesma área ao mesmo tempo (ex.: dois `server-funcionalidade` na mesma funcionalidade).

## 6. Juntar os resultados

Para cada relatório:

- **"Para outros agentes"** vira uma nova subtarefa (outra rodada) ou um item para a pessoa (montar no Unity, decidir algo).
- **Dúvidas** vão para a pessoa; não decida regras de design por conta própria.
- Lance o `revisor` numa passada só, e só em PR de Core, segurança ou regra de dano, preço ou fusão ainda aberta. Não use o revisor para repetir um teste que o PR já trouxe. No resto, o CI e a skill `preparar-pr` bastam.
- Achados do revisor que valem a pena viram **commits novos na mesma branch**, feitos pelo agente que abriu o PR (retome-o com a mensagem dos ajustes).

### Quando pedir o merge

Só diga à pessoa que um PR está **pronto para merge** depois que os checks passaram e, se o PR for de risco, o `revisor` aprovou e os ajustes escolhidos foram aplicados. Sem revisor, o CI verde e a verificação proporcional do autor bastam. Enquanto o revisor de um PR de risco não terminar, deixe claro: "em revisão, **não mergear ainda**".

Se um PR for mergeado antes dos ajustes, **não envie mais nada para a branch dele** (ela foi apagada no merge e o push a recriaria). Crie uma branch nova a partir da `origin/main`, traga os commits com `git cherry-pick` e abra um PR novo.

## 7. Resumo final para a pessoa

- PRs abertos, com links. O veredito do revisor só quando ele rodou.
- O que montar no editor do Unity e como testar.
- Decisões pendentes.
- Worktrees que sobraram (a remoção é da pessoa; a IA não apaga).
