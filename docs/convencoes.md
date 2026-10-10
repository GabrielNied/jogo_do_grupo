# Convenções

## Idioma

- Nomes de domínio do jogo em **português, sem acento**: `Itens`, `Profissoes`, `Fusao`, `Raridade`.
- Termos técnicos consagrados podem ficar em inglês: `Module`, `MessageIds`, `Handler`.
- Documentação em português.

## Nomes

| Item | Padrão | Exemplo |
|---|---|---|
| Pasta de funcionalidade | PascalCase | `Features/Profissoes/` |
| Namespace do servidor | `JogoDoGrupo.Server.Features.<X>` | `JogoDoGrupo.Server.Features.Itens` |
| Namespace do shared | `JogoDoGrupo.Shared.Features.<X>` | `JogoDoGrupo.Shared.Features.Itens` |
| Namespace do cliente | `JogoDoGrupo.Client.Features.<X>` | `JogoDoGrupo.Client.Features.Itens` |
| Classes, métodos, propriedades | PascalCase | `CalcularFusao()` |
| Campos privados | `_camelCase` | `_inventario` |
| Variáveis locais e parâmetros | camelCase | `itemSacrificado` |

## C#

- **Shared:** C# 9, `namespace X { }` com chaves, sem `UnityEngine`.
- **Servidor:** C# moderno, namespace de arquivo (`namespace X;`).
- **Cliente:** C# 9 (limite do Unity).

## Formatação

A formatação é automática e não se discute em review: quem decide é o [CSharpier](https://csharpier.com) (versão fixada em `dotnet-tools.json`). Ele formata os `.cs` e os `.csproj` de client, server e shared, e ignora o que está no `.gitignore`.

| Quando | Comando (na raiz do repo) |
|---|---|
| Uma vez por máquina | `dotnet tool restore` |
| Antes de abrir o PR | `dotnet csharpier format .` |
| Só conferir (o que o CI roda) | `dotnet csharpier check .` |

- O workflow [format.yml](../.github/workflows/format.yml) roda o `check` em todo PR e push na `main`. PR com código fora do padrão não passa.
- Plugin do editor, com "formatar ao salvar":
  - **VS Code e Cursor:** extensão `csharpier.csharpier-vscode` (o repo já recomenda em `.vscode/extensions.json`). O "formatar ao salvar" com o CSharpier já vem ligado pelo `.vscode/settings.json` do repo.
  - **Rider:** plugin "CSharpier" (Settings > Tools > CSharpier > Run on Save).
  - **Visual Studio:** extensão "CSharpier" (Tools > Options > CSharpier > Reformat on Save).
- Largura máxima da linha: 100 caracteres (padrão do CSharpier, explícito no `.editorconfig`).
- Para atualizar a versão do CSharpier: `dotnet tool update csharpier`, rode `dotnet csharpier format .` e faça um PR só com isso (`chore(core): atualiza CSharpier`).

## .editorconfig

O `.editorconfig` na raiz cuida do resto: charset UTF-8, fim de linha LF, indentação (4 espaços em C#, 2 em JSON/YAML/asmdef/csproj) e as regras de nomes. O CSharpier lê a indentação e a largura de linha dele.

As regras de nomes (ex.: `_camelCase` em campos privados) aparecem como **sugestão** no editor, mas ainda não quebram o build.

## Assets do Unity

- Nomes em PascalCase, sem espaços: `EspadaLendaria.prefab`, `RegiaoFloresta.unity`.
- Cada asset fica na pasta da funcionalidade dona dele.
