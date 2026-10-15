// Testes de scripts/git/subtarefa.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { corpoSubtarefa, pastaDaFuncionalidade, pastasDoPapel } from "./subtarefa.mjs";

test("a pasta vem da lista real, sem depender de maiúscula", () => {
  assert.equal(pastaDaFuncionalidade("profissoes", ["Itens", "Profissoes"]), "Profissoes");
  assert.equal(pastaDaFuncionalidade("mundo", []), "Mundo");
});

test("cada papel mexe na sua pasta", () => {
  assert.deepEqual(pastasDoPapel("client-core", "Itens"), ["client/Assets/_Game/Core/"]);
  assert.deepEqual(pastasDoPapel("server-core", "Itens"), ["server/src/JogoDoGrupo.Server/Core/"]);
  assert.equal(pastasDoPapel("outro", "Itens").length, 3);
});

test("o dono da funcionalidade mexe nas três partes, nos testes e na doc", () => {
  const pastas = pastasDoPapel("funcionalidade", "Mobs");
  assert.ok(pastas.includes("shared/Package/Runtime/Features/Mobs/"));
  assert.ok(pastas.includes("server/src/JogoDoGrupo.Server/Features/Mobs/"));
  assert.ok(pastas.includes("client/Assets/_Game/Features/Mobs/"));
  assert.ok(pastas.includes("docs/features/mobs.md"));
  assert.deepEqual(pastasDoPapel("itens", "Itens"), pastasDoPapel("funcionalidade", "Itens"));
});

test("o corpo aponta onde ler e onde mexer e mantém o Parent", () => {
  const corpo = corpoSubtarefa({
    pai: 64,
    tituloPai: "[Itens] Loot no chão",
    papel: "funcionalidade",
    escopo: "itens",
    pastas: ["Itens"],
    contexto: "Marcador do item e tecla F.",
  });
  assert.match(corpo, /^Subtarefa de #64 \(\[Itens\] Loot no chão\)/);
  assert.match(corpo, /\*\*O que fazer:\*\*\n\nMarcador do item e tecla F\./);
  assert.match(corpo, /docs\/features\/itens\.md/);
  assert.match(corpo, /- `client\/Assets\/_Game\/Features\/Itens\/`/);
  assert.match(corpo, /Parent: #64$/);
});

test("sem contexto, não deixa seção vazia", () => {
  const corpo = corpoSubtarefa({ pai: 1, tituloPai: "x", papel: "", escopo: "core" });
  assert.doesNotMatch(corpo, /O que fazer/);
  assert.match(corpo, /\*\*Papel:\*\* geral/);
});
