import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { gerar, nomeUss, executar, conferirEstilo } from "./gerar-config-visual.mjs";
import { contraste } from "../../laboratorio-unity/ferramentas/validar.mjs";

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
test("trocar a fonte central atualiza títulos novos e classes antigas, incluindo conteúdo dinâmico", () => {
  const c = base(); c.Fontes.Titulo = "Lumora/Fontes/LumoraTeste-800-SDF";
  const { uss } = gerar(c);
  assert(uss.includes('Fontes/LumoraTeste-800-SDF.asset'));
  assert.match(uss, /\.lumora-ui \.titulo[^{}]+\{ -unity-font-definition: url\("Fontes\/LumoraTeste-800-SDF.asset"\)/);
  assert(uss.includes('.lumora-ui .unity-button'));
});
test("estilos rejeitam cores avulsas, tokens errados e imagens inexistentes", () => {
  const tokens = gerar(base()).uss;
  const arquivo = "client/Assets/_Game/Core/UI/Visual/Resources/Lumora/ComponentesVisuais.uss";
  assert.throws(() => conferirEstilo('.botao { color: rgb(1, 2, 3); }', tokens, arquivo));
  assert.throws(() => conferirEstilo('.botao { color: var(--lumora-cores-errada); }', tokens, arquivo));
  assert.throws(() => conferirEstilo('.painel { background-image: url("Arte/ausente.png"); }', tokens, arquivo));
  assert.doesNotThrow(() => conferirEstilo('.botao { color: var(--lumora-cores-marfim); }', tokens, arquivo));
});
test("texto e estados dos controles mantêm contraste nas superfícies sólidas", () => {
  const { Cores: c } = base();
  for (const fundo of [c.Petroleo, c.Hover, c.Pressionado, c.Campo])
    assert(contraste(c.Marfim, fundo) >= 4.5);
  for (const fundo of [c.Ouro, c.HoverPrimario, c.BordaPrimario])
    assert(contraste(c.Petroleo, fundo) >= 4.5);
  for (const texto of [c.Erro, c.Ganho, c.Essencia, c.Sussurro, c.TextoSecundario])
    assert(contraste(texto, c.Petroleo) >= 4.5);
});
