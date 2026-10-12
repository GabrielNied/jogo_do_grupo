// Worktree novo nasce sem node_modules: o commitlint do hook de commit não existe ali e o
// primeiro commit do agente é recusado. Em vez de outro `npm install` (lento, e na nuvem só
// passa pelo proxy), o worktree aponta para o node_modules do checkout principal.

import { existsSync, lstatSync, symlinkSync } from "node:fs";
import { join, resolve } from "node:path";

/**
 * Liga `<worktree>/node_modules` ao `node_modules` de `raiz`. Devolve true se criou a ligação;
 * false se a raiz não tem node_modules ou o worktree já tem o seu.
 */
export function ligarNodeModules(raiz, worktree) {
  const origem = resolve(raiz, "node_modules");
  const destino = resolve(worktree, "node_modules");
  if (!existsSync(origem)) return false;
  try {
    lstatSync(destino);
    return false; // já existe (pasta, link ou link quebrado): não mexe
  } catch {
    // não existe: segue
  }
  // "junction" funciona no Windows sem permissão de administrador; fora dele é ignorado.
  symlinkSync(origem, destino, "junction");
  return true;
}

