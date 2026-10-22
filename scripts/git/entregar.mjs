// Commita o que está no stage, faz o push e abre o PR.
// O corpo vem de um arquivo: no PowerShell a crase parte um --body na linha de comando,
// e o commitlint recusa linha de corpo com mais de 100 caracteres.
//
// Uso: npm run entregar -- --titulo "tipo(escopo): descricao" --corpo <arquivo fora do commit, ex.: pr-corpo.md, ignorado pelo git>

import { execFileSync } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { escopos, tipos } from "../../commitlint.config.mjs";
import { erroDeBranch } from "./validar-branch.mjs";

export const LIMITE_LINHA = 100;

export function quebrarLinhas(texto, limite = LIMITE_LINHA) {
  const linhas = String(texto).replace(/\r\n/g, "\n").split("\n");
  const saida = [];
  for (const linha of linhas) {
    if (linha.length <= limite) {
      saida.push(linha);
      continue;
    }
    let resto = linha.trimEnd();
    while (resto.length > limite) {
      let corte = resto.lastIndexOf(" ", limite);
      if (corte <= 0) corte = limite;
      saida.push(resto.slice(0, corte).trimEnd());
      resto = resto.slice(corte).trimStart();
    }
    saida.push(resto);
  }
  return saida.join("\n").replace(/\n+$/, "");
}

// O que o commitlint e o job "Padrão do Git" recusariam no título, conferido antes do commit
// (sem depender do husky). Devolve a mensagem do erro, ou null se o título passa.
export function erroDeTitulo(titulo) {
  const cabecalho = String(titulo ?? "").trim();
  const lista = `Tipos: ${tipos.join(", ")}\n  Escopos: ${escopos.join(", ")}`;
  const partes = /^(\w+)\(([^)]*)\)!?: (.+)$/.exec(cabecalho);
  if (!partes) {
    return `✖ Título fora do padrão "tipo(escopo): descricao": "${cabecalho}"\n  ${lista}`;
  }
  const [, tipo, escopo, descricao] = partes;
  if (!tipos.includes(tipo)) return `✖ Tipo "${tipo}" não existe.\n  ${lista}`;
  if (!escopos.includes(escopo)) {
    return `✖ Escopo "${escopo}" não existe; use core para docs e processo gerais.\n  ${lista}`;
  }
  if (/^[A-Z]/.test(descricao)) return `✖ Descrição começa minúscula: "${descricao}".`;
  if (cabecalho.length > LIMITE_LINHA) {
    return `✖ Título com ${cabecalho.length} caracteres (máximo ${LIMITE_LINHA}).`;
  }
  return null;
}

export function mensagemDeCommit(titulo, corpo) {
  const cabecalho = String(titulo ?? "").trim();
  if (!cabecalho || cabecalho.includes("\n")) {
    throw new Error("O título do commit é uma linha só.");
  }
  if (cabecalho.length > LIMITE_LINHA) {
    throw new Error(`Cabeçalho com ${cabecalho.length} caracteres (máximo ${LIMITE_LINHA}).`);
  }
  const miolo = quebrarLinhas(corpo ?? "").trim();
  return miolo ? `${cabecalho}\n\n${miolo}\n` : `${cabecalho}\n`;
}

export function nomeDaBranchSeguinte(branch) {
  const partes = String(branch).split("/");
  if (partes.length !== 3 || partes.some((p) => !p)) {
    throw new Error(`Branch fora do padrão tipo/escopo/descricao: "${branch}".`);
  }
  let descricao = partes[2];
  const numerada = descricao.match(/^(.*)-segue-(\d+)$/);
  if (numerada) descricao = `${numerada[1]}-segue-${Number(numerada[2]) + 1}`;
  else if (descricao.endsWith("-segue")) descricao = `${descricao}-2`;
  else descricao = `${descricao}-segue`;
  return `${partes[0]}/${partes[1]}/${descricao}`;
}

export function commitsNovos(saidaCherry) {
  return String(saidaCherry)
    .split("\n")
    .filter((linha) => linha.startsWith("+ "))
    .map((linha) => linha.slice(2).trim())
    .filter(Boolean);
}

export function commitsParaLevar(saidaCherry, saidaRevList) {
  const novos = new Set(commitsNovos(saidaCherry));
  return String(saidaRevList)
    .split("\n")
    .map((linha) => linha.trim())
    .filter((sha) => novos.has(sha));
}

export function lerArgumentos(argv) {
  const args = { titulo: "", corpo: "" };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === "--titulo") args.titulo = argv[++i] ?? "";
    else if (argv[i] === "--corpo") args.corpo = argv[++i] ?? "";
    else throw new Error(`Argumento desconhecido: ${argv[i]}`);
  }
  if (!args.titulo || !args.corpo) {
    throw new Error("Informe --titulo e --corpo. O corpo é um arquivo, não o texto na linha de comando.");
  }
  return args;
}

// C# que o check `csharpier` do CI confere: os .cs de uma lista do git (um por linha).
export function arquivosCsharp(saidaNomes) {
  return String(saidaNomes)
    .split("\n")
    .map((linha) => linha.trim())
    .filter((nome) => nome.endsWith(".cs"));
}

function csharpier(acao, arquivos) {
  if (arquivos.length === 0) return true;
  try {
    execFileSync("dotnet", ["csharpier", acao, ...arquivos], {
      stdio: ["ignore", "ignore", "inherit"],
      windowsHide: true,
    });
    return true;
  } catch (erro) {
    if (erro.code === "ENOENT") {
      console.error("⚠ dotnet indisponível: CSharpier não conferido (o CI confere).");
      return true;
    }
    return false;
  }
}

