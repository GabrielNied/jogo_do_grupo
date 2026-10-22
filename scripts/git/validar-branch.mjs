// Valida o nome de uma branch: <tipo>/<escopo>/<descricao-em-kebab-case>
// Ex.: feature/itens/fusao-basica, fix/economia/taxa-leilao
// Uso: node scripts/git/validar-branch.mjs <nome-da-branch>
// Roda no hook pre-push (.husky/pre-push), no CI (.github/workflows/padrao-git.yml)
// e no `npm run entregar`, que importa erroDeBranch.

import { pathToFileURL } from "node:url";
import { escopos, tipos } from "../../commitlint.config.mjs";

// Na branch, "feat" vira "feature" (mais legível); os outros tipos são iguais aos do commit.
export const tiposDeBranch = tipos.map((t) => (t === "feat" ? "feature" : t));

const padrao = new RegExp(
  `^(${tiposDeBranch.join("|")})/(${escopos.join("|")})/[a-z0-9]+(-[a-z0-9]+)*$`,
);

// Mensagem do erro, ou null se a branch está no padrão.
export function erroDeBranch(branch) {
  if (branch === "main") {
    return "✖ Não envie direto para a main. Crie uma branch e abra um PR (veja docs/git.md).";
  }
  if (padrao.test(branch)) return null;
  const escopo = String(branch).split("/")[1];
  const dica =
    escopo && !escopos.includes(escopo)
      ? `\n  Escopo "${escopo}" não existe; use core para docs e processo gerais.\n`
      : "";
  return `✖ Nome de branch fora do padrão: "${branch}"
${dica}
  Formato:  <tipo>/<escopo>/<descricao-em-kebab-case>
  Exemplo:  feature/itens/fusao-basica

  Tipos:    ${tiposDeBranch.join(", ")}
  Escopos:  ${escopos.join(", ")}
  Descrição: minúsculas, números e hífens, sem acento.

  Para renomear a branch atual: git branch -m <novo-nome>
  Veja docs/git.md#branches`;
}

function principal() {
  const branch = process.argv[2];
  if (!branch) {
    console.error("Uso: node scripts/git/validar-branch.mjs <nome-da-branch>");
    process.exit(2);
  }
  const erro = erroDeBranch(branch);
  if (erro) {
    console.error(erro);
    process.exit(1);
  }
  console.log(`✔ Branch "${branch}" no padrão.`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) principal();
