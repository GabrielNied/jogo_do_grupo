// Roda no workflow "Client (Unity)" depois de uma execução verde num push na main. Fecha as
// pendências "pendente-unity" que só pediam compilar e rodar os testes EditMode, quando o PR
// delas já está no commit testado. Assim ninguém com Unity precisa refazer o que o CI fez.
//
// Uso: node scripts/git/fechar-pendencias-do-ci.mjs <sha-testado> <link-da-execucao>
// Precisa de GH_TOKEN com leitura de pull requests (pull-requests: read no workflow) e de
// GITHUB_REPOSITORY (o Actions já define os dois). Sem a permissão, GET /pulls/{n} devolve 403.

import { execFileSync } from "node:child_process";
import { LABEL_PENDENCIA, prDaPendencia, resolvidaPeloCi } from "./pendencia-unity.mjs";

const [sha, link] = process.argv.slice(2);
const repo = process.env.GITHUB_REPOSITORY;
if (!sha || !link || !repo) {
  console.error("Uso: node scripts/git/fechar-pendencias-do-ci.mjs <sha> <link> (com GITHUB_REPOSITORY)");
  process.exit(1);
}

function api(caminho, ...extra) {
  const saida = execFileSync("gh", ["api", caminho, ...extra], { encoding: "utf8" });
  return saida.trim() ? JSON.parse(saida) : null;
}

const pendencias = api(`repos/${repo}/issues?state=open&labels=${LABEL_PENDENCIA}&per_page=100`);
let fechadas = 0;
for (const issue of pendencias) {
  const pr = prDaPendencia(issue.body);
  const emAndamento = issue.labels.some((label) => label.name === "em-andamento");
  if (!pr || emAndamento || !resolvidaPeloCi(issue.body)) continue;

  const dados = api(`repos/${repo}/pulls/${pr}`);
  if (!dados.merged_at || !dados.merge_commit_sha) continue;
  // O merge do PR precisa estar no commit testado (ancestral ou o próprio).
  // Só o status: o compare completo traz o diff e estoura o buffer do Node.
  const status = execFileSync(
    "gh",
    ["api", "--jq", ".status", `repos/${repo}/compare/${dados.merge_commit_sha}...${sha}`],
    { encoding: "utf8" },
  ).trim();
  if (!["ahead", "identical"].includes(status)) continue;

  api(
    `repos/${repo}/issues/${issue.number}/comments`,
    "-X",
    "POST",
    "-f",
    `body=Resolvida pelo CI: o Client (Unity) compilou e passou nos testes EditMode no commit ${sha}, que já inclui o PR #${pr}. ${link}`,
  );
  api(`repos/${repo}/issues/${issue.number}`, "-X", "PATCH", "-f", "state=closed", "-f", "state_reason=completed");
  console.log(`Fechada #${issue.number} (PR #${pr})`);
  fechadas++;
}
console.log(`${fechadas} pendência(s) fechada(s) pelo CI.`);
