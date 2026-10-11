// O que fazer quando o ambiente não tem Unity (agente na nuvem, máquina sem o editor).
//
// Compilar e testar o client sem Unity é impossível, e tentar de novo só gasta rodada de
// agente. Então o comando sai 0 com um aviso claro, e quem confere é o CI: o workflow
// "Client (Unity)" compila e roda os testes EditMode em todo PR que mexe em client/ ou shared/.
//
// UNITY_OBRIGATORIO=1 faz o comando falhar (sai 2) em vez de pular.

import { existsSync } from "node:fs";

/** Onde o Unity Hub instala os editores. */
export const PASTA_DOS_EDITORES =
  process.platform === "darwin" ? "/Applications/Unity/Hub/Editor" : "C:/Program Files/Unity/Hub/Editor";

/** Há algum Unity nesta máquina (pelo Hub ou por UNITY_EDITOR_DATA)? */
export function temUnityNaMaquina() {
  return Boolean(process.env.UNITY_EDITOR_DATA) || existsSync(PASTA_DOS_EDITORES);
}

export function pularSemUnity(acao, motivo) {
  if (process.env.UNITY_OBRIGATORIO === "1") {
    console.error(`✖ ${acao}: ${motivo}`);
    process.exit(2);
  }
  console.log(`⏭ ${acao} pulado: ${motivo}`);
  console.log('  Não tente de novo nem contorne. O CI ("Client (Unity)") compila e testa o client no PR.');
  console.log("  .meta de script, pasta, .asmdef e estilo saem sem Unity: npm run unity:meta");
  console.log('  O que só o Unity faz vira pendência: npm run tarefa -- pendencia-unity <pr> --falta "..."');
  process.exit(0);
}