function gitOut(args) {
  return execFileSync("git", args, {
    encoding: "utf8",
    windowsHide: true,
    stdio: ["ignore", "pipe", "pipe"],
  }).trim();
}

function gitRun(args) {
  execFileSync("git", args, { stdio: "inherit", windowsHide: true });
}

function prNumero(branch, state) {
  const raw = execFileSync(
    "gh",
    ["pr", "list", "--state", state, "--head", branch, "--json", "number", "--limit", "1"],
    { encoding: "utf8", windowsHide: true, stdio: ["ignore", "pipe", "pipe"] },
  );
  return JSON.parse(raw || "[]")[0]?.number ?? null;
}

function temMudancaForaDoStage() {
  return gitOut(["status", "--porcelain"])
    .split("\n")
    .filter(Boolean)
    .some((linha) => linha[1] !== " ");
}

function principal() {
  let titulo;
  let corpoArquivo;
  try {
    ({ titulo, corpo: corpoArquivo } = lerArgumentos(process.argv.slice(2)));
  } catch (erro) {
    console.error(erro.message);
    process.exit(2);
  }

  // Título e branch antes de commitar: o hook do husky pode não rodar (worktree sem npm install).
  const erroTitulo = erroDeTitulo(titulo);
  if (erroTitulo) {
    console.error(erroTitulo);
    process.exit(1);
  }

  const branchInicial = gitOut(["branch", "--show-current"]);
  if (!branchInicial || branchInicial === "main") {
    console.error("✖ Não entregue na main. Crie uma branch (docs/git.md).");
    process.exit(1);
  }
  const erroBranch = erroDeBranch(branchInicial);
  if (erroBranch) {
    console.error(erroBranch);
    process.exit(1);
  }

  let mensagem;
  try {
    mensagem = mensagemDeCommit(titulo, readFileSync(corpoArquivo, "utf8"));
  } catch (erro) {
    console.error(`✖ ${erro.message}`);
    process.exit(1);
  }

  const temStage = Boolean(gitOut(["diff", "--cached", "--name-only"]));
  // Sem stage e com mudança solta, o PR sairia só com os commits antigos e sem o trabalho.
  if (!temStage && temMudancaForaDoStage()) {
    console.error("✖ Nada no stage, mas há mudanças fora dele. Dê git add no que vai no PR (ou guarde o resto).");
    process.exit(1);
  }
  if (temStage) {
    // Formata o C# do stage antes do commit. Arquivo com parte fora do stage fica de fora,
    // para o add não levar o que não foi escolhido (o check antes do push ainda o pega).
    const foraDoStage = new Set(arquivosCsharp(gitOut(["diff", "--name-only"])));
    const doStage = arquivosCsharp(
      gitOut(["diff", "--cached", "--name-only", "--diff-filter=ACMR"]),
    ).filter((nome) => !foraDoStage.has(nome));
    if (csharpier("format", doStage) && doStage.length > 0) gitRun(["add", "--", ...doStage]);
    // Num worktree, .git é um arquivo: a pasta certa vem do próprio git.
    const arquivoMensagem = join(gitOut(["rev-parse", "--git-dir"]), "MENSAGEM_COMMIT");
    writeFileSync(arquivoMensagem, mensagem);
    gitRun(["commit", "-F", arquivoMensagem]);
  }

  gitRun(["fetch", "origin", "main"]);
  let branch = branchInicial;
  const headAntes = gitOut(["rev-parse", "HEAD"]);
  let mergeado;
  try {
    mergeado = prNumero(branchInicial, "merged");
  } catch {
    console.error("✖ Não conferi se a branch já foi mergeada (gh indisponível).");
    process.exit(1);
  }

  if (mergeado) {
    if (temMudancaForaDoStage()) {
      console.error("✖ Há mudanças fora do stage. Adicione ou guarde antes de trocar de branch.");
      process.exit(1);
    }
    const novos = commitsParaLevar(
      gitOut(["cherry", "origin/main", headAntes]),
      gitOut(["rev-list", "--reverse", `origin/main..${headAntes}`]),
    );
    if (novos.length === 0) {
      console.error(`✖ O PR #${mergeado} já levou esta branch. Não há commit novo para outro PR.`);
      process.exit(1);
    }
    branch = nomeDaBranchSeguinte(branchInicial);
    console.error(
      `✖ A branch "${branchInicial}" já teve o PR #${mergeado} mergeado. Seguindo em ${branch}.`,
    );
    gitRun(["switch", "-c", branch, "origin/main"]);
    gitRun(["cherry-pick", ...novos]);
  }

  const head = gitOut(["rev-parse", "HEAD"]);
  if (gitOut(["rev-list", "--count", `origin/main..${head}`]) === "0") {
    console.error("✖ Nada para entregar. Coloque os arquivos no stage (git add) antes.");
    process.exit(1);
  }

  const doPr = arquivosCsharp(
    gitOut(["diff", "--name-only", "--diff-filter=ACMR", `origin/main...${head}`]),
  );
  if (!csharpier("check", doPr)) {
    console.error(
      "✖ C# fora do CSharpier nos commits da branch. Rode `dotnet csharpier format .`, commite e entregue de novo.",
    );
    process.exit(1);
  }

  gitRun(["push", "-u", "origin", "HEAD"]);
  let aberto;
  try {
    aberto = prNumero(branch, "open");
  } catch {
    console.error("✖ O push foi feito, mas o gh não abriu o PR.");
    process.exit(1);
  }
  if (aberto) {
    console.log(`PR #${aberto} já está aberto nesta branch.`);
    return;
  }
  execFileSync("gh", ["pr", "create", "--title", titulo, "--body-file", corpoArquivo], {
    stdio: "inherit",
    windowsHide: true,
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) principal();
