---
name: fazer-commit
description: Escreve commits e títulos de PR no padrão do commitlint do repo (tipo, escopo, descrição em português minúscula). Use para commitar, sugerir mensagem ou corrigir uma recusada.
---

# Fazer commit

Regras completas: [docs/git.md#commits](../../../docs/git.md#commits). Tipos e escopos válidos: `commitlint.config.mjs` (ele é a fonte da verdade).

## 1. Conferir a branch

`git branch --show-current`. Se for `main`, **não commite**: siga a skill `nova-branch` primeiro.

## 2. Ver o que vai entrar

```
git status
git diff --staged
```

Se nada estiver no stage, pergunte o que incluir ou adicione só os arquivos da tarefa (`git add <caminhos>`). Não use `git add .` sem olhar o `git status`: evite incluir arquivos de outra funcionalidade, `.env`, builds ou configurações pessoais.

Se houver C# alterado, rode `dotnet csharpier format .` antes e inclua o resultado.

Os testes vão **no mesmo commit** (ou no mesmo PR) do código que eles cobrem. Use o tipo `test` só para commits que mexem apenas em testes.

## 3. Escrever a mensagem

Formato `<tipo>(<escopo>): <descrição>`, com corpo e rodapé opcionais. Tipos, escopo, descrição, limite de caracteres e quebra de compatibilidade estão em [docs/git.md#commits](../../../docs/git.md#commits), a fonte única. Escopo `core` vale para infraestrutura, ferramentas e docs gerais.

Se a mudança cobre duas funcionalidades, ela provavelmente deveria ser dois commits ou PRs: avise a pessoa.

Exemplos:

```
feat(itens): adiciona limite de fusões por raridade
fix(economia): corrige arredondamento da taxa do leilão
docs(pvp): define penalidades do PvP no mundo aberto
chore(core): atualiza CSharpier para 1.4.0
```

## 4. Validar e commitar

Coloque os arquivos no stage e entregue pelo script ([docs/git.md#entregar](../../../docs/git.md#entregar)):

```
git add <arquivos da tarefa>
npm run entregar -- --titulo "tipo(escopo): descricao" --corpo pr-corpo.md
```

O corpo do commit e o do PR saem do arquivo. Não use `gh pr create --body` no PowerShell: a crase parte o comando. O hook `commit-msg` valida de novo. Se recusar, leia o erro, corrija e tente de novo. **Nunca use `--no-verify`** para passar por cima do hook.

## Título do PR

O merge é por squash, então **o título do PR vira o commit na `main`**. Ele segue exatamente as mesmas regras do cabeçalho acima e descreve a mudança inteira do PR, não o último commit.
