# Fluxo do Git

Ninguém envia código direto para a `main`. Todo trabalho segue o caminho:

```
main ──► branch da tarefa ──► commits ──► PR ──► CI + review ──► squash merge na main
```

O repositório é **privado** no plano gratuito do GitHub, que não oferece proteção de branch para repo privado. Por isso a proteção é **local e por combinado** ([ADR 0007](decisoes/0007-protecao-local-da-main.md)):

| Onde | O que confere | Arquivo |
|---|---|---|
| Na sua máquina, ao commitar | Mensagem do commit (commitlint) | `.husky/commit-msg` |
| Na sua máquina, ao dar push | **Bloqueia push na `main`** e branch fora do padrão | `.husky/pre-push` |
| No GitHub, no PR | Título do PR, nome da branch, formatação, build | `.github/workflows/` |
| No GitHub, na `main` | **Avisa** (check vermelho) quando um commit entra na `main` sem PR | `.github/workflows/padrao-git.yml` |
| ~~No GitHub, na `main`~~ | ~~PR obrigatório, aprovação, CODEOWNERS, CI verde~~ | `.github/rulesets/proteger-main.json`, **não ativo** enquanto o repo for privado no plano gratuito |

O GitHub não impede um merge sem aprovação nem um push com `--no-verify`. O que segura a `main` é o hook local, o aviso do CI e o combinado do grupo abaixo. **Nunca use `--no-verify`.**

Os tipos e escopos válidos ficam num lugar só: [commitlint.config.mjs](../commitlint.config.mjs).

## Configuração (uma vez por clone)

Precisa do [Node.js](https://nodejs.org) 20 ou mais novo (só para as ferramentas do git; o jogo continua em C#).

```
npm install
```

Isso instala o commitlint e ativa os hooks do husky (`git config core.hooksPath` passa a apontar para `.husky/_`). Sem esse passo os hooks locais não rodam, mas o CI confere tudo do mesmo jeito.

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

Os commits dentro da branch podem ser pequenos e frequentes. Como o merge é por squash, o que entra na `main` é **o título do PR**.

## Pull requests

- **Título do PR = mensagem do commit final**, no mesmo padrão: `feat(itens): adiciona limite de fusões por raridade`. O CI confere.
- Um PR = uma funcionalidade. Se precisar mexer em duas, separe em dois PRs.
- Preencha o [template](../.github/pull_request_template.md).
- **Combinado do grupo** para fazer o merge (o GitHub não bloqueia; cada um respeita):
  - checks verdes: `csharpier`, `build` (que também roda os testes) e `padrao-git`;
  - testes do que mudou, no próprio PR (PR de código sem teste não entra);
  - 1 aprovação de outra pessoa, incluindo o responsável pela pasta (veja [docs/responsaveis.md](responsaveis.md));
  - todas as conversas do review resolvidas;
  - branch atualizada com a `main` (botão "Update branch" no PR);
  - um novo push depois da aprovação pede uma nova aprovação.
- **Peça o review manualmente** (Reviewers, no PR). Em repo privado no plano gratuito, o `CODEOWNERS` não pede review sozinho; ele continua servindo de mapa de quem cuida de cada pasta.
- Merge sempre por **squash** (o repo só permite squash).

## Proteção da main (para o admin do repo)

**Situação atual:** o repo é privado no plano gratuito, e o GitHub recusa rulesets nesse caso. As opções de merge (passo 2 abaixo) já estão aplicadas. O ruleset (passo 1) fica pronto no repo para o dia em que o repo ficar público ou a conta passar para o GitHub Pro. Veja o [ADR 0007](decisoes/0007-protecao-local-da-main.md).

A proteção completa é um **ruleset** do GitHub, versionado em [.github/rulesets/proteger-main.json](../.github/rulesets/proteger-main.json). Ele bloqueia push direto, force push e exclusão da `main`, exige PR com as regras acima e só permite squash merge. Ninguém tem bypass, nem os admins.

Para aplicar (depois do primeiro push da `main`, porque o ruleset bloqueia até a criação da branch):

0. **Primeiro push, uma única vez:** a `main` ainda não existe no GitHub e o hook local bloqueia push na `main`, então esse push é a única exceção:
   ```
   git commit -m "chore(core): cria estrutura inicial do projeto"
   git push --no-verify -u origin main
   ```
1. *(Só com repo público ou GitHub Pro.)* **Settings > Rules > Rulesets > New ruleset > Import a ruleset** e escolha o arquivo `proteger-main.json`. Confira e salve. Pela linha de comando: `gh api -X POST repos/GabrielNied/jogo_do_grupo/rulesets --input .github/rulesets/proteger-main.json`.
2. **Settings > General > Pull Requests:**
   - desmarque "Allow merge commits" e "Allow rebase merging"; deixe só **"Allow squash merging"**, com a mensagem padrão **"Default to pull request title and commit details"**;
   - marque **"Always suggest updating pull request branches"** e **"Automatically delete head branches"**.

Mudou o ruleset no GitHub? Exporte de novo (Rulesets > ... > Export) e atualize o JSON no repo por PR.

Enquanto o `CODEOWNERS` estiver todo comentado, a exigência de review do responsável não tem efeito; vale só a aprovação de qualquer pessoa com acesso de escrita.

## Situações comuns

| Situação | O que fazer |
|---|---|
| O hook recusou a mensagem | Leia o erro, corrija e rode o `git commit` de novo. A mensagem anterior fica em `.git/COMMIT_EDITMSG`. |
| Errei a mensagem do último commit | `git commit --amend` (antes do push) |
| O CI recusou o título do PR | Edite o título no GitHub; o check roda de novo sozinho |
| Commitei na `main` local sem querer | `git switch -c feature/<x>/<descricao>` (leva os commits) e depois `git switch main && git reset --hard origin/main` |
| Branch desatualizada | `git pull --rebase origin main` e `git push --force-with-lease` |
| Pular os hooks | `--no-verify` só em emergência; o CI confere tudo de novo no PR |
