// Orçamento de contexto (`npm run contexto`): o que todo agente carrega antes de ler código
// (AGENTS.md, docs/agentes.md, a definição do agente e as skills) é reenviado em todas as
// rodadas. Cada arquivo tem um teto em caracteres; passou, o comando falha (o CI roda).
// Para crescer, enxugue outro trecho ou mova o detalhe para um doc lido só quando preciso.

import { readdirSync, readFileSync, existsSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

export const TETOS = {
  "AGENTS.md": 7000,
  "docs/agentes.md": 11000,
  agente: 3000,
  skill: 6000,
};

// Pacotes que entram juntos numa sessão típica, só para o relatório.
export const PACOTES = {
  "dono (funcionalidade)": [
    "AGENTS.md",
    "docs/agentes.md",
    ".claude/agents/funcionalidade.md",
    ".claude/skills/puxar-tarefa/SKILL.md",
    ".claude/skills/preparar-pr/SKILL.md",
  ],
  "coordenador (/trabalhe)": [
    "AGENTS.md",
    "docs/agentes.md",
    ".claude/skills/trabalhe/SKILL.md",
    ".claude/skills/puxar-tarefa/SKILL.md",
    ".claude/skills/preparar-pr/SKILL.md",
  ],
};

export function tetoDe(caminho) {
  if (TETOS[caminho]) return TETOS[caminho];
  if (/^\.claude\/agents\/[^/]+\.md$/.test(caminho)) return TETOS.agente;
  if (/^\.claude\/skills\/[^/]+\/SKILL\.md$/.test(caminho)) return TETOS.skill;
  return null;
}

export function tamanho(texto) {
  return [...texto].length;
}

/** Arquivos acima do teto, do mais estourado para o menos. */
export function estourados(medidas) {
  return medidas
    .map((m) => ({ ...m, teto: tetoDe(m.caminho) }))
    .filter((m) => m.teto !== null && m.tamanho > m.teto)
    .sort((a, b) => b.tamanho - b.teto - (a.tamanho - a.teto));
}

export function totalDoPacote(medidas, caminhos) {
  return caminhos.reduce((soma, caminho) => soma + (medidas.find((m) => m.caminho === caminho)?.tamanho ?? 0), 0);
}

function medir(raiz) {
  const caminhos = ["AGENTS.md", "docs/agentes.md"];
  for (const nome of readdirSync(join(raiz, ".claude/agents"))) {
    if (nome.endsWith(".md")) caminhos.push(`.claude/agents/${nome}`);
  }
  for (const nome of readdirSync(join(raiz, ".claude/skills"))) {
    const caminho = `.claude/skills/${nome}/SKILL.md`;
    if (existsSync(join(raiz, caminho))) caminhos.push(caminho);
  }
  return caminhos.map((caminho) => ({ caminho, tamanho: tamanho(readFileSync(join(raiz, caminho), "utf8")) }));
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  const raiz = fileURLToPath(new URL("../../", import.meta.url));
  const medidas = medir(raiz);
  for (const m of [...medidas].sort((a, b) => b.tamanho - a.tamanho)) {
    const teto = tetoDe(m.caminho);
    console.log(`${String(m.tamanho).padStart(6)} / ${String(teto ?? "-").padStart(5)}  ${m.caminho}`);
  }
  console.log("");
  for (const [nome, caminhos] of Object.entries(PACOTES)) {
    const total = totalDoPacote(medidas, caminhos);
    console.log(`Pacote ${nome}: ${total} caracteres (~${Math.round(total / 3.5 / 1000)} mil tokens)`);
  }
  const acima = estourados(medidas);
  if (acima.length > 0) {
    console.error(`\n✖ ${acima.length} arquivo(s) acima do teto:`);
    for (const m of acima) console.error(`  ${m.caminho}: ${m.tamanho} > ${m.teto}`);
    console.error("Enxugue o arquivo ou mova o detalhe para um doc lido só quando preciso.");
    process.exit(1);
  }
}
