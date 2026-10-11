import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { gerar, nomeUss, executar } from "./gerar-config-visual.mjs";

const base = () => JSON.parse(readFileSync(new URL("../config-visual.json", import.meta.url)));
test("configuração rejeita limites de leitura, alvos e limiares inválidos", () => {
  const c = base(); c.Tipografia.Leitura = 12; assert.throws(() => gerar(c));
  const d = base(); d.Dimensoes.AlvoMinimo = 30; assert.throws(() => gerar(d));
  const e = base(); e.Responsividade.EstreitoAte = 2000; assert.throws(() => gerar(e));
  const f = base(); f.Cores.Ouro = "inventado"; assert.throws(() => gerar(f));
  const g = base(); g.Tipografia.Titulo = "28px"; assert.throws(() => gerar(g));
  const h = base(); h.Dimensoes.DialogoPercentual = 130; assert.throws(() => gerar(h));
});
test("uma mudança de cor se propaga a C# e USS sem edição independente", () => {
  const c = base(); c.Cores.Ouro = "#E8AD55";
  const saida = gerar(c);
  assert(saida.cs.includes('Ouro = "#E8AD55"'));
  assert(saida.uss.includes('--lumora-cores-ouro: #E8AD55'));
  assert.equal(nomeUss("Cores", "TextoSecundario"), "--lumora-cores-texto-secundario");
});
test("arquivos gerados e fontes correspondem à configuração atual", () => executar(true));
