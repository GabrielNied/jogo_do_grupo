// No máximo 3 funcionalidades de jogo em andamento (docs/agentes.md).
// Core e desenvolvimento não ocupam vaga: não são o ciclo do marco.

import { escopos } from "../../commitlint.config.mjs";

export const LIMITE_FUNCIONALIDADES = 3;

export const FUNCIONALIDADES_DE_JOGO = escopos.filter((escopo) => escopo !== "core" && escopo !== "desenvolvimento");

export function funcionalidadesDe(labels) {
  return FUNCIONALIDADES_DE_JOGO.filter((funcionalidade) => labels.includes(funcionalidade));
}

export function podePuxarFuncionalidade(labelsDaIssue, gruposEmAndamento) {
  const desta = funcionalidadesDe(labelsDaIssue);
  if (desta.length === 0) return { ok: true, emCurso: [], novas: [] };

  const emCurso = new Set(gruposEmAndamento.flatMap((labels) => funcionalidadesDe(labels)));
  const novas = desta.filter((funcionalidade) => !emCurso.has(funcionalidade));
  const lista = [...emCurso].sort();
  if (emCurso.size + novas.length <= LIMITE_FUNCIONALIDADES) return { ok: true, emCurso: lista, novas };

  return { ok: false, emCurso: lista, novas };
}
