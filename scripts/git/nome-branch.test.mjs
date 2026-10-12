// Testes de scripts/git/nome-branch.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { sugerirNomeBranch } from "./nome-branch.mjs";

const padraoDeBranch = /^[a-z]+\/[a-z]+\/[a-z0-9]+(-[a-z0-9]+)*$/;

test("título no padrão do commit dá tipo e escopo à branch", () => {
  const nome = sugerirNomeBranch({
    number: 592,
    title: "chore(desenvolvimento): retomar contexto depois do /clear sem colar texto",
    labels: [],
  });
  assert.match(nome, /^chore\/desenvolvimento\//);
  assert.match(nome, padraoDeBranch);
});

test("feat vira feature na branch", () => {
  assert.match(sugerirNomeBranch({ number: 1, title: "feat(itens): baú de grupo", labels: [] }), /^feature\/itens\/bau-de-grupo$/);
});

test("título solto usa a label de escopo e nunca termina em hífen", () => {
  const nome = sugerirNomeBranch({ number: 7, title: "Inventário cabe na tela e mostra a comparação", labels: [{ name: "itens" }] });
  assert.match(nome, /^feature\/itens\//);
  assert.match(nome, padraoDeBranch);
});
