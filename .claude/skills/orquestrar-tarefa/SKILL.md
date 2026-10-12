---
name: orquestrar-tarefa
description: Coordena a implementação de issues do Jogo do Grupo com um agente dono por funcionalidade (funcionalidade, itens, economia), em paralelo entre funcionalidades, dentro do foco do marco; agentes de Core e por camada só quando precisa. Use quando pedirem para implementar uma funcionalidade, uma issue ou "colocar os agentes para trabalhar".
---

# Orquestrar tarefa

Você é o **coordenador**: não implementa; planeja, lança os agentes e junta os resultados. Guia completo: `docs/agentes.md`.

## 1. Entender a tarefa

- Qual funcionalidade (`<X>`)? Ela está no [foco atual do marco](../../../docs/features/fatia-jogavel.md#foco-atual)? Há vaga no limite de 3 funcionalidades de jogo em andamento (`npm run tarefa -- listar`)?
- Leia `docs/features/<x>.md` e o código existente.
- Regra de design aberta não trava: o dono decide e registra ([Decisões de design](../../../docs/agentes.md#decisões-de-design)).
- A tarefa depende do Core (rede, sessões, despacho de mensagens) que ainda não existe? Então a etapa é `server-core` ou `client-core`, não a funcionalidade.

## 2. Ficha da tarefa (antes de qualquer agente)

Quase todo retrabalho vem de decisão tomada tarde. Se a issue não tem, escreva nela uma **ficha** curta. Não espere aprovação: o dono completa o plano e as decisões ao puxar.

- **Objetivo:** o que funciona ao final, em 2–3 linhas.
- **Plano:** casos de uso a partir do ponto de entrada no jogo, os estados da checklist de experiência que se aplicam e o que aparece em cada um. É a entrega da funcionalidade. Detalhe em [Plano da tarefa](../../../docs/agentes.md#plano-da-tarefa).
- **Contrato:** mensagens, DTOs e erros, se já souber; senão o dono define.
- **Regras:** números, limites e quem decide (servidor).
- **Testes esperados:** lista dos casos (caminho feliz, cada erro, limites, concorrência).
- **Fora do escopo.**

Os prompts dos agentes saem da ficha. Issue de jogo vai inteira para **um** dono; subtarefa só para fatia grande demais (dividida por caso de uso) ou para Core:
```
npm run tarefa -- subtarefa <N> --titulo "<titulo da subtarefa>" --agente <papel> [--escopo <escopo>] --contexto "<o que fazer, em 2-4 linhas, com a seção da doc>"
```
Isso gera uma issue filha com label do agente, vinculada ao Project, garantindo que cada agente trave a sua parte individualmente.

## 3. Montar o plano e lançar

Mostre à pessoa a tabela e lance (pedir a implementação já autoriza):

| Funcionalidade | Agente dono | Issue | Depende de |
|---|---|---|---|
| Mobs | funcionalidade | #... | — |
| Combate | funcionalidade | #... | — |
| Core (se preciso) | server-core / client-core | #... | — |
| revisor | só se o PR for de risco (veja abaixo) | — | o PR |

Agente dono: `itens` para Itens, `economia` para Economia, `funcionalidade` para o resto. Os por camada (`shared-contratos`, `server-funcionalidade`, `client-funcionalidade`, `regras-do-jogo`) só se a fatia for dividida por camada ([ADR 0030](../../../docs/decisoes/0030-processo-v2-dono-da-funcionalidade.md)).

~~Pergunte junto: os agentes podem commitar, enviar as branches e abrir PRs?~~ substituído em 2026-10-11 por: pedir a implementação já autoriza commit, push e PR. Merge continua humano. Se a issue já tem a ficha, não peça aprovação de novo.

O revisor só entra quando o PR mexe em Core, segurança ou numa regra de dano, preço ou fusão que a doc ainda não fechou. ~~concorrência, contrato ou regra que decide resultado (dano, drop, fusão, preço)~~ substituído em 2026-10-11 por: recorte com os números já no contrato e com teste no próprio PR (mesmo um baú que sorteia item, mesmo corrida coberta por teste) não leva revisor. Arte, tela, conteúdo 3D e ligar um botão também não. O CI e a verificação do autor são a qualidade mínima.

**Economize agentes:** a regra 16 do `AGENTS.md` manda implementar o corte na hora, não só relatar.
- **Custo fixo de um agente: 50–80 mil tokens** (regras, worktree, puxar tarefa, verificações). Tarefa pequena (até ~50 linhas numa área: constantes, docs curtas, config) o coordenador faz direto, sem agente, sem ficha e sem tabela de aprovação.
- Várias tarefas pequenas da mesma área: **um agente só** para todas.
- Coordenador em **Sonnet** por padrão (Opus só para planejar marcos); **sessão nova por rodada ou marco** (`/clear`): o estado mora nas issues, fichas e docs.
- **Prompt do agente enxuto:** passe a ficha e os caminhos dos arquivos; não cole docs inteiras.
- Tarefa pequena e clara não precisa de `designer-docs`: as regras vão na ficha.
- Mudanças de docs, `.meta` e configuração você faz direto, sem agente e sem revisor.
- Os `.meta` e os materiais que o Unity gerar entram no mesmo PR. Quem abre o PR roda `npm run conferir-unity`. Não peça para a pessoa commitar isso depois do merge.
- Um Unity por vez na máquina, sem combinar: `unity:executar`, `unity:importar` e `unity:testes` esperam a vez sozinhos (até 30 min; `UNITY_ESPERA_MIN` muda). Pode lançar vários donos em paralelo. A primeira importação de um worktree novo é lenta; não abra outro worktree só para repetir isso.
- Sem Unity no ambiente (nuvem), ninguém espera pelo Unity: o agente abre o PR normal e registra o que faltou com `npm run tarefa -- pendencia-unity <PR> --falta "..."` ([ADR 0019](../../../docs/decisoes/0019-unity-no-ci.md)). O coordenador não relança o agente por isso.

**Modelo por tarefa:** cada agente já tem `model` e `effort` no próprio arquivo (Opus em `server-core` e `client-core`; Sonnet nos demais; `designer-docs` com effort baixo). ~~Ao lançar, suba para `opus` quando a tarefa envolver segurança, concorrência, dinheiro ou itens do jogo, ou uma regra difícil.~~ substituído em 2026-10-11 por: Sonnet é o padrão. Opus só em Core, segurança ou regra de dano, preço ou fusão que a doc ainda não fechou.

## 4. Core antes, se faltar

Se a funcionalidade precisa de algo novo em Core (API, rede, HUD base), lance `server-core` ou `client-core` antes e espere o merge: os donos partem da `main`.

## 5. Donos em paralelo

Lance os donos de funcionalidades diferentes **na mesma mensagem, em segundo plano**, um por issue. Cada agente deve começar puxando sua subtarefa (`npm run tarefa -- puxar <N> [branch] --agente <papel> --worktree .`, no worktree do próprio agente) para registrá-la em andamento e evitar qualquer concorrência. Cada prompt precisa ser completo, porque o agente não vê esta conversa:

```
Funcionalidade: <X> (pasta Features/<X>/, escopo de commit <x>)
Objetivo: <a cena, em 2–4 linhas: o que a pessoa faz e o que aparece>
Regras de design: docs/features/<x>.md, seções <...>. O que não estiver fechado, você decide e registra.
Ficha: issue #<N>. Escreva o plano nela antes de codar e siga sem esperar.
Issue: #<N> (Closes #<N>).
Puxar tarefa: rode `npm run tarefa -- puxar <N> <branch> --agente <papel> --worktree .` dentro do seu worktree (o agente já nasce em `.claude/worktrees/agent-*`; um caminho fora dele é barrado pela permissão e vira worktree órfão) antes de codificar.
Fora do escopo: <o que NÃO fazer>
Pronto quando: contrato, servidor e client com a cena no mesmo PR, com ponto de entrada e os estados do plano testados ([Entrega da funcionalidade](../../../docs/agentes.md#entrega-da-funcionalidade))
Autorização: pode commitar, enviar a branch e abrir PR. Não faça merge.
Base: <só se precisar de algo ainda não na main> passo 0: `git merge <branch>` (local) e conferir <arquivo>. Sem PR próprio? Commite local na sua branch e informe branch e hash.
```

O worktree do agente parte do commit do checkout principal; o que o coordenador não commitou não chega a ele ([Fechamento](../../../docs/agentes.md#fechamento-o-coordenador-integra-e-limpa-o-que-criou)).

Nunca lance dois agentes na mesma funcionalidade ao mesmo tempo.

## 6. Juntar os resultados

Para cada relatório:

- **"Para outros"** vira uma tarefa de Core ou de outra funcionalidade.
- **Decisões de design** do relatório vão no resumo para a pessoa, que pode rever depois.
- Lance o `revisor` numa passada só, e só em PR de Core, segurança ou regra de dano, preço ou fusão ainda aberta. Não use o revisor para repetir um teste que o PR já trouxe. No resto, o CI e a skill `preparar-pr` bastam.
- Achados do revisor que valem a pena viram **commits novos na mesma branch**, feitos pelo agente que abriu o PR (retome-o com a mensagem dos ajustes).
- Agente que commitou só local: traga o commit com `git cherry-pick` (nunca `git merge` da branch dele) e confira `git show --stat`. Detalhes em [Fechamento](../../../docs/agentes.md#fechamento-o-coordenador-integra-e-limpa-o-que-criou).

### Quando pedir o merge

Só diga à pessoa que um PR está **pronto para merge** depois que os checks passaram e, se o PR for de risco, o `revisor` aprovou e os ajustes escolhidos foram aplicados. Sem revisor, o CI verde e a verificação proporcional do autor bastam. Se o PR é a cena da funcionalidade, "Como testar" diz o que a pessoa faz e o que aparece, e o merge espera ela olhar essa cena. Enquanto o revisor de um PR de risco não terminar, deixe claro: "em revisão, **não mergear ainda**".

Se um PR for mergeado antes dos ajustes, **não envie mais nada para a branch dele** (ela foi apagada no merge e o push a recriaria). Crie uma branch nova a partir da `origin/main`, traga os commits com `git cherry-pick` e abra um PR novo.

## 7. Resumo final para a pessoa

- PRs abertos, com links. O veredito do revisor só quando ele rodou.
- Como testar cada cena e as decisões de design que os donos tomaram.
- Limpeza: é automática depois dos merges ([ADR 0031](../../../docs/decisoes/0031-limpeza-automatica.md)); não peça nada à pessoa. Se algo ficou, `.git/limpar-agentes.log` diz o motivo.
