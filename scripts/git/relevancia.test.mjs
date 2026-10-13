import assert from "node:assert/strict";
import { test } from "node:test";
import { MARCA_CONFERIDA, baseDaConferencia, comentarioConferida, docsDaIssue } from "./relevancia.mjs";

const existe = (doc) => doc !== "docs/features/core.md";

test("junta a doc da label com as docs citadas no corpo, sem repetir", () => {
  const corpo = "Ver [regra](docs/features/itens.md#invariantes) e https://github.com/x/y/blob/main/docs/decisoes/0016-eventos.md";
  assert.deepEqual(docsDaIssue(["itens", "prio:2"], corpo, existe), [
    "docs/decisoes/0016-eventos.md",
    "docs/features/itens.md",
  ]);
});

test("ignora label sem doc, o template e a doc de processo", () => {
  assert.deepEqual(docsDaIssue(["core"], "copie docs/features/_template.md, veja docs/agentes.md", existe), []);
});

test("a última conferência registrada vira a nova base", () => {
  const comentarios = [
    { body: comentarioConferida("ok"), createdAt: "2026-09-01T00:00:00Z" },
    { body: "outro comentário", createdAt: "2026-10-01T00:00:00Z" },
    { body: `${MARCA_CONFERIDA} de novo`, createdAt: "2026-09-20T00:00:00Z" },
  ];
  assert.equal(baseDaConferencia("2026-08-01T00:00:00Z", comentarios), "2026-09-20T00:00:00Z");
});

test("sem conferência, a base é a criação da issue", () => {
  assert.equal(baseDaConferencia("2026-08-01T00:00:00Z", []), "2026-08-01T00:00:00Z");
});
