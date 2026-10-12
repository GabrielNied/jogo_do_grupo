// Refaz sobre a main o PR de uma leva empilhada depois que a leva de baixo entrou por squash.
// O squash dá outro hash ao conteúdo da leva de baixo, e o PR de cima, que ainda carrega os
// commits originais, passa a conflitar consigo mesmo. Push forçado é barrado (regra 0), então
// os commits próprios do PR vão por cherry-pick para uma branch `<nome>-segue` a partir de
// origin/main, o PR novo herda título e corpo, e o antigo é fechado (a branch dele fica).
//
// As levas de baixo são os PRs citados na linha "Depende de" do corpo, já mergeados ou fechados.
//
// Uso: npm run reempilhar -- <PR>

import { execFileSync } from "node:child_process";
import { writeFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { nomeDaBranchSeguinte } from "./entregar.mjs";

/** Números dos PRs citados na linha "Depende de ..." do corpo. */
export function dependenciasDoCorpo(corpo) {
  const linha = /depende de([^\n]*)/i.exec(corpo ?? "");
  if (!linha) return [];
  return [...linha[1].matchAll(/#(\d+)/g)].map((m) => Number(m[1]));
}

/** Corpo do PR novo: a linha "Depende de" passa a citar só as levas ainda abertas. */
export function corpoReempilhado(corpo, prAntigo, abertas) {
  const aviso = `Substitui o #${prAntigo}, refeito sobre a main depois do squash da leva de baixo.`;
  const dependencia = abertas.length
    ? `Depende de ${abertas.map((n) => `#${n}`).join(" e ")} (mergear antes).`
    : "";
  const semLinha = String(corpo ?? "").replace(/^[^\n]*depende de[^\n]*\n?/im, "");
  return [aviso, dependencia, semLinha.trim()].filter(Boolean).join("\n\n") + "\n";
}

function git(args, opcoes = {}) {
  return execFileSync("git", args, { encoding: "utf8", stdio: ["ignore", "pipe", "inherit"], ...opcoes }).trim();
}

function gh(args) {
  return execFileSync("gh", args, { encoding: "utf8", stdio: ["ignore", "pipe", "inherit"] }).trim();
}

function principal() {
  const numero = Number(process.argv[2]);
  if (!numero) {
    console.error("Uso: npm run reempilhar -- <PR>");
    process.exit(1);
  }
  if (git(["status", "--porcelain"])) {
    console.error("✖ Há mudanças não commitadas aqui. Rode num checkout limpo.");
    process.exit(1);
  }
  const pr = JSON.parse(gh(["pr", "view", String(numero), "--json", "state,title,body,headRefName"]));
  if (pr.state !== "OPEN") {
    console.error(`✖ O PR #${numero} está ${pr.state}.`);
    process.exit(1);
  }
  const deps = dependenciasDoCorpo(pr.body);
  const estados = deps.map((n) => ({ n, state: JSON.parse(gh(["pr", "view", String(n), "--json", "state"])).state }));
  const fora = estados.filter((d) => d.state !== "OPEN").map((d) => d.n);
  const abertas = estados.filter((d) => d.state === "OPEN").map((d) => d.n);
  if (!fora.length) {
    console.error(`✖ Nenhuma leva de baixo já mergeada ou fechada na linha "Depende de" do #${numero}.`);
    process.exit(1);
  }

  // A ponta de cada leva de baixo marca onde terminam os commits que não são deste PR.
  const refs = fora.map((n) => `refs/reempilhar/${n}`);
  git(["fetch", "origin", "main", pr.headRefName, ...fora.map((n, i) => `+pull/${n}/head:${refs[i]}`)]);
  const proprios = git([
    "rev-list", "--reverse", "--no-merges", `origin/${pr.headRefName}`, "^origin/main", ...refs.map((r) => `^${r}`),
  ]).split("\n").filter(Boolean);
  if (!proprios.length) {
    console.error("✖ Nenhum commit próprio sobrou depois de tirar as levas de baixo.");
    process.exit(1);
  }

  const nova = nomeDaBranchSeguinte(pr.headRefName);
  console.log(`🌿 ${nova} a partir de origin/main com ${proprios.length} commit(s) do #${numero}...`);
  git(["switch", "--no-track", "-c", nova, "origin/main"]);
  try {
    git(["cherry-pick", ...proprios], { stdio: "inherit" });
  } catch {
    console.error("✖ O cherry-pick parou num conflito. Resolva, `git cherry-pick --continue` e rode de novo.");
    process.exit(1);
  }
  for (const ref of refs) git(["update-ref", "-d", ref]);
  git(["push", "-u", "origin", nova], { stdio: "inherit" });

  const arquivo = join(git(["rev-parse", "--git-dir"]), "pr-reempilhado.md");
  writeFileSync(arquivo, corpoReempilhado(pr.body, numero, abertas));
  const url = gh(["pr", "create", "--title", pr.title, "--body-file", arquivo, "--head", nova]);
  gh(["pr", "close", String(numero), "--comment", `Substituído por ${url}: o mesmo conteúdo refeito sobre a main.`]);
  console.log(`✔ ${url} (o #${numero} foi fechado; a branch dele fica).`);
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) principal();
