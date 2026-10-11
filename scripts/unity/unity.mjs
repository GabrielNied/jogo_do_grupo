// Atalhos do Unity CLI para o projeto do client (ADR 0014).
//
//   npm run unity:recompilar              compila no editor aberto e mostra os erros reais
//   npm run unity:testes                  roda os testes EditMode do client
//   npm run unity:executar -- <Classe.Metodo>
//                                         roda o Unity em modo batch e executa um método de
//                                         editor (ex.: um script que gera prefabs ou cenas).
//                                         O editor precisa estar fechado.
//   npm run unity:importar                abre o projeto em batch, gera os .meta que faltam
//                                         e fecha. O editor precisa estar fechado.
//
// O CLI vem com o Unity Hub. Em outro lugar, defina UNITY_CLI com o caminho do executável.

import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const RAIZ = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const CLIENT = join(RAIZ, "client");

const candidatos = [
  process.env.UNITY_CLI,
  "C:/Program Files/Unity Hub/resources/unity.exe",
  "/Applications/Unity Hub.app/Contents/Resources/unity",
].filter(Boolean);
const cli = candidatos.find((c) => existsSync(c)) ?? "unity";

const [acao, ...resto] = process.argv.slice(2);
const comandos = {
  recompilar: ["recompile", "--project-path", CLIENT],
  testes: ["test", CLIENT, "--mode", "EditMode", "--output", join(tmpdir(), "jogo-do-grupo-unity-testes.xml")],
  executar: () => {
    if (!resto[0]) {
      console.error("Uso: npm run unity:executar -- <Classe.Metodo>");
      process.exit(2);
    }
    return ["run", CLIENT, "--", "-executeMethod", resto[0], "-quit"];
  },
  // -quit vai depois de `--`: é argumento do editor, não do CLI. O import grava os .meta.
  importar: ["run", CLIENT, "--", "-quit"],
};

const montar = comandos[acao];
if (!montar) {
  console.error(`Ação desconhecida: ${acao}. Use: ${Object.keys(comandos).join(", ")}.`);
  process.exit(2);
}
const args = typeof montar === "function" ? montar() : montar;
const resultado = spawnSync(cli, ["--non-interactive", "--no-banner", ...args], { stdio: "inherit" });
if (resultado.error) {
  console.error(`✖ Não consegui rodar o Unity CLI (${cli}). Instale o Unity Hub ou defina UNITY_CLI.`);
  process.exit(2);
}
process.exit(resultado.status ?? 1);
