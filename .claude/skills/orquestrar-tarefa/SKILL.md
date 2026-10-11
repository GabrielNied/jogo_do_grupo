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

Com a ficha aprovada, os prompts dos agentes saem dela. Se o PR for de risco e houver revisor, ele compara o PR com a ficha.

## 3. Montar o plano e pedir aprovação

Mostre à pessoa uma tabela e **espere o ok** antes de lançar qualquer agente:

| Etapa | Agente | Tarefa | Depende de |
|---|---|---|---|
| 1 | shared-contratos | ... | — |
| 2a | server-funcionalidade | ... | 1 mergeado |
| 2b | client-funcionalidade | ... | 1 mergeado |
| 3 | revisor | só se o PR for de risco (veja abaixo) | 2a, 2b |

Pergunte junto: os agentes podem commitar, enviar as branches e abrir PRs? (Merge é sempre humano.)

A etapa 3 só entra no plano quando o PR mexe em Core, segurança, concorrência, contrato ou regra que decide resultado (dano, drop, fusão, preço). Arte, tela, conteúdo 3D e ligar um botão ficam de fora: o CI e a verificação do autor são a qualidade mínima.

**Economize agentes:**
- Tarefa pequena e clara não precisa de `designer-docs`: as regras vão na ficha.
- Mudanças de docs, `.meta` e configuração você faz direto, sem agente e sem revisor.
- Os `.meta` e os materiais que o Unity gerar entram no mesmo PR. Quem abre o PR roda `npm run conferir-unity`. Não peça para a pessoa commitar isso depois do merge.

**Modelo por tarefa:** cada agente já tem `model` e `effort` no próprio arquivo (Opus em `server-core` e `client-core`; Sonnet nos demais; `designer-docs` com effort baixo). Ao lançar, suba para `opus` (parâmetro `model` da chamada) quando a tarefa de funcionalidade envolver segurança, concorrência, dinheiro ou itens do jogo, ou uma regra difícil; mantenha o padrão no resto.

## 4. Contrato primeiro

Se há contrato, lance o `shared-contratos` sozinho. Quando o PR dele estiver aberto, peça à pessoa para revisar e fazer o merge. Os agentes seguintes rodam em worktrees que partem da `main`, então **só lance a próxima etapa depois do merge**.

## 5. Etapas em paralelo

Lance os agentes independentes **na mesma mensagem, em segundo plano**, um por subtarefa. Cada prompt precisa ser completo, porque o agente não vê esta conversa:

```
Funcionalidade: <X> (pasta Features/<X>/, escopo de commit <x>)
Objetivo: <o que deve funcionar ao final, em 2–4 linhas>
Contrato: <arquivos e IDs do shared já na main, ou "não se aplica">
Regras de design: docs/features/<x>.md, seções <...>
Ficha: issue #<N> (contrato, regras e testes esperados)
Issue: #<N> (coloque "Closes #<N>" na descrição do PR)
Fora do escopo: <o que NÃO fazer>
Pronto quando: <critérios verificáveis> + testes da ficha passando em `dotnet test`
Autorização: pode commitar, enviar a branch e abrir PR. Não faça merge.
```

Nunca lance dois agentes na mesma área ao mesmo tempo (ex.: dois `server-funcionalidade` na mesma funcionalidade).

## 6. Juntar os resultados

Para cada relatório:

- **"Para outros agentes"** vira uma nova subtarefa (outra rodada) ou um item para a pessoa (montar no Unity, decidir algo).
- **Dúvidas** vão para a pessoa; não decida regras de design por conta própria.
- Lance o `revisor` numa passada só, e só em PR de risco (Core, segurança, concorrência, contrato ou regra que decide resultado). Use `opus` nesses casos. No resto, não lance revisor: o CI e a skill `preparar-pr` bastam.
- Achados do revisor que valem a pena viram **commits novos na mesma branch**, feitos pelo agente que abriu o PR (retome-o com a mensagem dos ajustes).

### Quando pedir o merge

Só diga à pessoa que um PR está **pronto para merge** depois que os checks passaram e, se o PR for de risco, o `revisor` aprovou e os ajustes escolhidos foram aplicados. Sem revisor, o CI verde e a verificação proporcional do autor bastam. Enquanto o revisor de um PR de risco não terminar, deixe claro: "em revisão, **não mergear ainda**".

Se um PR for mergeado antes dos ajustes, **não envie mais nada para a branch dele** (ela foi apagada no merge e o push a recriaria). Crie uma branch nova a partir da `origin/main`, traga os commits com `git cherry-pick` e abra um PR novo.

## 7. Resumo final para a pessoa

- PRs abertos, com links. O veredito do revisor só quando ele rodou.
- O que montar no editor do Unity e como testar.
- Decisões pendentes.
- Worktrees que sobraram (a remoção é da pessoa; a IA não apaga).
