// Prepara uma sessão de IA na nuvem (issue #227): instala as dependências do npm (commitlint e
// hooks do husky) e o SDK do .NET 10 com o CSharpier, para o agente formatar e rodar
// `dotnet test` antes do push em vez de descobrir no CI.
//
// Uso: npm run nuvem:preparar          (espera terminar; ~1 min na primeira vez)
//      node scripts/agentes/preparar-nuvem.mjs --hook
//        (hook de início de sessão: fora da nuvem não faz nada; na nuvem dispara em segundo
//         plano, sai na hora e escreve o log em ~/.cache/jogo-do-grupo/preparar-nuvem.log)
//
// As duas formas não rodam ao mesmo tempo: a do hook grava o pid em preparar-nuvem.pid, e o
// `npm run nuvem:preparar` espera ela terminar antes de começar (antes, as duas rodavam juntas
// e o `dotnet tool restore` falhava com "file is being used by another process").
//
// Na nuvem do Claude Code, registry.npmjs.org vem na lista de "sem proxy", mas não há saída
// direta: o npm só funciona pelo proxy. Por isso o npm roda com a lista de "sem proxy" trocada.

import { execFileSync, spawn } from "node:child_process";
import {
  existsSync,
  mkdirSync,
  openSync,
  readFileSync,
  symlinkSync,
  unlinkSync,
  writeFileSync,
} from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const RAIZ = fileURLToPath(new URL("../..", import.meta.url));
const DOTNET_DIR = join(homedir(), ".dotnet");
const INSTALADOR = "https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh";
const PASTA_CACHE = join(homedir(), ".cache", "jogo-do-grupo");
const ARQUIVO_PID = join(PASTA_CACHE, "preparar-nuvem.pid");
const ESPERA_MAXIMA_MS = 15 * 60 * 1000;

/** Esta sessão roda na nuvem do Claude Code? */
export function ehNuvem(env = process.env) {
  return Boolean(env.CLAUDE_CODE_REMOTE_ENVIRONMENT_TYPE || env.CLAUDE_CODE_REMOTE === "true");
}

/**
 * Ambiente para o npm passar pelo proxy: tira o registry das listas de "sem proxy". Fora de
 * proxy (sem HTTPS_PROXY), devolve o ambiente como veio.
 */
export function ambienteDoNpm(env = process.env) {
  if (!env.HTTPS_PROXY && !env.https_proxy) return { ...env };
  const resultado = { ...env };
  for (const chave of ["NO_PROXY", "no_proxy", "npm_config_noproxy", "GLOBAL_AGENT_NO_PROXY"]) {
    delete resultado[chave];
  }
  resultado.npm_config_noproxy = "localhost,127.0.0.1";
  return resultado;
}

function processoVivo(pid) {
  try {
    process.kill(pid, 0);
    return true;
  } catch (erro) {
    return erro.code === "EPERM";
  }
}

/**
 * Pid de outra preparação ainda rodando, ou null. `conteudo` é o que está no arquivo de pid
 * (null se não existe); o pid do próprio processo não conta.
 */
export function outraPreparacaoAtiva(conteudo, meuPid, vivo = processoVivo) {
  const pid = Number.parseInt(conteudo ?? "", 10);
  if (!Number.isInteger(pid) || pid <= 0 || pid === meuPid) return null;
  return vivo(pid) ? pid : null;
}

function lerPid() {
  try {
    return readFileSync(ARQUIVO_PID, "utf8");
  } catch {
    return null;
  }
}

/** Espera a preparação do hook (em segundo plano) acabar, para não rodar duas ao mesmo tempo. */
function esperarOutraPreparacao() {
  const limite = Date.now() + ESPERA_MAXIMA_MS;
  let avisou = false;
  for (let pid; (pid = outraPreparacaoAtiva(lerPid(), process.pid)); ) {
    if (Date.now() > limite) {
      console.log(`A preparação ${pid} passou de 15 min; seguindo sem esperar.`);
      return;
    }
    if (!avisou) {
      console.log(`Esperando a preparação em segundo plano (pid ${pid}) terminar...`);
      avisou = true;
    }
    execFileSync("sleep", ["1"]);
  }
}

function rodar(comando, args, opcoes = {}) {
  console.log(`> ${comando} ${args.join(" ")}`);
  execFileSync(comando, args, { cwd: RAIZ, stdio: "inherit", ...opcoes });
}

function temComando(comando) {
  try {
    execFileSync(comando, ["--version"], { stdio: "ignore" });
    return true;
  } catch {
    return false;
  }
}

function prepararNpm() {
  if (existsSync(join(RAIZ, "node_modules", ".bin", "commitlint"))) return;
  rodar("npm", ["install", "--no-audit", "--no-fund"], { env: ambienteDoNpm() });
}

function prepararDotnet() {
  if (!temComando("dotnet")) {
    const script = join(homedir(), ".cache", "dotnet-install.sh");
    mkdirSync(join(homedir(), ".cache"), { recursive: true });
    rodar("curl", ["-sSL", INSTALADOR, "-o", script]);
    rodar("bash", [script, "--channel", "10.0", "--install-dir", DOTNET_DIR]);
    // Os comandos seguintes do agente não herdam PATH: põe o dotnet num lugar que já está nele.
    const link = "/usr/local/bin/dotnet";
    try {
      if (existsSync(link)) unlinkSync(link);
      symlinkSync(join(DOTNET_DIR, "dotnet"), link);
    } catch {
      console.log(`Não deu para criar ${link}; use ${DOTNET_DIR}/dotnet ou ponha no PATH.`);
    }
  }
  rodar("dotnet", ["tool", "restore"], {
    env: { ...process.env, DOTNET_ROOT: DOTNET_DIR, PATH: `${DOTNET_DIR}:${process.env.PATH}` },
  });
}

function preparar() {
  esperarOutraPreparacao();
  mkdirSync(PASTA_CACHE, { recursive: true });
  writeFileSync(ARQUIVO_PID, String(process.pid));
  try {
    return prepararTudo();
  } finally {
    if (lerPid() === String(process.pid)) unlinkSync(ARQUIVO_PID);
  }
}

function prepararTudo() {
  let ok = true;
  for (const passo of [prepararNpm, prepararDotnet]) {
    try {
      passo();
    } catch (erro) {
      ok = false;
      console.error(`✖ ${passo.name}: ${erro.message}`);
    }
  }
  console.log(ok ? "✔ Ambiente pronto: npm, dotnet e CSharpier." : "✖ Preparação incompleta (veja acima).");
  return ok;
}

const direto = process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1];
if (direto) {
  if (process.argv.includes("--hook")) {
    if (!ehNuvem()) process.exit(0);
    mkdirSync(PASTA_CACHE, { recursive: true });
    const log = join(PASTA_CACHE, "preparar-nuvem.log");
    const saida = openSync(log, "a");
    const filho = spawn(process.execPath, [fileURLToPath(import.meta.url)], {
      cwd: RAIZ,
      detached: true,
      stdio: ["ignore", saida, saida],
    });
    // Grava o pid já aqui: um `npm run nuvem:preparar` logo em seguida precisa vê-lo.
    writeFileSync(ARQUIVO_PID, String(filho.pid));
    filho.unref();
    console.log(
      `Sessão na nuvem: npm, .NET 10 e CSharpier estão sendo instalados em segundo plano (log em ${log}). Antes do primeiro commit ou de \`dotnet\`, rode \`npm run nuvem:preparar\` (espera terminar).`,
    );
    process.exit(0);
  }
  process.exit(preparar() ? 0 : 1);
}
