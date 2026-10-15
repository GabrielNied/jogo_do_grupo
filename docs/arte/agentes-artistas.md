# Agentes artistas

O que vale para `artista-mundo`, `artista-personagens` e `artista-itens` ([ADR 0014](../decisoes/0014-conteudo-3d-e-agentes-artistas.md)). Cada agente tem só o próprio papel em [.claude/agents/](../../.claude/agents/). Estilo: **low-poly estilizado**, medieval; modelos próprios em [padrao-de-modelos.md](padrao-de-modelos.md). Antes de modelar ou buscar asset, siga as [referências visuais](referencias-visuais/instrucoes-agentes.md) (bíblia + imagens locais, sem buscar na web).

## Buscar assets

1. Antes de baixar, procure em `client/Assets/ThirdParty/` e em [licencas.md](licencas.md).
2. Fontes: **Kenney** (kenney.nl), **OpenGameArt** (inclui Quaternius), **Poly Pizza** (API com `POLY_PIZZA_KEY` do `.env`; sem chave, use as outras). Só **CC0**; CC-BY só com o crédito registrado. Nada pago nem da Asset Store.
3. Baixe numa pasta nova em `%TEMP%`, extraia lá e traga **só** modelos (`.fbx`, `.glb`, `.obj`), texturas (`.png`) e áudio; nunca scripts, DLLs ou executáveis. Mais de 50 MB numa tarefa: pare e peça aprovação (cota de LFS).
4. Coloque em `client/Assets/ThirdParty/<Fonte>/<Pacote>/` e registre cada pacote em [licencas.md](licencas.md), no mesmo PR.

## Montar pelo Unity

Siga [Usar o Unity](../agentes.md#usar-o-unity). Para o artista:

- Script de editor em `Features/<X>/Editor/`, rodado com `npm run unity:executar -- <Classe.Metodo>`. Não abra o editor sem pedido ([Usar o Unity](../agentes.md#usar-o-unity)).
- Prefira **script reproduzível** (rodar de novo dá o mesmo resultado) a clique único.
- Lógica pura em C# (gerador, componente) tem teste EditMode em `Features/<X>/Tests/`; o CI roda depois do merge.
- Commite os `.meta`, `.mat` e prefabs que o Unity gerou na sua pasta; `npm run conferir-unity` precisa passar.
- Visual liga a um **identificador** que vem do contrato; regra de jogo (atributo, drop) não é do artista. O servidor decide; o client mostra.

## Trabalhar e relatar

Leia a ficha e `docs/features/<x>.md`, puxe com a branch `feature/<x>/<descricao>-arte` (skill `puxar-tarefa`), monte, verifique (`dotnet csharpier format .` e `unity:compilar`) e abra o PR. O relatório segue [Regras para todos os agentes](../agentes.md#regras-para-todos-os-agentes), com mais duas linhas: **Assets** (pacotes, fonte, licença e tamanho) e **Para conferir no editor** (o que abrir e o que olhar).
