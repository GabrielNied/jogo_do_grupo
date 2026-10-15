// Testes de scripts/git/revisar.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { linhaDeRevisao, ordenarParaRevisar } from "./revisar.mjs";

test("a conferência mais antiga vem primeiro; no empate, a issue mais antiga", () => {
  const itens = [
    { issue: { number: 9 }, desde: "2026-10-13T00:00:00Z" },
    { issue: { number: 7 }, desde: "2026-10-11T00:00:00Z" },
    { issue: { number: 3 }, desde: "2026-10-11T00:00:00Z" },
  ];
  assert.deepEqual(
    ordenarParaRevisar(itens).map((item) => item.issue.number),
    [3, 7, 9],
  );
});

test("a linha diz a idade, a prioridade e o que falta", () => {
  const linha = linhaDeRevisao({
    issue: { number: 66, title: "[Mundo] Dungeon", body: "Depende de #425" },
    labels: ["prio:2-core-loop", "agente:client-funcionalidade", "decisao"],
    desde: "2026-10-11T00:00:00Z",
    commitsDoc: 3,
    abertas: new Set([425]),
    agoraMs: Date.parse("2026-10-14T12:00:00Z"),
  });
  assert.equal(
    linha,
    "#66  3d  prio:2  ⏳ depende de #425 · sem ficha · rótulos v1 · doc mudou 3x · decisao  [Mundo] Dungeon",
  );
});

test("issue fichada e em dia sai como ok", () => {
  const linha = linhaDeRevisao({
    issue: { number: 1, title: "x", body: "<!-- ficha -->" },
    labels: [],
    desde: "2026-10-14T00:00:00Z",
    abertas: new Set(),
    agoraMs: Date.parse("2026-10-14T01:00:00Z"),
  });
  assert.equal(linha, "#1  0d  sem prio  ok  x");
});
