// Testes de scripts/git/prioridade-lista.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { compararDisponiveis, ordemDaPrioridade, textoDaPrioridade } from "./prioridade-lista.mjs";

test("a ordem segue o número do rótulo, e quem não tem vai para o fim", () => {
  assert.equal(ordemDaPrioridade(["prio:2-core-loop", "foco"]), 2);
  assert.equal(ordemDaPrioridade(["mobs"]), 99);
});

test("o texto avisa a issue sem rótulo", () => {
  assert.equal(textoDaPrioridade(["prio:1-erros"]), "prio:1-erros");
  assert.match(textoDaPrioridade([]), /sem prio/);
});

test("a fila livre começa pela prioridade menor e, no empate, pela issue mais nova", () => {
  const itens = [
    { issue: { number: 10 }, labels: ["prio:5-features"] },
    { issue: { number: 3 }, labels: ["prio:1-erros"] },
    { issue: { number: 8 }, labels: [] },
    { issue: { number: 20 }, labels: ["prio:1-erros"] },
  ];
  itens.sort(compararDisponiveis);
  assert.deepEqual(
    itens.map((item) => item.issue.number),
    [20, 3, 10, 8],
  );
});
