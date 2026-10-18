// Refaz sobre a main o PR de uma leva empilhada depois que a leva de baixo entrou por squash.
// O squash dá outro hash ao conteúdo da leva de baixo, e o PR de cima, que ainda carrega os
// commits originais, passa a conflitar consigo mesmo. Os commits próprios do PR vão por
// cherry-pick para cima de origin/main e a branch do PR é atualizada com push forçado
// `--force-with-lease` (exceção à regra 0, ADR 0049): o PR, o review e as aprovações continuam.
//
// As levas de baixo são os PRs citados na linha "Depende de" do corpo, já mergeados ou fechados.
// Só usa a API REST do GitHub (`gh api`): o GraphQL não responde nas sessões na nuvem.
//
// Uso: npm run reempilhar -- <PR>
//      npm run reempilhar -- --depois-do-merge <PR mergeado>   (o que o CI roda sozinho)

import { execFileSync } from "node:child_process";
import { writeFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";

/** Números dos PRs citados na linha "Depende de ..." do corpo. */
export function dependenciasDoCorpo(corpo) {
  const linha = /depende de([^\n]*)/i.exec(corpo ?? "");
  if (!linha) return [];
  return [...linha[1].matchAll(/#(\d+)/g)].map((m) => Number(m[1]));
}

/** Corpo depois de reempilhar: a linha "Depende de" passa a citar só as levas ainda abertas. */
export function corpoReempilhado(corpo, abertas) {
  const texto = String(corpo ?? "");
  const nova = abertas.length ? `Depende de ${abertas.map((n) => `#${n}`).join(" e ")} (mergear antes).` : null;
  if (!/^[^\n]*depende de/im.test(texto)) return texto;
  const resultado = texto.replace(/^[^\n]*depende de[^\n]*(\n\n?|$)/im, nova ? `${nova}$1` : "");
  return resultado.trimStart();
}

/** "dono/repo" do remoto origin (https ou ssh). */
export function repoDaUrl(url) {
  const m = /github\.com[/:]([^/]+\/[^/]+?)(?:\.git)?\/?$/.exec(String(url).trim());
  if (!m) throw new Error(`Remoto origin não é do GitHub: "${url}".`);
  return m[1];
}

export function git(args, opcoes = {}) {
  const saida = execFileSync("git", args, {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "inherit"],
    ...opcoes,
  });
  return typeof saida === "string" ? saida.trim() : "";
}

const repo = () => process.env.GITHUB_REPOSITORY || repoDaUrl(git(["remote", "get-url", "origin"]));

function api(caminho, { metodo = "GET", campos = {}, arquivos = {} } = {}) {
  const args = ["api", "-X", metodo, `repos/${repo()}/${caminho}`];
  for (const [k, v] of Object.entries(campos)) args.push("-f", `${k}=${v}`);
  for (const [k, v] of Object.entries(arquivos)) args.push("-F", `${k}=@${v}`);
  const saida = execFileSync("gh", args, { encoding: "utf8", stdio: ["ignore", "pipe", "inherit"] });
  return saida.trim() ? JSON.parse(saida) : null;
}

const lerPr = (n) => api(`pulls/${n}`);
const comentar = (n, texto) => api(`issues/${n}/comments`, { metodo: "POST", campos: { body: texto } });

function prsAbertos() {
  const todos = [];
  for (let pagina = 1; ; pagina++) {
    const lote = api(`pulls?state=open&per_page=100&page=${pagina}`);
    todos.push(...lote);
    if (lote.length < 100) return todos;
  }
}

/** Refaz o PR sobre a main, na mesma branch. Lança erro se não der. */
export function reempilhar(numero) {
  const pr = lerPr(numero);
  if (pr.state !== "open") throw new Error(`O PR #${numero} está ${pr.state}.`);
  const deps = dependenciasDoCorpo(pr.body);
  const estados = deps.map((n) => ({ n, aberto: lerPr(n).state === "open" }));
  const fora = estados.filter((d) => !d.aberto).map((d) => d.n);
  const abertas = estados.filter((d) => d.aberto).map((d) => d.n);
  if (!fora.length) throw new Error(`Nenhuma leva de baixo já mergeada ou fechada na linha "Depende de" do #${numero}.`);
  if (pr.head.repo?.full_name !== repo()) throw new Error("PR de fork: reempilhe na branch do fork.");

  // A ponta de cada leva de baixo marca onde terminam os commits que não são deste PR.
  const refs = fora.map((n) => `refs/reempilhar/${n}`);
  const branch = pr.head.ref;
  const pontaAntiga = pr.head.sha;
  git([
    "fetch", "origin",
    "+refs/heads/main:refs/remotes/origin/main",
    `+refs/heads/${branch}:refs/remotes/origin/${branch}`,
    ...fora.map((n, i) => `+pull/${n}/head:${refs[i]}`),
  ]);
  const proprios = git([
    "rev-list", "--reverse", "--no-merges", pontaAntiga, "^origin/main", ...refs.map((r) => `^${r}`),
  ]).split("\n").filter(Boolean);
  for (const ref of refs) git(["update-ref", "-d", ref]);
  if (!proprios.length) throw new Error("Nenhum commit próprio sobrou depois de tirar as levas de baixo.");

  console.log(`🌿 ${branch}: ${proprios.length} commit(s) do #${numero} sobre origin/main...`);
  git(["switch", "--detach", "origin/main"]);
  try {
    git(["cherry-pick", ...proprios], { stdio: "inherit" });
  } catch {
    try { git(["cherry-pick", "--abort"]); } catch { /* nada em andamento */ }
    throw new Error(
      `O cherry-pick parou num conflito de verdade. Resolva à mão: \`git switch --detach origin/main\`, ` +
        `\`git cherry-pick ${proprios.join(" ")}\`, e publique com ` +
        `\`git push --force-with-lease=${branch}:${pontaAntiga} origin HEAD:${branch}\`.`,
    );
  }
  // Só sobrescreve se a branch remota ainda estiver onde o script a leu.
  git(["push", `--force-with-lease=refs/heads/${branch}:${pontaAntiga}`, "origin", `HEAD:refs/heads/${branch}`], {
    stdio: "inherit",
  });

  const arquivo = join(git(["rev-parse", "--git-dir"]), "pr-reempilhado.md");
  writeFileSync(arquivo, corpoReempilhado(pr.body, abertas));
  api(`pulls/${numero}`, { metodo: "PATCH", arquivos: { body: arquivo } });
  comentar(
    numero,
    `Reempilhado sobre a main depois do squash de ${fora.map((n) => `#${n}`).join(" e ")}: ` +
      `${proprios.length} commit(s) próprios aplicados, branch atualizada com \`--force-with-lease\` ` +
      `(ponta anterior \`${pontaAntiga.slice(0, 7)}\`, [ADR 0049](docs/decisoes/0049-reempilhar-com-push-forcado.md)).`,
  );
  console.log(`✔ #${numero} reempilhado na mesma branch.`);
}

/** Reempilha todo PR aberto cuja linha "Depende de" cita o PR que acabou de entrar. */
function depoisDoMerge(mergeado) {
  const dependentes = prsAbertos().filter((p) => dependenciasDoCorpo(p.body).includes(mergeado));
  if (!dependentes.length) return console.log(`Nenhum PR aberto depende do #${mergeado}.`);
  let falhou = false;
  for (const p of dependentes) {
    try {
      reempilhar(p.number);
    } catch (erro) {
      falhou = true;
      console.error(`✖ #${p.number}: ${erro.message}`);
      comentar(
        p.number,
        `O #${mergeado} entrou por squash, mas não deu para reempilhar este PR sozinho: ${erro.message}\n\n` +
          `Rode \`npm run reempilhar -- ${p.number}\` localmente ([docs/git.md](docs/git.md#leva-empilhada)).`,
      );
    }
    git(["switch", "--detach", "origin/main"]);
  }
  if (falhou) process.exit(1);
}

function principal() {
  const args = process.argv.slice(2);
  const automatico = args[0] === "--depois-do-merge";
  const numero = Number(automatico ? args[1] : args[0]);
  if (!numero) {
    console.error("Uso: npm run reempilhar -- <PR>  |  --depois-do-merge <PR mergeado>");
    process.exit(1);
  }
  if (git(["status", "--porcelain"])) {
    console.error("✖ Há mudanças não commitadas aqui. Rode num checkout limpo.");
    process.exit(1);
  }
  if (automatico) return depoisDoMerge(numero);
  try {
    reempilhar(numero);
  } catch (erro) {
    console.error(`✖ ${erro.message}`);
    process.exit(1);
  }
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) principal();
