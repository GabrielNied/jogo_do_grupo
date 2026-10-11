// Testes de scripts/git/pendencia-unity.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import {
  branchDaPendencia,
  corpoDoPrComPendencia,
  corpoPendencia,
  faltasDaPendencia,
  prDaPendencia,
  resolvidaPeloCi,
  tituloPendencia,
} from "./pendencia-unity.mjs";

test("o corpo traz PR e branch, e o puxar consegue ler de volta", () => {
  const corpo = corpoPendencia({
    pr: 150,
    branch: "feature/mundo/biomas-e-relevo",
    faltas: [".meta das texturas de bioma", "teste visual do relevo"],
  });

  assert.equal(branchDaPendencia(corpo), "feature/mundo/biomas-e-relevo");
  assert.equal(prDaPendencia(corpo), 150);
  assert.match(corpo, /- \[ \] \.meta das texturas de bioma\n- \[ \] teste visual do relevo/);
});

test("corpo sem os campos não inventa valores", () => {
  assert.equal(branchDaPendencia("Uma issue qualquer"), null);
  assert.equal(prDaPendencia("Uma issue que fala do PR #3"), null);
  assert.equal(branchDaPendencia(null), null);
  assert.equal(prDaPendencia(undefined), null);
});

test("título usa a primeira falta e corta texto longo", () => {
  assert.equal(tituloPendencia(7, ["gerar .meta"]), "[Unity] PR #7: gerar .meta");
  const longo = tituloPendencia(7, ["x".repeat(100)]);
  assert.equal(longo.length, "[Unity] PR #7: ".length + 60);
  assert.ok(longo.endsWith("..."));
});

test("o PR menciona a pendência uma vez só, sem Closes (o merge não pode fechá-la)", () => {
  const uma = corpoDoPrComPendencia("Closes #135\n", 160);

  assert.equal(uma, "Closes #135\n\nPendência de Unity: #160 (continua aberta depois do merge).\n");
  assert.equal(corpoDoPrComPendencia(uma, 160), uma);
  assert.doesNotMatch(uma, /Closes #160/);
  assert.match(corpoDoPrComPendencia(null, 3), /^\n\nPendência de Unity: #3 /);
});

const corpoCom = (...faltas) => corpoPendencia({ pr: 1, branch: "feature/x/y", faltas });

test("lê os itens da lista Falta e para no fim dela", () => {
  assert.deepEqual(faltasDaPendencia(corpoCom("compilar", "rodar os testes EditMode")), [
    "compilar",
    "rodar os testes EditMode",
  ]);
  assert.deepEqual(faltasDaPendencia("sem lista"), []);
  assert.deepEqual(faltasDaPendencia(null), []);
});

test("só compilar e testes EditMode o CI resolve", () => {
  assert.ok(resolvidaPeloCi(corpoCom("Compilar o shared no Unity (npm run unity:compilar): contrato de dia e noite")));
  assert.ok(resolvidaPeloCi(corpoCom("rodar o teste EditMode Mostrar_DisparaOEvento (worktree sem Library)")));
  assert.ok(resolvidaPeloCi(corpoCom("compilar e rodar os testes EditMode")));
});

test("o que pede gente, olho ou asset continua aberto", () => {
  assert.equal(resolvidaPeloCi(corpoCom("compilar", ".meta das texturas novas")), false);
  assert.equal(
    resolvidaPeloCi(corpoCom("rodar os testes EditMode de ProjecaoDoMapaTestes e, no Play Mode, conferir o minimapa")),
    false,
  );
  assert.equal(resolvidaPeloCi(corpoCom("unity:compilar; teste visual do Inspector")), false);
  assert.equal(resolvidaPeloCi(corpoCom("Rodar GeradorJogadorHumano.Gerar (unity:executar)")), false);
  assert.equal(resolvidaPeloCi("Issue sem lista de faltas"), false);
});
