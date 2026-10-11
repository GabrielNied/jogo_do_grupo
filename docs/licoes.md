# Lições do processo

Problemas que já aconteceram trabalhando no repositório (com pessoas e com agentes de IA) e o que fazer para não repetir. É um registro vivo: **novo problema, nova entrada** (skill [registrar-licao](../.claude/skills/registrar-licao/SKILL.md)). Cada lição diz o que houve, a regra para evitar e onde a regra já está escrita.

Formato: `### AAAA-MM-DD · título` com **O que houve**, **Regra** e **Origem** (PR ou issue).

## Regras gerais

- **Commite em etapas pequenas, mesmo em trabalho em andamento.** O que não está num commit pode sumir.
- **Um problema novo vira lição no mesmo PR que o corrigiu** (ou num PR de docs logo depois), não só uma conversa.

### 2026-10-11 · Raças dos personagens (PR #383)

- **Um Blender por vez.** Vários `blender.exe` ao mesmo tempo (agentes ou comandos em paralelo) travaram e reiniciaram o computador. O artista roda um modelo por vez, em primeiro plano, com limite de tempo, e confere com `tasklist` que nenhum ficou vivo antes do próximo. Prévia em baixa resolução, uma por modelo. Coordenador nenhum dispara Blender em paralelo nem por subagente.
- **Commit por etapa, mesmo em WIP.** Uma mudança grande não commitada no checkout principal foi perdida quando algo fora da sessão reverteu a árvore de trabalho (nem `stash` guardou). O agente commita ao fim de cada etapa grande (scripts, binários gerados, gerador, docs) na branch da tarefa; o commit pode ser ajustado depois, o arquivo perdido não volta. Trabalho longo de artista pede worktree próprio quando houver outras sessões no mesmo diretório.
- **`npm run unity:editor` olha qualquer Unity aberto**, até o de outro projeto (ex.: `jogo_do_grupo_pendencias`), e então bloqueia o modo batch. Se bloquear, confira quem está aberto na saída do comando e espere ou peça para fechar essa janela.
- **O coordenador reparte por arquivo.** Artista só em `arte/` e `Features/Personagem/{Art,Editor,Resources}`; contrato, servidor, tela e testes ficam com o coordenador (ou outro agente). Assim os dois trabalham ao mesmo tempo sem mexer no mesmo arquivo; o nome do prefab (`Jogador_<NomeDoEnum>`) é o único combinado entre eles.
- **Push depois do merge não entra na `main`.** O PR #383 foi mergeado enquanto eu ainda commitava docs na mesma branch; o último commit ficou de fora. Antes do push final, `gh pr view <N> --json state`; se já foi mergeado, parta de `origin/main` e abra um PR novo (foi o #389).
- **Shell: heredoc grande com muitas aspas e `sed` em cadeia falha por inteiro.** Um erro de parse não executou nada e quase passou despercebido. Para arquivos de código use a ferramenta de escrita de arquivos; `sed` só para trocas de uma linha.
