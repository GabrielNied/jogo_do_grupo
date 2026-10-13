// Testes das regras do limpar-agentes: node --test scripts/git/limpar-agentes.test.mjs
import assert from "node:assert/strict";
import { test } from "node:test";
import { assuntoEquivalenteNaMain, decidir, dentroDe, lerWorktrees, patchesNaMain } from "./limpar-agentes.mjs";

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

const mesclado = (extra = {}) => ({ numero: 7, estado: "mesclado", headContemLocal: true, ...extra });

test("mudança não commitada sempre fica, mesmo com PR mesclado", () => {
  assert.equal(decidir({ sujo: true, commitsProprios: 3, pr: mesclado() }).remover, false);
});

test("sem commits fora da main sai", () => {
  assert.equal(decidir({ commitsProprios: 0 }).remover, true);
});

test("PR mesclado sai mesmo se mexido há pouco (o agente já terminou)", () => {
  const d = decidir({ commitsProprios: 2, pr: mesclado(), recente: true });
  assert.equal(d.remover, true);
  assert.match(d.motivo, /PR #7 mesclado/);
});

test("PR aberto fica, mesmo com tudo no GitHub", () => {
  const d = decidir({ commitsProprios: 2, pr: { numero: 8, estado: "aberto" }, naOrigem: true });
  assert.equal(d.remover, false);
  assert.match(d.motivo, /PR #8 aberto/);
});

test("sem PR e mexido há pouco fica, mesmo sem commits (agente começando)", () => {
  const d = decidir({ commitsProprios: 0, recente: true });
  assert.equal(d.remover, false);
  assert.match(d.motivo, /usado há pouco/);
});

test("commit feito depois do merge fica, e o motivo avisa", () => {
  const d = decidir({ commitsProprios: 3, pr: mesclado({ numero: 44, headContemLocal: false }) });
  assert.equal(d.remover, false);
  assert.match(d.motivo, /depois do merge do PR #44/);
});

test("commits depois do merge que já chegaram à main por outro PR: sai", () => {
  const d = decidir({ commitsProprios: 3, pr: mesclado({ headContemLocal: false, extrasNaMain: true }) });
  assert.equal(d.remover, true);
});

test("PR fechado sem merge sai do local: a ponta fica em refs/pull no GitHub", () => {
  const d = decidir({ commitsProprios: 1, pr: { numero: 9, estado: "fechado", headContemLocal: true } });
  assert.equal(d.remover, true);
  assert.match(d.motivo, /refs\/pull\/9\/head/);
});

test("branch sem PR cujo conteúdo entrou por cherry-pick ou squash: sai", () => {
  const d = decidir({ commitsProprios: 4, conteudoNaMain: true });
  assert.equal(d.remover, true);
  assert.match(d.motivo, /já está na main/);
});

test("git cherry só com +: patches já na main", () => {
  const r = patchesNaMain({
    linhasCherry: [
      "+ aaa111 feat(x): um",
      "+ bbb222 feat(x): dois",
    ],
    assuntosNaMain: [],
    assuntoDoCommit: () => null,
  });
  assert.equal(r.ok, true);
  const d = decidir({ commitsProprios: 4, patchesNaMain: r });
  assert.equal(d.remover, true);
  assert.match(d.motivo, /patches já estão na main/);
});

test("assunto na main com (#NNN) de squash equivale ao local", () => {
  assert.equal(
    assuntoEquivalenteNaMain("feat(x): foo", ["feat(x): foo (#12)"]),
    true,
  );
});

test("git cherry com - cujo assunto já existe na main: reimplementação (squash)", () => {
  const assunto = "feat(combate): ritmo do golpe";
  const r = patchesNaMain({
    linhasCherry: ["+ aaa111 feat(x): guarda", "- ccc333 feat(combate): ritmo do golpe"],
    assuntosNaMain: ["feat(combate): ritmo do golpe (#597)"],
    assuntoDoCommit: (sha) => (sha === "ccc333" ? assunto : null),
  });
  assert.equal(r.ok, true);
  const d = decidir({ commitsProprios: 4, patchesNaMain: r });
  assert.equal(d.remover, true);
  assert.match(d.motivo, /reimplementados na main/);
});

test("git cherry com - sem par na main: não entrega", () => {
  const r = patchesNaMain({
    linhasCherry: ["- ddd444 feat(y): só local"],
    assuntosNaMain: ["feat(y): outra coisa"],
    assuntoDoCommit: () => "feat(y): só local",
  });
  assert.equal(r.ok, false);
});

test("branch sem PR já enviada ao origin: sai do local", () => {
  assert.equal(decidir({ commitsProprios: 1, naOrigem: true }).remover, true);
});

test("commit só nesta máquina, ou GitHub fora do ar sem outra cópia: fica", () => {
  const d = decidir({ commitsProprios: 1 });
  assert.equal(d.remover, false);
  assert.match(d.motivo, /só nesta máquina/);
  assert.equal(decidir({ commitsProprios: 1, erroPr: "gh sem login" }).remover, false);
});

test("PR mesclado conta como entregue, para worktree fora da pasta de agentes", () => {
  const d = decidir({ commitsProprios: 2, pr: mesclado() });
  assert.equal(d.remover, true);
  assert.equal(d.entregue, true);
});

test("conteúdo já na main conta como entregue (squash ou cherry-pick)", () => {
  const d = decidir({ commitsProprios: 4, conteudoNaMain: true });
  assert.equal(d.entregue, true);
});

test("sem commits fora da main não é entrega: pode ser um checkout pessoal", () => {
  assert.equal(decidir({ commitsProprios: 0 }).entregue, false);
});

test("ponta no origin não é entrega: a pessoa pode estar trabalhando nessa pasta", () => {
  assert.equal(decidir({ commitsProprios: 1, naOrigem: true }).entregue, false);
});

test("commit depois do merge não é entrega", () => {
  const d = decidir({ commitsProprios: 3, pr: mesclado({ numero: 44, headContemLocal: false }) });
  assert.equal(d.remover, false);
  assert.equal(d.entregue, false);
});

test("dentroDe compara pastas sem diferenciar maiúsculas e sem pegar prefixo parecido", () => {
  assert.equal(dentroDe("/repo/.claude/worktrees", "/repo/.claude/worktrees/pulo"), true);
  assert.equal(dentroDe("/repo/.claude/worktrees", "/REPO/.claude/Worktrees/pulo"), true);
  assert.equal(dentroDe("/repo/.claude/worktrees", "/repo/.claude/worktrees-velhos/x"), false);
  assert.equal(dentroDe("/repo/.claude/worktrees", "/repo"), false);
});
