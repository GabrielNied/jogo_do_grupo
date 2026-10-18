// Testes de scripts/git/reempilhar.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import {
  corpoComDependenciaTrocada,
  corpoReempilhado,
  dependenciasDoCorpo,
  git,
  repoDaUrl,
} from "./reempilhar.mjs";

test("comando Git com saída herdada conclui sem falso conflito", () => {
  assert.match(git(["--version"]), /git version/);
  assert.equal(git(["--version"], { stdio: "inherit" }), "");
});

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

test("reempilhar de novo não acumula avisos de substituição", () => {
  const corpo = corpoReempilhado("Substitui o #1077, refeito sobre a main.\n\nDepende de #1082.\n\nCloses #1", 1083, []);
  assert.match(corpo, /^Substitui o #1083/);
  assert.doesNotMatch(corpo, /#1077/);
});

test("o PR de cima passa a depender do PR novo", () => {
  assert.equal(
    corpoComDependenciaTrocada("Depende de #1083 (mergear antes)\n\nVer #1083 no texto", 1083, 1085),
    "Depende de #1085 (mergear antes)\n\nVer #1083 no texto",
  );
  assert.equal(corpoComDependenciaTrocada("Depende de #10830", 1083, 1085), null);
  assert.equal(corpoComDependenciaTrocada("Closes #1083", 1083, 1085), null);
});

test("lê dono/repo do remoto origin", () => {
  assert.equal(repoDaUrl("https://github.com/GabrielNied/jogo_do_grupo"), "GabrielNied/jogo_do_grupo");
  assert.equal(repoDaUrl("git@github.com:GabrielNied/jogo_do_grupo.git"), "GabrielNied/jogo_do_grupo");
  assert.throws(() => repoDaUrl("https://gitlab.com/x/y"));
});
