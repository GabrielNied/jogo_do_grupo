// Testes das regras do limpar-agentes: node --test scripts/git/limpar-agentes.test.mjs
import assert from "node:assert/strict";
import { test } from "node:test";
import { decidir, dentroDe, lerWorktrees } from "./limpar-agentes.mjs";

test("lê o porcelain do git worktree list, com e sem branch", () => {
  const saida = [
    "worktree C:/Dev/jogo_do_grupo",
    "HEAD aaa",
    "branch refs/heads/main",
    "",
    "worktree C:/Dev/jogo_do_grupo/.claude/worktrees/pulo",
    "HEAD bbb",
    "branch refs/heads/feature/personagem/pulo",
    "",
    "worktree C:/Dev/jogo_do_grupo/.claude/worktrees/agent-a02",
    "HEAD ccc",
    "detached",
    "",
  ].join("\r\n");
  assert.deepEqual(lerWorktrees(saida), [
    { caminho: "C:/Dev/jogo_do_grupo", head: "aaa", branch: "main" },
    { caminho: "C:/Dev/jogo_do_grupo/.claude/worktrees/pulo", head: "bbb", branch: "feature/personagem/pulo" },
    { caminho: "C:/Dev/jogo_do_grupo/.claude/worktrees/agent-a02", head: "ccc", branch: null },
  ]);
});

test("mudança não commitada sempre fica, mesmo com PR mesclado", () => {
  const d = decidir({ sujo: true, commitsProprios: 3, pr: { numero: 1, mesclado: true, headContemLocal: true } });
  assert.equal(d.remover, false);
});

test("sem commits fora da main sai", () => {
  assert.equal(decidir({ commitsProprios: 0 }).remover, true);
});

test("PR mesclado com a ponta local contida sai", () => {
  assert.equal(decidir({ commitsProprios: 2, pr: { numero: 7, mesclado: true, headContemLocal: true } }).remover, true);
});

test("commit feito depois do merge fica, e o motivo avisa", () => {
  const d = decidir({ commitsProprios: 3, pr: { numero: 44, mesclado: true, headContemLocal: false } });
  assert.equal(d.remover, false);
  assert.match(d.motivo, /depois do merge do PR #44/);
});

test("sem PR, PR fechado sem merge ou GitHub fora do ar: fica", () => {
  assert.equal(decidir({ commitsProprios: 1 }).remover, false);
  assert.equal(decidir({ commitsProprios: 1, pr: { numero: 9, mesclado: false, headContemLocal: true } }).remover, false);
  assert.equal(decidir({ commitsProprios: 1, erroPr: "gh sem login" }).remover, false);
});

test("dentroDe compara pastas sem diferenciar maiúsculas e sem pegar prefixo parecido", () => {
  assert.equal(dentroDe("/repo/.claude/worktrees", "/repo/.claude/worktrees/pulo"), true);
  assert.equal(dentroDe("/repo/.claude/worktrees", "/REPO/.claude/Worktrees/pulo"), true);
  assert.equal(dentroDe("/repo/.claude/worktrees", "/repo/.claude/worktrees-velhos/x"), false);
  assert.equal(dentroDe("/repo/.claude/worktrees", "/repo"), false);
});

test("usado há pouco fica, mesmo limpo e sem commits", () => {
  const d = decidir({ commitsProprios: 0, recente: true });
  assert.equal(d.remover, false);
  assert.match(d.motivo, /usado há pouco/);
});

test("commits depois do merge que já chegaram à main por outro PR: sai", () => {
  const d = decidir({ commitsProprios: 3, pr: { numero: 44, mesclado: true, headContemLocal: false, extrasNaMain: true } });
  assert.equal(d.remover, true);
});
