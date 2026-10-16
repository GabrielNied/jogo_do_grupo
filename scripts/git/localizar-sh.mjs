// Localiza o executável sh e utilitários POSIX do Git for Windows quando rodando no PowerShell/cmd.
// No Linux/macOS ou Git Bash, usa o "sh" do ambiente diretamente.

import { execSync } from "node:child_process";
import { existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";

/**
 * Retorna caminhos padrão prováveis de instalação do Git for Windows.
 */
function caminhosPadraoGit(env = process.env) {
  const lista = ["C:/Program Files/Git", "C:/Program Files (x86)/Git"];
  if (env.LOCALAPPDATA) {
    lista.push(join(env.LOCALAPPDATA, "Programs/Git"));
  }
  return lista;
}

/**
 * Tenta descobrir a raiz do Git a partir de `git --exec-path`.
 * Exemplo: "C:/Program Files/Git/mingw64/libexec/git-core" -> "C:/Program Files/Git"
 */
export function raizDoGit(execPath) {
  if (!execPath) {
    try {
      execPath = execSync("git --exec-path", { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] }).trim();
    } catch {
      return null;
    }
  }
  if (!execPath) return null;
  const normalizado = execPath.replaceAll("\\", "/");
  const idx = normalizado.toLowerCase().indexOf("/libexec/git-core");
  if (idx !== -1) {
    // Sobe a partir de mingw64/libexec/git-core ou usr/libexec/git-core
    const antesLibexec = normalizado.slice(0, idx);
    return resolve(antesLibexec, "..");
  }
  return resolve(normalizado, "../../..");
}

/**
 * Localiza os diretórios do Git que contêm ferramentas POSIX (usr/bin e bin).
 */
export function encontrarDiretoriosGit(execPath, env = process.env) {
  const raizes = [];
  const raizDetectada = raizDoGit(execPath);
  if (raizDetectada) raizes.push(raizDetectada);
  raizes.push(...caminhosPadraoGit(env));

  const dirs = [];
  for (const r of raizes) {
    const usrBin = join(r, "usr/bin");
    const bin = join(r, "bin");
    if (existsSync(usrBin) && !dirs.includes(usrBin)) dirs.push(usrBin);
    if (existsSync(bin) && !dirs.includes(bin)) dirs.push(bin);
  }
  return dirs;
}

/**
 * Retorna o executável do sh para execução de scripts.
 * No Windows, devolve o caminho absoluto para o sh.exe se encontrado.
 */
export function obterExecutavelSh(opts = {}) {
  const plataforma = opts.platform ?? process.platform;
  const env = opts.env ?? process.env;

  if (env.GIT_SH && existsSync(env.GIT_SH)) {
    return env.GIT_SH;
  }

  if (plataforma !== "win32") {
    return "sh";
  }

  const dirs = encontrarDiretoriosGit(opts.execPath, env);
  for (const d of dirs) {
    const candidato = join(d, "sh.exe");
    if (existsSync(candidato)) {
      return candidato;
    }
  }

  return "sh";
}
