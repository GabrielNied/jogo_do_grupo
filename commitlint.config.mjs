// Padrão de mensagens de commit (e de títulos de PR, que viram o commit na main).
// Fonte única dos tipos e escopos: o validador de branch (scripts/git/validar-branch.mjs)
// importa as listas daqui. Documentação: docs/git.md.

// Tipos do Conventional Commits.
export const tipos = [
  "feat", // funcionalidade nova
  "fix", // correção de bug
  "docs", // só documentação
  "refactor", // mudança de código sem mudar comportamento
  "perf", // melhoria de desempenho
  "test", // testes
  "build", // build, dependências, pacotes
  "ci", // workflows do GitHub Actions
  "chore", // manutenção que não entra em nenhum dos outros
  "revert", // desfaz um commit anterior
];

// Escopos: "core" e uma entrada por funcionalidade (nome da pasta em minúsculas).
// Funcionalidade nova? Adicione aqui (a skill nova-funcionalidade faz isso).
export const escopos = [
  "core",
  "mundo",
  "npcs",
  "personagem",
  "skills",
  "talentos",
  "combate",
  "itens",
  "profissoes",
  "economia",
  "missoes",
  "mobs",
  "instancias",
  "social",
  "pvp",
];

export default {
  extends: ["@commitlint/config-conventional"],
  rules: {
    "type-enum": [2, "always", tipos],
    "scope-enum": [2, "always", escopos],
    "scope-empty": [2, "never"],
    "header-max-length": [2, "always", 100],
  },
  helpUrl: "https://github.com/GabrielNied/jogo_do_grupo/blob/main/docs/git.md#commits",
};
