// Reserva manual de fila por escopo (prefixo [Personagem], etc.). Lógica pura; o GitHub fica em tarefas.mjs.

import { escopos } from "../../commitlint.config.mjs";

export const MARCADOR_REGISTRO = "<!-- segure-registro-v1 -->";
export const LABEL_REGISTRO = "segure-registro";
export const TITULO_REGISTRO = "[Desenvolvimento] Registro segure";

export function normalizarEscopo(texto) {
  if (!texto) return null;
  const chave = texto.trim().toLowerCase();
  return escopos.includes(chave) ? chave : null;
}

/** Escopo a partir do prefixo do título, ex. `[Mundo] …` → `mundo`. Ignora `[#123]` de subtarefa. */
export function escopoDoTitulo(titulo) {
  const match = titulo.match(/^\[([^\]#][^\]]*)\]/);
  if (!match) return null;
  return normalizarEscopo(match[1]);
}

export function labelSegure(escopo) {
  return `segure:${escopo}`;
}

export function escopoDoLabelSegure(nomeLabel) {
  if (!nomeLabel.startsWith("segure:")) return null;
  return normalizarEscopo(nomeLabel.slice("segure:".length));
}

export function parseRegistro(corpo) {
  if (!corpo?.includes(MARCADOR_REGISTRO)) return {};
  const match = corpo.match(/```json\s*([\s\S]*?)\s*```/);
  if (!match) return {};
  try {
    const bruto = JSON.parse(match[1]);
    if (!bruto || typeof bruto !== "object" || Array.isArray(bruto)) return {};
    const blocos = {};
    for (const [chave, valor] of Object.entries(bruto)) {
      const escopo = normalizarEscopo(chave);
      if (!escopo || !valor || typeof valor !== "object") continue;
      const dono = typeof valor.dono === "string" ? valor.dono.trim() : "";
      const desde = typeof valor.desde === "string" ? valor.desde.trim() : "";
      if (dono) blocos[escopo] = { dono, desde: desde || new Date(0).toISOString() };
    }
    return blocos;
  } catch {
    return {};
  }
}

export function corpoRegistro(blocos) {
  const ordenado = Object.fromEntries(
    Object.keys(blocos)
      .sort()
      .map((escopo) => [escopo, blocos[escopo]]),
  );
  return [
    "Registro automático do comando `npm run tarefa -- segure`. Não edite à mão; use `segure liberar`.",
    "",
    MARCADOR_REGISTRO,
    "```json",
    JSON.stringify(ordenado, null, 2),
    "```",
  ].join("\n");
}

/** Escopo da issue para segure: prefixo do título, label `segure:*` ou label da funcionalidade. */
export function escopoDaIssue(titulo, labels) {
  const doTitulo = escopoDoTitulo(titulo);
  if (doTitulo) return doTitulo;
  for (const label of labels) {
    const doSegure = escopoDoLabelSegure(label);
    if (doSegure) return doSegure;
  }
  return escopos.find((escopo) => labels.includes(escopo)) ?? null;
}

export function blocoAtivoParaIssue({ titulo, labels, blocos }) {
  const escopo = escopoDaIssue(titulo, labels);
  if (!escopo || !blocos[escopo]) return null;
  return { escopo, ...blocos[escopo] };
}

export function recusaPuxarSegure({ login, titulo, labels, blocos }) {
  const ativo = blocoAtivoParaIssue({ titulo, labels, blocos });
  if (!ativo) return null;
  if (login === ativo.dono) return null;
  return `# segure:${ativo.escopo} — reservado para @${ativo.dono} (desde ${ativo.desde.slice(0, 10)}). Só quem fez o segure pode puxar.`;
}

export function labelsSegureParaIssue({ titulo, escopoExplicito, labels, blocos }) {
  const escopo = escopoExplicito ? normalizarEscopo(escopoExplicito) : escopoDaIssue(titulo, labels);
  if (!escopo || !blocos[escopo]) return [];
  if (labels.includes(labelSegure(escopo))) return [];
  return [labelSegure(escopo)];
}

export function deveRotularSegure({ titulo, labels, blocos, emAndamento }) {
  if (emAndamento) return false;
  const extras = labelsSegureParaIssue({ titulo, labels, blocos });
  return extras.length > 0;
}
