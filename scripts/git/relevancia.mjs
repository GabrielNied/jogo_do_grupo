// Relevância da tarefa: a doc que a issue segue mudou depois que ela foi escrita ou conferida?
// O script só aponta os commits; quem decide se a issue ainda vale é o agente
// (skill puxar-tarefa, "Conferir a relevância").

import { escopos } from "../../commitlint.config.mjs";

export const MARCA_CONFERIDA = "<!-- relevancia-conferida -->";

// Só as regras do jogo: doc de processo muda todo dia e não torna a tarefa obsoleta.
const LINK_DE_DOC = /docs\/(?:features|decisoes)\/[\w.-]+?\.md/g;

// Docs que a issue segue: a doc das funcionalidades das labels e as docs de regra citadas no corpo.
export function docsDaIssue(labels, corpo, existe) {
  const daLabel = escopos.filter((escopo) => labels.includes(escopo)).map((escopo) => `docs/features/${escopo}.md`);
  const citadas = (corpo || "").match(LINK_DE_DOC) || [];
  return [...new Set([...daLabel, ...citadas])].filter((doc) => !doc.endsWith("_template.md") && existe(doc)).sort();
}

// A partir de quando olhar: a criação da issue ou a última conferência registrada nela.
export function baseDaConferencia(criadaEm, comentarios) {
  const conferidas = (comentarios || []).filter((c) => (c.body || "").includes(MARCA_CONFERIDA)).map((c) => c.createdAt);
  return [criadaEm, ...conferidas].sort().at(-1);
}

export function comentarioConferida(nota) {
  return `${MARCA_CONFERIDA}\n🔎 **Relevância conferida** com a doc atual da \`main\`.\n\n${nota}`;
}
