import assert from "node:assert/strict";
import { test } from "node:test";
import {
  corpoRegistro,
  escopoDoTitulo,
  labelSegure,
  labelsSegureParaIssue,
  parseRegistro,
  recusaPuxarSegure,
} from "./segure.mjs";

test("escopoDoTitulo usa o prefixo da funcionalidade", () => {
  assert.equal(escopoDoTitulo("[Mundo] Chão e água"), "mundo");
  assert.equal(escopoDoTitulo("[Personagem] Retorno"), "personagem");
  assert.equal(escopoDoTitulo("[#501] subtarefa"), null);
  assert.equal(escopoDoTitulo("sem prefixo"), null);
});

test("parseRegistro e corpoRegistro são simétricos", () => {
  const blocos = { personagem: { dono: "GabrielNied", desde: "2026-03-15T12:00:00.000Z" } };
  const corpo = corpoRegistro(blocos);
  assert.deepEqual(parseRegistro(corpo), blocos);
});

test("recusa puxar quando o bloco é de outro dono", () => {
  const blocos = { itens: { dono: "GabrielNied", desde: "2026-03-15T12:00:00.000Z" } };
  assert.match(
    recusaPuxarSegure({
      login: "outro",
      titulo: "[Itens] Baú",
      labels: ["itens"],
      blocos,
    }),
    /reservado para @GabrielNied/,
  );
  assert.equal(
    recusaPuxarSegure({
      login: "GabrielNied",
      titulo: "[Itens] Baú",
      labels: [],
      blocos,
    }),
    null,
  );
});

test("labelsSegureParaIssue quando há bloco ativo", () => {
  const blocos = { combate: { dono: "GabrielNied", desde: "2026-03-15T12:00:00.000Z" } };
  assert.deepEqual(
    labelsSegureParaIssue({ titulo: "[Combate] Poção", labels: ["combate"], blocos }),
    [labelSegure("combate")],
  );
  assert.deepEqual(
    labelsSegureParaIssue({
      titulo: "[Combate] Poção",
      labels: [labelSegure("combate")],
      blocos,
    }),
    [],
  );
});
