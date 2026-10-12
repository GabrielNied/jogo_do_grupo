// Remove worktrees e branches locais que já cumpriram o papel (ADR 0018 e 0031).
// Só apaga o que não se perde: nada de mudança não commitada, e cada commit está na main,
// num PR do GitHub (refs/pull/N/head) ou numa branch do origin. O resto fica, com o motivo.
// Roda sozinho em segundo plano (--fundo) no início da sessão do Claude Code, no git pull
// e no npm run tarefa -- puxar.
//
// Uso:
//   npm run limpar-agentes                 # simula: mostra o que sairia e o que fica
//   npm run limpar-agentes -- --aplicar    # remove de verdade
//   npm run limpar-agentes -- --todos      # inclui worktrees fora de .claude/worktrees/
//   npm run limpar-agentes -- --sem-branches  # só worktrees, sem apagar branches soltas
//   npm run limpar-agentes -- --remotas    # também apaga no GitHub branches mescladas que sobraram
//   npm run limpar-agentes -- --horas 0    # ignora a espera mínima de quem não tem PR (padrão: 2 h)
//   node scripts/git/limpar-agentes.mjs --fundo  # aplica em segundo plano e volta na hora
//
// Precisa do gh autenticado para conferir PRs mesclados (squash merge não deixa rastro no git).

import { execFileSync, spawn } from "node:child_process";
import { closeSync, existsSync, openSync, rmSync, statSync, writeFileSync } from "node:fs";
import { join, resolve, sep } from "node:path";
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
//   pr: { numero, estado: "aberto" | "mesclado" | "fechado", headContemLocal, extrasNaMain }, ou null
//     headContemLocal: a ponta local está no PR (o GitHub guarda em refs/pull/N/head)
//     extrasNaMain: os arquivos dos commits feitos depois do merge já estão iguais na main
//   conteudoNaMain: os arquivos que a branch mudou já estão iguais na main (cherry-pick, squash)
//   naOrigem: a ponta local está numa branch do origin
//   erroPr: texto se não deu para consultar o GitHub
//   recente: commit ou checkout há menos que a espera mínima (pelo reflog, não pelo index,
//     que o VS Code atualiza sozinho). Só protege quem não tem PR fechado: um agente pode estar
//     começando agora.
export function decidir({
  sujo = false,
  commitsProprios = 0,
  pr = null,
  conteudoNaMain = false,
  naOrigem = false,
  erroPr = null,
  recente = false,
}) {
  const fechado = pr && pr.estado !== "aberto";
  if (sujo) return { remover: false, motivo: "mudanças não commitadas" };
  if (pr?.estado === "aberto") return { remover: false, motivo: `PR #${pr.numero} aberto` };
  if (recente && !fechado) return { remover: false, motivo: "usado há pouco, sem PR (pode ter agente trabalhando)" };
  if (commitsProprios === 0) return { remover: true, motivo: "sem commits fora da main" };
  if (pr?.estado === "mesclado" && pr.headContemLocal) return { remover: true, motivo: `PR #${pr.numero} mesclado` };
  if (pr?.estado === "mesclado" && pr.extrasNaMain)
    return { remover: true, motivo: `PR #${pr.numero} mesclado; o que veio depois já está na main` };
  if (pr?.estado === "fechado" && pr.headContemLocal)
    return { remover: true, motivo: `PR #${pr.numero} fechado sem merge; a ponta fica no GitHub (refs/pull/${pr.numero}/head)` };
  if (conteudoNaMain) return { remover: true, motivo: "o conteúdo já está na main" };
  if (naOrigem) return { remover: true, motivo: "a ponta está no GitHub (origin)" };
  if (erroPr) return { remover: false, motivo: `não deu para conferir o PR (${erroPr})` };
  if (pr?.estado === "mesclado")
    return { remover: false, motivo: `commits depois do merge do PR #${pr.numero} (traga com cherry-pick num PR novo)` };
  if (pr?.estado === "fechado")
    return { remover: false, motivo: `commits depois do PR #${pr.numero}, fechado sem merge` };
  return { remover: false, motivo: `${commitsProprios} commit(s) só nesta máquina, sem PR (envie ou abra o PR)` };
}

export function dentroDe(pasta, caminho) {
  const a = resolve(pasta).toLowerCase() + sep;
  return resolve(caminho).toLowerCase().startsWith(a);
}

// ---------- Execução ----------

function git(args, { cwd, ignorarErro = false } = {}) {
  try {
    return execFileSync("git", args, { cwd, encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], windowsHide: true }).trim();
  } catch (erro) {
    if (ignorarErro) return null;
    throw new Error(`git ${args.join(" ")}: ${erro.stderr?.toString().trim() || erro.message}`);
  }
}

function gh(args) {
  return execFileSync("gh", args, { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], windowsHide: true }).trim();
}

let donoDoRepo = null;
function prDaBranch(branch, headLocal, cwd) {
  donoDoRepo ??= gh(["api", "repos/{owner}/{repo}", "--jq", ".owner.login"]);
  const json = gh([
    "api",
    `repos/{owner}/{repo}/pulls?state=all&per_page=10&head=${donoDoRepo}:${branch}`,
  ]);
  const prs = JSON.parse(json);
  const pr = prs.find((p) => p.state === "open") ?? prs.find((p) => p.merged_at) ?? prs[0];
  if (!pr) return null;
  const estado = pr.state === "open" ? "aberto" : pr.merged_at ? "mesclado" : "fechado";
  if (estado === "aberto") return { numero: pr.number, estado };
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
  return { numero: pr.number, estado, headContemLocal, extrasNaMain };
}

