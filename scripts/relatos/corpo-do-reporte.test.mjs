// Testes de scripts/relatos/corpo-do-reporte.mjs e da fila do reporte.mjs.
// Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { test } from "node:test";
import {
  TAMANHO_MAXIMO_DO_CORPO,
  corpoDoReporte,
  cortarNoMeio,
  lerCabecalho,
  prioridadeDoReporte,
  rotuloDaArea,
  rotulosDoReporte,
  tituloDoReporte,
  trechoDoLog,
} from "./corpo-do-reporte.mjs";
import { reportesPendentes } from "./reporte.mjs";

function manifesto(campos = {}) {
  return {
    Id: "2026-10-15_120430",
    Tipo: "bug",
    Severidade: "media",
    Area: "Personagem",
    Frase: "Posso andar enquanto morto",
    Quando: "2026-10-15 12:04:30",
    QuandoUtc: "2026-10-15T15:04:30.000Z",
    Versao: "0.1.0",
    Editor: true,
    Tela: "1280x720",
    LatenciaMs: 32,
    Branch: "feature/desenvolvimento/relato-e-ajuste",
    Commit: "a0b28d3",
    SegundosDaLinhaDoTempo: 15,
    SegundosDeQuadros: 3,
    AjustesF10: "",
    ...campos,
  };
}

test("título leva a área como escopo e corta a frase longa", () => {
  assert.equal(tituloDoReporte(manifesto()), "[Personagem] Report: Posso andar enquanto morto");
  assert.equal(
    tituloDoReporte(manifesto({ Area: "" })),
    "[Triagem] Report: Posso andar enquanto morto",
  );
  const longo = tituloDoReporte(manifesto({ Frase: "a".repeat(100) }));
  assert.ok(longo.endsWith("...") && longo.length < 100);
});

test("prioridade segue a régua do quadro", () => {
  assert.equal(
    prioridadeDoReporte(manifesto({ Severidade: "critica", Area: "Social" })),
    "prio:1-erros",
  );
  assert.equal(prioridadeDoReporte(manifesto({ Area: "Combate" })), "prio:2-core-loop");
  assert.equal(
    prioridadeDoReporte(manifesto({ Severidade: "baixa", Area: "Combate" })),
    "prio:3-experiencia",
  );
  assert.equal(prioridadeDoReporte(manifesto({ Area: "Social" })), "prio:3-experiencia");
  assert.equal(
    prioridadeDoReporte(manifesto({ Tipo: "melhoria", Area: "Combate" })),
    "prio:3-experiencia",
  );
  assert.equal(prioridadeDoReporte(manifesto({ Tipo: "ux" })), "prio:3-experiencia");
  assert.equal(
    prioridadeDoReporte(manifesto({ Tipo: "novidade", Severidade: "critica" })),
    "prio:5-features",
  );
});

test("rótulos só os que existem no repositório, sem acento na área", () => {
  assert.equal(rotuloDaArea("Profissões"), "profissoes");
  const existentes = new Set(["reporte-beta", "prio:5-features", "enhancement", "profissoes"]);
  assert.deepEqual(
    rotulosDoReporte(manifesto({ Tipo: "novidade", Area: "Profissões" }), existentes),
    ["reporte-beta", "prio:5-features", "enhancement", "profissoes"],
  );
  assert.deepEqual(
    rotulosDoReporte(manifesto({ Area: "Inexistente" }), new Set(["reporte-beta"])),
    ["reporte-beta"],
  );
});

