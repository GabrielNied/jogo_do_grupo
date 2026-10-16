import assert from "node:assert/strict";
import { test } from "node:test";
import { encontrarDiretoriosGit, obterExecutavelSh, raizDoGit } from "./localizar-sh.mjs";

test("em plataforma não-windows retorna sh", () => {
  assert.equal(obterExecutavelSh({ platform: "linux" }), "sh");
  assert.equal(obterExecutavelSh({ platform: "darwin" }), "sh");
});

test("GIT_SH tem precedência quando o arquivo existe", () => {
  // Passa o executável de node como substituto de arquivo existente
  const caminhoNode = process.execPath;
  assert.equal(obterExecutavelSh({ platform: "win32", env: { GIT_SH: caminhoNode } }), caminhoNode);
});

test("raizDoGit resolve a partir de caminho de exec-path", () => {
  const raiz = raizDoGit("C:/Program Files/Git/mingw64/libexec/git-core");
  assert.ok(raiz.toLowerCase().includes("git"));
});

test("encontrarDiretoriosGit localiza diretórios existentes na máquina", () => {
  const dirs = encontrarDiretoriosGit();
  if (process.platform === "win32") {
    assert.ok(dirs.length > 0, "deve encontrar ao menos um diretório de binários do Git no Windows");
    assert.ok(dirs.some((d) => d.toLowerCase().includes("bin")), "deve conter pasta bin");
  }
});

test("no windows encontra executável válido do sh quando Git está instalado", () => {
  if (process.platform === "win32") {
    const sh = obterExecutavelSh();
    assert.ok(sh.toLowerCase().endsWith("sh.exe") || sh === "sh");
  }
});
