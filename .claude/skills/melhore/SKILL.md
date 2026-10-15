---
name: melhore
description: Procura melhorias no que já existe no Jogo do Grupo (produto, qualidade de código, desempenho do jogo e custo das IAs), com evidência, e abre as issues já com a ficha para o /trabalhe. Use para "/melhore" ou "criar tarefas de melhoria". Não implementa.
---

# Melhore

Você **vasculha** doc, histórico git e código na `origin/main` atrás de problemas reais (incluindo desencontro client/servidor/shared como [#826](https://github.com/GabrielNied/jogo_do_grupo/pull/826)) e abre issue com caminho pesquisado. Não implementa, não puxa. `/melhore` já autoriza escolher e abrir; não pergunte, não ofereça lista.

Argumentos (opcionais): **filtro** (`itens`, `codigo`, `produto`, …) e **N** issues (padrão 5, piores primeiro).

## 0. Varredura base (sempre)

- [Doc e regras atuais](../../../docs/agentes.md#doc-e-regras-atuais): `git fetch origin main`. Leia [padroes-de-fix.md](../../../docs/desenvolvimento/padroes-de-fix.md).
- Checkout alinhado à `origin/main` (ou leia só via `git show` / `git grep origin/main -- …`). Rode `npm run conferir-contratos-rede`; se falhar, **issue** (ou evidência numa aberta).
- `git log origin/main --oneline -50 --grep='^fix('`: agrupe por escopo; cruzar com a tabela de padroes-de-fix (sintoma na feature, causa em Core/contrato/evento/teste).
- `git grep -n -E 'ChannelsCount\s*=\s*[0-9]+' origin/main -- '*.cs'`: literal fora de `ThirdParty` → candidato (deveria ser `TransporteTempoReal.Canais` ou constante no shared).
- Contornos de rede: `git grep -n -i 'canal 0\|channel 0' origin/main -- server/ client/Assets/_Game/ shared/` (comentário “igual ao servidor” só num lado também conta).
- **Dois clients UDP:** se o servidor manda `Sequenciada` com canal por entidade, exija teste no `TransporteLiteNetLib` (EditMode), não só `ClienteTempoRealDeTeste`.
- **Eventos:** em `docs/features/*.md`, eventos que invalidam estado (`EquipamentoMudou`, entrada/saída de área, …) → `git grep Assinar origin/main -- server/src/JogoDoGrupo.Server/Features/` e caches “em memória” nos mesmos fluxos; assinatura faltando = issue com módulo e teste esperado.
- **Corte já na main:** script/hook que impede o erro (ex.: `conferir-contratos-rede`) → não abra issue para recriar; abra só **extensão** (novo contrato sem check) ou **contorno** ainda no código.

## 1. Lentes (depois da varredura)

[Foco atual](../../../docs/features/fatia-jogavel.md#foco-atual) primeiro. Ideia sem evidência não entra.

- **Produto:** cena vs [entrega](../../../docs/agentes.md#entrega-da-funcionalidade) e checklist de experiência; fluxo que a doc promete e o código não fecha.
- **Código:** regras 2, 3 e 5; contrato/regra sem teste; duplicação; `TODO`/`FIXME`; warnings de build; achados da §0 ainda sem issue.
- **Desempenho:** alocação por quadro/pacote, tick, consulta em laço, carga de cena — com trecho que prova.
- **Custo de IA:** `npm run contexto`; CI falhando (`gh run list --status failure --limit 30`); PRs com vários `fix`; [licoes.md](../../../docs/licoes.md) sem script; skill que relê a issue.

Corte pequeno (regra 16) cabe num PR de processo: **não** vira issue. Fora disso, **abre task**.

Não reabra **Definido**; sistema novo é `/decida`.

## 2. Evidência e duplicata

Evidência: `arquivo:linha`, PR ou run. Sem evidência, não abre. `gh issue list --state all --search "…"`: se já existe, comente com evidência nova.

## 3. Abrir

Até N issues. Título `[<Escopo>] Melhoria: <o que passa a acontecer>`. Corpo = [ficha](../../../docs/agentes.md#ficha-da-tarefa): **Hoje**, **Onde mexer**, **Fora do escopo**, **Evidência:**, **Lote sugerido** se couber.

Contrato de fio: diga shared + server + client Unity + teste (xUnit e/ou EditMode) e `conferir-contratos-rede` se aplicável.

Rótulos: funcionalidade; `prio:*` (1 se quebra/trava, 2 se core-loop, 3 experiência, 6 segurança); `qualidade` (código/desempenho); `desenvolvimento` (IA).

Depois: `npm run tarefa -- relevancia <N> --conferida "aberta pelo /melhore"`; `segure rotular` se houver segure no escopo.

## 4. Relatório

Issues criadas (número, lente, uma linha); o que a varredura §0 rodou; o que ficou quieto (ok na main, issue existente, sem evidência).
