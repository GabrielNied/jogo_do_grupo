// Recomendado até 3 lotes de jogo em andamento (docs/agentes.md#lote, ADR 0037 emenda 2026-10-16). Um lote é uma
// branch com uma ou mais issues, de uma ou várias funcionalidades. Core e desenvolvimento não
// ocupam vaga: não são o ciclo do marco.

import { escopos } from "../../commitlint.config.mjs";

export const LIMITE_LOTES = 3;

export const FUNCIONALIDADES_DE_JOGO = escopos.filter((escopo) => escopo !== "core" && escopo !== "desenvolvimento");

export function funcionalidadesDe(labels) {
  return FUNCIONALIDADES_DE_JOGO.filter((funcionalidade) => labels.includes(funcionalidade));
}

/**
 * `labelsDoLote`: rótulos de todas as issues do lote, juntos.
 * `emAndamento`: [{ numero, labels, branch }] das issues já em andamento (branch null se a trava não diz).
 * `branchDoLote`: a branch que o lote vai usar; juntar-se a um lote em curso não abre vaga nova.
 */
export function podePuxarLote(labelsDoLote, emAndamento, branchDoLote = null) {
  const lotes = new Set(
    emAndamento
      .filter((item) => funcionalidadesDe(item.labels).length > 0)
      .map((item) => item.branch ?? `#${item.numero}`),
  );
  const emCurso = [...lotes].sort();
  if (funcionalidadesDe(labelsDoLote).length === 0) return { ok: true, emCurso, acimaDoRecomendado: false };
  if (branchDoLote && lotes.has(branchDoLote)) return { ok: true, emCurso, acimaDoRecomendado: false };
  const acimaDoRecomendado = lotes.size >= LIMITE_LOTES;
  return { ok: true, emCurso, acimaDoRecomendado };
}
