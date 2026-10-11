// Remove worktrees e branches locais que já cumpriram o papel (ADR 0018).
// Só apaga o que está garantido na main: nada de mudança não commitada, nada de commit
// que não tenha entrado por PR mesclado. O resto fica, com o motivo.
//
// Uso:
//   npm run limpar-agentes                 # simula: mostra o que sairia e o que fica
//   npm run limpar-agentes -- --aplicar    # remove de verdade
//   npm run limpar-agentes -- --todos      # inclui worktrees fora de .claude/worktrees/
//   npm run limpar-agentes -- --sem-branches  # só worktrees, sem apagar branches soltas
//   npm run limpar-agentes -- --remotas    # também apaga no GitHub branches mescladas que sobraram
//   npm run limpar-agentes -- --horas 0    # ignora a espera mínima (padrão: 2 h parado)
//
// Precisa do gh autenticado para conferir PRs mesclados (squash merge não deixa rastro no git).

import { execFileSync } from "node:child_process";
import { existsSync, statSync } from "node:fs";
import { resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

// ---------- Regras puras (testadas em limpar-agentes.test.mjs) ----------

// Lê a saída de `git worktree list --porcelain`.
export function lerWorktrees(saida) {
  const lista = [];
  let atual = null;
  for (const linha of saida.split(/\r?\n/)) {
    if (linha.startsWith("worktree ")) {
      atual = { caminho: linha.slice(9), head: null, branch: null };
      lista.push(atual);
    } else if (atual && linha.startsWith("HEAD ")) {
      atual.head = linha.slice(5);
    } else if (atual && linha.startsWith("branch ")) {
      atual.branch = linha.slice(7).replace(/^refs\/heads\//, "");
    }
  }
  return lista;
}

// Decide se um worktree ou branch pode sair. Recebe fatos já coletados.
//   sujo: tem mudança não commitada ou arquivo novo fora do .gitignore
//   commitsProprios: commits que não estão na origin/main
//   pr: { numero, mesclado, headContemLocal, extrasNaMain } do PR fechado da branch, ou null
//     extrasNaMain: os arquivos dos commits feitos depois do merge já estão iguais na main
//   erroPr: texto se não deu para consultar o GitHub
//   recente: mexido há menos que a espera mínima (um agente pode estar começando agora)
export function decidir({ sujo = false, commitsProprios = 0, pr = null, erroPr = null, recente = false }) {
  if (recente) return { remover: false, motivo: "usado há pouco (pode ter agente trabalhando)" };
  if (sujo) return { remover: false, motivo: "mudanças não commitadas" };
  if (commitsProprios === 0) return { remover: true, motivo: "sem commits fora da main" };
  if (erroPr) return { remover: false, motivo: `não deu para conferir o PR (${erroPr})` };
  if (!pr) return { remover: false, motivo: `${commitsProprios} commit(s) sem PR mesclado` };
  if (!pr.mesclado) return { remover: false, motivo: `PR #${pr.numero} fechado sem merge` };
  if (!pr.headContemLocal && pr.extrasNaMain)
    return { remover: true, motivo: `PR #${pr.numero} mesclado; o que veio depois já está na main` };
  if (!pr.headContemLocal)
    return {
      remover: false,
      motivo: `commits depois do merge do PR #${pr.numero} (traga com cherry-pick num PR novo)`,
    };
  return { remover: true, motivo: `PR #${pr.numero} mesclado` };
}

export function dentroDe(pasta, caminho) {
  const a = resolve(pasta).toLowerCase() + sep;
  return resolve(caminho).toLowerCase().startsWith(a);
}

// ---------- Execução ----------

function git(args, { cwd, ignorarErro = false } = {}) {
  try {
    return execFileSync("git", args, { cwd, encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] }).trim();
  } catch (erro) {
    if (ignorarErro) return null;
    throw new Error(`git ${args.join(" ")}: ${erro.stderr?.toString().trim() || erro.message}`);
  }
}

function gh(args) {
  return execFileSync("gh", args, { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] }).trim();
}

