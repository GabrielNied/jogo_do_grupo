// Refaz sobre a main o PR de uma leva empilhada depois que a leva de baixo entrou por squash.
// O squash dá outro hash ao conteúdo da leva de baixo, e o PR de cima, que ainda carrega os
// commits originais, passa a conflitar consigo mesmo. Push forçado é barrado (regra 0), então
// os commits próprios do PR vão por cherry-pick para uma branch `<nome>-segue` a partir de
// origin/main, o PR novo herda título e corpo, o antigo é fechado (a branch dele fica) e os PRs
// que dependiam do antigo passam a citar o novo.
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
  const semLinhas = String(corpo ?? "")
    .replace(/^[^\n]*depende de[^\n]*\n?/im, "")
    .replace(/^Substitui o #\d+[^\n]*\n?/im, "");
  return [aviso, dependencia, semLinhas.trim()].filter(Boolean).join("\n\n") + "\n";
}

/** Troca, na linha "Depende de", o PR antigo pelo novo. Devolve null se o corpo não cita o antigo. */
export function corpoComDependenciaTrocada(corpo, antigo, novo) {
  const texto = String(corpo ?? "");
  if (!dependenciasDoCorpo(texto).includes(antigo)) return null;
  return texto.replace(/^([^\n]*depende de[^\n]*)$/im, (linha) =>
    linha.replace(new RegExp(`#${antigo}(?!\\d)`, "g"), `#${novo}`),
  );
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

/** Refaz o PR sobre a main. Devolve o número do PR novo; lança erro se não der. */
export function reempilhar(numero) {
  const pr = lerPr(numero);
  if (pr.state !== "open") throw new Error(`O PR #${numero} está ${pr.state}.`);
  const deps = dependenciasDoCorpo(pr.body);
  const estados = deps.map((n) => ({ n, aberto: lerPr(n).state === "open" }));
  const fora = estados.filter((d) => !d.aberto).map((d) => d.n);
  const abertas = estados.filter((d) => d.aberto).map((d) => d.n);
  if (!fora.length) throw new Error(`Nenhuma leva de baixo já mergeada ou fechada na linha "Depende de" do #${numero}.`);

  // A ponta de cada leva de baixo marca onde terminam os commits que não são deste PR.
  const refs = fora.map((n) => `refs/reempilhar/${n}`);
  const branch = pr.head.ref;
  git([
    "fetch", "origin",
    "+refs/heads/main:refs/remotes/origin/main",
    `+refs/heads/${branch}:refs/remotes/origin/${branch}`,
    ...fora.map((n, i) => `+pull/${n}/head:${refs[i]}`),
  ]);
  const proprios = git([
    "rev-list", "--reverse", "--no-merges", `origin/${branch}`, "^origin/main", ...refs.map((r) => `^${r}`),
  ]).split("\n").filter(Boolean);
  if (!proprios.length) throw new Error("Nenhum commit próprio sobrou depois de tirar as levas de baixo.");

  const nova = nomeDaBranchSeguinte(branch);
  console.log(`🌿 ${nova} a partir de origin/main com ${proprios.length} commit(s) do #${numero}...`);
  git(["switch", "--no-track", "-c", nova, "origin/main"]);
  try {
    git(["cherry-pick", ...proprios], { stdio: "inherit" });
  } catch {
    throw new Error("O cherry-pick parou num conflito de verdade. Resolva, `git cherry-pick --continue` e rode de novo.");
  } finally {
    for (const ref of refs) git(["update-ref", "-d", ref]);
  }
  git(["push", "-u", "origin", nova], { stdio: "inherit" });

  const arquivo = join(git(["rev-parse", "--git-dir"]), "pr-reempilhado.md");
  writeFileSync(arquivo, corpoReempilhado(pr.body, numero, abertas));
  const novo = api("pulls", {
    metodo: "POST",
    campos: { title: pr.title, head: nova, base: pr.base.ref },
    arquivos: { body: arquivo },
  }).number;
  comentar(numero, `Substituído por #${novo}: o mesmo conteúdo refeito sobre a main.`);
  api(`pulls/${numero}`, { metodo: "PATCH", campos: { state: "closed" } });

  // Quem dependia do antigo passa a depender do novo.
  for (const acima of prsAbertos()) {
    const corpo = corpoComDependenciaTrocada(acima.body, numero, novo);
    if (!corpo) continue;
    writeFileSync(arquivo, corpo);
    api(`pulls/${acima.number}`, { metodo: "PATCH", arquivos: { body: arquivo } });
    console.log(`↪ #${acima.number} agora depende de #${novo}.`);
  }
  console.log(`✔ #${novo} substitui o #${numero} (fechado; a branch dele fica).`);
  return novo;
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
      try { git(["cherry-pick", "--abort"]); } catch { /* não havia cherry-pick em andamento */ }
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
