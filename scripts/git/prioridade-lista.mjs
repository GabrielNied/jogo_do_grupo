// Ordem do `npm run tarefa -- listar`. A prioridade mora no rótulo prio:*
// (docs/agentes.md#prioridade-das-tarefas). Aqui ficam só as partes puras.

const AVISO_SEM_PRIORIDADE = "⚠ sem prio:* (ver docs/agentes.md#prioridade-das-tarefas)";

// Ficha escrita pelo /decida, /melhore ou coordenador (docs/agentes.md#ficha-da-tarefa).
export const MARCA_FICHA = "<!-- ficha -->";

// Rótulos dos agentes por camada, removidos no ADR 0037; o /decida troca pelo dono.
export const ROTULOS_V1 = [
  "agente:shared-contratos",
  "agente:server-funcionalidade",
  "agente:client-funcionalidade",
  "agente:regras-do-jogo",
];

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

/**
 * Issues de que esta depende: "Depende de #12", "depende da #12 e da #66", "bloqueada por #3".
 * Itens de checklist ("- [ ] #617 ... depende da #616") são a lista de um épico, não dependência dele.
 */
export function dependenciasDe(corpo) {
  const numeros = new Set();
  for (const linha of String(corpo || "").split("\n")) {
    if (/^\s*[-*]\s*\[[ xX]\]/.test(linha)) continue;
    for (const trecho of linha.matchAll(/(?:depende\s+d[aeo]s?|bloquead[ao]\s+por)\b[^\n.;)]*/gi)) {
      for (const ref of trecho[0].matchAll(/#(\d+)/g)) numeros.add(Number(ref[1]));
    }
  }
  return [...numeros];
}

/** Dependências ainda abertas, dado o conjunto de números das issues abertas. */
export function dependenciasAbertas(corpo, abertas) {
  return dependenciasDe(corpo).filter((numero) => abertas.has(numero));
}

export function temFicha(corpo) {
  return String(corpo || "").includes(MARCA_FICHA);
}

/** Avisos curtos que o /trabalhe e o /decida leem sem abrir a issue. */
export function sinaisDaIssue({ labels, corpo, abertas }) {
  const sinais = [];
  const deps = dependenciasAbertas(corpo, abertas);
  if (deps.length > 0) sinais.push(`⏳ depende de ${deps.map((n) => `#${n}`).join(", ")}`);
  if (!temFicha(corpo)) sinais.push("sem ficha");
  if (labels.some((label) => ROTULOS_V1.includes(label))) sinais.push("rótulos v1");
  return sinais;
}

/**
 * Ordem da fila livre: prio:* menor, depois quem não espera dependência, depois `foco`,
 * depois a issue mais antiga. `esperando` vem do listar (dependência aberta).
 */
export function compararDisponiveis(a, b) {
  return (
    ordemDaPrioridade(a.labels) - ordemDaPrioridade(b.labels) ||
    Number(Boolean(a.esperando)) - Number(Boolean(b.esperando)) ||
    Number(!a.labels.includes("foco")) - Number(!b.labels.includes("foco")) ||
    a.issue.number - b.issue.number
  );
}