let donoDoRepo = null;
function prDaBranch(branch, headLocal, cwd) {
  donoDoRepo ??= gh(["api", "repos/{owner}/{repo}", "--jq", ".owner.login"]);
  const json = gh([
    "api",
    `repos/{owner}/{repo}/pulls?state=closed&per_page=10&head=${donoDoRepo}:${branch}`,
  ]);
  const prs = JSON.parse(json);
  const pr = prs.find((p) => p.merged_at) ?? prs[0];
  if (!pr) return null;
  const headPr = pr.head.sha;
  let headContemLocal = headPr === headLocal;
  if (!headContemLocal) {
    // A branch remota some no merge; o GitHub guarda a ponta em refs/pull/N/head.
    if (git(["cat-file", "-e", `${headPr}^{commit}`], { cwd, ignorarErro: true }) === null) {
      git(["fetch", "--quiet", "origin", `refs/pull/${pr.number}/head`], { cwd, ignorarErro: true });
    }
    headContemLocal =
      git(["merge-base", "--is-ancestor", headLocal, headPr], { cwd, ignorarErro: true }) !== null;
  }
  let extrasNaMain = false;
  if (!headContemLocal && git(["cat-file", "-e", `${headPr}^{commit}`], { cwd, ignorarErro: true }) !== null) {
    const base = git(["merge-base", headLocal, headPr], { cwd, ignorarErro: true });
    const arquivos = base ? git(["diff", "--name-only", base, headLocal], { cwd }).split(/\r?\n/).filter(Boolean) : [];
    extrasNaMain =
      arquivos.length > 0 &&
      git(["diff", "--quiet", "origin/main", headLocal, "--", ...arquivos], { cwd, ignorarErro: true }) !== null;
  }
  return { numero: pr.number, mesclado: Boolean(pr.merged_at), headContemLocal, extrasNaMain };
}

// Última atividade: mtime do index (worktree) ou do reflog da branch.
function horasParado(cwd, caminhoGit) {
  const arquivo = git(["rev-parse", "--path-format=absolute", "--git-path", caminhoGit], { cwd, ignorarErro: true });
  if (!arquivo || !existsSync(arquivo)) return Infinity;
  return (Date.now() - statSync(arquivo).mtimeMs) / 3_600_000;
}

function avaliar({ head, branch, cwd, sujo, recente }) {
  if (recente) return decidir({ recente });
  const commitsProprios = Number(git(["rev-list", "--count", `origin/main..${head}`], { cwd }));
  let pr = null;
  let erroPr = null;
  if (!sujo && commitsProprios > 0 && branch) {
    try {
      pr = prDaBranch(branch, head, cwd);
    } catch (erro) {
      erroPr = erro.message.split("\n")[0].slice(0, 80);
    }
  }
  return decidir({ sujo, commitsProprios, pr, erroPr });
}

