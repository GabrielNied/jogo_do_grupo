# Client (Unity)

## Versão

**Unity 6000.6.5f1**, template Universal 3D (URP). Todo o grupo usa exatamente essa versão (instale pelo Unity Hub, em Installs). A versão fica registrada em `ProjectSettings/ProjectVersion.txt`; trocar de versão é decisão do core, num PR próprio (`chore(core): atualiza Unity para ...`).

## Abrindo o projeto

O passo a passo (instalar a versão certa, abrir pelo Hub, conferir o Console e o pacote Shared, escolher o editor de código) está nos [Primeiros passos do README](../README.md#7-abra-o-cliente-no-unity).

O pacote compartilhado vem referenciado em `Packages/manifest.json` (`"com.jogodogrupo.shared": "file:../../shared/Package"`). Os `.meta` que o Unity gera em `shared/Package/` vão para o git junto.

Configurações que já vêm no repo e **não devem ser alteradas** (Edit > Project Settings > Editor):

- Version Control: **Visible Meta Files**
- Asset Serialization: **Force Text**
- Line Endings For New Scripts: **Unix** (o repo usa LF)

## Como o projeto foi criado

Para referência, caso precise recriar. O Unity Hub não cria projetos em pastas que já têm arquivos, então o projeto foi criado numa pasta temporária (Universal 3D, **sem** controle de versão e sem AI Assistant) e copiado para cá:

- `Packages/` e `ProjectSettings/` inteiros.
- Configurações do URP em `Assets/_Game/Core/Settings/`, ações de input em `Assets/_Game/Core/Input/` e a cena de exemplo em `Assets/_Game/Core/Scenes/` (sempre junto com os `.meta`, que guardam os GUIDs das referências).
- Ficaram de fora o tutorial (`TutorialInfo/`, `Readme.asset`) e o pacote `com.unity.collab-proxy` (Unity Version Control; usamos git).

## Organização

```
Assets/_Game/
├── Core/                     Infra do cliente (dono: core)
│   ├── Input/                Ações do Input System
│   ├── Scenes/               Cenas principais (inicialização, login)
│   ├── Scripts/              Conexão, carregamento de cenas, serviços
│   ├── Settings/             URP: pipeline, renderers, volumes
│   └── UI/                   UI base
└── Features/<X>/
    ├── JogoDoGrupo.Client.<X>.asmdef
    ├── Scripts/
    ├── Prefabs/
    ├── UI/
    └── Art/
```

Cada funcionalidade tem um **Assembly Definition** próprio. Isso deixa a compilação mais rápida e impede dependências escondidas: uma funcionalidade só enxerga o `Core` e o `Shared`, nunca outra funcionalidade diretamente.

Assets da Asset Store e plugins de terceiros ficam fora de `_Game/` (em `Assets/Plugins/` ou na pasta que o pacote criar).

## Cenas

- Cada região do jogo é uma cena em `Features/Mundo/Scenes/Regioes/`, carregada de forma aditiva.
- Cada dungeon e raid é uma cena em `Features/Instancias/Scenes/`.
- Cenas de `Core/Scenes/` (inicialização, login) são do responsável do core.
- Não edite cenas de outras pessoas. Veja o [CONTRIBUTING.md](../CONTRIBUTING.md).