test("corpo traz o cabeçalho escondido, a evidência e o que a IA faz", () => {
  const corpo = corpoDoReporte({
    manifesto: manifesto(),
    linhaDoTempo: "   0 ms  q000  início\n3120 ms  q187  rede  JogadorMorreu {}",
    estado: "vida: 0",
    console: "",
    log: ["15:04:29.100 Information CidadeDoMundo: Personagem 1 morreu"],
    imagens: [{ titulo: "Folha de impacto", legenda: "12 quadros", url: "https://x/y.png" }],
    parecidas: [{ number: 812, title: "[Mobs] Pausa" }],
  });

  const cabecalho = lerCabecalho(corpo);
  assert.equal(cabecalho.kind, "reporte-beta");
  assert.equal(cabecalho.reporte_id, "2026-10-15_120430");
  assert.equal(cabecalho.area, "Personagem");
  assert.equal(cabecalho.commit, "a0b28d3");
  assert.match(corpo, /^<!-- reporte-beta\n/);
  assert.match(corpo, /> Posso andar enquanto morto/);
  assert.match(corpo, /!\[Folha de impacto\]\(https:\/\/x\/y\.png\)/);
  assert.match(corpo, /3120 ms {2}q187/);
  assert.match(corpo, /CidadeDoMundo: Personagem 1 morreu/);
  assert.match(corpo, /- #812 \[Mobs\] Pausa/);
  assert.match(corpo, /docs\/features\/personagem\.md/);
  assert.doesNotMatch(corpo, /Console do Unity/);
});

test("linha do tempo enorme é cortada no meio e o corpo cabe no GitHub", () => {
  const linhas = Array.from(
    { length: 5000 },
    (_, i) => `${i} ms  q${i}  entrada  ↓W ${"x".repeat(20)}`,
  );
  const corpo = corpoDoReporte({ manifesto: manifesto(), linhaDoTempo: linhas.join("\n") });
  assert.ok(corpo.length <= TAMANHO_MAXIMO_DO_CORPO, `corpo com ${corpo.length}`);
  assert.match(corpo, /^0 ms {2}q0 /m);
  assert.match(corpo, /^4999 ms {2}q4999 /m);
  assert.match(corpo, /linhas cortadas/);
});

test("três crases do texto não fecham o bloco", () => {
  const corpo = corpoDoReporte({ manifesto: manifesto(), estado: "antes ``` depois" });
  assert.match(corpo, /antes ˋˋˋ depois/);
});

test("cortar no meio não mexe no texto que já cabe", () => {
  assert.equal(cortarNoMeio("a\nb", 100), "a\nb");
});

test("trecho do log fica na janela, sem o ruído do framework, com a mensagem montada", () => {
  const linhas = [
    JSON.stringify({ "@t": "2026-10-15T15:04:10.000Z", "@mt": "antes da janela" }),
    JSON.stringify({
      "@t": "2026-10-15T15:04:20.000Z",
      "@mt": "Executed DbCommand",
      SourceContext: "Microsoft.EntityFrameworkCore.Database.Command",
    }),
    JSON.stringify({
      "@t": "2026-10-15T15:04:21.500Z",
      "@mt": "passo demorou {Duracao}ms, limite {Limite}ms",
      "@l": "Warning",
      Duracao: 134.8,
      Limite: 100,
      SourceContext: "JogoDoGrupo.Server.Core.TempoReal.ServidorTempoReal",
    }),
    "linha quebrada",
    JSON.stringify({ "@t": "2026-10-15T15:04:40.000Z", "@mt": "depois da janela" }),
  ];
  const inicio = Date.parse("2026-10-15T15:04:15Z");
  const fim = Date.parse("2026-10-15T15:04:32Z");
  assert.deepEqual(trechoDoLog(linhas, inicio, fim), [
    "15:04:21.500 Warning ServidorTempoReal: passo demorou 134.8ms, limite 100ms",
  ]);
});

test("fila lê só as pastas pendentes com reporte.json, em ordem", () => {
  const pasta = fs.mkdtempSync(path.join(os.tmpdir(), "relatos-"));
  try {
    for (const id of ["2026-10-15_120500", "2026-10-15_120430", "sem-manifesto"]) {
      fs.mkdirSync(path.join(pasta, "pendentes", id), { recursive: true });
    }
    fs.writeFileSync(path.join(pasta, "pendentes", "2026-10-15_120500", "reporte.json"), "{}");
    fs.writeFileSync(path.join(pasta, "pendentes", "2026-10-15_120430", "reporte.json"), "{}");

    assert.deepEqual(
      reportesPendentes(pasta).map((dir) => path.basename(dir)),
      ["2026-10-15_120430", "2026-10-15_120500"],
    );
    assert.deepEqual(
      reportesPendentes(pasta, "2026-10-15_120500").map((dir) => path.basename(dir)),
      ["2026-10-15_120500"],
    );
    assert.deepEqual(reportesPendentes(path.join(pasta, "nao-existe")), []);
  } finally {
    fs.rmSync(pasta, { recursive: true, force: true });
  }
});
