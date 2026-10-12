// Bilhete de retomada entre sessões do Claude Code (issue #592).
// Antes de sugerir /clear com algo pendente, a IA grava o bilhete com
// `node scripts/agentes/retomar.mjs gravar "<o que falta e o que a pessoa vai responder>"`.
// O hook SessionStart (`--hook`) mostra o bilhete na sessão nova, junto com a branch e as
// mudanças não commitadas, e o apaga. Fica em .git/ (local, fora do commit).
// Sem git ou sem bilhete, não diz nada e sai 0: nunca atrapalha a sessão.

import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const PASTA_DO_SCRIPT = dirname(fileURLToPath(import.meta.url));

function git(args) {
  return execFileSync("git", args, { cwd: PASTA_DO_SCRIPT, encoding: "utf8", stdio: ["ignore", "pipe", "ignore"], timeout: 5000 }).trim();
}

// Mesmo arquivo em qualquer worktree: o bilhete é da pasta principal do repositório.
export function caminhoDoBilhete() {
  return resolve(git(["rev-parse", "--git-common-dir"]), "claude-retomar.md");
}

const [comando, ...resto] = process.argv.slice(2);

try {
  if (comando === "gravar") {
    const texto = resto.join(" ").trim();
    if (!texto) {
      console.error('Uso: node scripts/agentes/retomar.mjs gravar "<texto>"');
      process.exit(1);
    }
    writeFileSync(caminhoDoBilhete(), `${texto}\n`, "utf8");
    console.log("Bilhete gravado. Na sessão nova (/clear), ele aparece sozinho.");
  } else if (comando === "--hook") {
    const bilhete = caminhoDoBilhete();
    if (!existsSync(bilhete)) process.exit(0);
    const texto = readFileSync(bilhete, "utf8").trim();
    rmSync(bilhete);
    const branch = git(["branch", "--show-current"]) || "(sem branch)";
    const mudancas = git(["status", "--short"]).split("\n").filter(Boolean).length;
    console.log("Bilhete de retomada da sessão anterior (a pessoa pode responder só \"sim\" ou \"continue\" a ele):");
    console.log(texto);
    console.log(`Branch atual: ${branch}; ${mudancas} arquivo(s) com mudança não commitada.`);
  }
} catch (erro) {
  // No hook, nunca atrapalha a sessão; ao gravar, a IA precisa saber que falhou.
  if (comando === "gravar") {
    console.error(`Não gravei o bilhete: ${erro.message}`);
    process.exit(1);
  }
  process.exit(0);
}
