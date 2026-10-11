import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { conferirFonte, contraste } from "./validar.mjs";

test("contraste: extremos, igualdade, simetria e cor inválida", () => {
  assert.equal(contraste("#000000", "#FFFFFF"), 21);
  assert.equal(contraste("#173B50", "#173B50"), 1);
  assert.equal(contraste("#F4E6CB", "#173B50"), contraste("#173B50", "#F4E6CB"));
  assert.throws(() => contraste("azul", "#FFFFFF"));
});

test("fontes: aceita fonte preparada e recusa peso errado, LFS e arquivo truncado", () => {
  const fonte = readFileSync(new URL("../fontes/LumoraSource-500.ttf", import.meta.url));
  assert.doesNotThrow(() => conferirFonte(fonte, 500));
  assert.throws(() => conferirFonte(fonte, 700));
  assert.throws(() => conferirFonte(Buffer.from("version https://git-lfs.github.com/spec/v1"), 500));
  assert.throws(() => conferirFonte(fonte.subarray(0, 20), 500));
});
