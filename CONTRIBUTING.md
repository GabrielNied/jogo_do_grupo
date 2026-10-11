# Como contribuir

## Regra de ouro: não mexa na pasta dos outros

Cada funcionalidade tem um responsável (veja [docs/responsaveis.md](docs/responsaveis.md)). Você trabalha nas pastas `Features/<SuaFeature>/` do client, server e shared.

Precisa de algo de outra funcionalidade?
1. Abra uma issue marcando o responsável, **ou**
2. Faça o PR e peça review do responsável (em Reviewers, no PR; o repo é privado no plano gratuito, então o CODEOWNERS não pede sozinho).

Arquivos compartilhados (`Core/`, `ProjectSettings/`, `Packages/manifest.json`, cenas principais) pertencem ao responsável pelo **core**. Mudanças neles sempre passam por review.

## Branches, commits e PRs

A `main` é protegida: ninguém envia direto para ela, nem os admins. Todo trabalho vai por branch e PR. O guia completo está em **[docs/git.md](docs/git.md)**. Resumo:

- **Uma vez por clone:** `npm install` (ativa os hooks que conferem commits e branches).
- **Branch:** `<tipo>/<escopo>/<descricao>` → `feature/itens/fusao-basica`, `fix/economia/taxa-leilao`.
- **Commit:** `<tipo>(<escopo>): <descrição em minúsculas>` → `feat(itens): adiciona limite de fusões por raridade`.
- **PR:** o título segue o mesmo padrão do commit e vira o commit na `main` (squash merge). Um PR = uma funcionalidade.
- **Antes de abrir o PR:** siga a verificação proporcional da skill `preparar-pr`: documentação e configuração não exigem build/testes globais; código C# exige os testes da camada alterada (client: também `npm run unity:compilar`). Detalhes em [docs/convencoes.md](docs/convencoes.md#testes).
- **Testes junto com o código:** todo PR de código traz os testes do que mudou, na mesma branch. PR de código sem teste não entra.
- **Para entrar na main (combinado do grupo):** checks `csharpier`, `build` e `padrao-git` verdes e 1 aprovação de outra pessoa, incluindo o responsável pela pasta. O repo é privado no plano gratuito, então o GitHub não bloqueia: cada um respeita, e o review é pedido à mão.

## Unity: evitando conflitos em cenas e prefabs

Cenas e prefabs são a maior fonte de conflito em projetos Unity.

- **Nunca edite uma cena que não é sua.** Cada região do mapa é uma cena separada, carregada de forma aditiva.
- Prefira **prefabs** a objetos soltos na cena. Duas pessoas mexendo em prefabs diferentes não conflitam.
- Para arquivos binários (PSD, FBX, Blend), use o lock do Git LFS antes de editar:
  ```
  git lfs lock client/Assets/_Game/Features/Mundo/Art/mapa.psd
  git lfs unlock client/Assets/_Game/Features/Mundo/Art/mapa.psd
  ```
- Configure o merge inteligente do Unity para cenas e prefabs (passo 5 dos [Primeiros passos](README.md#5-instale-as-ferramentas-do-projeto-uma-vez-por-clone)).

## Usando IA

Pode usar o assistente que preferir. As instruções do projeto ficam em um lugar só, o [AGENTS.md](AGENTS.md), e cada ferramenta encontra esse arquivo assim:

| Ferramenta | Como ela lê as instruções |
|---|---|
| Codex / ChatGPT, Cursor, Copilot, Gemini CLI, Windsurf, Zed... | Leem o `AGENTS.md` direto |
| Claude Code | Lê o `CLAUDE.md`, que só importa o `AGENTS.md` (`@AGENTS.md`) |
| ChatGPT/Claude no navegador (sem acesso ao repo) | Cole o conteúdo do `AGENTS.md` no começo da conversa |

Procedimentos prontos (skills) ficam em `.claude/skills/<nome>/SKILL.md`, no formato aberto de Agent Skills. Claude Code, Cursor e Copilot carregam essas skills sozinhos. Nas outras ferramentas, o `AGENTS.md` lista as skills e diz qual arquivo abrir para cada tarefa.

Regras:

- **A IA nunca apaga o que as pessoas criaram**: arquivos, docs, ideias, assets, Figma, branches, issues. Ela pode ajustar e melhorar; o que ficar obsoleto é marcado, não removido, e apagar fica sempre com uma pessoa. É a regra 0 do [AGENTS.md](AGENTS.md). Se o seu assistente tentar apagar algo, recuse e avise o core. A exceção são worktrees e branches temporários dos próprios agentes, que saem depois do merge com `npm run limpar-agentes -- --aplicar` ([ADR 0018](docs/decisoes/0018-limpeza-do-que-os-agentes-criam.md)).
- **Não edite o `CLAUDE.md`** nem crie arquivos de instrução específicos de uma ferramenta (`.cursorrules`, `GEMINI.md`, `.github/copilot-instructions.md`...). Regra nova vai no `AGENTS.md`, para valer em todas.
- Skill nova: crie `.claude/skills/<nome>/SKILL.md` (nome em minúsculas com hífen, igual ao da pasta, e `description` dizendo quando usar) e adicione na tabela de procedimentos do `AGENTS.md`.
- Configurações pessoais ficam fora do git (`.claude/settings.local.json`, `CLAUDE.local.md`, `AGENTS.local.md`).
- O código gerado por IA é responsabilidade de quem abre o PR: revise, rode o build e o CSharpier.
- **Vários agentes em paralelo:** cada agente tem uma área fixa e trabalha no seu worktree; as pessoas decidem, montam o que é visual no Unity e fazem o merge. Veja [docs/agentes.md](docs/agentes.md).

## Documentação

Mudou uma regra do jogo? Atualize o `docs/features/<funcionalidade>.md` no mesmo PR.
Tomou uma decisão técnica importante? Crie um ADR em `docs/decisoes/`.
