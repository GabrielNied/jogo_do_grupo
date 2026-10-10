---
name: preparar-pr
description: Confere se a mudança atual está pronta para abrir PR neste repo (formatação CSharpier, build, pastas da funcionalidade, docs, branch e commit no padrão) e escreve a descrição do PR no template. Use quando pedirem para revisar antes do PR, preparar o PR, fazer commit ou "ver se está tudo certo".
---

# Preparar PR

Rode as verificações na ordem e **relate o resultado de cada uma**. Corrija o que for automático (formatação); para o resto, explique o problema e pergunte antes de mudar.

## 1. O que mudou

```
git status
git diff --stat main...HEAD
git diff --stat
```

Identifique a funcionalidade `<X>` da mudança.

## 2. Pastas

Os arquivos alterados devem estar em:

- `client/Assets/_Game/Features/<X>/`, `server/src/JogoDoGrupo.Server/Features/<X>/`, `shared/Package/Runtime/Features/<X>/`, `docs/features/<x>.md`

Avise se houver arquivos:

- de **outra funcionalidade**: sugira separar em outro PR ou pedir review do responsável (ver `docs/responsaveis.md`);
- de **Core** (`*/Core/`, `client/ProjectSettings/`, `client/Packages/`, `.github/`, cenas de `Core/Scenes/`): exigem review do responsável do core;
- **cenas `.unity`** de outra pessoa ou binários (PSD, FBX, Blend) sem `git lfs lock`.

## 3. Regras de código

Nos arquivos alterados, confira:

- Nenhuma referência direta entre funcionalidades (nem `using JogoDoGrupo.*.Features.<Outra>` nem referência entre `.asmdef` de features).
- `shared/` sem `UnityEngine` e sem recursos acima de C# 9 (namespace de arquivo, `record struct`, `required`, etc.).
- IDs de mensagem dentro de `<X>MessageIds.Faixa`.
- Lógica que decide resultado (dano, drop, fusão, preço) no servidor, não no cliente.
- Nomes conforme `docs/convencoes.md`.

## 4. Formatação e build

```
dotnet csharpier format .
dotnet csharpier check .
dotnet build JogoDoGrupo.slnx -c Release
```

O `check` precisa passar e o build não pode ter erros (avisos novos devem ser explicados). Se o `format` mudou arquivos, inclua essas mudanças.

## 5. Documentação

- Mudou regra do jogo? `docs/features/<x>.md` atualizado.
- Decisão técnica nova? Existe ADR (skill `novo-adr`).

## 6. Branch e commit

- Branch: confira com `npm run validar-branch -- "$(git branch --show-current)"`. Se estiver na `main` ou fora do padrão, siga a skill `nova-branch`. Nunca faça push na `main`.
- Commits: siga a skill `fazer-commit`. Confira as mensagens da branch com `npx --no -- commitlint --from origin/main --to HEAD`.
- Só faça commit ou push se a pessoa pedir.

## 7. Título e descrição do PR

**Título:** um cabeçalho de commit válido que resume o PR inteiro (ele vira o commit na `main` no squash merge). Valide com `printf '%s\n' "<título>" | npx --no -- commitlint`.

**Descrição:** siga `.github/pull_request_template.md`: "O que muda" (1–3 frases), "Funcionalidade", checklist marcado conforme o que foi verificado acima e "Como testar" com passos concretos.
