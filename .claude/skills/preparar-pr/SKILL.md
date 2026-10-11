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

- **Testes:** todo código novo ou alterado tem teste em `tests/` (caminho feliz, erros, limites, concorrência quando houver). Faltou? Escreva antes de abrir o PR. Mudança só de docs, `.meta` ou configuração não precisa.

- Nenhuma referência direta entre funcionalidades (nem `using JogoDoGrupo.*.Features.<Outra>` nem referência entre `.asmdef` de features).
- `shared/` sem `UnityEngine` e sem recursos acima de C# 9 (namespace de arquivo, `record struct`, `required`, etc.).
- IDs de mensagem dentro de `<X>MessageIds.Faixa`.
- Lógica que decide resultado (dano, drop, fusão, preço) no servidor, não no cliente.
- Nomes conforme `docs/convencoes.md`.

## 4. Verificação proporcional

Sempre rode `git diff --check`. Depois aplique **só** as linhas que cobrem os arquivos alterados; não rode build, testes .NET ou Unity por ritual. O CI continua sendo a confirmação completa no PR.

| Alteração | Verificação local mínima |
|---|---|
| Só Markdown/documentação | `git diff --check`; confira links, comandos e referências alterados. Não rode CSharpier, build, testes ou Unity. |
| Instruções/configuração de IA (`AGENTS.md`, `.claude/`, `.codex/`, `.agents/`) | `git diff --check` e valide diretamente o formato/comportamento alterado (ex.: `node --check` para hook, parse de JSON/TOML). |
| Script Node ou ferramenta de automação | `node --check <arquivo>` e o teste/comando específico do script. Rode testes do projeto só se o script os altera ou dispara. |
| C# de servidor ou shared | `dotnet csharpier format .`, `dotnet csharpier check .` e `dotnet test JogoDoGrupo.slnx -c Release`. Não rode `dotnet build` separado: o teste já compila. |
| C# de client | `dotnet csharpier format .`, `dotnet csharpier check .`, `npm run unity:compilar`, `npm run conferir-unity` e `npm run unity:testes` quando houver testes EditMode afetados ou adicionados. |
| Shared usado pelo client | Além da linha de server/shared, `npm run unity:compilar`. |
| Conteúdo Unity gerado (cena, prefab, asset, animação) | O comando Unity que gerou/alterou o conteúdo, `npm run conferir-unity` e `npm run unity:recompilar` (editor aberto) ou `npm run unity:compilar` (fechado); `npm run unity:testes` se a lógica/teste EditMode mudou. |
| Dependência, CI ou configuração de build | O comando que usa a configuração alterada e o teste/build especificamente afetado. |

**Sem Unity no ambiente** (nuvem): os comandos `unity:*` saem 0 com "pulado". Não tente de novo nem peça à pessoa para rodar na máquina dela. Gere os `.meta` com `npm run unity:meta` e abra o PR normal (não em rascunho; ele pode ir para merge sem a validação do Unity). O CI "Client (Unity)" compila e roda os testes EditMode quando a licença estiver configurada ([ADR 0017](../../../docs/decisoes/0017-unity-no-ci.md)). Logo depois de abrir o PR, registre o que ficou sem fazer ou sem validar: `npm run tarefa -- pendencia-unity <PR> --falta "<item>"` (um `--falta` por item, ex.: "compilar e rodar os testes EditMode", ".meta das texturas novas", "teste visual da tela"). A pendência continua aberta depois do merge, e o próximo agente com Unity puxa e resolve.

Se o risco extrapolar essa matriz (segurança, concorrência, migração, mudança ampla de Core), acrescente a verificação necessária e explique o motivo. Se o CSharpier alterar arquivos, inclua essas mudanças.

## 5. Documentação

- Mudou regra do jogo? `docs/features/<x>.md` atualizado.
- Decisão técnica nova? Existe ADR (skill `novo-adr`).

## 6. Branch e commit

- Branch: confira com `npm run validar-branch -- "$(git branch --show-current)"`. Se estiver na `main` ou fora do padrão, siga a skill `nova-branch`. Nunca faça push na `main`.
- Commits: siga a skill `fazer-commit`. Confira as mensagens da branch com `npx --no -- commitlint --from origin/main --to HEAD`.
- Se a branch mexe em `client/Assets/` ou `shared/Package/`, rode `npm run conferir-unity` **antes** de abrir o PR. Se falhar, não abra: faltam `.meta` ou o Unity alterou `.mat`/prefab que ficou fora do commit. Para script C#, pasta, `.asmdef`, `.uss`/`.tss` e texto, gere com `npm run unity:meta` (não precisa do Unity nem de editor fechado). Para o resto, gere com `npm run unity:importar` (editor fechado, neste worktree). Sem Unity no ambiente, abra o PR assim mesmo e registre o que falta com `npm run tarefa -- pendencia-unity <PR> --falta "<o quê>"`; `npm run conferir-unity` falhando por esses arquivos não impede o PR nesse caso. Inclua no commit. Não escreva esses arquivos à mão. `ProjectSettings/` continua de fora, salvo pedido da pessoa.
- Só faça commit ou push se a pessoa pedir.

## 7. Título e descrição do PR

**Título:** um cabeçalho de commit válido que resume o PR inteiro (ele vira o commit na `main` no squash merge). Valide com `printf '%s\n' "<título>" | npx --no -- commitlint`.

**Descrição:** siga `.github/pull_request_template.md`: "O que muda" (1–3 frases), "Funcionalidade", checklist marcado conforme o que foi verificado acima e "Como testar" com passos concretos.
