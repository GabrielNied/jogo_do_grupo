import assert from "node:assert/strict";
import { test } from "node:test";
import { podePuxarFuncionalidade } from "./limite-funcionalidades.mjs";

const tres = [["mobs"], ["itens"], ["personagem"]];

test("a quarta funcionalidade de jogo é recusada", () => {
  const decisao = podePuxarFuncionalidade(["combate"], tres);
  assert.equal(decisao.ok, false);
  assert.deepEqual(decisao.novas, ["combate"]);
  assert.deepEqual(decisao.emCurso, ["itens", "mobs", "personagem"]);
});

test("outra issue da mesma funcionalidade ainda cabe", () => {
  assert.equal(podePuxarFuncionalidade(["mobs", "foco"], tres).ok, true);
});

test("core e desenvolvimento não ocupam vaga", () => {
  assert.equal(podePuxarFuncionalidade(["core"], tres).ok, true);
  assert.equal(podePuxarFuncionalidade(["desenvolvimento"], tres).ok, true);
});

test("com vaga livre, uma funcionalidade nova entra", () => {
  assert.equal(podePuxarFuncionalidade(["combate"], [["mobs"], ["itens"]]).ok, true);
});
