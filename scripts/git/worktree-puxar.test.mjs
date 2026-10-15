// Testes de scripts/git/worktree-puxar.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import {
  checkoutPrincipal,
  recusarPuxarNoCheckoutCompartilhado,
  worktreeSeparado,
} from "./worktree-puxar.mjs";

test("checkout principal tem .git na raiz; worktree linkado tem worktrees no caminho", () => {
  assert.equal(checkoutPrincipal(".git"), true);
  assert.equal(checkoutPrincipal("C:/repo/.git/worktrees/agent-a"), false);
});

test("worktree separado é pasta diferente da raiz atual", () => {
  assert.equal(worktreeSeparado(".claude/worktrees/lote", "C:/repo"), true);
  assert.equal(worktreeSeparado(".", "C:/repo"), false);
  assert.equal(worktreeSeparado(null, "C:/repo"), false);
});

test("no checkout principal, agente ou outro lote exige pasta nova", () => {
  const base = { checkoutPrincipal: true, worktreeSeparado: false };
  assert.match(recusarPuxarNoCheckoutCompartilhado({ ...base, nomeAgente: "funcionalidade" }), /checkout principal/);
  assert.match(recusarPuxarNoCheckoutCompartilhado({ ...base, outroLoteEmAndamento: true }), /outro lote/);
  assert.equal(recusarPuxarNoCheckoutCompartilhado({ ...base }), null);
});

test("worktree linkado ou --worktree fora da raiz libera", () => {
  assert.equal(
    recusarPuxarNoCheckoutCompartilhado({
      checkoutPrincipal: false,
      worktreeSeparado: false,
      outroLoteEmAndamento: true,
      nomeAgente: "funcionalidade",
    }),
    null,
  );
  assert.equal(
    recusarPuxarNoCheckoutCompartilhado({
      checkoutPrincipal: true,
      worktreeSeparado: true,
      outroLoteEmAndamento: true,
    }),
    null,
  );
});
