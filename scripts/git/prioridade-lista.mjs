// Ordem do `npm run tarefa -- listar`. A prioridade mora no rótulo prio:*
// (docs/agentes.md#prioridade-das-tarefas). Aqui ficam só as partes puras.

const AVISO_SEM_PRIORIDADE = "⚠ sem prio:* (ver docs/agentes.md#prioridade-das-tarefas)";

export function rotuloDePrioridade(labels) {
  return labels.find((label) => label.startsWith("prio:")) ?? null;
}

export function ordemDaPrioridade(labels) {
  const rotulo = rotuloDePrioridade(labels);
  const numero = rotulo ? Number(rotulo.charAt(5)) : NaN;
  return Number.isFinite(numero) ? numero : 99;
}

export function textoDaPrioridade(labels) {
  return rotuloDePrioridade(labels) ?? AVISO_SEM_PRIORIDADE;
}

export function compararDisponiveis(a, b) {
  return ordemDaPrioridade(a.labels) - ordemDaPrioridade(b.labels) || b.issue.number - a.issue.number;
}
