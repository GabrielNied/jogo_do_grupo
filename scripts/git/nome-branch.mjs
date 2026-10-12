// Sugestão de nome de branch para uma issue, usada pelo `npm run tarefa -- puxar`.

import { escopos, tipos } from "../../commitlint.config.mjs";

export function gerarSlug(texto) {
  return texto
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/\[.*?\]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .slice(0, 30)
    .replace(/^-+|-+$/g, "");
}

export function sugerirNomeBranch(issue) {
  // Título já no padrão do commit ("chore(desenvolvimento): ..."): tipo e escopo vêm dele.
  const padrao = /^(\w+)\(([\w-]+)\):\s*(.+)$/.exec(issue.title);
  if (padrao && tipos.includes(padrao[1]) && escopos.includes(padrao[2])) {
    const tipo = padrao[1] === "feat" ? "feature" : padrao[1];
    return `${tipo}/${padrao[2]}/${gerarSlug(padrao[3]) || `task-${issue.number}`}`;
  }

  const labels = (issue.labels || []).map((l) => (typeof l === "string" ? l : l.name));
  let escopoEncontrado = "core";

  for (const escopo of escopos) {
    if (labels.includes(escopo)) {
      escopoEncontrado = escopo;
      break;
    }
  }

  const slug = gerarSlug(issue.title) || `task-${issue.number}`;
  return `feature/${escopoEncontrado}/${slug}`;
}
