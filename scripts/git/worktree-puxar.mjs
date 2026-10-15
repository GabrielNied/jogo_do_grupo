// Quando o `puxar` exige pasta separada (checkout principal compartilhado).

import { isAbsolute, resolve } from "node:path";

export function checkoutPrincipal(gitDir) {
  return !gitDir.replaceAll("\\", "/").includes("/worktrees/");
}

export function worktreeSeparado(worktreePath, raizAtual) {
  if (!worktreePath) return false;
  const raiz = resolve(raizAtual);
  const pasta = isAbsolute(worktreePath) ? resolve(worktreePath) : resolve(raiz, worktreePath);
  return pasta.toLowerCase() !== raiz.toLowerCase();
}

/** @returns {string|null} mensagem de erro ou null se pode puxar no checkout atual */
export function recusarPuxarNoCheckoutCompartilhado({
  checkoutPrincipal: principal,
  worktreeSeparado: separado,
  outroLoteEmAndamento,
  nomeAgente,
}) {
  if (!principal) return null;
  if (separado) return null;
  if (nomeAgente) {
    return "Agente no checkout principal: rode o puxar dentro do worktree do agente com --worktree ., ou passe --worktree <pasta> fora deste checkout.";
  }
  if (outroLoteEmAndamento) {
    return "Já há outro lote em andamento neste checkout. Passe --worktree <pasta> (ex.: .claude/worktrees/meu-lote).";
  }
  return null;
}
