// Corpo das subtarefas (`npm run tarefa -- subtarefa`). Aqui ficam só as partes puras, testadas
// em subtarefa.test.mjs.
//
// Uma subtarefa só com "Subtarefa de #N" obrigava o agente a reler a issue pai, a doc e o
// contrato para descobrir o que fazer (lição de 2026-10-11). O corpo já aponta onde ler e onde
// mexer, e leva o recado do coordenador (--contexto).

/** Pasta da funcionalidade a partir do escopo do commitlint ("itens" -> "Itens"). */
export function pastaDaFuncionalidade(escopo, pastasExistentes = []) {
  const achada = pastasExistentes.find((p) => p.toLowerCase() === String(escopo).toLowerCase());
  if (achada) return achada;
  const texto = String(escopo);
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

/** Onde o papel mexe, para a funcionalidade `pasta`. */
export function pastasDoPapel(papel, pasta) {
  const server = `server/src/JogoDoGrupo.Server/Features/${pasta}/`;
  const client = `client/Assets/_Game/Features/${pasta}/`;
  const shared = `shared/Package/Runtime/Features/${pasta}/`;
  switch (papel) {
    case "funcionalidade":
    case "itens":
    case "economia":
      // Dono da fatia vertical (ADR 0030): as três partes, os testes e a doc.
      return [shared, server, client, "tests/", `docs/features/${pasta.toLowerCase()}.md`];
    case "designer-docs":
      return [`docs/features/${pasta.toLowerCase()}.md`];
    case "client-core":
      return ["client/Assets/_Game/Core/"];
    case "server-core":
      return ["server/src/JogoDoGrupo.Server/Core/"];
    default:
      return [shared, server, client];
  }
}

export function corpoSubtarefa({ pai, tituloPai, papel, escopo, pastas = [], contexto = "" }) {
  const pasta = pastaDaFuncionalidade(escopo, pastas);
  const linhas = [
    `Subtarefa de #${pai} (${tituloPai})`,
    "",
    `**Papel:** ${papel || "geral"}`,
    `**Escopo:** ${escopo}`,
    "",
  ];
  if (contexto.trim()) {
    linhas.push("**O que fazer:**", "", contexto.trim(), "");
  }
  linhas.push(
    "**Onde ler (só isto, antes de abrir mais nada):**",
    `- a ficha da #${pai} (objetivo, testes esperados, fora do escopo);`,
    `- \`docs/features/${pasta.toLowerCase()}.md\`, seção da #${pai};`,
    `- o contrato em \`shared/Package/Runtime/Features/${pasta}/\`.`,
    "",
    "**Onde mexer:**",
    ...pastasDoPapel(papel, pasta).map((p) => `- \`${p}\``),
    "",
    `Parent: #${pai}`,
  );
  return linhas.join("\n");
}
