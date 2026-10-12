// Testes de scripts/git/worktree-npm.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { existsSync, mkdirSync, mkdtempSync, realpathSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { test } from "node:test";
import { ligarNodeModules } from "./worktree-npm.mjs";

function pastas() {
  const base = mkdtempSync(join(tmpdir(), "worktree-npm-"));
  const raiz = join(base, "raiz");
  const worktree = join(base, "wt");
  mkdirSync(raiz);
  mkdirSync(worktree);
  return { base, raiz, worktree };
}

test("liga o node_modules do checkout principal no worktree", () => {
  const { base, raiz, worktree } = pastas();
  mkdirSync(join(raiz, "node_modules", ".bin"), { recursive: true });
  writeFileSync(join(raiz, "node_modules", ".bin", "commitlint"), "");

  assert.equal(ligarNodeModules(raiz, worktree), true);
  assert.ok(existsSync(join(worktree, "node_modules", ".bin", "commitlint")));
  assert.equal(realpathSync(join(worktree, "node_modules")), realpathSync(join(raiz, "node_modules")));
  rmSync(base, { recursive: true, force: true });
});

test("não mexe se o worktree já tem node_modules", () => {
  const { base, raiz, worktree } = pastas();
  mkdirSync(join(raiz, "node_modules"));
  mkdirSync(join(worktree, "node_modules"));
  writeFileSync(join(worktree, "node_modules", "proprio"), "");

  assert.equal(ligarNodeModules(raiz, worktree), false);
  assert.ok(existsSync(join(worktree, "node_modules", "proprio")));
  rmSync(base, { recursive: true, force: true });
});

test("sem node_modules na raiz, não faz nada", () => {
  const { base, raiz, worktree } = pastas();

  assert.equal(ligarNodeModules(raiz, worktree), false);
  assert.ok(!existsSync(join(worktree, "node_modules")));
  rmSync(base, { recursive: true, force: true });
});
