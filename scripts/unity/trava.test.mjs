// Testes de scripts/unity/trava.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { test } from "node:test";
import { lerTrava, soltar, tentarPegar } from "./trava.mjs";

const novoArquivo = () => join(mkdtempSync(join(tmpdir(), "trava-unity-")), "trava");
const dono = (pid) => ({ pid, acao: "executar", cwd: "C:/wt", inicio: "2026-10-11T00:00:00Z" });

test("o primeiro pega a trava e o segundo espera enquanto o dono está vivo", () => {
  const arquivo = novoArquivo();
  assert.equal(tentarPegar(dono(1), { arquivo, vivo: () => true }), true);
  assert.equal(tentarPegar(dono(2), { arquivo, vivo: () => true }), false);
  assert.equal(lerTrava(arquivo).pid, 1);
});

test("trava de processo morto é retomada", () => {
  const arquivo = novoArquivo();
  tentarPegar(dono(1), { arquivo, vivo: () => true });
  assert.equal(tentarPegar(dono(2), { arquivo, vivo: (pid) => pid !== 1 }), true);
  assert.equal(lerTrava(arquivo).pid, 2);
});

test("trava ilegível (arquivo cortado) é retomada", () => {
  const arquivo = novoArquivo();
  writeFileSync(arquivo, "{");
  assert.equal(tentarPegar(dono(3), { arquivo, vivo: () => true }), true);
});

test("só o dono solta a trava", () => {
  const arquivo = novoArquivo();
  tentarPegar(dono(1), { arquivo, vivo: () => true });
  soltar(2, arquivo);
  assert.equal(lerTrava(arquivo).pid, 1);
  soltar(1, arquivo);
  assert.equal(lerTrava(arquivo), null);
  assert.equal(tentarPegar(dono(2), { arquivo, vivo: () => true }), true);
});
