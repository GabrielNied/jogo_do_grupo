// Pendência de Unity: o que um agente sem Unity (ex.: na nuvem) não conseguiu gerar ou
// validar num PR. O PR segue normal (pode ir para merge); o que faltou vira uma issue com a
// label `pendente-unity`, que continua aberta depois do merge. O próximo agente numa máquina
// com Unity puxa (`npm run tarefa -- puxar <N>`): se o PR ainda está aberto, trabalha na
// branch dele; se já entrou na main, numa branch nova, cujo PR fecha a pendência (ADR 0019).
//
// Aqui ficam só as partes puras, testadas em pendencia-unity.test.mjs. Os comandos estão em
// tarefas.mjs.

export const LABEL_PENDENCIA = "pendente-unity";

export function tituloPendencia(pr, faltas) {
  const resumo = faltas[0].length > 60 ? `${faltas[0].slice(0, 57)}...` : faltas[0];
  return `[Unity] PR #${pr}: ${resumo}`;
}

/** As linhas "**PR:**" e "**Branch:**" são lidas de volta quando alguém puxa a pendência. */
export function corpoPendencia({ pr, branch, faltas }) {
  return [
    "Pendência de Unity. Um agente sem Unity abriu o PR abaixo e não conseguiu fazer o que está na lista.",
    "Esta issue continua aberta depois do merge do PR, até alguém com Unity resolver.",
    "",
    `**PR:** #${pr}`,
    `**Branch:** \`${branch}\``,
    "",
    "**Falta:**",
    ...faltas.map((falta) => `- [ ] ${falta}`),
    "",
    "**Para quem tem Unity na máquina:**",
    "",
    "1. `npm run tarefa -- puxar <este número>`. Com o PR aberto, ele abre a branch do PR; com o PR já na main, cria uma branch nova a partir da main.",
    "2. Com o editor fechado: `npm run unity:meta`, `npm run unity:importar`, `npm run unity:compilar` e `npm run unity:testes`, mais o que a lista acima pedir. Corrija o que quebrar.",
    "3. `npm run conferir-unity`, commit e push.",
    `4. Na branch do PR: comente no PR #${pr} o que foi feito e acrescente \`Closes <este número>\` ao corpo dele. Em branch nova: abra um PR com \`Closes <este número>\`.`,
  ].join("\n");
}

export function branchDaPendencia(corpo) {
  const achado = /^\*\*Branch:\*\* `([^`]+)`/m.exec(corpo ?? "");
  return achado ? achado[1] : null;
}

export function prDaPendencia(corpo) {
  const achado = /^\*\*PR:\*\* #(\d+)/m.exec(corpo ?? "");
  return achado ? Number(achado[1]) : null;
}

/**
 * Menciona a pendência no corpo do PR, sem repetir. Não usa "Closes": o PR pode ir para merge
 * antes de a pendência ser resolvida, e ela precisa continuar aberta.
 */
export function corpoDoPrComPendencia(corpoAtual, numero) {
  const corpo = corpoAtual ?? "";
  const linha = `Pendência de Unity: #${numero} (continua aberta depois do merge).`;
  if (corpo.includes(linha)) return corpo;
  return `${corpo.trimEnd()}\n\n${linha}\n`;
}

/** Itens da lista "**Falta:**" do corpo da pendência (marcados ou não). */
export function faltasDaPendencia(corpo) {
  const linhas = (corpo ?? "").split(/\r?\n/);
  const inicio = linhas.findIndex((linha) => linha.trim() === "**Falta:**");
  if (inicio < 0) return [];
  const faltas = [];
  for (const linha of linhas.slice(inicio + 1)) {
    const item = /^\s*- \[[ xX]\] (.+)$/.exec(linha);
    if (item) faltas.push(item[1].trim());
    else if (linha.trim() !== "") break;
  }
  return faltas;
}

// O CI "Client (Unity)" compila todos os assemblies e roda todos os testes EditMode. O que
// pede outra coisa (Play Mode, olhar, gerar .meta/assets, rodar gerador) continua com gente.
const PEDE_SO_O_CI = /compil|edit ?mode|unity:testes|\btestes?\b/i;
const PEDE_GENTE =
  /play|visual|olhar|conferir no|ver no|inspector|\.meta|\bmeta\b|importar|unity:executar|gerador|gerar|prefab|cena|textura|material|modelo|blender/i;

/**
 * A pendência só pede o que o CI "Client (Unity)" já faz na main (compilar e rodar os testes
 * EditMode)? Então uma execução verde depois do merge do PR a resolve.
 */
export function resolvidaPeloCi(corpo) {
  const faltas = faltasDaPendencia(corpo);
  return (
    faltas.length > 0 &&
    faltas.every((falta) => PEDE_SO_O_CI.test(falta) && !PEDE_GENTE.test(falta))
  );
}
