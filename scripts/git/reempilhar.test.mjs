// Testes de scripts/git/reempilhar.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import {
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

test("depois de reempilhar, a linha Depende de cita só as levas abertas", () => {
  const corpo = corpoReempilhado("Depende de #597 e #607.\n\nCloses #475\n", [607]);
  assert.match(corpo, /^Depende de #607 \(mergear antes\)/);
  assert.doesNotMatch(corpo, /#597/);
  assert.match(corpo, /Closes #475/);
});

test("sem levas abertas, a linha Depende de some e o resto fica", () => {
  assert.equal(corpoReempilhado("Depende de #597.\n\nCloses #1", []), "Closes #1");
  assert.equal(corpoReempilhado("Closes #1", []), "Closes #1");
});

test("lê dono/repo do remoto origin", () => {
  assert.equal(repoDaUrl("https://github.com/GabrielNied/jogo_do_grupo"), "GabrielNied/jogo_do_grupo");
  assert.equal(repoDaUrl("git@github.com:GabrielNied/jogo_do_grupo.git"), "GabrielNied/jogo_do_grupo");
  assert.throws(() => repoDaUrl("https://gitlab.com/x/y"));
});
