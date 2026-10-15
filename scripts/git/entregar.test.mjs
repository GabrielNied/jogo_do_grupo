import assert from "node:assert/strict";
import { test } from "node:test";
import {
  arquivosCsharp,
  commitsParaLevar,
  lerArgumentos,
  mensagemDeCommit,
  nomeDaBranchSeguinte,
  quebrarLinhas,
} from "./entregar.mjs";

test("linha curta permanece", () => {
  assert.equal(quebrarLinhas("uma linha curta"), "uma linha curta");
});

test("linha longa quebra no espaço antes de 100", () => {
  const palavras = Array.from({ length: 30 }, () => "palavra");
  const linhas = quebrarLinhas(palavras.join(" ")).split("\n");
  assert.ok(linhas.length > 1);
  for (const linha of linhas) assert.ok(linha.length <= 100);
});

test("linha sem espaço quebra no limite", () => {
  const linhas = quebrarLinhas("a".repeat(150)).split("\n");
  assert.deepEqual(linhas.map((l) => l.length), [100, 50]);
});

test("mensagem separa título e corpo", () => {
  const texto = mensagemDeCommit("chore(desenvolvimento): entrega o commit", "Por que.\n\nCloses #1");
  assert.equal(
    texto,
    "chore(desenvolvimento): entrega o commit\n\nPor que.\n\nCloses #1\n",
  );
});

test("título longo é recusado", () => {
  assert.throws(() => mensagemDeCommit("a".repeat(101), ""), /100/);
});

test("branch seguinte evita a que já foi mergeada", () => {
  assert.equal(
    nomeDaBranchSeguinte("chore/desenvolvimento/entregar"),
    "chore/desenvolvimento/entregar-segue",
  );
  assert.equal(
    nomeDaBranchSeguinte("chore/desenvolvimento/entregar-segue"),
    "chore/desenvolvimento/entregar-segue-2",
  );
  assert.equal(
    nomeDaBranchSeguinte("chore/desenvolvimento/entregar-segue-2"),
    "chore/desenvolvimento/entregar-segue-3",
  );
});

test("só leva o commit que o squash ainda não tem", () => {
  const cherry = "- aaa\n+ bbb\n- ccc";
  const revList = "aaa\nbbb\nccc";
  assert.deepEqual(commitsParaLevar(cherry, revList), ["bbb"]);
});

test("corpo na linha de comando não é argumento", () => {
  assert.throws(() => lerArgumentos(["--titulo", "chore(core): x"]), /--corpo/);
  assert.deepEqual(lerArgumentos(["--titulo", "chore(core): x", "--corpo", ".git/pr.md"]), {
    titulo: "chore(core): x",
    corpo: ".git/pr.md",
  });
});

test("só arquivos .cs vão para o CSharpier", () => {
  assert.deepEqual(arquivosCsharp("a/B.cs\nc/D.cs.meta\ndocs/x.md\n\nE.cs\n"), ["a/B.cs", "E.cs"]);
});
