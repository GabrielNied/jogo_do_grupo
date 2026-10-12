import assert from "node:assert/strict";
import { test } from "node:test";
import { mensagemPushMergeado } from "./push-mergeado.mjs";

test("sem PR mergeado o push segue", () => {
  assert.equal(mensagemPushMergeado("fix/core/nova", null), null);
});

test("PR mergeado explica que a branch tem de ser outra", () => {
  const texto = mensagemPushMergeado("fix/core/worktree-com-node-modules", 538);
  assert.match(texto, /PR #538/);
  assert.match(texto, /origin\/main/);
});
