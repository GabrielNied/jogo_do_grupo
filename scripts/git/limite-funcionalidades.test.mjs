import assert from "node:assert/strict";
import { test } from "node:test";
import { podePuxarLote } from "./limite-funcionalidades.mjs";

// Três lotes: um deles (feature/mobs/lote) tem duas issues de funcionalidades diferentes.
const tres = [
  { numero: 1, labels: ["mobs"], branch: "feature/mobs/lote" },
  { numero: 2, labels: ["combate"], branch: "feature/mobs/lote" },
  { numero: 3, labels: ["itens"], branch: "feature/itens/bau" },
  { numero: 4, labels: ["personagem"], branch: null },
];

test("o quarto lote de jogo é recusado", () => {
  const decisao = podePuxarLote(["social"], tres, "feature/social/amigos");
  assert.equal(decisao.ok, false);
  assert.deepEqual(decisao.emCurso, ["#4", "feature/itens/bau", "feature/mobs/lote"]);
});

test("várias funcionalidades num lote só ocupam uma vaga", () => {
  const dois = tres.slice(0, 3);
  assert.equal(podePuxarLote(["social", "economia", "mundo"], dois, "feature/social/x").ok, true);
});

test("juntar-se a um lote em curso não abre vaga nova", () => {
  assert.equal(podePuxarLote(["mundo"], tres, "feature/mobs/lote").ok, true);
});

test("core e desenvolvimento não ocupam vaga", () => {
  assert.equal(podePuxarLote(["core"], tres).ok, true);
  assert.equal(podePuxarLote(["desenvolvimento"], tres).ok, true);
  const comCore = [...tres.slice(0, 2), { numero: 9, labels: ["core"], branch: "fix/core/x" }];
  assert.equal(podePuxarLote(["social"], comCore, "feature/social/y").ok, true);
});
