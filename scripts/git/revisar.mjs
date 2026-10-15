// `npm run tarefa -- revisar [N]`: a fila de conferência do /decida. As tarefas livres há mais
// tempo sem conferência vêm primeiro, com os sinais que dizem o que fazer sem abrir a issue.

import { sinaisDaIssue, textoDaPrioridade } from "./prioridade-lista.mjs";

export const REVISAR_PADRAO = 15;

const DIA_MS = 86_400_000;

/** Da conferência mais antiga para a mais nova; no empate, a issue mais antiga. */
export function ordenarParaRevisar(itens) {
  return [...itens].sort((a, b) => a.desde.localeCompare(b.desde) || a.issue.number - b.issue.number);
}

export function linhaDeRevisao({ issue, labels, desde, commitsDoc = 0, abertas, agoraMs }) {
  const dias = Math.max(0, Math.floor((agoraMs - Date.parse(desde)) / DIA_MS));
  const sinais = sinaisDaIssue({ labels, corpo: issue.body, abertas });
  if (commitsDoc > 0) sinais.push(`doc mudou ${commitsDoc}x`);
  for (const label of ["bloqueada", "decisao", "pessoa"]) if (labels.includes(label)) sinais.push(label);
  const prio = textoDaPrioridade(labels).startsWith("prio:") ? textoDaPrioridade(labels).slice(0, 6) : "sem prio";
  return `#${issue.number}  ${dias}d  ${prio}  ${sinais.join(" · ") || "ok"}  ${issue.title}`;
}
