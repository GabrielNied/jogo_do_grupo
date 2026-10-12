// Testes de scripts/agentes/preparar-nuvem.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { ambienteDoNpm, ehNuvem, outraPreparacaoAtiva } from "./preparar-nuvem.mjs";

test("reconhece a nuvem do Claude Code", () => {
  assert.equal(ehNuvem({ CLAUDE_CODE_REMOTE_ENVIRONMENT_TYPE: "cloud_default" }), true);
  assert.equal(ehNuvem({ CLAUDE_CODE_REMOTE: "true" }), true);
  assert.equal(ehNuvem({}), false);
});

test("com proxy, o npm deixa de pular o proxy para o registry", () => {
  const env = ambienteDoNpm({
    HTTPS_PROXY: "http://127.0.0.1:1",
    NO_PROXY: "localhost,registry.npmjs.org",
    no_proxy: "registry.npmjs.org",
    npm_config_noproxy: "registry.npmjs.org",
    GLOBAL_AGENT_NO_PROXY: "registry.npmjs.org",
    OUTRA: "fica",
  });
  assert.equal(env.NO_PROXY, undefined);
  assert.equal(env.no_proxy, undefined);
  assert.equal(env.GLOBAL_AGENT_NO_PROXY, undefined);
  assert.ok(!env.npm_config_noproxy.includes("registry.npmjs.org"));
  assert.equal(env.OUTRA, "fica");
});

test("sem proxy, o ambiente fica igual", () => {
  assert.deepEqual(ambienteDoNpm({ NO_PROXY: "x", A: "b" }), { NO_PROXY: "x", A: "b" });
});

test("espera só por outra preparação viva", () => {
  const vivos = new Set([200]);
  const vivo = (pid) => vivos.has(pid);
  assert.equal(outraPreparacaoAtiva("200", 100, vivo), 200);
  assert.equal(outraPreparacaoAtiva("200\n", 100, vivo), 200);
  assert.equal(outraPreparacaoAtiva("300", 100, vivo), null); // já terminou
  assert.equal(outraPreparacaoAtiva("100", 100, vivo), null); // é este processo (o filho do hook)
  assert.equal(outraPreparacaoAtiva(null, 100, vivo), null); // sem arquivo
  assert.equal(outraPreparacaoAtiva("lixo", 100, vivo), null);
  assert.equal(outraPreparacaoAtiva("0", 100, vivo), null);
});
