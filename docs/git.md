# Fluxo do Git

Ninguém envia código direto para a `main`. Todo trabalho segue o caminho:

```
main ──► branch da tarefa ──► commits ──► PR ──► CI + review ──► squash merge na main
```

O repositório é **público**, e a `main` é protegida pelo GitHub com um ruleset ([ADR 0029](decisoes/0029-main-protegida-com-repo-publico.md)), além dos hooks locais:

| Onde | O que confere | Arquivo |
|---|---|---|
| Na sua máquina, ao commitar | Mensagem do commit (commitlint) | `.husky/commit-msg` |
| Na sua máquina, ao dar push | **Bloqueia push na `main`**, branch fora do padrão, branch cujo PR já foi mergeado e `.meta` que o `npm run unity:meta` gera faltando no commit | `.husky/pre-push` |
| No GitHub, no PR | Título do PR, nome da branch, formatação, build | `.github/workflows/` |
| No GitHub, na `main` | PR obrigatório, só squash, checks `csharpier`, `build` e `padrao-git` verdes **com a branch atualizada** | `.github/rulesets/proteger-main.json` (ativo) |
| No GitHub, no PR de client | Compila e roda os testes EditMode no Unity (~10 min; não obrigatório) | `.github/workflows/client.yml` |

**Nunca use `--no-verify`.**

Os tipos e escopos válidos ficam num lugar só: [commitlint.config.mjs](../commitlint.config.mjs).

## Configuração (uma vez por clone)

