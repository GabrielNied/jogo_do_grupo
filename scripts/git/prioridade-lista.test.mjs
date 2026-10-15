// Testes de scripts/git/prioridade-lista.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import {
  compararDisponiveis,
  dependenciasAbertas,
  dependenciasDe,
  ordemDaPrioridade,
  sinaisDaIssue,
  textoDaPrioridade,
} from "./prioridade-lista.mjs";

test("a ordem segue o número do rótulo, e quem não tem vai para o fim", () => {
  assert.equal(ordemDaPrioridade(["prio:2-core-loop", "foco"]), 2);
  assert.equal(ordemDaPrioridade(["mobs"]), 99);
});

test("o texto avisa a issue sem rótulo", () => {
  assert.equal(textoDaPrioridade(["prio:1-erros"]), "prio:1-erros");
  assert.match(textoDaPrioridade([]), /sem prio/);
});

test("na mesma faixa vem o foco, depois a issue mais antiga; quem espera dependência vai para o fim da faixa", () => {
  const itens = [
    { issue: { number: 10 }, labels: ["prio:5-features"] },
    { issue: { number: 3 }, labels: ["prio:1-erros"] },
    { issue: { number: 8 }, labels: [] },
    { issue: { number: 20 }, labels: ["prio:1-erros", "foco"] },
    { issue: { number: 2 }, labels: ["prio:1-erros", "foco"], esperando: true },
    { issue: { number: 5 }, labels: ["prio:1-erros"] },
  ];
  itens.sort(compararDisponiveis);
  assert.deepEqual(
    itens.map((item) => item.issue.number),
    [20, 3, 5, 2, 10, 8],
  );
});

test("lê as dependências do corpo, sem confundir com a checklist de um épico", () => {
  const corpo = [
    "**Depende de:** #12 e #66",
    "Esta fica bloqueada por #3.",
    "- [ ] #617 Materiais (depende da #616)",
    "Cita a #99 sem depender dela.",
  ].join("\n");
  assert.deepEqual(dependenciasDe(corpo).sort((a, b) => a - b), [3, 12, 66]);
  assert.deepEqual(dependenciasAbertas(corpo, new Set([66, 99])), [66]);
});

test("os sinais apontam dependência aberta, falta de ficha e rótulo v1", () => {
  const sinais = sinaisDaIssue({
    labels: ["agente:server-funcionalidade"],
    corpo: "Depende de #4",
    abertas: new Set([4]),
  });
  assert.deepEqual(sinais, ["⏳ depende de #4", "sem ficha", "rótulos v1"]);
  assert.deepEqual(sinaisDaIssue({ labels: [], corpo: "<!-- ficha -->", abertas: new Set() }), []);
});
