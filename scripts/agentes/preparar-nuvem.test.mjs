// Testes de scripts/agentes/preparar-nuvem.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { ambienteDoNpm, ehNuvem } from "./preparar-nuvem.mjs";

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
