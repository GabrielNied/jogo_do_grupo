// Testes do comportamento sem Unity (scripts/unity/sem-unity.mjs). Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const UNITY_MJS = fileURLToPath(new URL("./unity.mjs", import.meta.url));

function rodar(args, env = {}) {
  return spawnSync(process.execPath, [UNITY_MJS, ...args], {
    encoding: "utf8",
    env: { ...process.env, UNITY_CLI: "/nao/existe/unity", UNITY_OBRIGATORIO: "", ...env },
  });
}

test("sem Unity CLI, recompilar pula com aviso e sai 0", () => {
  const resultado = rodar(["recompilar"]);

  assert.equal(resultado.status, 0);
  assert.match(resultado.stdout, /unity:recompilar pulado/);
  assert.match(resultado.stdout, /Não tente de novo/);
});

test("com UNITY_OBRIGATORIO=1, a falta de Unity é erro", () => {
  const resultado = rodar(["recompilar"], { UNITY_OBRIGATORIO: "1" });

  assert.equal(resultado.status, 2);
  assert.match(resultado.stderr, /não há Unity CLI/);
});

test("executar não tem substituto: sem Unity continua sendo erro", () => {
  const resultado = rodar(["executar", "Classe.Metodo"]);

  // 2 = sem Unity; 1 = editor aberto nesta máquina (o Windows confere antes). Nunca 0.
  assert.notEqual(resultado.status, 0);
  assert.doesNotMatch(resultado.stdout, /pulado/);
});
