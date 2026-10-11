// Confere o aviso automático quando o build do server falha na main (#398).

import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";

const raiz = new URL("../../", import.meta.url);
const ler = (rel) => readFileSync(new URL(rel, raiz), "utf8");

test("workflow server abre issue core unica por quebra na main", () => {
  const workflow = ler(".github/workflows/server.yml");
  assert.match(workflow, /name: Abre issue quando a main quebra/);
  assert.match(workflow, /failure\(\) && github\.event_name == 'push'/);
  assert.match(workflow, /startswith\("\[Core\] Build falhou na main"\)/);
  assert.match(workflow, /labels\[\]=core/);
});

test("docs git explicam quem conserta e atualizacao de branch", () => {
  const doc = ler("docs/git.md");
  assert.match(doc, /## Main com build vermelho/);
  assert.match(doc, /Update branch/);
  assert.match(doc, /\[Core\] Build falhou na main/);
});
