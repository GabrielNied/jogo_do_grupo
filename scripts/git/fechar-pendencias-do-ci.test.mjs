// O job verde na main chama fechar-pendencias-do-ci.mjs, que lê o PR da pendência.
// Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";

const workflow = readFileSync(new URL("../../.github/workflows/client.yml", import.meta.url), "utf8");

test("o workflow lê pull requests para fechar pendências sem marcar o client como quebrado", () => {
  assert.match(workflow, /pull-requests: read/);
  assert.match(workflow, /fechar-pendencias-do-ci\.mjs/);
  assert.match(workflow, /id: testes-editmode/);
  assert.match(
    workflow,
    /failure\(\) && github\.event_name == 'push' && steps\.testes-editmode\.outcome == 'failure'/,
  );
});
