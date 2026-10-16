// Gerenciador de tarefas (issues e project) do Jogo do Grupo.
// Garante fluxo seguro para múltiplos desenvolvedores e agentes em paralelo:
// quando alguém puxa uma tarefa, ela entra em "em andamento" (com label e project status)
// e impede que outra pessoa ou agente trabalhe na mesma tarefa ao mesmo tempo.
//
// Uso:
//   npm run tarefa -- listar
//   npm run tarefa -- puxar <numero> [<numero>...] [nome-da-branch] [--agente <nome>] [--worktree <caminho>] [--base <branch>]
//   npm run tarefa -- liberar <numero> [<numero>...] [--motivo "<motivo>"]
//   npm run tarefa -- revisar [N]
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
import { LIMITE_LOTES, podePuxarLote } from "./limite-funcionalidades.mjs";
import {
  compararDisponiveis,
  dependenciasAbertas,
  sinaisDaIssue,
  textoDaPrioridade,
} from "./prioridade-lista.mjs";
import { REVISAR_PADRAO, linhaDeRevisao, ordenarParaRevisar } from "./revisar.mjs";
import { motivoTravaAbandonada, travaAbandonada, ultimaTrava } from "./trava-abandonada.mjs";
import { corpoSubtarefa } from "./subtarefa.mjs";
import { baseDaConferencia, comentarioConferida, docsDaIssue } from "./relevancia.mjs";
import { sugerirNomeBranch } from "./nome-branch.mjs";
import { ligarNodeModules } from "./worktree-npm.mjs";
import {
  checkoutPrincipal,
  recusarPuxarNoCheckoutCompartilhado,
  worktreeSeparado,
} from "./worktree-puxar.mjs";
import {
  LABEL_PENDENCIA,
  branchDaPendencia,
  corpoDoPrComPendencia,
  corpoPendencia,
  prDaPendencia,
  tituloPendencia,
} from "./pendencia-unity.mjs";
import {
  LABEL_REGISTRO,
  TITULO_REGISTRO,
  blocoAtivoParaIssue,
  corpoRegistro,
  escopoDaIssue,
  labelSegure,
  labelsSegureParaIssue,
  normalizarEscopo,
  parseRegistro,
  recusaPuxarSegure,
} from "./segure.mjs";

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

