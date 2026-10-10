// Valida o nome de uma branch: <tipo>/<escopo>/<descricao-em-kebab-case>
// Ex.: feature/itens/fusao-basica, fix/economia/taxa-leilao
// Uso: node scripts/git/validar-branch.mjs <nome-da-branch>
// Roda no hook pre-push (.husky/pre-push) e no CI (.github/workflows/padrao-git.yml).

import { escopos, tipos } from "../../commitlint.config.mjs";

// Na branch, "feat" vira "feature" (mais legível); os outros tipos são iguais aos do commit.
const tiposDeBranch = tipos.map((t) => (t === "feat" ? "feature" : t));

const branch = process.argv[2];

if (!branch) {
  console.error("Uso: node scripts/git/validar-branch.mjs <nome-da-branch>");
  process.exit(2);
}

if (branch === "main") {
  console.error(
    "✖ Não envie direto para a main. Crie uma branch e abra um PR (veja docs/git.md).",
  );
  process.exit(1);
}

const padrao = new RegExp(
  `^(${tiposDeBranch.join("|")})/(${escopos.join("|")})/[a-z0-9]+(-[a-z0-9]+)*$`,
);

if (!padrao.test(branch)) {
  console.error(`✖ Nome de branch fora do padrão: "${branch}"

  Formato:  <tipo>/<escopo>/<descricao-em-kebab-case>
  Exemplo:  feature/itens/fusao-basica

  Tipos:    ${tiposDeBranch.join(", ")}
  Escopos:  ${escopos.join(", ")}
  Descrição: minúsculas, números e hífens, sem acento.

  Para renomear a branch atual: git branch -m <novo-nome>
  Veja docs/git.md#branches`);
  process.exit(1);
}

console.log(`✔ Branch "${branch}" no padrão.`);
