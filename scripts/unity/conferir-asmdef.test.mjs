import assert from "node:assert/strict";
import { test } from "node:test";
import { lerAsmdefs, verificarReferencias } from "./conferir-asmdef.mjs";

const VISUAL = { nome: "Client.Visual", caminho: "client/Assets/Core/Visual/Client.Visual.asmdef", referencias: [] };
const MISSOES = { nome: "Client.Missoes", caminho: "client/Assets/Missoes/Client.Missoes.asmdef", referencias: [] };
const ARQUIVO_VISUAL = {
  caminho: "client/Assets/Core/Visual/Estilo.cs",
  texto: "namespace JogoDoGrupo.Core.Visual { public static class Estilo { } }",
};

function missoes(texto, referencias = []) {
  return verificarReferencias(
    [VISUAL, { ...MISSOES, referencias }],
    [ARQUIVO_VISUAL, { caminho: "client/Assets/Missoes/Janela.cs", texto }],
  );
}

test("aceita using de namespace referenciado", () => {
  assert.deepEqual(missoes("using JogoDoGrupo.Core.Visual;\nclass A {}", ["Client.Visual"]), []);
});

test("rejeita using de namespace de asmdef não referenciado (#1158)", () => {
  const erros = missoes("using System;\nusing JogoDoGrupo.Core.Visual;\nclass A {}");
  assert.equal(erros.length, 1);
  assert.match(erros[0], /Janela\.cs:2:.*JogoDoGrupo.Core\.Visual.*Client\.Visual.*Client\.Missoes/);
});

test("rejeita nome completo sem using", () => {
  const erros = missoes("class A { object o = JogoDoGrupo.Core.Visual.Estilo.Cor; }");
  assert.equal(erros.length, 1);
});

test("aceita namespace do próprio asmdef e de fora do projeto", () => {
  const erros = verificarReferencias(
    [MISSOES],
    [
      { caminho: "client/Assets/Missoes/A.cs", texto: "namespace JogoDoGrupo.Missoes { class A {} }" },
      { caminho: "client/Assets/Missoes/B.cs", texto: "using System.Linq;\nusing JogoDoGrupo.Missoes;\nclass B {}" },
    ],
  );
  assert.deepEqual(erros, []);
});

test("ignora comentário e literal", () => {
  const texto = [
    "// using JogoDoGrupo.Core.Visual;",
    "/// <c>JogoDoGrupo.Core.Visual.Estilo</c>",
    '[assembly: InternalsVisibleTo("JogoDoGrupo.Core.Visual")]',
    'var s = "http://x"; var t = @"JogoDoGrupo.Core.Visual";',
    "class A {}",
  ].join("\n");
  assert.deepEqual(missoes(texto), []);
});

test("script fora de qualquer asmdef não é conferido", () => {
  const erros = verificarReferencias(
    [VISUAL],
    [ARQUIVO_VISUAL, { caminho: "client/Assets/Solto.cs", texto: "using JogoDoGrupo.Core.Visual;" }],
  );
  assert.deepEqual(erros, []);
});

test("resolve referência por GUID pelo .meta", () => {
  const arquivos = {
    "a/A.asmdef": '{"name":"A","references":["GUID:abc123"]}',
    "a/A.asmdef.meta": "guid: 111",
    "b/B.asmdef": '{"name":"B"}',
    "b/B.asmdef.meta": "fileFormatVersion: 2\nguid: abc123\n",
  };
  const asmdefs = lerAsmdefs(["a/A.asmdef", "b/B.asmdef"], (c) => arquivos[c]);
  assert.deepEqual(asmdefs.find((a) => a.nome === "A").referencias, ["B"]);
});
