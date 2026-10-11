---
name: nova-funcionalidade
description: Cria uma funcionalidade nova do jogo nas três partes do monorepo (client Unity, server .NET, shared) e na documentação, com a próxima faixa de IDs de mensagem livre. Use quando pedirem para adicionar/criar uma feature, sistema ou área nova do jogo (ex.: "criar a feature Montarias").
---

# Nova funcionalidade

Uma funcionalidade é uma fatia vertical: uma pasta em client, server, shared e uma página em docs. Esta é a única tarefa em que é normal mexer em arquivos fora de `Features/<X>/` (README, CODEOWNERS, responsáveis).

## Antes de começar

1. Defina o **nome da pasta** `<X>`: português, sem acento, PascalCase, plural quando fizer sentido (`Montarias`, `Profissoes`). Confirme com a pessoa se houver dúvida.
2. Confira se ela já não existe: `ls client/Assets/_Game/Features server/src/JogoDoGrupo.Server/Features shared/Package/Runtime/Features`.
3. Pegue a **próxima faixa de IDs livre** na tabela "Funcionalidades" do `README.md` (faixas de 1000 em 1000; depois de `14000–14999` vem `15000–15999`). Chame de `<INICIO>`–`<FIM>`.

Use uma funcionalidade existente (ex.: `Itens`) como modelo e copie o formato exato dos arquivos.

## Passos

### 1. Shared

`shared/Package/Runtime/Features/<X>/<X>MessageIds.cs` (C# 9, namespace com chaves):

```csharp
using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.<X>
{
    /// <summary>IDs de mensagem de <X>. Use somente valores entre <INICIO> e <FIM>.</summary>
    public static class <X>MessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(<INICIO>, <FIM>);
    }
}
```

### 2. Server

`server/src/JogoDoGrupo.Server/Features/<X>/<X>Module.cs` (namespace de arquivo). Não registre em nenhum lugar: o `Program.cs` encontra o módulo sozinho.

```csharp
using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.<X>;

public sealed class <X>Module : IFeatureModule
{
    public string Nome => "<X>";

    public void Inicializar(ServerContext contexto) { }
}
```

### 3. Client

Em `client/Assets/_Game/Features/<X>/`:

- Pastas `Art/`, `Prefabs/`, `Scripts/`, `UI/`, cada uma com um `.gitkeep` vazio. Adicione `Scenes/` só se a funcionalidade tiver cenas próprias.
- `JogoDoGrupo.Client.<X>.asmdef`, copiado de outra funcionalidade, trocando `name` para `JogoDoGrupo.Client.<X>` e `rootNamespace` para `JogoDoGrupo.Client.Features.<X>`. As `references` são **só** `JogoDoGrupo.Client.Core` e `JogoDoGrupo.Shared`.
- Não crie `.meta` à mão; o Unity gera ao abrir o projeto.

### 4. Documentação

- `docs/features/<x>.md` (nome do arquivo em minúsculas) a partir de `docs/features/_template.md`, com status `ideia` ou `em definição` e a faixa de IDs preenchida.
- `README.md`: nova linha na tabela "Funcionalidades" (nome, pasta, faixa, descrição curta).
- `docs/responsaveis.md`: nova linha com o responsável em branco.
- `.github/CODEOWNERS`: novo bloco comentado no mesmo formato dos outros (client, server, shared, doc).
- `commitlint.config.mjs`: adicione `"<x>"` (a pasta em minúsculas) na lista `escopos`. Sem isso, commits e branches da funcionalidade são recusados.

### 5. Testes

Crie as pastas de teste da funcionalidade, para quem implementar já saber onde escrever:

- `tests/JogoDoGrupo.Shared.Tests/Features/<X>/` (regras do shared)
- `tests/JogoDoGrupo.Server.Tests/Features/<X>/` (rotas e lógica do servidor)

Cada uma com um primeiro teste simples, por exemplo a faixa de IDs: `Assert.Equal(<INICIO>, <X>MessageIds.Faixa.Inicio)`. Daí em diante, todo PR da funcionalidade traz os testes do que mudou ([docs/convencoes.md](../../../docs/convencoes.md#testes)).

### 6. Conferir

```
dotnet csharpier format .
dotnet test JogoDoGrupo.slnx
npm run unity:compilar
```

O build deve terminar sem avisos. Ao rodar o servidor, o novo módulo aparece como `[ok] <X>`.

Branch e commit: como o escopo novo só existe depois deste PR, use `core` (`feature/core/funcionalidade-<x>` e `feat(core): cria estrutura da funcionalidade <X>`).
