// Exporta o board do FigJam (brainstorm) para docs/brainstorm/figjam.md,
// para que pessoas e qualquer IA leiam as ideias sem acessar o Figma.
//
// Uso:
//   npm run figma:exportar                    lê o board pela API REST da Figma
//   npm run figma:exportar -- --json <arq>    converte um JSON já baixado (formato GET /v1/files)
//
// Precisa de um token pessoal da Figma em FIGMA_TOKEN (variável de ambiente ou arquivo .env
// na raiz, que está no .gitignore). Veja docs/brainstorm/README.md.

import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const RAIZ = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const FILE_KEY = "VMO2jdDLMZGrZpL4SGGHF7";
const URL_BOARD = `https://www.figma.com/board/${FILE_KEY}`;
const SAIDA = resolve(RAIZ, "docs/brainstorm/figjam.md");

function carregarEnv() {
  const arquivo = resolve(RAIZ, ".env");
  if (!existsSync(arquivo)) return;
  for (const linha of readFileSync(arquivo, "utf8").split(/\r?\n/)) {
    const m = linha.match(/^\s*([A-Z_][A-Z0-9_]*)\s*=\s*(.*?)\s*$/);
    if (m && process.env[m[1]] === undefined) process.env[m[1]] = m[2].replace(/^["']|["']$/g, "");
  }
}

async function baixarArquivo() {
  carregarEnv();
  const token = process.env.FIGMA_TOKEN;
  if (!token) {
    console.error("✖ Defina FIGMA_TOKEN (variável de ambiente ou .env). Veja docs/brainstorm/README.md.");
    process.exit(1);
  }
  const resposta = await fetch(`https://api.figma.com/v1/files/${FILE_KEY}`, {
    headers: { "X-Figma-Token": token },
  });
  if (!resposta.ok) {
    console.error(`✖ A API da Figma respondeu ${resposta.status}: ${await resposta.text()}`);
    if (resposta.status === 429) console.error("  Limite de requisições atingido; tente mais tarde.");
    process.exit(1);
  }
  return resposta.json();
}

const TIPOS_COM_TEXTO = new Set(["STICKY", "SHAPE_WITH_TEXT", "TEXT"]);

const texto = (no) => (no.characters ?? "").replace(/\s*\n+\s*/g, " ").trim();
const pos = (no) => no.absoluteBoundingBox ?? { x: 0, y: 0 };
const porLinha = (a, b) => pos(a).y - pos(b).y || pos(a).x - pos(b).x;
const porColuna = (a, b) => pos(a).x - pos(b).x || pos(a).y - pos(b).y;

// Junta os nós com texto e os conectores de uma seção (sem entrar nas subseções).
function coletar(no, textos = [], conectores = []) {
  for (const filho of no.children ?? []) {
    if (filho.type === "SECTION") continue;
    if (TIPOS_COM_TEXTO.has(filho.type) && texto(filho)) textos.push(filho);
    else if (filho.type === "CONNECTOR") conectores.push(filho);
    coletar(filho, textos, conectores);
  }
  return { textos, conectores };
}

// Com conectores, vira uma árvore (mapa mental); sem, uma lista na ordem de leitura.
function renderizarItens(textos, conectores) {
  const porId = new Map(textos.map((t) => [t.id, t]));
  const filhos = new Map();
  const temPai = new Set();
  for (const c of conectores) {
    const de = c.connectorStart?.endpointNodeId;
    const para = c.connectorEnd?.endpointNodeId;
    if (!porId.has(de) || !porId.has(para)) continue;
    if (!filhos.has(de)) filhos.set(de, []);
    filhos.get(de).push(porId.get(para));
    temPai.add(para);
  }
  const linhas = [];
  const visitados = new Set();
  const visitar = (no, nivel) => {
    if (visitados.has(no.id)) return;
    visitados.add(no.id);
    linhas.push(`${"  ".repeat(nivel)}- ${texto(no)}`);
    for (const f of (filhos.get(no.id) ?? []).sort(porLinha)) visitar(f, nivel + 1);
  };
  const raizes = textos.filter((t) => !temPai.has(t.id) && filhos.has(t.id)).sort(porLinha);
  for (const r of raizes) visitar(r, 0);
  for (const t of [...textos].sort(porLinha)) visitar(t, 0);
  return linhas;
}

function renderizarSecao(secao, nivel) {
  const linhas = [`${"#".repeat(Math.min(nivel, 6))} ${secao.name.trim()}`, ""];
  const { textos, conectores } = coletar(secao);
  const subsecoes = (secao.children ?? []).filter((f) => f.type === "SECTION").sort(porColuna);
  if (textos.length) linhas.push(...renderizarItens(textos, conectores), "");
  for (const s of subsecoes) linhas.push(...renderizarSecao(s, nivel + 1));
  if (!textos.length && !subsecoes.length) linhas.push("_(seção vazia)_", "");
  return linhas;
}

function gerarMarkdown(arquivo) {
  const linhas = [
    `# Brainstorm: ${arquivo.name ?? "FigJam"}`,
    "",
    "> **Arquivo gerado, não edite à mão.** Cópia do board do FigJam para pessoas e IAs lerem sem acessar o Figma.",
    `> Fonte: [board no FigJam](${URL_BOARD}) · última alteração no Figma: ${arquivo.lastModified ?? "desconhecida"} · gerado em ${new Date().toISOString().slice(0, 10)}.`,
    "> Para atualizar: `npm run figma:exportar` (veja [README.md](README.md)).",
    ">",
    "> São ideias **não validadas**. O que foi aprovado vive em [docs/features/](../features/).",
    "",
  ];
  for (const pagina of arquivo.document?.children ?? []) {
    const { textos, conectores } = coletar(pagina);
    const secoes = (pagina.children ?? []).filter((f) => f.type === "SECTION").sort(porColuna);
    if ((arquivo.document.children ?? []).length > 1) linhas.push(`## ${pagina.name}`, "");
    for (const s of secoes) linhas.push(...renderizarSecao(s, 2));
    if (textos.length) linhas.push("## Fora de seções", "", ...renderizarItens(textos, conectores), "");
  }
  return linhas.join("\n").replace(/\n{3,}/g, "\n\n").trimEnd() + "\n";
}

const iJson = process.argv.indexOf("--json");
const arquivo =
  iJson > -1 ? JSON.parse(readFileSync(resolve(process.argv[iJson + 1]), "utf8")) : await baixarArquivo();

mkdirSync(dirname(SAIDA), { recursive: true });
writeFileSync(SAIDA, gerarMarkdown(arquivo));
console.log(`✔ Brainstorm exportado para ${SAIDA.replace(RAIZ + "\\", "").replace(RAIZ + "/", "")}`);