function branchLocalExiste(nome) {
  return spawnSync("git", ["show-ref", "--verify", "--quiet", `refs/heads/${nome}`]).status === 0;
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

function obterIssueRegistroSegure({ criarSeFaltar = false } = {}) {
  const lote =
    apiGh(`${REPO}/issues?state=open&labels=${encodeURIComponent(LABEL_REGISTRO)}&per_page=5`) || [];
  const bruta = lote.find((item) => !item.pull_request);
  if (bruta) return normalizarIssue(bruta);
  if (!criarSeFaltar) return null;
  const criada = apiGh(`${REPO}/issues`, {
    metodo: "POST",
    campos: {
      title: TITULO_REGISTRO,
      body: corpoRegistro({}),
      labels: ["desenvolvimento", LABEL_REGISTRO],
    },
  });
  return normalizarIssue(criada);
}

function carregarBlocosSegure() {
  const registro = obterIssueRegistroSegure();
  if (!registro) return {};
  return parseRegistro(registro.body);
}

function salvarBlocosSegure(blocos) {
  const registro = obterIssueRegistroSegure({ criarSeFaltar: true });
  apiGh(`${REPO}/issues/${registro.number}`, {
    metodo: "PATCH",
    campos: { body: corpoRegistro(blocos) },
  });
}

function rotularIssueSegure(numero, blocos) {
  const issue = obterIssue(numero);
  if (issue.state !== "OPEN") return 0;
  const labels = nomesDasLabels(issue);
  if (labels.includes("em-andamento")) return 0;
  const extras = labelsSegureParaIssue({ titulo: issue.title, labels, blocos });
  for (const label of extras) adicionarLabel(numero, label);
  return extras.length;
}

function removerLabelsSegureDoEscopo(escopo) {
  const alvo = labelSegure(escopo);
  for (const issue of listarIssuesAbertas()) {
    if (nomesDasLabels(issue).includes(alvo)) removerLabel(issue.number, alvo);
  }
}

function mesclarLabelsSegure({ titulo, escopoExplicito, labels, blocos }) {
  return [
    ...new Set([
      ...labels,
      ...labelsSegureParaIssue({ titulo, escopoExplicito, labels, blocos }),
    ]),
  ];
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
  executarGit(["fetch", "origin", "main"], { silencioso: true });
  const liberadas = soltarTravasAbandonadas();
  if (liberadas.length > 0) {
    console.log(`🔓 Trava abandonada liberada: ${liberadas.map((numero) => `#${numero}`).join(", ")}`);
  }
  const issues = listarIssuesAbertas();
  const blocosSegure = carregarBlocosSegure();
  const itensProjeto = buscarItensDoProjeto();

  const emAndamento = [];
  const disponiveis = [];
  const bloqueadas = [];
  const pendenciasUnity = [];
  const paraPessoas = [];
  const abertas = new Set(issues.map((issue) => issue.number));

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
    } else if (labels.includes("pessoa")) {
      paraPessoas.push({ issue, labels, itemProj });
    } else {
      const sinais = sinaisDaIssue({ labels, corpo: issue.body, abertas });
      if (labels.includes("decisao")) sinais.push("decisao (/decida)");
      const esperando = dependenciasAbertas(issue.body, abertas).length > 0;
      disponiveis.push({ issue, labels, itemProj, sinais, esperando });
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
    for (const { issue, labels, sinais } of disponiveis) {
      const agentesTags = labels.filter((l) => l.startsWith("agente:")).join(", ") || "geral";
      const foco = labels.includes("foco") ? " | foco" : "";
      const avisos = sinais.length > 0 ? ` | ${sinais.join(" · ")}` : "";
      const ativo = blocoAtivoParaIssue({ titulo: issue.title, labels, blocos: blocosSegure });
      const segure = ativo ? ` | segure @${ativo.dono}` : "";
      console.log(`   #${issue.number} - ${issue.title}`);
      console.log(`     ${textoDaPrioridade(labels)}${foco} | Recomendado para: [${agentesTags}]${avisos}${segure}`);
    }
  }

  if (pendenciasUnity.length > 0) {
    console.log(`\n🧩 PENDÊNCIAS DE UNITY (${pendenciasUnity.length}) - só para quem tem Unity na máquina:`);
    for (const { issue } of pendenciasUnity) {
      console.log(`   #${issue.number} - ${issue.title}`);
    }
  }

  if (paraPessoas.length > 0) {
    console.log(`\n🙋 PARA PESSOAS (${paraPessoas.length}) - fora do /trabalhe:`);
    for (const { issue } of paraPessoas) {
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
  console.log("  npm run tarefa -- puxar <numero> [<numero>...] [branch] [--worktree <pasta>] [--agente <nome>]");
  console.log("-------------------------------------------------------\n");
}

// Puxa uma tarefa ou um lote (várias issues numa branch só, docs/agentes.md#lote).
// Confere todas antes de marcar qualquer uma: lote pela metade não fica travado.
async function puxarTarefa(args) {
  const numeros = [];
  let nomeBranch = null;
  let nomeAgente = null;
  let worktreePath = null;
  // Branch de base de uma leva empilhada (ainda sem merge); o padrão é a main.
  let base = "main";

  for (let i = 0; i < args.length; i++) {
    if (args[i] === "--agente" && args[i + 1]) {
      nomeAgente = args[i + 1];
      i++;
    } else if (args[i] === "--worktree" && args[i + 1]) {
      worktreePath = args[i + 1];
      i++;
    } else if (args[i] === "--base" && args[i + 1]) {
      base = args[i + 1].replace(/^origin\//, "");
      i++;
    } else if (/^#?\d+$/.test(args[i])) {
      numeros.push(parseInt(args[i].replace("#", ""), 10));
    } else if (!args[i].startsWith("--") && !nomeBranch) {
      nomeBranch = args[i];
    }
  }
  if (numeros.length === 0) {
    console.error("✖ Informe o número da tarefa (ou do lote). Exemplo: npm run tarefa -- puxar 36 [37 38]");
    process.exit(1);
  }

  const branchAqui = executarGit(["rev-parse", "--abbrev-ref", "HEAD"], { silencioso: true });
  const issues = [];
  const jaPuxadas = [];
  const recusas = [];
  const blocosSegure = carregarBlocosSegure();
  const loginAtual = obterUsuarioAtual();
  const itensProjeto = buscarItensDoProjeto();
  for (const numero of numeros) {
    console.log(`🔍 Verificando tarefa #${numero}...`);
    const issue = obterIssue(numero);
    const labels = nomesDasLabels(issue);
    if (issue.state !== "OPEN") {
      recusas.push(`#${numero} está com estado "${issue.state}"; só tarefa aberta pode ser puxada.`);
      continue;
    }
    const itemProj = itensProjeto.find((i) => i.content && i.content.number === issue.number);
    if (labels.includes("em-andamento") || itemProj?.status === "In Progress") {
      // Puxar repetido pelo mesmo agente (o primeiro já passou): a trava aponta a branch dele.
      const trava = ultimaTrava(comentariosDa(numero));
      const mesmaBranch =
        trava &&
        (trava.branch === branchAqui || (trava.branch === nomeBranch && branchLocalExiste(nomeBranch)));
      if (mesmaBranch) {
        jaPuxadas.push({ numero, branch: trava.branch });
        continue;
      }
      const responsaveis = (issue.assignees || []).map((a) => a.login).join(", ") || "outro desenvolvedor/agente";
      recusas.push(
        `#${numero} ("${issue.title}") já está EM ANDAMENTO por: ${responsaveis}. Se foi abandonada, peça para liberar: npm run tarefa -- liberar ${numero}`,
      );
      continue;
    }
    if (labels.includes("bloqueada")) {
      recusas.push(`#${numero} está BLOQUEADA por dependência ou decisão; resolva antes (/decida).`);
      continue;
    }
    const recusaSegure = recusaPuxarSegure({
      login: loginAtual,
      titulo: issue.title,
      labels,
      blocos: blocosSegure,
    });
    if (recusaSegure) {
      recusas.push(`#${numero} ("${issue.title}") ${recusaSegure}`);
      continue;
    }
    issues.push({ issue, labels });
  }

  if (recusas.length > 0) {
    console.error(`\n⛔ Nada foi puxado:`);
    for (const recusa of recusas) console.error(`   ${recusa}`);
    console.error("Tire essas do lote e puxe de novo.\n");
    process.exit(1);
  }
  if (issues.length === 0) {
    const branch = jaPuxadas[0].branch;
    console.log(`\n✔ ${jaPuxadas.map((j) => `#${j.numero}`).join(", ")} já puxada(s) por você, na branch ${branch}. Siga nela.`);
    process.exit(0);
  }
  if (!nomeBranch && jaPuxadas.length > 0) nomeBranch = jaPuxadas[0].branch;

  const [primeira] = issues;
  const numero = primeira.issue.number;

  // Pendência de Unity: com o PR ainda aberto, trabalha na branch dele em vez de criar outra.
  // Com o PR já fechado, segue o fluxo normal numa branch nova a partir da main.
  let branchExistente = null;
  if (primeira.labels.includes(LABEL_PENDENCIA)) {
    const pr = prDaPendencia(primeira.issue.body);
    const dadosPr = pr ? apiGh(`${REPO}/pulls/${pr}`, { ignorarErro: true }) : null;
    if (dadosPr && dadosPr.state === "open") {
      branchExistente = dadosPr.head.ref;
      nomeBranch = branchExistente;
      console.log(`ℹ Pendência de Unity do PR #${pr}, ainda aberto: usando a branch dele (${nomeBranch}).`);
    } else if (!nomeBranch) {
      const escopoDoPr = (branchDaPendencia(primeira.issue.body) || "").split("/")[1];
      nomeBranch = `chore/${escopos.includes(escopoDoPr) ? escopoDoPr : "core"}/unity-pendencia-${numero}`;
      console.log(`ℹ O PR #${pr} já foi fechado: a pendência vai numa branch nova a partir da main.`);
    }
  }

  if (!nomeBranch) {
    nomeBranch = sugerirNomeBranch(primeira.issue);
    console.log(`ℹ Nome de branch sugerido automaticamente: ${nomeBranch}`);
  }
  if (!padraoBranch.test(nomeBranch)) {
    console.error(`✖ Nome de branch inválido: "${nomeBranch}"`);
    console.error("  Formato: <tipo>/<escopo>/<descricao-em-kebab-case>");
    console.error(`  Tipos válidos: ${tiposDeBranch.join(", ")}`);
    console.error(`  Escopos válidos: ${escopos.join(", ")}`);
    process.exit(1);
  }

  soltarTravasAbandonadas();
  const doLote = new Set(numeros);
  const outroLoteEmAndamento = listarIssuesAbertas().some(
    (aberta) => !doLote.has(aberta.number) && nomesDasLabels(aberta).includes("em-andamento"),
  );
  const raizAtual = executarGit(["rev-parse", "--show-toplevel"], { silencioso: true });
  const recusaWorktree = recusarPuxarNoCheckoutCompartilhado({
    checkoutPrincipal: checkoutPrincipal(executarGit(["rev-parse", "--git-dir"], { silencioso: true })),
    worktreeSeparado: worktreeSeparado(worktreePath, raizAtual),
    outroLoteEmAndamento,
    nomeAgente,
  });
  if (recusaWorktree) {
    console.error(`\n⛔ ${recusaWorktree}`);
    console.error("  Ver docs/agentes.md#worktree-ao-puxar\n");
    process.exit(1);
  }

  // Limite de lotes em andamento, conferido uma vez para o lote inteiro.
  const emAndamento = listarIssuesAbertas()
    .filter((aberta) => !doLote.has(aberta.number) && nomesDasLabels(aberta).includes("em-andamento"))
    .map((aberta) => ({
      numero: aberta.number,
      labels: nomesDasLabels(aberta),
      branch: ultimaTrava(aberta.totalComentarios > 0 ? comentariosDa(aberta.number) : [])?.branch ?? null,
    }));
  const vaga = podePuxarLote(
    issues.flatMap((item) => item.labels),
    emAndamento,
    nomeBranch,
  );
  if (vaga.acimaDoRecomendado) {
    console.warn(
      `\n⚠ Recomendado no máximo ${LIMITE_LOTES} lotes de jogo em andamento (ADR 0037); em curso: ${vaga.emCurso.join(", ")}.`,
    );
    console.warn(`Continuando mesmo assim. Feche um lote ou junte a um branch em curso se quiser foco.\n`);
  }

  // Tarefa antiga pode pedir regra que a doc já mudou: só avisa, quem decide é o agente.
  let docMudou = false;
  executarGit(["fetch", "origin", "main"], { silencioso: true });
  for (const { issue, labels } of issues) {
    if (labels.includes(LABEL_PENDENCIA)) continue;
    const mudancas = mudancasNasDocs(issue);
    if (mudancas.commits.length > 0) {
      docMudou = true;
      avisarRelevancia(issue, mudancas);
    }
  }

  const usuario = nomeAgente ? `agente ${nomeAgente}` : `@${obterUsuarioAtual()}`;
  const lote = issues.map(({ issue }) => `#${issue.number}`).join(", ");
  for (const { issue } of issues) {
    console.log(`\n🔒 Reivindicando tarefa #${issue.number} para ${usuario}...`);
    adicionarLabel(issue.number, "em-andamento");
    if (!nomeAgente) atribuir(issue.number, obterUsuarioAtual());
    sincronizarStatusProjeto(issue.number, issue.url, "In Progress");
    const doMesmoLote = issues.length > 1 ? `\n- **Lote:** ${lote}` : "";
    comentar(
      issue.number,
      `🚀 **Tarefa em andamento**\n\n- **Responsável:** ${usuario}\n- **Branch:** \`${nomeBranch}\`${doMesmoLote}\n- **Início:** ${new Date().toISOString()}\n\n*Esta tarefa foi puxada e está bloqueada para outros agentes/desenvolvedores até ser concluída ou liberada.*`,
    );
  }

  // Configura Git (Worktree ou Branch local). Branch que já existe aqui (lote acrescentado depois) fica como está.
  // Agente que já nasceu num worktree e passa o próprio caminho: cria a branch ali mesmo
  // (o git worktree add falharia depois de a issue já estar marcada).
  if (worktreePath && resolve(worktreePath).toLowerCase() === resolve(raizAtual).toLowerCase()) worktreePath = null;
  console.log(`🌿 Configurando branch "${nomeBranch}"...`);
  if (branchAqui === nomeBranch) {
    console.log(`✔ Já na branch ${nomeBranch}.`);
  } else if (branchExistente) {
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

  const closes = issues.map(({ issue }) => `Closes #${issue.number}`).join(", ");
  console.log(`\n=======================================================`);
  console.log(`✔ ${issues.length > 1 ? `LOTE ${lote}` : `TAREFA #${numero}`} PUXADO COM SUCESSO!`);
  console.log(`=======================================================`);
  for (const { issue } of issues) console.log(`Tarefa:     #${issue.number} - ${issue.title}`);
  console.log(`Atribuída:  ${usuario}`);
  console.log(`Branch:     ${nomeBranch}`);
  console.log(`\nPróximos passos:`);
  if (docMudou) {
    console.log(`0. A doc mudou depois da issue (aviso acima): confira a relevância antes de codar.`);
  }
  console.log(`1. Desenvolva e teste${issues.length > 1 ? ", um commit por issue" : ""}.`);
  if (branchExistente) {
    console.log(`2. Dê push na mesma branch e comente no PR o que foi feito. Feche a pendência com "${closes}" no corpo do PR.`);
  } else {
    console.log(`2. No corpo do PR: ${closes}.`);
  }
  console.log(`3. Se precisar interromper sem concluir: npm run tarefa -- liberar ${numeros.join(" ")}\n`);
}

async function liberarTarefa(args) {
  const iMotivo = args.indexOf("--motivo");
  const motivo = iMotivo >= 0 && args[iMotivo + 1] ? args[iMotivo + 1] : "Não especificado";
  const numeros = args
    .filter((arg, i) => (iMotivo < 0 || i !== iMotivo + 1) && /^#?\d+$/.test(arg))
    .map((arg) => parseInt(arg.replace("#", ""), 10));
  if (numeros.length === 0) {
    console.error("✖ Informe o número da tarefa. Exemplo: npm run tarefa -- liberar 36 [37]");
    process.exit(1);
  }

  for (const numero of numeros) {
    console.log(`🔓 Liberando tarefa #${numero} e devolvendo para a fila...`);
    soltarIssue(numero, motivo);
    console.log(`✔ Tarefa #${numero} liberada! Ela voltou para o estado DISPONÍVEL.`);
  }
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

// Fila do /decida: as livres há mais tempo sem conferência, com os sinais de cada uma.
// Conferir com `relevancia <N> --conferida "..."` tira a issue do topo da próxima varredura.
async function revisarQuadro(args) {
  const limite = parseInt(args.find((a) => /^\d+$/.test(a)) ?? REVISAR_PADRAO, 10);
  executarGit(["fetch", "origin", "main"], { silencioso: true });
  const abertas = listarIssuesAbertas();
  const numerosAbertos = new Set(abertas.map((issue) => issue.number));
  const livres = abertas
    .filter((issue) => {
      const labels = nomesDasLabels(issue);
      return !labels.includes("em-andamento") && !labels.includes(LABEL_PENDENCIA);
    })
    .map((issue) => {
      const comentarios = issue.totalComentarios > 0 ? comentariosDa(issue.number) : [];
      return { issue: { ...issue, comments: comentarios }, desde: baseDaConferencia(issue.criadaEm, comentarios) };
    });
  const fila = ordenarParaRevisar(livres).slice(0, limite);
  const agoraMs = Date.now();
  console.log(`\nConferir (${fila.length} de ${livres.length} livres, as há mais tempo sem conferência):`);
  for (const { issue, desde } of fila) {
    const commitsDoc = mudancasNasDocs(issue).commits.length;
    console.log(`  ${linhaDeRevisao({ issue, labels: nomesDasLabels(issue), desde, commitsDoc, abertas: numerosAbertos, agoraMs })}`);
  }
  console.log(`\nDepois do veredito: npm run tarefa -- relevancia <N> --conferida "<o que ficou>"`);
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

  const blocosSegure = carregarBlocosSegure();
  let labels = [escopo];
  if (agente) {
    labels.push(`agente:${agente}`);
  }

  const tituloCompleto = `[#${parentNumero}] ${titulo}`;
  labels = mesclarLabelsSegure({
    titulo: tituloCompleto,
    escopoExplicito: escopo,
    labels,
    blocos: blocosSegure,
  });
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
  const blocosSegure = carregarBlocosSegure();
  let labels = [LABEL_PENDENCIA, ...(escopos.includes(escopoDoPr) ? [escopoDoPr] : [])];
  const titulo = tituloPendencia(pr, faltas);
  labels = mesclarLabelsSegure({
    titulo,
    escopoExplicito: escopos.includes(escopoDoPr) ? escopoDoPr : null,
    labels,
    blocos: blocosSegure,
  });

  console.log(`🧩 Registrando pendência de Unity do PR #${pr}...`);
  const issue = apiGh(`${REPO}/issues`, {
    metodo: "POST",
    campos: { title: titulo, body: corpoPendencia({ pr, branch, faltas }), labels },
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

async function comandoSegure(args) {
  const login = obterUsuarioAtual();

  if (args[0] === "status") {
    const blocos = carregarBlocosSegure();
    const escoposAtivos = Object.keys(blocos).sort();
    if (escoposAtivos.length === 0) {
      console.log("Nenhum segure ativo.");
      return;
    }
    console.log("Segures ativos:");
    for (const escopo of escoposAtivos) {
      const { dono, desde } = blocos[escopo];
      console.log(`  ${escopo}: @${dono} (desde ${desde.slice(0, 10)})`);
    }
    return;
  }

  if (args[0] === "liberar") {
    const escopo = normalizarEscopo(args[1]);
    if (!escopo) {
      console.error("✖ Informe o escopo. Exemplo: npm run tarefa -- segure liberar personagem");
      process.exit(1);
    }
    const blocos = carregarBlocosSegure();
    if (!blocos[escopo]) {
      console.error(`✖ Não há segure ativo para "${escopo}".`);
      process.exit(1);
    }
    if (blocos[escopo].dono !== login) {
      console.error(`✖ Só @${blocos[escopo].dono} pode liberar o segure de ${escopo}.`);
      process.exit(1);
    }
    delete blocos[escopo];
    salvarBlocosSegure(blocos);
    removerLabelsSegureDoEscopo(escopo);
    console.log(`✔ Segure de ${escopo} liberado.`);
    return;
  }

  if (args[0] === "rotular") {
    const blocos = carregarBlocosSegure();
    const numeros = args.slice(1).map((a) => parseInt(a.replace("#", ""), 10)).filter((n) => !isNaN(n));
    let total = 0;
    if (numeros.length === 0) {
      for (const issue of listarIssuesAbertas()) {
        total += rotularIssueSegure(issue.number, blocos);
      }
    } else {
      for (const numero of numeros) total += rotularIssueSegure(numero, blocos);
    }
    console.log(`✔ ${total} label(s) segure aplicada(s).`);
    return;
  }

  const escopo = normalizarEscopo(args[0]);
  if (!escopo) {
    console.error("✖ Escopo inválido. Use um escopo do commitlint (ex.: personagem, itens, mundo).");
    console.error("  npm run tarefa -- segure <escopo> | status | liberar <escopo> | rotular [#N ...]");
    process.exit(1);
  }

  const blocos = carregarBlocosSegure();
  if (blocos[escopo] && blocos[escopo].dono !== login) {
    console.error(`✖ ${escopo} já está seguro para @${blocos[escopo].dono}. Peça para liberar antes.`);
    process.exit(1);
  }
  blocos[escopo] = { dono: login, desde: new Date().toISOString() };
  salvarBlocosSegure(blocos);

  let rotuladas = 0;
  for (const issue of listarIssuesAbertas()) {
    const labels = nomesDasLabels(issue);
    if (labels.includes("em-andamento")) continue;
    if (escopoDaIssue(issue.title, labels) !== escopo) continue;
    rotuladas += rotularIssueSegure(issue.number, blocos);
  }

  console.log(`🔒 Segure ${escopo} ativo para @${login}. ${rotuladas} tarefa(s) livre(s) rotulada(s).`);
  console.log("  Issues novas: npm run tarefa -- segure rotular <numero> (ou rotular sem número para varrer).");
}

function exibirAjuda() {
  console.log(`
Uso: npm run tarefa -- <comando> [argumentos]

Comandos disponíveis:
  listar                          Lista tarefas abertas, separando as disponíveis das em andamento.
  puxar <numero>... [branch] [opts]
                                  Reivindica a tarefa (ou o lote: várias issues numa branch só),
                                  bloqueia contra duplicidade e cria a branch.
                                  Opções:
                                    --agente <nome>     Identifica o papel do agente (ex: server-core).
                                    --worktree <path>   Cria a branch em um worktree separado.
                                    --base <branch>     Parte de outra branch do origin (leva empilhada).
  liberar <numero>... [--motivo ..] Libera tarefas em andamento de volta para a fila de disponíveis.
  soltar-travas                  Libera em-andamento sem branch no origin e sem PR aberto há mais de 24 h.
  segure <escopo>                Reserva tarefas livres do prefixo [Escopo] para quem rodou (IA incluída).
  segure status                  Lista segures ativos.
  segure liberar <escopo>        Encerra a reserva (só o dono).
  segure rotular [#N ...]        Aplica label segure em issues novas (todas abertas se omitir #N).
  subtarefa <pai> [opts]          Cria uma subtarefa para um agente específico vinculada à tarefa pai.
                                  Opções:
                                    --titulo "..."      Título da subtarefa.
                                    --agente <nome>     Papel do agente (ex: client-core).
                                    --escopo <escopo>   Escopo funcional (ex: core, personagem).
                                    --contexto "..."    O que o agente precisa fazer (vai no corpo).
  status <numero>                 Exibe detalhes completos e o estado de bloqueio da tarefa.
  relevancia [numero...]          Aponta as tarefas livres cuja doc mudou depois delas (o puxar também avisa).
                                    --conferida "..."   Registra na issue que ela foi conferida com a doc atual.
  revisar [N]                     Fila do /decida: as N livres há mais tempo sem conferência (padrão 15).
  concluir <numero>               Atualiza o status no projeto para "Done".
  pendencia-unity <pr> --falta .. Registra o que ficou sem Unity num PR (issue "pendente-unity"),
                                  para o próximo agente com Unity puxar. Repita --falta por item.
  ajuda                          Mostra esta mensagem de ajuda.

Exemplos:
  npm run tarefa -- listar
  npm run tarefa -- puxar 36
  npm run tarefa -- puxar 36 feature/core/entrada-no-jogo-client --agente client-core
  npm run tarefa -- puxar 40 41 52 feature/mobs/lote-do-chefe --agente funcionalidade --worktree .
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
  case "revisar":
    await revisarQuadro(argumentos);
    break;
  case "concluir":
  case "done":
    await concluirTarefa(argumentos);
    break;
  case "pendencia-unity":
    await criarPendenciaUnity(argumentos);
    break;
  case "segure":
    await comandoSegure(argumentos);
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