Precisa do [Node.js](https://nodejs.org) 20 ou mais novo (só para as ferramentas do git; o jogo continua em C#).

```
npm install
```

Isso instala o commitlint e ativa os hooks do husky (`git config core.hooksPath` passa a apontar para `.husky/_`). No Windows, a preparação ajusta os hooks para localizar automaticamente o `sh.exe` do Git for Windows, permitindo commitar e dar push diretamente no PowerShell sem exigir Git Bash manual. Sem esse passo os hooks locais não rodam, mas o CI confere tudo do mesmo jeito.

## Ciclo de vida de uma tarefa

Para evitar que mais de uma pessoa ou agente trabalhe na mesma tarefa ao mesmo tempo, toda tarefa segue o fluxo:

```
Issue aberta ──► npm run tarefa -- puxar <N> ──► branch criada + In Progress ──► commits ──► PR (Closes #N) ──► merge (Done)
```

1. **Listar tarefas disponíveis:**
   ```
   npm run tarefa -- listar
   ```
   Mostra quais estão livres (`DISPONÍVEIS`) e quais já estão sendo feitas (`EM ANDAMENTO`).

2. **Puxar a tarefa (bloqueia concorrência e cria branch):**
   ```
   npm run tarefa -- puxar <numero> [nome-da-branch] [--agente <nome>]
   ```
   - Marca a issue com `em-andamento`;
   - Move o status no GitHub Projects para `In Progress`;
   - Atribui ao desenvolvedor (`@me`) ou anota o agente;
   - Publica comentário com quem assumiu e em qual branch;
   - Cria a branch a partir da `origin/main` atualizada.
   - **Se outra pessoa ou agente já tiver puxado a tarefa, o comando bloqueia a ação.**

3. **Subtarefas em orquestrações paralelas:**
   Se uma tarefa envolve múltiplos agentes em paralelo (ex.: contrato, server e client), crie subtarefas para que cada agente trave a sua parte:
   ```
   npm run tarefa -- subtarefa <pai> --titulo "..." --agente <nome>
   ```

4. **Liberar a tarefa (se pausar ou transferir):**
   ```
   npm run tarefa -- liberar <numero> --motivo "Pausa para almoço"
   ```
   Devolve a tarefa para a fila de disponíveis.

5. **Concluir via PR:**
   No corpo do PR, inclua `Closes #<numero>`. O merge na `main` fecha a issue e move para `Done`.

## Branches

Formato: **`<tipo>/<escopo>/<descricao>`**

```
feature/itens/fusao-basica
fix/economia/taxa-leilao
docs/pvp/regras-mundo-aberto
chore/core/atualiza-csharpier
```

- **tipo:** o mesmo do commit, mas `feat` vira `feature`: `feature`, `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
- **escopo:** a funcionalidade em minúsculas (`itens`, `economia`, `pvp`...) ou `core`.
- **descrição:** curta, minúsculas, números e hífens, sem acento.

Para conferir um nome: `npm run validar-branch -- feature/itens/fusao-basica`.

Criando a branch:

```
git switch main
git pull --rebase origin main
git switch -c feature/itens/fusao-basica
```

Errou o nome? `git branch -m feature/itens/nome-certo` (antes do primeiro push).

Branches curtas: abra o PR em poucos dias. Mantenha a branch atualizada com `git pull --rebase origin main`. A branch é apagada sozinha depois do merge.

## Commits

Padrão [Conventional Commits](https://www.conventionalcommits.org/pt-br/), conferido pelo [commitlint](https://commitlint.js.org):

```
<tipo>(<escopo>): <descrição>

[corpo opcional: o porquê da mudança]

[rodapé opcional: Closes #12, BREAKING CHANGE: ...]
```

```
feat(itens): adiciona limite de fusões por raridade
fix(economia): corrige cálculo da taxa do leilão
docs(pvp): define regras de PvP no mundo aberto
refactor(combate): separa cálculo de dano crítico
chore(core): atualiza CSharpier para 1.4.0
```

| Tipo | Quando usar |
|---|---|
| `feat` | Funcionalidade nova ou comportamento novo |
| `fix` | Correção de bug |
| `docs` | Só documentação |
| `refactor` | Muda o código sem mudar o comportamento |
| `perf` | Melhora desempenho |
| `test` | Testes |
| `build` | Build, dependências, pacotes, `.csproj` |
| `ci` | Workflows do GitHub Actions |
| `chore` | Manutenção que não se encaixa nos outros |
| `revert` | Desfaz um commit anterior |

Regras:

- **Escopo obrigatório**: a funcionalidade (`itens`, `economia`...) ou `core` para infraestrutura, ferramentas e documentação geral.
- **Descrição em português**, começando com **letra minúscula** e verbo no presente: "adiciona", "corrige", "remove". Sem ponto final.
- Cabeçalho com no máximo **100 caracteres**.
- Corpo e rodapé separados por uma linha em branco.
- Mudança que quebra compatibilidade (ex.: muda o formato de uma mensagem de rede): `feat(itens)!: ...` e um rodapé `BREAKING CHANGE: <o que quebra>`.

Para testar uma mensagem sem commitar: `echo "feat(itens): adiciona fusão" | npx commitlint`.

### Entregar

No PowerShell, `gh pr create --body` quebra na crase e um `-m` longo passa de 100 caracteres. Use:

```
git add <arquivos da tarefa>
npm run entregar -- --titulo "tipo(escopo): descricao" --corpo pr-corpo.md
```

O script commita o que já está no stage (`git commit -F`, corpo quebrado em linhas de 100), faz o push e abre o PR com `--body-file`. O arquivo do corpo não entra no commit: `pr-corpo.md` na raiz está no `.gitignore` e funciona também em worktree (onde `.git` é um arquivo, não uma pasta). Se a branch já teve PR mergeado, os commits que o squash ainda não tem seguem numa branch `<nome>-segue` a partir de `origin/main`.

### Leva empilhada

Um PR que parte de outro ainda aberto nasce com `npm run tarefa -- puxar <N> <branch> --base <branch-do-PR-de-baixo>` e diz no topo do corpo `Depende de #<PR de baixo> (mergear antes)`. Quando o de baixo entra por squash, o de cima conflita consigo mesmo: ainda carrega os commits originais, que na `main` viraram outro hash. Não resolva os conflitos à mão nem force o push.

**O CI reempilha sozinho** ([`reempilhar.yml`](../.github/workflows/reempilhar.yml)): no merge de um PR, todo PR aberto que cita ele em `Depende de` é refeito sobre a `main`. Se o cherry-pick der conflito de verdade, o job comenta no PR e para. À mão (ou quando o job falhou):

```
npm run reempilhar -- <PR de cima>
```

  A [lição do falso conflito](licoes.md#2026-10-13--reempilhar-informava-conflito-falso) registra a falha que interrompia a publicação depois de um cherry-pick concluído.

O script lê a linha `Depende de`, leva só os commits próprios do PR (cherry-pick) para uma branch `<nome>-segue` a partir de `origin/main`, abre o PR novo com o mesmo título e corpo, fecha o antigo e troca o número na linha `Depende de` dos PRs de cima. Usa só a API REST (`gh api`), que funciona também nas sessões na nuvem.

O PR que o job abre só dispara os checks obrigatórios se o segredo `REEMPILHAR_TOKEN` existir (token fine-grained deste repo com escrita em Contents e Pull requests); com o token padrão do Actions, o GitHub não roda workflows no PR novo.

Empilhe só quando o PR de cima usa de fato o código do de baixo; funcionalidades independentes saem da `origin/main`.

Os commits dentro da branch podem ser pequenos e frequentes. Como o merge é por squash, o que entra na `main` é **o título do PR**.

## Pull requests

- **Título do PR = mensagem do commit final**, no mesmo padrão: `feat(itens): adiciona limite de fusões por raridade`. O CI confere.
- Um PR = uma funcionalidade. Se precisar mexer em duas, separe em dois PRs.
- Preencha o [template](../.github/pull_request_template.md).
- **Para o merge** (o GitHub bloqueia o que estiver em negrito):
  - **checks verdes: `csharpier`, `build` (que também roda os testes) e `padrao-git`**;
  - **branch atualizada com a `main`** (botão "Update branch" no PR);
  - **todas as conversas do review resolvidas**;
  - testes do que mudou, no próprio PR (PR de código sem teste não entra);
  - PR de client: olhe o check "Testes EditMode (Unity)" antes de mesclar;
  - PR que entrega uma funcionalidade de jogo: a cena está no PR e "Como testar" diz o que a pessoa faz e o que aparece ([Entrega da funcionalidade](agentes.md#entrega-da-funcionalidade));
  - aprovação não é obrigatória; PR que mexe em `Core/` ou na pasta de outra pessoa pede review ao responsável (veja [docs/responsaveis.md](responsaveis.md)).
- **Auto-merge:** "Enable auto-merge" no PR mescla sozinho quando os checks passarem.
- Merge sempre por **squash** (o repo só permite squash).

## Proteção da main (para o admin do repo)

**Situação atual:** o ruleset está ativo desde 2026-10-11, quando o repo ficou público ([ADR 0029](decisoes/0029-main-protegida-com-repo-publico.md)). As opções de merge (passo 2) e o auto-merge estão ligados.

A proteção é um **ruleset** do GitHub, versionado em [.github/rulesets/proteger-main.json](../.github/rulesets/proteger-main.json). Ele bloqueia push direto, force push e exclusão da `main`, exige PR com as regras acima e só permite squash merge. Admins só passam por cima pelo próprio PR, em emergência.

Para aplicar (depois do primeiro push da `main`, porque o ruleset bloqueia até a criação da branch):

0. **Primeiro push, uma única vez:** a `main` ainda não existe no GitHub e o hook local bloqueia push na `main`, então esse push é a única exceção:
   ```
   git commit -m "chore(core): cria estrutura inicial do projeto"
   git push --no-verify -u origin main
   ```
1. *(Só com repo público ou GitHub Pro.)* Já aplicado. **Settings > Rules > Rulesets > New ruleset > Import a ruleset** e escolha o arquivo `proteger-main.json`. Confira e salve. Pela linha de comando: `gh api -X POST repos/GabrielNied/jogo_do_grupo/rulesets --input .github/rulesets/proteger-main.json`.
2. **Settings > General > Pull Requests:**
   - desmarque "Allow merge commits" e "Allow rebase merging"; deixe só **"Allow squash merging"**, com a mensagem padrão **"Default to pull request title and commit details"**;
   - marque **"Always suggest updating pull request branches"** e **"Automatically delete head branches"**.

Mudou o ruleset no GitHub? Exporte de novo (Rulesets > ... > Export) e atualize o JSON no repo por PR.

Enquanto o `CODEOWNERS` estiver todo comentado, a exigência de review do responsável não tem efeito; vale só a aprovação de qualquer pessoa com acesso de escrita.

## Main com build vermelho

Dois PRs verdes podem entrar na `main` um após o outro sem ninguém atualizar a branch; juntos, quebram o `build` do servidor/shared. O CI do PR só enxerga a `main` do momento em que rodou.

Quando o workflow [Server, Shared e testes](../.github/workflows/server.yml) falha num **push na `main`**, o GitHub abre (ou reabre com comentário) uma issue `[Core] Build falhou na main` com link para a rodada. Só existe **uma** issue aberta por quebra; pushes seguintes comentam nela.

| Quem | O que fazer |
|---|---|
| Quem puder primeiro | `npm run tarefa -- puxar <N>` na issue, conserta na `main` (branch curta + PR ou hotfix acordado com o grupo). |
| Autores de PR abertos | Depois que a `main` volta verde, **Update branch** no PR antes do merge. |
| Todos | Não mergear PR verde se a `main` já estiver vermelha por outro motivo; conserte a `main` antes. |

**Branch sempre atualizada antes do merge:** exigida pelo ruleset desde o [ADR 0029](decisoes/0029-main-protegida-com-repo-publico.md). A issue continua para o que escapar (ex.: bypass de admin).

## Situações comuns

| Situação | O que fazer |
|---|---|
| Issue `[Core] Build falhou na main` aberta | Siga [Main com build vermelho](#main-com-build-vermelho). |
| O hook recusou a mensagem | Leia o erro, corrija e rode o `git commit` de novo. A mensagem anterior fica em `.git/COMMIT_EDITMSG`. |
| Errei a mensagem do último commit | `git commit --amend` (antes do push) |
| O CI recusou o título do PR | Edite o título no GitHub; o check roda de novo sozinho |
| Commitei na `main` local sem querer | `git switch -c feature/<x>/<descricao>` (leva os commits) e depois `git switch main && git reset --hard origin/main` |
| Branch desatualizada | `git pull --rebase origin main` e `git push --force-with-lease` |
| Pular os hooks | `--no-verify` só em emergência; o CI confere tudo de novo no PR |
No Windows, se os hooks falharem por ausência de sh, rode `npm run prepare` para reconfigurar a resolução do Git for Windows.
