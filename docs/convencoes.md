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

## Valores de balanceamento

- Separe os valores ajustáveis do jogo (XP, velocidades, capacidade de inventário, custos, tempos e limites) dos cálculos. Não espalhe números literais pela lógica.
- Para poucos valores, use constantes nomeadas reunidas no topo da classe. Para um conjunto de ajustes, use `Config<Assunto>` na pasta da funcionalidade responsável. Exemplo: [ConfigExperiencia](../shared/Package/Runtime/Features/Personagem/ConfigExperiencia.cs).
- Documente unidade, finalidade e efeito do ajuste. Mantenha uma única fonte de valores; outras classes referenciam a configuração, sem copiar números.
- Regras compartilhadas ficam no shared; configurações exclusivas do servidor ou do client ficam no respectivo lado. O servidor continua decidindo resultados; valores enviados pelo client nunca substituem a configuração do servidor.
- Constantes são adequadas ao balanceamento que muda entre versões (exigem recompilar). Se for necessário ajustar em execução, use configuração carregada e validada pelo servidor; o client recebe apenas o necessário para apresentação.
- **Sensação no client** (hit-stop, tremor, flash, recuo, tempo e velocidade de animação, transição, câmera, número na tela, volume de efeito): `public static` em `Config<Assunto>` do client, registrado na `Sintonia` (Core) com mínimo, máximo e unidade. Aparece no painel F10 e muda ao vivo ([Report beta e ajuste ao vivo](features/desenvolvimento.md#report-beta-e-ajuste-ao-vivo)). Exemplo: [ConfigSensacaoCombate](../client/Assets/_Game/Features/Combate/Scripts/ConfigSensacaoCombate.cs). Valor que decide resultado continua no servidor. Valor de sensação antigo ainda em `const` migra quando for mexido ([ADR 0040](decisoes/0040-sensacao-ajustavel-e-relato.md)).
- Teste os valores padrão e os limites das regras; ao recalibrar, atualize as expectativas de balanceamento. Não migre outras funcionalidades sem uma tarefa para isso.

Motivo da decisão: [ADR 0017](decisoes/0017-valores-de-balanceamento.md).

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

## Testes

Decisão: [ADR 0013](decisoes/0013-testes-automatizados.md).

| Comando | O que faz |
|---|---|
| `dotnet test JogoDoGrupo.slnx` | Roda todos os testes (shared e servidor). É o que o CI roda. |
| `npm run unity:compilar` | Compila o client e o shared contra as DLLs do Unity, sem abrir o editor |

- **Onde:** `tests/JogoDoGrupo.Shared.Tests/` e `tests/JogoDoGrupo.Server.Tests/`, com pastas que espelham o código (`Core/<Area>/`, `Features/<X>/`). Uma classe de teste por classe testada: `<Classe>Testes`.
- **Nomes dos testes:** frases curtas em português com `_`, dizendo o comportamento: `Registrar_usuario_repetido_sem_diferenciar_maiusculas_da_409`.
- **API do servidor:** use `IClassFixture<ServidorDeTeste>` (`Infra/ServidorDeTeste.cs`). Ele sobe o servidor de verdade com um banco SQLite temporário por classe; dentro da classe, use `ServidorDeTeste.NomeUnico()` para não depender da ordem dos testes. Exemplo: `Core/Contas/ContasRotasTestes.cs`.
- **O que testar** ([ADR 0034](decisoes/0034-menos-verificacao-repetida.md)): o contrato que mudou (rota ou mensagem: sucesso e os erros que o client trata) e as regras que decidem resultado (dano, drop, item, moeda), com pedidos simultâneos quando algo puder duplicar ou sumir. O objetivo é saber que nada quebrou, não cobrir cada limite.
- **Client:** teste EditMode em `Features/<X>/Tests/` só para lógica pura que já quebrou ou é difícil ver na tela; tela e layout não têm teste. O CI roda os testes EditMode depois do merge (no PR, com a label `testar-unity`); localmente, só compile. Compilação: `npm run unity:recompilar` com o editor aberto, ou `npm run unity:compilar` com ele fechado. O visual continua sendo conferido por uma pessoa no editor.

## .editorconfig

O `.editorconfig` na raiz cuida do resto: charset UTF-8, fim de linha LF, indentação (4 espaços em C#, 2 em JSON/YAML/asmdef/csproj) e as regras de nomes. O CSharpier lê a indentação e a largura de linha dele.

As regras de nomes (ex.: `_camelCase` em campos privados) aparecem como **sugestão** no editor, mas ainda não quebram o build.

## Assets do Unity

- Nomes em PascalCase, sem espaços: `EspadaLendaria.prefab`, `RegiaoFloresta.unity`.
- Cada asset fica na pasta da funcionalidade dona dele.
