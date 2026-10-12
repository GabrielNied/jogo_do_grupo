// Atalhos do Unity CLI para o projeto do client (ADR 0014).
//
//   npm run unity:recompilar              compila no editor aberto e mostra os erros reais
//   npm run unity:testes                  roda os testes EditMode do client
//   npm run unity:executar -- <Classe.Metodo>
//                                         roda o Unity em modo batch e executa um método de
//                                         editor (ex.: um script que gera prefabs ou cenas).
//                                         Espera a vez se outro Unity estiver rodando.
//   npm run unity:importar                abre o projeto em batch, gera os .meta que faltam
//                                         e fecha. Espera a vez como o executar.
//   npm run unity:editor                  só o editor do projeto (não o Hub). Sai 1 se aberto.
//
// O CLI vem com o Unity Hub. Em outro lugar, defina UNITY_CLI com o caminho do executável.

import { spawnSync } from "node:child_process";
import { editorDoProjetoAberto } from "./editor-aberto.mjs";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { pularSemUnity } from "./sem-unity.mjs";
import { esperarAVez } from "./trava.mjs";

const RAIZ = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const CLIENT = join(RAIZ, "client");

// UNITY_CLI, quando definido, é o único candidato (um caminho errado não cai em outro Unity).
const candidatos = process.env.UNITY_CLI
  ? [process.env.UNITY_CLI]
  : ["C:/Program Files/Unity Hub/resources/unity.exe", "/Applications/Unity Hub.app/Contents/Resources/unity"];
const cli = candidatos.find((c) => existsSync(c)) ?? process.env.UNITY_CLI ?? "unity";

const [acao, ...resto] = process.argv.slice(2);
const comandos = {
  recompilar: ["recompile", "--project-path", CLIENT],
  testes: ["test", CLIENT, "--mode", "EditMode", "--output", join(tmpdir(), "jogo-do-grupo-unity-testes.xml")],
  executar: () => {
    if (!resto[0]) {
      console.error("Uso: npm run unity:executar -- <Classe.Metodo>");
      process.exit(2);
    }
    return ["run", CLIENT, "--", "-executeMethod", resto[0]];
  },
  // `unity run` já abre em batch e fecha. Repassar `-quit` conflita com a flag reservada do CLI.
  importar: ["run", CLIENT],
};

const montar = comandos[acao];
if (!montar) {
  console.error(`Ação desconhecida: ${acao}. Use: ${Object.keys(comandos).join(", ")}.`);
  process.exit(2);
}
// Batch: um Unity por vez na máquina. Espera o outro comando ou o editor fechar, em vez de
// sair com erro e virar outra rodada do agente.
if (acao === "importar" || acao === "testes" || acao === "executar") {
  esperarAVez({ acao, editoresAbertos: editorDoProjetoAberto, cwd: RAIZ });
}
const args = typeof montar === "function" ? montar() : montar;
const resultado = spawnSync(cli, ["--non-interactive", "--no-banner", ...args], { stdio: "inherit" });
// Sem Unity neste ambiente (ex.: agente na nuvem): compilar, testar e importar ficam com o CI
// e com o npm run unity:meta. Gerar conteúdo (executar) não tem substituto e continua sendo erro.
if (resultado.error?.code === "ENOENT" && acao !== "executar") {
  pularSemUnity(`unity:${acao}`, `não há Unity CLI neste ambiente (${process.platform}).`);
}
if (resultado.error) {
  console.error(`✖ Não consegui rodar o Unity CLI (${cli}). Instale o Unity Hub ou defina UNITY_CLI.`);
  process.exit(2);
}
process.exit(resultado.status ?? 1);