// Última atividade: mtime do reflog (commit, checkout, reset). O index não serve: o VS Code o
// atualiza sozinho em todo worktree aberto na pasta.
function horasParado(cwd, caminhoGit) {
  const arquivo = git(["rev-parse", "--path-format=absolute", "--git-path", caminhoGit], { cwd, ignorarErro: true });
  if (!arquivo || !existsSync(arquivo)) return Infinity;
  return (Date.now() - statSync(arquivo).mtimeMs) / 3_600_000;
}

// Os arquivos que a branch mudou desde a base já estão iguais na main.
function conteudoNaMain(head, cwd) {
  const base = git(["merge-base", "origin/main", head], { cwd, ignorarErro: true });
  if (!base) return false;
  const arquivos = git(["diff", "--name-only", base, head], { cwd }).split(/\r?\n/).filter(Boolean);
  if (arquivos.length === 0) return true;
  return git(["diff", "--quiet", "origin/main", head, "--", ...arquivos], { cwd, ignorarErro: true }) !== null;
}

// remota: a branch do origin não conta como cópia de si mesma, e PR fechado sem merge fica.
function avaliar({ head, branch, cwd, sujo, recente, remota = false }) {
  if (sujo) return decidir({ sujo });
  const commitsProprios = Number(git(["rev-list", "--count", `origin/main..${head}`], { cwd }));
  let pr = null;
  let erroPr = null;
  if (branch) {
    try {
      pr = prDaBranch(branch, head, cwd);
    } catch (erro) {
      erroPr = erro.message.split("\n")[0].slice(0, 80);
    }
  }
  if (remota && pr?.estado === "fechado") pr = null;
  if (commitsProprios === 0 || pr?.estado === "aberto") return decidir({ commitsProprios, pr, erroPr, recente });
  const naOrigem =
    !remota &&
    git(["for-each-ref", "--contains", head, "--count=1", "refs/remotes/origin"], { cwd }) !== "";
  return decidir({ commitsProprios, pr, erroPr, recente, naOrigem, conteudoNaMain: conteudoNaMain(head, cwd) });
}

function principal() {
  const args = process.argv.slice(2);
  const aplicar = args.includes("--aplicar");
  const todos = args.includes("--todos");
  const comBranches = !args.includes("--sem-branches");
  const comRemotas = args.includes("--remotas");
  const silencioso = args.includes("--silencioso");
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
    const recente = horasParado(wt.caminho, "logs/HEAD") < horasMinimas;
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
        : avaliar({ head, branch, cwd: raiz, sujo: false, recente, remota: true });
      if (!decisao.remover) {
        mantidos.push(`origin/${branch}: ${decisao.motivo}`);
        continue;
      }
      if (aplicar) git(["push", "--quiet", "origin", "--delete", branch], { cwd: raiz });
      removidos.push(`origin/${branch}: ${decisao.motivo}`);
    }
  }

  const verbo = aplicar ? "Removido" : "Sairia (simulação; use --aplicar)";
  const relatorio = [removidos.length ? `${verbo}:\n  ${removidos.join("\n  ")}` : "Nada para remover."];
  if (mantidos.length) relatorio.push(`\nFica:\n  ${mantidos.join("\n  ")}`);
  if (!silencioso) console.log(relatorio.join("\n"));
  // O último relatório fica em .git/limpar-agentes.log, para conferir o que a rodada automática fez.
  writeFileSync(join(raiz, ".git", "limpar-agentes.log"), `${new Date().toISOString()}\n${relatorio.join("\n")}\n`);
}

// Trava de uma rodada por vez em .git/; trava com mais de 10 min é de rodada que morreu.
function caminhoDaTrava() {
  return join(git(["rev-parse", "--path-format=absolute", "--git-common-dir"]), "limpar-agentes.trava");
}

function travaViva(trava) {
  return existsSync(trava) && Date.now() - statSync(trava).mtimeMs < 10 * 60_000;
}

// --fundo: sobe uma cópia desligada com --aplicar --silencioso e volta na hora, para os
// gatilhos automáticos (início da sessão, git pull, tarefa puxar) não travarem ninguém.
function emFundo() {
  if (travaViva(caminhoDaTrava())) return;
  spawn(process.execPath, [fileURLToPath(import.meta.url), "--aplicar", "--silencioso", "--com-trava"], {
    detached: true,
    stdio: "ignore",
    windowsHide: true,
  }).unref();
}

function comTrava(fazer) {
  const trava = caminhoDaTrava();
  if (travaViva(trava)) return;
  closeSync(openSync(trava, "w"));
  try {
    fazer();
  } finally {
    rmSync(trava, { force: true });
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    if (process.argv.includes("--fundo")) emFundo();
    else if (process.argv.includes("--com-trava")) comTrava(principal);
    else principal();
  } catch (erro) {
    console.error(`✖ ${erro.message}`);
    process.exit(1);
  }
}
