// Testes de scripts/git/reempilhar.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { corpoReempilhado, dependenciasDoCorpo } from "./reempilhar.mjs";

test("lê os PRs da linha Depende de", () => {
  assert.deepEqual(dependenciasDoCorpo("**Depende de #597 e #607 (mergear nessa ordem)**\n\nCloses #1"), [597, 607]);
  assert.deepEqual(dependenciasDoCorpo("Closes #1"), []);
});

test("o corpo novo cita o PR antigo e só as levas ainda abertas", () => {
  const corpo = corpoReempilhado("Depende de #597 e #607.\n\nCloses #475\n", 610, [607]);
  assert.match(corpo, /^Substitui o #610/);
  assert.match(corpo, /Depende de #607 \(mergear antes\)/);
  assert.doesNotMatch(corpo, /#597/);
  assert.match(corpo, /Closes #475/);
});

test("sem levas abertas, a linha Depende de some", () => {
  assert.doesNotMatch(corpoReempilhado("Depende de #597.\nCloses #1", 602, []), /Depende/);
});
