// Testes de scripts/unity/gerar-meta.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { test } from "node:test";
import {
  gravar,
  ignoradoPeloUnity,
  metaDePasta,
  metaDeScript,
  novoGuid,
  procurarSemMeta,
} from "./gerar-meta.mjs";

function pastaTemporaria(t) {
  const pasta = mkdtempSync(join(tmpdir(), "gerar-meta-"));
  t.after(() => rmSync(pasta, { recursive: true, force: true }));
  return pasta;
}

test("guid tem 32 dígitos hexadecimais e não repete", () => {
  const guids = new Set(Array.from({ length: 200 }, novoGuid));
  assert.equal(guids.size, 200);
  for (const guid of guids) assert.match(guid, /^[0-9a-f]{32}$/);
});

test("meta de script é igual ao que o Unity grava: duas linhas, sem quebra no fim", () => {
  assert.equal(
    metaDeScript("e08a0174c0cf40a45975d7931d6a4e5e"),
    "fileFormatVersion: 2\nguid: e08a0174c0cf40a45975d7931d6a4e5e",
  );
});

test("meta de pasta é igual ao que o Unity grava", () => {
  assert.equal(
    metaDePasta("5a99d1bdf297cff4b94e53eea8d14c04"),
    "fileFormatVersion: 2\nguid: 5a99d1bdf297cff4b94e53eea8d14c04\nfolderAsset: yes\n" +
      "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n" +
      "  assetBundleVariant: \n",
  );
});

test("o formato bate com os .meta que o Unity já gravou no repo", () => {
  const raiz = new URL("../../shared/Package/Runtime/Features/", import.meta.url);
  const script = readFileSync(new URL("Mobs/MobsMessageIds.cs.meta", raiz), "utf8");
  const pasta = readFileSync(new URL("Mobs.meta", raiz), "utf8");
  const guid = (texto) => /^guid: ([0-9a-f]{32})$/m.exec(texto)[1];

  assert.equal(metaDeScript(guid(script)), script);
  assert.equal(metaDePasta(guid(pasta)), pasta);
});

test("acha script e pasta sem meta, inclusive pasta dentro de pasta nova", (t) => {
  const raiz = pastaTemporaria(t);
  mkdirSync(join(raiz, "Nova", "Dentro"), { recursive: true });
  writeFileSync(join(raiz, "Nova", "Dentro", "Regra.cs"), "");

  const { gerar, soUnity } = procurarSemMeta(raiz);

  assert.deepEqual(
    gerar.map(({ caminho, tipo }) => [caminho, tipo]).sort(),
    [
      [join(raiz, "Nova", "Dentro", "Regra.cs.meta"), "script"],
      [join(raiz, "Nova", "Dentro.meta"), "pasta"],
      [join(raiz, "Nova.meta"), "pasta"],
    ].sort(),
  );
  assert.deepEqual(soUnity, []);
});

test("não mexe no que já tem meta", (t) => {
  const raiz = pastaTemporaria(t);
  writeFileSync(join(raiz, "Antigo.cs"), "");
  writeFileSync(join(raiz, "Antigo.cs.meta"), "fileFormatVersion: 2\nguid: abc");

  const { gerar } = procurarSemMeta(raiz);
  gravar(gerar);

  assert.deepEqual(gerar, []);
  assert.equal(readFileSync(join(raiz, "Antigo.cs.meta"), "utf8"), "fileFormatVersion: 2\nguid: abc");
});

test("arquivo que não é script fica para o Unity", (t) => {
  const raiz = pastaTemporaria(t);
  writeFileSync(join(raiz, "Peca.prefab"), "");
  writeFileSync(join(raiz, "Modulo.asmdef"), "");

  const { gerar, soUnity } = procurarSemMeta(raiz);

  assert.deepEqual(gerar, []);
  assert.deepEqual(soUnity.sort(), [join(raiz, "Modulo.asmdef"), join(raiz, "Peca.prefab")]);
});

test("ignora o que o Unity ignora", (t) => {
  const raiz = pastaTemporaria(t);
  mkdirSync(join(raiz, ".oculta"));
  mkdirSync(join(raiz, "Exemplos~"));
  writeFileSync(join(raiz, ".gitkeep"), "");
  writeFileSync(join(raiz, "Exemplos~", "Exemplo.cs"), "");

  assert.deepEqual(procurarSemMeta(raiz), { gerar: [], soUnity: [] });
  assert.equal(ignoradoPeloUnity("Normal.cs"), false);
});

test("gravar cria os arquivos, cada um com guid diferente, e rodar de novo não acha nada", (t) => {
  const raiz = pastaTemporaria(t);
  mkdirSync(join(raiz, "Nova"));
  writeFileSync(join(raiz, "Nova", "A.cs"), "");
  writeFileSync(join(raiz, "Nova", "B.cs"), "");

  gravar(procurarSemMeta(raiz).gerar);

  const a = readFileSync(join(raiz, "Nova", "A.cs.meta"), "utf8");
  const b = readFileSync(join(raiz, "Nova", "B.cs.meta"), "utf8");
  assert.match(a, /^fileFormatVersion: 2\nguid: [0-9a-f]{32}$/);
  assert.notEqual(a, b);
  assert.match(readFileSync(join(raiz, "Nova.meta"), "utf8"), /\nfolderAsset: yes\n/);
  assert.deepEqual(procurarSemMeta(raiz).gerar, []);
});

test("gravar recusa sobrescrever um meta que apareceu no meio do caminho", (t) => {
  const raiz = pastaTemporaria(t);
  writeFileSync(join(raiz, "A.cs"), "");
  const { gerar } = procurarSemMeta(raiz);
  writeFileSync(join(raiz, "A.cs.meta"), "fileFormatVersion: 2\nguid: do-unity");

  assert.throws(() => gravar(gerar), { code: "EEXIST" });
  assert.equal(readFileSync(join(raiz, "A.cs.meta"), "utf8"), "fileFormatVersion: 2\nguid: do-unity");
});

test("pasta que não existe não é erro", () => {
  assert.deepEqual(procurarSemMeta(join(tmpdir(), "nao-existe-gerar-meta")), {
    gerar: [],
    soUnity: [],
  });
});
