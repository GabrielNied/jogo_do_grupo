// Testes de scripts/agentes/orcamento-contexto.mjs. Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { test } from "node:test";
import { estourados, tamanho, tetoDe, totalDoPacote } from "./orcamento-contexto.mjs";

test("cada tipo de arquivo tem o seu teto", () => {
  assert.equal(tetoDe("AGENTS.md"), 7000);
  assert.equal(tetoDe(".claude/agents/funcionalidade.md"), 3000);
  assert.equal(tetoDe(".claude/skills/trabalhe/SKILL.md"), 6000);
  assert.equal(tetoDe("docs/visao-geral.md"), null);
});

test("conta caracteres, não bytes", () => {
  assert.equal(tamanho("ação"), 4);
});

test("lista só o que passou do teto, o mais estourado primeiro", () => {
  const acima = estourados([
    { caminho: "AGENTS.md", tamanho: 7100 },
    { caminho: ".claude/agents/itens.md", tamanho: 2000 },
    { caminho: ".claude/skills/decida/SKILL.md", tamanho: 9000 },
    { caminho: "docs/outro.md", tamanho: 99999 },
  ]);
  assert.deepEqual(
    acima.map((m) => m.caminho),
    [".claude/skills/decida/SKILL.md", "AGENTS.md"],
  );
});

test("o pacote soma os arquivos que existem", () => {
  const medidas = [
    { caminho: "AGENTS.md", tamanho: 10 },
    { caminho: "docs/agentes.md", tamanho: 5 },
  ];
  assert.equal(totalDoPacote(medidas, ["AGENTS.md", "docs/agentes.md", "sumiu.md"]), 15);
});
