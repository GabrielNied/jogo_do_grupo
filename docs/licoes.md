# Lições do processo

Tropeços que ainda **não** viraram script, hook ou regra num doc. Quando viram, saem daqui: o commit guarda a história (skill [registrar-licao](../.claude/skills/registrar-licao/SKILL.md)). Ninguém precisa ler este arquivo inteiro; os docs apontam para a entrada certa.

Formato: `### AAAA-MM-DD · título` com **O que houve**, **Regra** e **Origem**.

## Regras gerais

- **Commite em etapas pequenas, mesmo em trabalho em andamento.** O que não está num commit pode sumir (uma mudança grande no checkout principal foi revertida por fora da sessão, e nem o `stash` guardou).

### 2026-10-11 · `unity:editor` olha qualquer Unity aberto

- **O que houve:** um editor aberto em outro projeto (`jogo_do_grupo_pendencias`) bloqueou o modo batch deste.
- **Regra:** se o comando disser que o editor está aberto, confira na saída qual janela é e espere ou peça para fechar.
- **Origem:** PR #383.

### 2026-10-11 · Hooks no PowerShell sem sh no PATH

- **O que houve:** o commit da #519 falhou com `/usr/bin/env: sh: No such file or directory`, com o Git instalado.
- **Regra:** rode o commit pelo Git Bash, com `/usr/bin:/bin` no PATH da sessão. Mantenha os hooks ativos, sem `--no-verify`.
- **Origem:** issue #519.

### 2026-10-11 · Edição no Windows

- **O que houve:** na #516, o alias Python do Windows não executou o script, e a edição exigiu corrigir codificação e quebras de linha.
- **Regra:** edite código com a ferramenta de patch. Se precisar de Python, use o runtime do ambiente com UTF-8 e quebras de linha explícitas.
- **Origem:** issue #516.

### 2026-10-12 · Unity do CI morto por outro job com a mesma licença

- **O que houve:** execuções do Client (Unity) morreram com `Killed` / exit 137 logo depois de ativar a licença, sem compilar nada, porque outro job do Unity rodava ao mesmo tempo (a licença Personal é uma só). Uma pendência foi "corrigida" com código enquanto a causa era essa.
- **Regra:** `Killed`/137 logo após a licença não é erro de código: confira se havia outro job do Unity e qual cache da `client/Library` foi restaurado. O grupo de concorrência `client-unity-licenca` e a troca do prefixo do cache (`Library-v3-`) resolveram até agora. Um `Killed` seguido esconde falhas reais: leia o log da execução que chegou a rodar os testes.
- **Origem:** issue #529, PRs #550 e #600.

### 2026-10-13 · Reempilhar informava conflito falso

- **O que houve:** o `reempilhar` aplicou o commit do #714, mas informou conflito e parou antes de publicar, porque tratou a saída herdada do Git como texto.
- **Regra:** em script, comando com `stdio: "inherit"` aceita retorno vazio; teste o caminho completo antes de atribuir a falha a conflito de cherry-pick.
- **Origem:** issue #716.

### 2026-10-15 · Sintoma na funcionalidade, causa no contrato

- **O que houve:** "mobs parados e saltando" virou `fix(mobs)` e `fix(core)` em série (#725, #732) até o #826 achar a causa: o client Unity ficou com 1 canal da LiteNetLib e o servidor mandava em 32 (hoje barrado por `npm run conferir-contratos-rede`). No mesmo padrão, atributos que "não atualizavam" eram um evento (`EquipamentoMudou`) que Combate não assinava (#720).
- **Regra:** quando um `fix(<feature>)` volta, olhe primeiro o contrato com o outro lado (canal, DTO, evento assinado, cache que o evento devia invalidar) e prove com teste que cruza os dois lados.
- **Origem:** PRs #720, #826 e #846.
