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

## 2. Montar o plano e pedir aprovação

Mostre à pessoa uma tabela e **espere o ok** antes de lançar qualquer agente:

| Etapa | Agente | Tarefa | Depende de |
|---|---|---|---|
| 1 | shared-contratos | ... | — |
| 2a | server-funcionalidade | ... | 1 mergeado |
| 2b | client-funcionalidade | ... | 1 mergeado |
| 3 | revisor | revisar os PRs de 2a e 2b | 2a, 2b |

Pergunte junto: os agentes podem commitar, enviar as branches e abrir PRs? (Merge é sempre humano.)

## 3. Contrato primeiro

Se há contrato, lance o `shared-contratos` sozinho. Quando o PR dele estiver aberto, peça à pessoa para revisar e fazer o merge. Os agentes seguintes rodam em worktrees que partem da `main`, então **só lance a próxima etapa depois do merge**.

## 4. Etapas em paralelo

Lance os agentes independentes **na mesma mensagem, em segundo plano**, um por subtarefa. Cada prompt precisa ser completo, porque o agente não vê esta conversa:

```
Funcionalidade: <X> (pasta Features/<X>/, escopo de commit <x>)
Objetivo: <o que deve funcionar ao final, em 2–4 linhas>
Contrato: <arquivos e IDs do shared já na main, ou "não se aplica">
Regras de design: docs/features/<x>.md, seções <...>
Fora do escopo: <o que NÃO fazer>
Pronto quando: <critérios verificáveis>
Autorização: pode commitar, enviar a branch e abrir PR. Não faça merge.
```

Nunca lance dois agentes na mesma área ao mesmo tempo (ex.: dois `server-funcionalidade` na mesma funcionalidade).

## 5. Juntar os resultados

Para cada relatório:

- **"Para outros agentes"** vira uma nova subtarefa (outra rodada) ou um item para a pessoa (montar no Unity, decidir algo).
- **Dúvidas** vão para a pessoa; não decida regras de design por conta própria.
- Lance o `revisor` para cada PR aberto.

## 6. Resumo final para a pessoa

- PRs abertos, com links, e o veredito do revisor de cada um.
- O que montar no editor do Unity e como testar.
- Decisões pendentes.
- Worktrees que sobraram (a remoção é da pessoa; a IA não apaga).
