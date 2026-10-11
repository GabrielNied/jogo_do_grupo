# 0014. Conteúdo 3D com assets gratuitos, gerado por script e feito por agentes artistas

**Data:** 2026-10-11
**Status:** aceita

## Contexto

O jogo precisa de personagens, mundo, itens e inimigos em 3D. O grupo decidiu começar só com assets **gratuitos**, no estilo **low-poly estilizado**, e quer agentes especializados por domínio (mundo, personagens, itens...). Os agentes não editam cenas nem prefabs à mão (regra 9), e o editor do Unity não é compartilhável entre agentes em paralelo. O Unity CLI (`unity`, que vem com o Unity Hub) permite rodar o editor em modo batch, executar métodos de editor, recompilar e rodar testes pela linha de comando.

## Decisão

- **Fontes:** assets **CC0** (Quaternius, Kenney, OpenGameArt, Poly Pizza). CC-BY só com o crédito registrado. Nada pago por enquanto.
- **Onde ficam:** pacotes de terceiros em `client/Assets/ThirdParty/<Fonte>/<Pacote>/`, fora de `_Game/`. O que o jogo usa (prefabs, materiais, animadores) fica em `Features/<X>/` e referencia os arquivos de `ThirdParty`.
- **Licenças:** todo asset baixado é registrado em [docs/arte/licencas.md](../arte/licencas.md) (fonte, licença, link, data, quem baixou).
- **Gerado por script:** prefabs, controladores de animação, cenas e o mundo são **gerados por scripts de editor** (pasta `Editor/` da funcionalidade) e executados pelo Unity CLI. O mundo de jogo (hub e dungeons) é montado **por código em tempo de execução** sempre que possível, com a dungeon gerada a partir de uma semente. Editar `.unity`, `.prefab` e `.asset` à mão continua proibido.
- **Agentes artistas** por domínio: `artista-mundo`, `artista-personagens`, `artista-itens` (outros, como mobs e efeitos, quando necessário). Eles buscam os assets conforme a necessidade, organizam, geram os assets por script e escrevem testes do Unity quando houver lógica.
- **Um Unity por vez:** passos que usam o Unity rodam em sequência, coordenados pelo coordenador; busca de assets e código rodam em paralelo.
- **Cota de LFS** (cerca de 1 GB no plano gratuito): baixar só o necessário; pacotes grandes pedem aprovação.

## Consequências

- Os agentes conseguem produzir conteúdo jogável sem uma pessoa montando cenas, e tudo fica reproduzível pelo script.
- Cada worktree de agente que abre o Unity reimporta o projeto (`Library/`): é lento na primeira vez. Se pesar, usar o Unity Accelerator (`--accelerator` do CLI).
- O visual final ainda pede o olho de uma pessoa no editor.
- Assets CC0 podem ser trocados depois por arte própria ou paga, sem mudar o código, porque o jogo referencia os prefabs de `Features/`.
