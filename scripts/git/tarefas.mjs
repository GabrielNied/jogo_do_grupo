// Gerenciador de tarefas (issues e project) do Jogo do Grupo.
// Garante fluxo seguro para múltiplos desenvolvedores e agentes em paralelo:
// quando alguém puxa uma tarefa, ela entra em "em andamento" (com label e project status)
// e impede que outra pessoa ou agente trabalhe na mesma tarefa ao mesmo tempo.
//
// Uso:
//   npm run tarefa -- listar
//   npm run tarefa -- puxar <numero> [nome-da-branch] [--agente <nome>] [--worktree <caminho>] [--base <branch>]
//   npm run tarefa -- liberar <numero> [--motivo "<motivo>"]
//   npm run tarefa -- soltar-travas
//   npm run tarefa -- status <numero>
//   npm run tarefa -- relevancia [numero...] [--conferida "<nota>"]
//   npm run tarefa -- concluir <numero>
//   npm run tarefa -- pendencia-unity <pr> --falta "<o que faltou>" [--falta "..."]

import { execFileSync, spawnSync } from "node:child_process";
import { escopos, tipos } from "../../commitlint.config.mjs";
import { existsSync, readdirSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { LIMITE_FUNCIONALIDADES, podePuxarFuncionalidade } from "./limite-funcionalidades.mjs";
import { compararDisponiveis, textoDaPrioridade } from "./prioridade-lista.mjs";
import { motivoTravaAbandonada, travaAbandonada, ultimaTrava } from "./trava-abandonada.mjs";
import { corpoSubtarefa } from "./subtarefa.mjs";
import { baseDaConferencia, comentarioConferida, docsDaIssue } from "./relevancia.mjs";
import { sugerirNomeBranch } from "./nome-branch.mjs";
import { ligarNodeModules } from "./worktree-npm.mjs";
import {
  LABEL_PENDENCIA,
  branchDaPendencia,
  corpoDoPrComPendencia,
  corpoPendencia,
  prDaPendencia,
  tituloPendencia,
} from "./pendencia-unity.mjs";

const PROJECT_OWNER = "GabrielNied";
const PROJECT_NUMBER = "1";
const PROJECT_ID = "PVT_kwHOASeGls4BmgwX";
const STATUS_FIELD_ID = "PVTSSF_lAHOASeGls4BmgwXzhlJfmU";
const STATUS_OPTIONS = {
  Todo: "f75ad846",
  "In Progress": "47fc9ee4",
  Done: "98236657",
};

const tiposDeBranch = tipos.map((t) => (t === "feat" ? "feature" : t));
const padraoBranch = new RegExp(
  `^(${tiposDeBranch.join("|")})/(${escopos.join("|")})/[a-z0-9]+(-[a-z0-9]+)*$`,
);

function executarGh(args, opcoes = {}) {
  try {
    const res = execFileSync("gh", args, {
      encoding: "utf8",
      stdio: ["pipe", "pipe", "pipe"],
      ...opcoes,
    });
    return res.trim();
  } catch (erro) {
    if (opcoes.ignorarErro) {
      return null;
    }
    const msg = erro.stderr ? erro.stderr.toString().trim() : erro.message;
    throw new Error(`Falha ao executar 'gh ${args.join(" ")}':\n${msg}`);
  }
}

// Issues pela API REST (`gh api`), não por `gh issue`: o `gh issue` usa GraphQL, que é
// bloqueado em sessões de IA na nuvem. O REST funciona igual na máquina local.
// `{owner}/{repo}` é preenchido pelo gh a partir do remote do repositório atual.
const REPO = "repos/{owner}/{repo}";

function apiGh(caminho, { metodo = "GET", campos = {}, ignorarErro = false } = {}) {
  const args = ["api", "-X", metodo, caminho];
  for (const [chave, valor] of Object.entries(campos)) {
    for (const v of Array.isArray(valor) ? valor : [valor]) {
      args.push("-f", `${chave}${Array.isArray(valor) ? "[]" : ""}=${v}`);
    }
  }
  const res = executarGh(args, { ignorarErro });
  return res ? JSON.parse(res) : null;
}

// Converte a resposta REST para o formato que o resto do script já usava (o do `gh issue --json`).
function normalizarIssue(bruta) {
  return {
    number: bruta.number,
    title: bruta.title,
    state: (bruta.state || "").toUpperCase(),
    labels: (bruta.labels || []).map((l) => ({ name: typeof l === "string" ? l : l.name })),
    assignees: (bruta.assignees || []).map((a) => ({ login: a.login })),
    url: bruta.html_url,
    body: bruta.body || "",
    criadaEm: bruta.created_at,
    totalComentarios: bruta.comments || 0,
  };
}

function obterIssue(numero, { comComentarios = false } = {}) {
  const issue = normalizarIssue(apiGh(`${REPO}/issues/${numero}`));
  if (comComentarios) issue.comments = comentariosDa(numero);
  return issue;
}

function comentariosDa(numero) {
  const comentarios = apiGh(`${REPO}/issues/${numero}/comments?per_page=100`) || [];
  return comentarios.map((c) => ({ author: { login: c.user.login }, body: c.body, createdAt: c.created_at }));
}

// Commits da origin/main que mexeram nas docs da issue depois que ela foi escrita ou conferida.
function mudancasNasDocs(issue) {
  const docs = docsDaIssue(nomesDasLabels(issue), issue.body, (doc) => existsSync(doc));
  if (docs.length === 0) return { docs, desde: issue.criadaEm, commits: [] };
  const comentarios = issue.comments || (issue.totalComentarios > 0 ? comentariosDa(issue.number) : []);
  const desde = baseDaConferencia(issue.criadaEm, comentarios);
  const log = executarGit(["log", "origin/main", `--since=${desde}`, "--format=%h %as %s", "--", ...docs], {
    silencioso: true,
  });
  return { docs, desde, commits: log ? log.split("\n") : [] };
}

const COMMITS_NO_AVISO = 5;
const LIMITE_VARREDURA = 10;

function avisarRelevancia(issue, mudancas) {
  const { docs, desde, commits } = mudancas;
  console.log(`\n⚠ REVISAR RELEVÂNCIA da #${issue.number} ("${issue.title}")`);
  console.log(`  ${commits.length} commit(s) mudaram a regra que ela segue depois de ${desde.slice(0, 10)}:`);
  for (const commit of commits.slice(0, COMMITS_NO_AVISO)) console.log(`    ${commit}`);
  if (commits.length > COMMITS_NO_AVISO) console.log(`    ... e mais ${commits.length - COMMITS_NO_AVISO}`);
  // Ler só o que mudou sai mais barato que reler a doc inteira contra a issue.
  const antes = executarGit(["rev-list", "-1", `--before=${desde}`, "origin/main"], { silencioso: true });
  console.log(`  O que mudou: git diff ${antes ? antes.slice(0, 7) : "<base>"} origin/main -- ${docs.join(" ")}`);
  console.log(`  Compare antes de implementar (skill puxar-tarefa, "Conferir a relevância").`);
}

function listarIssuesAbertas() {
  const issues = [];
  for (let pagina = 1; ; pagina++) {
    const lote = apiGh(`${REPO}/issues?state=open&per_page=100&page=${pagina}`) || [];
    // A rota de issues também devolve PRs; eles não são tarefas.
    issues.push(...lote.filter((i) => !i.pull_request).map(normalizarIssue));
    if (lote.length < 100) return issues;
  }
}

function adicionarLabel(numero, label) {
  apiGh(`${REPO}/issues/${numero}/labels`, { metodo: "POST", campos: { labels: [label] } });
}

function removerLabel(numero, label) {
  apiGh(`${REPO}/issues/${numero}/labels/${encodeURIComponent(label)}`, {
    metodo: "DELETE",
    ignorarErro: true,
  });
}

function atribuir(numero, login) {
  apiGh(`${REPO}/issues/${numero}/assignees`, {
    metodo: "POST",
    campos: { assignees: [login] },
    ignorarErro: true,
  });
}

function desatribuir(numero, login) {
  apiGh(`${REPO}/issues/${numero}/assignees`, {
    metodo: "DELETE",
    campos: { assignees: [login] },
    ignorarErro: true,
  });
}

function comentar(numero, corpo) {
  apiGh(`${REPO}/issues/${numero}/comments`, {
    metodo: "POST",
    campos: { body: corpo },
    ignorarErro: true,
  });
}

function executarGit(args, opcoes = {}) {
  const res = spawnSync("git", args, {
    encoding: "utf8",
    stdio: opcoes.silencioso ? ["pipe", "pipe", "pipe"] : "inherit",
    ...opcoes,
  });
  if (res.status !== 0) {
    throw new Error(`Falha no git ${args.join(" ")}`);
  }
  return res.stdout ? res.stdout.trim() : "";
}

function obterUsuarioAtual() {
  try {
    const res = executarGh(["api", "user", "--jq", ".login"], { ignorarErro: true });
    return res || "desenvolvedor";
  } catch {
    return "desenvolvedor";
  }
}

function buscarItensDoProjeto() {
  try {
    const raw = executarGh(
      ["project", "item-list", PROJECT_NUMBER, "--owner", PROJECT_OWNER, "--format", "json"],
      { ignorarErro: true },
    );
    if (!raw) return [];
    const dados = JSON.parse(raw);
    return dados.items || [];
  } catch {
    return [];
  }
}

function sincronizarStatusProjeto(numeroIssue, urlIssue, novoStatusNome) {
  const optionId = STATUS_OPTIONS[novoStatusNome];
  if (!optionId) return;

  try {
    const itens = buscarItensDoProjeto();
    let item = itens.find((i) => i.content && i.content.number === numeroIssue);

    if (!item && urlIssue) {
      const addRes = executarGh(
        ["project", "item-add", PROJECT_NUMBER, "--owner", PROJECT_OWNER, "--url", urlIssue, "--format", "json"],
        { ignorarErro: true },
      );
      if (addRes) {
        const itemCriado = JSON.parse(addRes);
        item = itemCriado;
      }
    }

    if (item && item.id) {
      executarGh(
        [
          "project",
          "item-edit",
          "--id",
          item.id,
          "--field-id",
          STATUS_FIELD_ID,
          "--single-select-option-id",
          optionId,
          "--project-id",
          PROJECT_ID,
        ],
        { ignorarErro: true },
      );
    }
  } catch {
    // Integração com projeto é best-effort; a label na issue é autoritativa
  }
}

// -------------------------------------------------------------
// Comandos
// -------------------------------------------------------------

function nomesDasLabels(issue) {
  return (issue.labels || []).map((l) => (typeof l === "string" ? l : l.name));
}

function branchesNoOrigin(nomes) {
  const unicos = [...new Set(nomes)];
  if (unicos.length === 0) return new Set();
  const saida = executarGit(["ls-remote", "--heads", "origin", ...unicos], { silencioso: true });
  const presentes = new Set();
  for (const linha of saida.split(/\r?\n/)) {
    const ref = linha.split("\t")[1];
    if (ref?.startsWith("refs/heads/")) presentes.add(ref.slice("refs/heads/".length));
  }
  return presentes;
}

function branchesComPrAberto() {
  const raw = executarGh(["pr", "list", "--state", "open", "--limit", "100", "--json", "headRefName"], {
    ignorarErro: true,
  });
  if (!raw) return new Set();
  return new Set(JSON.parse(raw).map((pr) => pr.headRefName));
}

function soltarIssue(numero, motivo) {
  const issue = obterIssue(numero);
  removerLabel(numero, "em-andamento");
  for (const assignee of issue.assignees || []) {
    desatribuir(numero, assignee.login);
  }
  sincronizarStatusProjeto(numero, issue.url, "Todo");
  const autor = obterUsuarioAtual();
  comentar(
    numero,
    `🔓 **Tarefa liberada**\n\nA tarefa foi liberada por @${autor} e está disponível para outros desenvolvedores ou agentes.\n**Motivo:** ${motivo}`,
  );
}

function soltarTravasAbandonadas() {
  try {
    return soltarTravasAbandonadasAgora();
  } catch (erro) {
    console.error(`ℹ Não soltei travas abandonadas: ${erro.message.split("\n")[0]}`);
    return [];
  }
}

function soltarTravasAbandonadasAgora() {
  const candidatas = listarIssuesAbertas().filter((issue) => nomesDasLabels(issue).includes("em-andamento"));
  const agoraMs = Date.now();
  const comTrava = [];
  for (const issue of candidatas) {
    const completa = obterIssue(issue.number, { comComentarios: true });
    const trava = ultimaTrava(completa.comments || []);
    if (trava) comTrava.push({ numero: issue.number, trava });
  }

  const noOrigin = branchesNoOrigin(comTrava.map((item) => item.trava.branch));
  const prAberto = branchesComPrAberto();
  const motivo = motivoTravaAbandonada();
  const liberadas = [];
  for (const { numero, trava } of comTrava) {
    if (
      !travaAbandonada({
        trava,
        agoraMs,
        branchNoOrigin: noOrigin.has(trava.branch),
        prAberto: prAberto.has(trava.branch),
      })
    ) {
      continue;
    }
    soltarIssue(numero, motivo);
    liberadas.push(numero);
  }
  return liberadas;
}

async function listarTarefas() {
  console.log("🔍 Consultando tarefas abertas no GitHub...");
  const liberadas = soltarTravasAbandonadas();
  if (liberadas.length > 0) {
    console.log(`🔓 Trava abandonada liberada: ${liberadas.map((numero) => `#${numero}`).join(", ")}`);
  }
  const issues = listarIssuesAbertas();
  const itensProjeto = buscarItensDoProjeto();

  const emAndamento = [];
  const disponiveis = [];
  const bloqueadas = [];
  const pendenciasUnity = [];

  for (const issue of issues) {
    const labels = (issue.labels || []).map((l) => (typeof l === "string" ? l : l.name));
    const temLabelAndamento = labels.includes("em-andamento");
    const temLabelBloqueada = labels.includes("bloqueada");

    const itemProj = itensProjeto.find((i) => i.content && i.content.number === issue.number);
    const statusProj = itemProj ? itemProj.status : null;
    const estaEmAndamento = temLabelAndamento || statusProj === "In Progress" || (issue.assignees && issue.assignees.length > 0);

    if (temLabelBloqueada) {
      bloqueadas.push({ issue, labels, itemProj });
    } else if (estaEmAndamento) {
      emAndamento.push({ issue, labels, itemProj });
    } else if (labels.includes(LABEL_PENDENCIA)) {
      pendenciasUnity.push({ issue, labels, itemProj });
    } else {
      disponiveis.push({ issue, labels, itemProj });
    }
  }

  console.log("\n=======================================================");
  console.log("📋 QUADRO DE TAREFAS - JOGO DO GRUPO");
  console.log("=======================================================\n");

  console.log(`🚀 EM ANDAMENTO (${emAndamento.length}):`);
  if (emAndamento.length === 0) {
    console.log("   (Nenhuma tarefa em andamento no momento)");
  } else {
    for (const { issue, labels } of emAndamento) {
      const responsaveis = (issue.assignees || []).map((a) => a.login).join(", ") || "agente / não atribuído";
      const agentesTags = labels.filter((l) => l.startsWith("agente:")).join(", ") || "";
      console.log(`   #${issue.number} - ${issue.title}`);
      console.log(`     Responsável: ${responsaveis}${agentesTags ? ` | Papel: ${agentesTags}` : ""}`);
    }
  }

  console.log(`\n✨ DISPONÍVEIS PARA PUXAR (${disponiveis.length}):`);
  if (disponiveis.length === 0) {
    console.log("   (Nenhuma tarefa livre no momento)");
  } else {
    disponiveis.sort(compararDisponiveis);
    for (const { issue, labels } of disponiveis) {
      const agentesTags = labels.filter((l) => l.startsWith("agente:")).join(", ") || "geral";
      console.log(`   #${issue.number} - ${issue.title}`);
      console.log(`     ${textoDaPrioridade(labels)} | Recomendado para: [${agentesTags}]`);
    }
  }

  if (pendenciasUnity.length > 0) {
    console.log(`\n🧩 PENDÊNCIAS DE UNITY (${pendenciasUnity.length}) - só para quem tem Unity na máquina:`);
    for (const { issue } of pendenciasUnity) {
      console.log(`   #${issue.number} - ${issue.title}`);
    }
  }

  if (bloqueadas.length > 0) {
    console.log(`\n⛔ BLOQUEADAS (${bloqueadas.length}):`);
    for (const { issue } of bloqueadas) {
      console.log(`   #${issue.number} - ${issue.title}`);
    }
  }

  console.log("\n-------------------------------------------------------");
  console.log("Para assumir uma tarefa:");
  console.log("  npm run tarefa -- puxar <numero> [nome-da-branch] [--agente <nome>]");
  console.log("-------------------------------------------------------\n");
}

async function puxarTarefa(args) {
  const numero = parseInt(args[0], 10);
  if (isNaN(numero)) {
    console.error("✖ Informe o número da tarefa. Exemplo: npm run tarefa -- puxar 36");
    process.exit(1);
  }

  let nomeBranch = null;
  let nomeAgente = null;
  let worktreePath = null;
  // Branch de base de uma leva empilhada (ainda sem merge); o padrão é a main.
  let base = "main";

  for (let i = 1; i < args.length; i++) {
    if (args[i] === "--agente" && args[i + 1]) {
      nomeAgente = args[i + 1];
      i++;
    } else if (args[i] === "--worktree" && args[i + 1]) {
      worktreePath = args[i + 1];
      i++;
    } else if (args[i] === "--base" && args[i + 1]) {
      base = args[i + 1].replace(/^origin\//, "");
      i++;
    } else if (!args[i].startsWith("--") && !nomeBranch) {
      nomeBranch = args[i];
    }
  }

  console.log(`🔍 Verificando tarefa #${numero}...`);
  const issue = obterIssue(numero);
  if (issue.state !== "OPEN") {
    console.error(`✖ A tarefa #${numero} está com estado "${issue.state}". Só é possível puxar tarefas abertas.`);
    process.exit(1);
  }

  const labels = (issue.labels || []).map((l) => (typeof l === "string" ? l : l.name));
  const temLabelAndamento = labels.includes("em-andamento");

  const itensProjeto = buscarItensDoProjeto();
  const itemProj = itensProjeto.find((i) => i.content && i.content.number === issue.number);
  const statusProj = itemProj ? itemProj.status : null;

  // Verificação de concorrência
  if (temLabelAndamento || statusProj === "In Progress") {
    const responsaveis = (issue.assignees || []).map((a) => a.login).join(", ") || "outro desenvolvedor/agente";
    console.error(`\n⛔ BLOQUEADO: A tarefa #${numero} ("${issue.title}") já está EM ANDAMENTO por: ${responsaveis}!`);
    console.error("Para evitar retrabalho e conflitos, múltiplos desenvolvedores ou agentes não podem atuar na mesma tarefa.");
    console.error(`Se a tarefa foi abandonada ou transferida, peça para o responsável liberar com:`);
    console.error(`  npm run tarefa -- liberar ${numero}\n`);
    process.exit(1);
  }

  if (labels.includes("bloqueada")) {
    console.error(`\n⛔ ATENÇÃO: A tarefa #${numero} está marcada como BLOQUEADA por dependência ou decisão técnica.`);
    console.error("Resolva os impedimentos antes de iniciar o desenvolvimento.\n");
    process.exit(1);
  }

  soltarTravasAbandonadas();
  const emAndamento = listarIssuesAbertas()
    .filter((aberta) => aberta.number !== numero && nomesDasLabels(aberta).includes("em-andamento"))
    .map((aberta) => nomesDasLabels(aberta));
  const vaga = podePuxarFuncionalidade(labels, emAndamento);
  if (!vaga.ok) {
    console.error(
      `\n⛔ O marco já tem ${LIMITE_FUNCIONALIDADES} funcionalidades de jogo em andamento: ${vaga.emCurso.join(", ")}.`,
    );
    console.error(`A #${numero} abriria mais uma (${vaga.novas.join(", ")}). Espere uma fechar.\n`);
    process.exit(1);
  }

  // Tarefa antiga pode pedir regra que a doc já mudou: só avisa, quem decide é o agente.
  let docMudou = false;
  if (!labels.includes(LABEL_PENDENCIA)) {
    executarGit(["fetch", "origin", "main"], { silencioso: true });
    const mudancas = mudancasNasDocs(issue);
    docMudou = mudancas.commits.length > 0;
    if (docMudou) avisarRelevancia(issue, mudancas);
  }

  // Pendência de Unity: com o PR ainda aberto, trabalha na branch dele em vez de criar outra.
  // Com o PR já fechado, segue o fluxo normal numa branch nova a partir da main.
  let branchExistente = null;
  if (labels.includes(LABEL_PENDENCIA)) {
    const pr = prDaPendencia(issue.body);
    const dadosPr = pr ? apiGh(`${REPO}/pulls/${pr}`, { ignorarErro: true }) : null;
    if (dadosPr && dadosPr.state === "open") {
      branchExistente = dadosPr.head.ref;
      nomeBranch = branchExistente;
      console.log(`ℹ Pendência de Unity do PR #${pr}, ainda aberto: usando a branch dele (${nomeBranch}).`);
    } else if (!nomeBranch) {
      const escopoDoPr = (branchDaPendencia(issue.body) || "").split("/")[1];
      nomeBranch = `chore/${escopos.includes(escopoDoPr) ? escopoDoPr : "core"}/unity-pendencia-${numero}`;
      console.log(`ℹ O PR #${pr} já foi fechado: a pendência vai numa branch nova a partir da main.`);
    }
  }

  // Nome da branch
  if (!nomeBranch) {
    nomeBranch = sugerirNomeBranch(issue);
    console.log(`ℹ Nome de branch sugerido automaticamente: ${nomeBranch}`);
  }

  if (!padraoBranch.test(nomeBranch)) {
    console.error(`✖ Nome de branch inválido: "${nomeBranch}"`);
    console.error("  Formato: <tipo>/<escopo>/<descricao-em-kebab-case>");
    console.error(`  Tipos válidos: ${tiposDeBranch.join(", ")}`);
    console.error(`  Escopos válidos: ${escopos.join(", ")}`);
    process.exit(1);
  }

  const usuario = nomeAgente ? `agente ${nomeAgente}` : `@${obterUsuarioAtual()}`;
  console.log(`\n🔒 Reivindicando tarefa #${numero} para ${usuario}...`);

  // 1. Marca na issue como em-andamento
  adicionarLabel(numero, "em-andamento");

  // 2. Atribui no GitHub se for pessoa
  if (!nomeAgente) {
    atribuir(numero, obterUsuarioAtual());
  }

  // 3. Move no Project para In Progress
  sincronizarStatusProjeto(numero, issue.url, "In Progress");

  // 4. Comenta na issue
  const comentario = `🚀 **Tarefa em andamento**\n\n- **Responsável:** ${usuario}\n- **Branch:** \`${nomeBranch}\`\n- **Início:** ${new Date().toISOString()}\n\n*Esta tarefa foi puxada e está bloqueada para outros agentes/desenvolvedores até ser concluída ou liberada.*`;
  comentar(numero, comentario);

  // 5. Configura Git (Worktree ou Branch local)
  // Agente que já nasceu num worktree e passa o próprio caminho: cria a branch ali mesmo
  // (o git worktree add falharia depois de a issue já estar marcada).
  const raizAtual = executarGit(["rev-parse", "--show-toplevel"], { silencioso: true });
  if (worktreePath && resolve(worktreePath).toLowerCase() === resolve(raizAtual).toLowerCase()) worktreePath = null;
  console.log(`🌿 Configurando branch "${nomeBranch}"...`);
  if (branchExistente) {
    // Branch do PR, que já existe no origin: o git cria a local acompanhando a remota.
    executarGit(["fetch", "origin", branchExistente]);
    if (worktreePath) {
      executarGit(["worktree", "add", worktreePath, branchExistente]);
      console.log(`✔ Worktree criado em: ${worktreePath}`);
    } else {
      executarGit(["switch", branchExistente]);
      executarGit(["pull", "--ff-only", "origin", branchExistente]);
    }
  } else if (worktreePath) {
    executarGit(["fetch", "origin", base]);
    executarGit(["worktree", "add", worktreePath, "-b", nomeBranch, `origin/${base}`]);
    console.log(`✔ Worktree criado em: ${worktreePath}`);
  } else {
    // Branch local no repositório atual, sempre da origin (main ou --base): a main local pode
    // estar velha ou presa (aberta em outro worktree, mudança não commitada barrando o pull).
    executarGit(["fetch", "origin", base]);
    executarGit(["switch", "--no-track", "-c", nomeBranch, `origin/${base}`]);
  }
  // Worktree novo nasce sem node_modules: sem isto o commitlint do hook recusa o 1º commit.
  if (worktreePath && ligarNodeModules(raizAtual, worktreePath)) {
    console.log("✔ node_modules do checkout principal ligado no worktree (commitlint e hooks).");
  }

  // Começo de tarefa é um bom momento para limpar o que já entrou na main (ADR 0031).
  // Em segundo plano: não atrasa o puxar, e erro na limpeza não afeta a tarefa.
  spawnSync(process.execPath, [fileURLToPath(new URL("./limpar-agentes.mjs", import.meta.url)), "--fundo"], {
    stdio: "ignore",
    windowsHide: true,
  });

  console.log(`\n=======================================================`);
  console.log(`✔ TAREFA #${numero} PUXADA COM SUCESSO!`);
  console.log(`=======================================================`);
  console.log(`Tarefa:     #${numero} - ${issue.title}`);
  console.log(`Atribuída:  ${usuario}`);
  console.log(`Branch:     ${nomeBranch}`);
  console.log(`Status:     Em andamento (bloqueada contra concorrência)`);
  console.log(`\nPróximos passos:`);
  if (docMudou) {
    console.log(`0. A doc mudou depois da issue (aviso acima): confira a relevância antes de codar.`);
  }
  console.log(`1. Desenvolva as alterações e escreva os testes necessários.`);
  console.log(`2. Formate e confira com: npm run preparar-pr (ou verificação proporcional).`);
  if (branchExistente) {
    console.log(`3. Dê push na mesma branch e comente no PR o que foi feito. Feche a pendência #${numero} com "Closes #${numero}" no corpo do PR.`);
  } else {
    console.log(`3. Ao abrir o PR, inclua "Closes #${numero}" na descrição para fechar a tarefa automaticamente no merge.`);
  }
  console.log(`4. Se precisar interromper sem concluir, libere com: npm run tarefa -- liberar ${numero}\n`);
}

async function liberarTarefa(args) {
  const numero = parseInt(args[0], 10);
  if (isNaN(numero)) {
    console.error("✖ Informe o número da tarefa. Exemplo: npm run tarefa -- liberar 36");
    process.exit(1);
  }

  let motivo = "Não especificado";
  for (let i = 1; i < args.length; i++) {
    if (args[i] === "--motivo" && args[i + 1]) {
      motivo = args[i + 1];
      break;
    }
  }

  console.log(`🔍 Verificando tarefa #${numero}...`);
  console.log(`🔓 Liberando tarefa #${numero} e devolvendo para a fila...`);
  soltarIssue(numero, motivo);
  console.log(`✔ Tarefa #${numero} liberada com sucesso! Ela voltou para o estado DISPONÍVEL.`);
}

async function verStatus(args) {
  const numero = parseInt(args[0], 10);
  if (isNaN(numero)) {
    console.error("✖ Informe o número da tarefa. Exemplo: npm run tarefa -- status 36");
    process.exit(1);
  }

  const issue = obterIssue(numero, { comComentarios: true });
  const labels = (issue.labels || []).map((l) => (typeof l === "string" ? l : l.name));
  const temLabelAndamento = labels.includes("em-andamento");

  const itensProjeto = buscarItensDoProjeto();
  const itemProj = itensProjeto.find((i) => i.content && i.content.number === issue.number);
  const statusProj = itemProj ? itemProj.status : "Não definido no projeto";

  console.log(`\n=======================================================`);
  console.log(`STATUS DA TAREFA #${issue.number}`);
  console.log(`=======================================================`);
  console.log(`Título:        ${issue.title}`);
  console.log(`Estado:        ${issue.state}`);
  console.log(`Em andamento?  ${temLabelAndamento ? "SIM (Bloqueada)" : "NÃO (Livre)"}`);
  console.log(`Projeto:       ${statusProj}`);
  console.log(`Responsáveis:  ${(issue.assignees || []).map((a) => a.login).join(", ") || "Nenhum"}`);
  console.log(`Labels:        ${labels.join(", ") || "Nenhuma"}`);
  console.log(`URL:           ${issue.url}`);

  if (issue.comments && issue.comments.length > 0) {
    const ultimoComentario = issue.comments[issue.comments.length - 1];
    console.log(`\nÚltimo comentário (${ultimoComentario.author.login}):`);
    console.log(ultimoComentario.body.slice(0, 300) + (ultimoComentario.body.length > 300 ? "..." : ""));
  }
  console.log("=======================================================\n");
}

// Sem número: varre as livres e mostra só as que a doc deixou para trás (para o /decida).
// Com --conferida: registra na issue que ela foi comparada com a doc atual e o aviso some.
async function conferirRelevancia(args) {
  const numeros = args.filter((a) => /^\d+$/.test(a)).map(Number);
  const iNota = args.indexOf("--conferida");
  if (iNota >= 0) {
    const nota = args[iNota + 1];
    if (numeros.length !== 1 || !nota) {
      console.error('✖ Uso: npm run tarefa -- relevancia <numero> --conferida "o que foi conferido ou ajustado"');
      process.exit(1);
    }
    comentar(numeros[0], comentarioConferida(nota));
    console.log(`✔ Relevância da #${numeros[0]} registrada.`);
    return;
  }

  executarGit(["fetch", "origin", "main"], { silencioso: true });
  if (numeros.length > 0) {
    for (const numero of numeros) {
      const issue = obterIssue(numero, { comComentarios: true });
      const mudancas = mudancasNasDocs(issue);
      if (mudancas.commits.length > 0) avisarRelevancia(issue, mudancas);
      else console.log(`✔ #${numero}: a regra que ela segue não mudou depois dela.`);
    }
    return;
  }

  // Varredura: as mais defasadas primeiro, uma linha cada, para o /decida revisar só o topo.
  const defasadas = listarIssuesAbertas()
    .filter((issue) => {
      const labels = nomesDasLabels(issue);
      return !labels.includes("em-andamento") && !labels.includes(LABEL_PENDENCIA);
    })
    .map((issue) => ({ issue, mudancas: mudancasNasDocs(issue) }))
    .filter(({ mudancas }) => mudancas.commits.length > 0)
    .sort((a, b) => b.mudancas.commits.length - a.mudancas.commits.length);
  if (defasadas.length === 0) {
    console.log("✔ Nenhuma tarefa livre com regra mudada depois dela.");
    return;
  }
  console.log(`\nTarefas livres cuja regra mudou depois delas (${defasadas.length}; as ${LIMITE_VARREDURA} mais defasadas):`);
  for (const { issue, mudancas } of defasadas.slice(0, LIMITE_VARREDURA)) {
    const docs = mudancas.docs.map((doc) => doc.replace(/^docs\//, "")).join(", ");
    console.log(`  #${issue.number}  ${mudancas.commits.length} commit(s) desde ${mudancas.desde.slice(0, 10)} em ${docs}  ${issue.title}`);
  }
  console.log(`\nDetalhe de uma: npm run tarefa -- relevancia <numero>`);
}

async function concluirTarefa(args) {
  const numero = parseInt(args[0], 10);
  if (isNaN(numero)) {
    console.error("✖ Informe o número da tarefa. Exemplo: npm run tarefa -- concluir 36");
    process.exit(1);
  }

  console.log(`🏁 Marcando tarefa #${numero} como concluída no projeto...`);
  const issue = obterIssue(numero);

  removerLabel(numero, "em-andamento");
  sincronizarStatusProjeto(numero, issue.url, "Done");

  console.log(`✔ Tarefa #${numero} movida para "Done" no projeto.`);
}

async function criarSubtarefa(args) {
  const parentNumero = parseInt(args[0], 10);
  if (isNaN(parentNumero)) {
    console.error("✖ Informe o número da tarefa pai. Exemplo: npm run tarefa -- subtarefa 36 --titulo '...' --agente client-core");
    process.exit(1);
  }

  let titulo = null;
  let agente = null;
  let escopo = null;
  let contexto = "";

  for (let i = 1; i < args.length; i++) {
    if (args[i] === "--titulo" && args[i + 1]) {
      titulo = args[i + 1];
      i++;
    } else if (args[i] === "--agente" && args[i + 1]) {
      agente = args[i + 1];
      i++;
    } else if (args[i] === "--escopo" && args[i + 1]) {
      escopo = args[i + 1];
      i++;
    } else if (args[i] === "--contexto" && args[i + 1]) {
      contexto = args[i + 1];
      i++;
    }
  }

  if (!titulo) {
    console.error("✖ O título da subtarefa é obrigatório (--titulo '...')");
    process.exit(1);
  }

  console.log(`🔍 Verificando tarefa pai #${parentNumero}...`);
  const parent = obterIssue(parentNumero);

  // Escopo padrão deduzido da tarefa pai ou "core"
  if (!escopo) {
    const parentLabels = (parent.labels || []).map((l) => (typeof l === "string" ? l : l.name));
    escopo = escopos.find((e) => parentLabels.includes(e)) || "core";
  }

  const labels = [escopo];
  if (agente) {
    labels.push(`agente:${agente}`);
  }

  const tituloCompleto = `[#${parentNumero}] ${titulo}`;
  let pastas = [];
  try {
    pastas = readdirSync("shared/Package/Runtime/Features");
  } catch {
    // Sem a pasta (rodando fora da raiz): a pasta sai do escopo com a primeira letra maiúscula.
  }
  const corpo = corpoSubtarefa({
    pai: parentNumero,
    tituloPai: parent.title,
    papel: agente,
    escopo,
    pastas,
    contexto,
  });

  console.log(`✨ Criando subtarefa "${tituloCompleto}"...`);
  const novaIssue = apiGh(`${REPO}/issues`, {
    metodo: "POST",
    campos: { title: tituloCompleto, body: corpo, labels },
  });
  const novoNumero = novaIssue ? novaIssue.number : null;
  const novaIssueUrl = novaIssue ? novaIssue.html_url : null;

  if (novoNumero) {
    sincronizarStatusProjeto(novoNumero, novaIssueUrl, "Todo");
    comentar(parentNumero, `- [ ] #${novoNumero} (${titulo}) - Papel: \`${agente || "geral"}\``);
    console.log(`✔ Subtarefa #${novoNumero} criada com sucesso e vinculada à tarefa #${parentNumero}!`);
    console.log(`URL: ${novaIssueUrl}`);
  }
}

async function criarPendenciaUnity(args) {
  const pr = parseInt(args[0], 10);
  const faltas = [];
  for (let i = 1; i < args.length; i++) {
    if (args[i] === "--falta" && args[i + 1]) {
      faltas.push(args[i + 1]);
      i++;
    }
  }
  if (isNaN(pr) || faltas.length === 0) {
    console.error('✖ Uso: npm run tarefa -- pendencia-unity <pr> --falta "<o que faltou>" [--falta "..."]');
    process.exit(1);
  }

  const dadosPr = apiGh(`${REPO}/pulls/${pr}`);
  const branch = dadosPr.head.ref;
  const escopoDoPr = branch.split("/")[1];
  const labels = [LABEL_PENDENCIA, ...(escopos.includes(escopoDoPr) ? [escopoDoPr] : [])];

  console.log(`🧩 Registrando pendência de Unity do PR #${pr}...`);
  const issue = apiGh(`${REPO}/issues`, {
    metodo: "POST",
    campos: { title: tituloPendencia(pr, faltas), body: corpoPendencia({ pr, branch, faltas }), labels },
  });
  sincronizarStatusProjeto(issue.number, issue.html_url, "Todo");

  apiGh(`${REPO}/pulls/${pr}`, {
    metodo: "PATCH",
    campos: { body: corpoDoPrComPendencia(dadosPr.body, issue.number) },
    ignorarErro: true,
  });
  comentar(
    pr,
    `🧩 Sem Unity neste ambiente. O que faltou ficou na pendência #${issue.number}, que continua aberta depois do merge até alguém com Unity resolver.`,
  );

  console.log(`✔ Pendência #${issue.number} criada: ${issue.html_url}`);
  console.log("  O PR segue normal. Quem tiver Unity puxa com: npm run tarefa -- puxar " + issue.number);
}

function exibirAjuda() {
  console.log(`
Uso: npm run tarefa -- <comando> [argumentos]

Comandos disponíveis:
  listar                          Lista tarefas abertas, separando as disponíveis das em andamento.
  puxar <numero> [branch] [opts]  Reivindica a tarefa, bloqueia contra duplicidade e cria a branch.
                                  Opções:
                                    --agente <nome>     Identifica o papel do agente (ex: server-core).
                                    --worktree <path>   Cria a branch em um worktree separado.
                                    --base <branch>     Parte de outra branch do origin (leva empilhada).
  liberar <numero> [--motivo ...] Libera uma tarefa em andamento de volta para a fila de disponíveis.
  soltar-travas                  Libera em-andamento sem branch no origin e sem PR aberto há mais de 2 h.
  subtarefa <pai> [opts]          Cria uma subtarefa para um agente específico vinculada à tarefa pai.
                                  Opções:
                                    --titulo "..."      Título da subtarefa.
                                    --agente <nome>     Papel do agente (ex: client-core).
                                    --escopo <escopo>   Escopo funcional (ex: core, personagem).
                                    --contexto "..."    O que o agente precisa fazer (vai no corpo).
  status <numero>                 Exibe detalhes completos e o estado de bloqueio da tarefa.
  relevancia [numero...]          Aponta as tarefas livres cuja doc mudou depois delas (o puxar também avisa).
                                    --conferida "..."   Registra na issue que ela foi conferida com a doc atual.
  concluir <numero>               Atualiza o status no projeto para "Done".
  pendencia-unity <pr> --falta .. Registra o que ficou sem Unity num PR (issue "pendente-unity"),
                                  para o próximo agente com Unity puxar. Repita --falta por item.
  ajuda                          Mostra esta mensagem de ajuda.

Exemplos:
  npm run tarefa -- listar
  npm run tarefa -- puxar 36
  npm run tarefa -- puxar 36 feature/core/entrada-no-jogo-client --agente client-core
  npm run tarefa -- subtarefa 36 --titulo "Controle de 3a pessoa" --agente client-core
  npm run tarefa -- liberar 36 --motivo "Pausa para almoço"
`);
}

// -------------------------------------------------------------
// Entrypoint
// -------------------------------------------------------------

const comando = process.argv[2] || "listar";
const argumentos = process.argv.slice(3);

switch (comando) {
  case "listar":
  case "list":
  case "ls":
    await listarTarefas();
    break;
  case "puxar":
  case "iniciar":
  case "start":
  case "claim":
    await puxarTarefa(argumentos);
    break;
  case "liberar":
  case "abandonar":
  case "release":
    await liberarTarefa(argumentos);
    break;
  case "soltar-travas": {
    const liberadas = soltarTravasAbandonadas();
    if (liberadas.length > 0) {
      console.log(`Trava abandonada liberada: ${liberadas.map((numero) => `#${numero}`).join(", ")}`);
    }
    break;
  }
  case "subtarefa":
  case "subtask":
    await criarSubtarefa(argumentos);
    break;
  case "status":
    await verStatus(argumentos);
    break;
  case "relevancia":
    await conferirRelevancia(argumentos);
    break;
  case "concluir":
  case "done":
    await concluirTarefa(argumentos);
    break;
  case "pendencia-unity":
    await criarPendenciaUnity(argumentos);
    break;
  case "ajuda":
  case "help":
  case "--help":
  case "-h":
    exibirAjuda();
    break;
  default:
    console.error(`✖ Comando desconhecido: "${comando}"`);
    exibirAjuda();
    process.exit(1);
}