function principal() {
  const args = process.argv.slice(2);
  const aplicar = args.includes("--aplicar");
  const todos = args.includes("--todos");
  const comBranches = !args.includes("--sem-branches");
  const comRemotas = args.includes("--remotas");
  const iHoras = args.indexOf("--horas");
  const horasMinimas = iHoras >= 0 ? Number(args[iHoras + 1]) : 2;

  const raiz = git(["rev-parse", "--path-format=absolute", "--git-common-dir"]).replace(/[\\/]\.git$/, "");
  const atual = resolve(git(["rev-parse", "--show-toplevel"]));
  const pastaAgentes = resolve(raiz, ".claude", "worktrees");

  git(["fetch", "--quiet", "--prune", "origin"], { cwd: raiz });
  git(["worktree", "prune"], { cwd: raiz });

  const worktrees = lerWorktrees(git(["worktree", "list", "--porcelain"], { cwd: raiz })).slice(1);
  const emUso = new Set([git(["branch", "--show-current"], { cwd: raiz }), "main"]);
  const removidos = [];
  const mantidos = [];

  for (const wt of worktrees) {
    if (resolve(wt.caminho) === atual) {
      if (wt.branch) emUso.add(wt.branch);
      continue;
    }
    if (!todos && !dentroDe(pastaAgentes, wt.caminho)) {
      if (wt.branch) emUso.add(wt.branch);
      continue;
    }
    if (!existsSync(wt.caminho)) continue;
    // Antes do status, que atualiza o index.
    const recente = horasParado(wt.caminho, "index") < horasMinimas;
    const sujo = git(["status", "--porcelain"], { cwd: wt.caminho }) !== "";
    const decisao = avaliar({ head: wt.head, branch: wt.branch, cwd: raiz, sujo, recente });
    const nome = `${wt.caminho} (${wt.branch ?? "sem branch"})`;
    if (!decisao.remover) {
      mantidos.push(`${nome}: ${decisao.motivo}`);
      if (wt.branch) emUso.add(wt.branch);
      continue;
    }
    if (aplicar) {
      const ok = git(["worktree", "remove", wt.caminho], { cwd: raiz, ignorarErro: true }) !== null;
      if (!ok) {
        mantidos.push(`${nome}: git worktree remove falhou (Unity aberto nessa pasta?)`);
        if (wt.branch) emUso.add(wt.branch);
        continue;
      }
    }
    removidos.push(`worktree ${nome}: ${decisao.motivo}`);
    if (wt.branch && !emUso.has(wt.branch)) {
      if (aplicar) git(["branch", "-D", wt.branch], { cwd: raiz, ignorarErro: true });
      removidos.push(`branch ${wt.branch}: era do worktree acima`);
      emUso.add(wt.branch); // já tratada
    }
  }

  if (comBranches) {
    const locais = git(["for-each-ref", "--format=%(refname:short) %(objectname)", "refs/heads"], { cwd: raiz })
      .split(/\r?\n/)
      .filter(Boolean)
      .map((l) => l.split(" "));
    for (const [branch, head] of locais) {
      if (emUso.has(branch)) continue;
      const recente = horasParado(raiz, `logs/refs/heads/${branch}`) < horasMinimas;
      const decisao = avaliar({ head, branch, cwd: raiz, sujo: false, recente });
      if (!decisao.remover) {
        mantidos.push(`branch ${branch}: ${decisao.motivo}`);
        continue;
      }
      if (aplicar) git(["branch", "-D", branch], { cwd: raiz });
      removidos.push(`branch ${branch}: ${decisao.motivo}`);
    }
  }

  if (comRemotas) {
    const remotas = git(
      ["for-each-ref", "--format=%(refname:lstrip=3) %(objectname) %(committerdate:unix)", "refs/remotes/origin"],
      { cwd: raiz },
    )
      .split(/\r?\n/)
      .filter(Boolean)
      .map((l) => l.split(" "));
    for (const [branch, head, data] of remotas) {
      if (branch === "HEAD" || branch === "main") continue;
      const recente = (Date.now() / 1000 - Number(data)) / 3600 < horasMinimas;
      let aberto = false;
      try {
        donoDoRepo ??= gh(["api", "repos/{owner}/{repo}", "--jq", ".owner.login"]);
        aberto = gh(["api", `repos/{owner}/{repo}/pulls?state=open&head=${donoDoRepo}:${branch}`, "--jq", "length"]) !== "0";
      } catch {
        aberto = true; // na dúvida, não apaga
      }
      const decisao = aberto
        ? { remover: false, motivo: "PR aberto (ou GitHub indisponível)" }
        : avaliar({ head, branch, cwd: raiz, sujo: false, recente });
      if (!decisao.remover) {
        mantidos.push(`origin/${branch}: ${decisao.motivo}`);
        continue;
      }
      if (aplicar) git(["push", "--quiet", "origin", "--delete", branch], { cwd: raiz });
      removidos.push(`origin/${branch}: ${decisao.motivo}`);
    }
  }

  const verbo = aplicar ? "Removido" : "Sairia (simulação; use --aplicar)";
  console.log(removidos.length ? `${verbo}:\n  ${removidos.join("\n  ")}` : "Nada para remover.");
  if (mantidos.length) console.log(`\nFica:\n  ${mantidos.join("\n  ")}`);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    principal();
  } catch (erro) {
    console.error(`✖ ${erro.message}`);
    process.exit(1);
  }
}
