// Impede PR de client ou shared sem os arquivos que o Unity gera.
//
// Dois casos que já escaparam:
// - script novo commitado sem o .meta (o Unity ainda não tinha importado);
// - .mat/.prefab alterado pelo Unity e deixado de fora do commit.
//
// Uso: npm run conferir-unity
// No CI: node scripts/git/conferir-arquivos-unity.mjs <sha-da-base> --so-commit

import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";

const base = process.argv[2] && !process.argv[2].startsWith("--") ? process.argv[2] : "origin/main";
const soCommit = process.argv.includes("--so-commit") || process.env.CI === "true";

const extensoesSoltas = new Set([
  ".meta",
  ".mat",
  ".prefab",
  ".asset",
  ".controller",
  ".overridecontroller",
  ".anim",
  ".uss",
  ".uxml",
  ".fbx",
  ".png",
  ".jpg",
  ".tga",
  ".wav",
  ".mp3",
]);

function git(args) {
  return execFileSync("git", args, { encoding: "utf8" }).trim();
}

function existe(rev, caminho) {
  try {
    execFileSync("git", ["cat-file", "-e", `${rev}:${caminho}`], { stdio: "ignore" });
    return true;
  } catch {
    return false;
  }
}

function igualNaBase(caminho) {
  try {
    const naBase = execFileSync("git", ["show", `${base}:${caminho}`]);
    return Buffer.compare(naBase, readFileSync(caminho)) === 0;
  } catch {
    return false;
  }
}

function sobUnity(caminho) {
  return caminho.startsWith("client/Assets/") || caminho.startsWith("shared/Package/");
}

// O mesmo critério de scripts/unity/gerar-meta.mjs: o Unity não importa esses nomes
// e portanto não cria .meta. .gitkeep entra aqui.
function ignoradoPeloUnity(caminho) {
  const nome = caminho.split("/").pop();
  return nome.startsWith(".") || nome.endsWith("~") || nome === "cvs" || nome.endsWith(".tmp");
}

function extensao(caminho) {
  const ponto = caminho.lastIndexOf(".");
  return ponto === -1 ? "" : caminho.slice(ponto).toLowerCase();
}

function pastas(arquivo) {
  const partes = arquivo.split("/");
  const inicio = arquivo.startsWith("client/Assets/") || arquivo.startsWith("shared/Package/") ? 2 : -1;
  if (inicio < 0) return [];
  const lista = [];
  for (let i = inicio; i < partes.length - 1; i++) {
    lista.push(partes.slice(0, i + 1).join("/"));
  }
  return lista;
}

let adicionados;
try {
  adicionados = git(["diff", "--name-only", "--diff-filter=AR", `${base}...HEAD`])
    .split("\n")
    .filter(Boolean);
} catch {
  console.error(`✖ Não comparei com ${base}. Rode git fetch origin main.`);
  process.exit(2);
}

const faltam = [];
for (const arquivo of adicionados) {
  if (!sobUnity(arquivo) || arquivo.endsWith(".meta") || ignoradoPeloUnity(arquivo)) continue;
  const meta = `${arquivo}.meta`;
  if (!existe("HEAD", meta)) faltam.push(meta);
  for (const pasta of pastas(arquivo)) {
    const metaPasta = `${pasta}.meta`;
    if (!existe(base, metaPasta) && !existe("HEAD", metaPasta) && !faltam.includes(metaPasta)) {
      faltam.push(metaPasta);
    }
  }
}

const soltos = [];
if (!soCommit) {
  for (const linha of git(["status", "--porcelain", "-uall"]).split("\n").filter(Boolean)) {
    const codigo = linha.slice(0, 2);
    const foraDoCommit = codigo === "??" || codigo[1] === "M" || codigo[1] === "D" || codigo[1] === "A";
    if (!foraDoCommit) continue;
    const caminho = linha.slice(3).replace(/^"|"$/g, "");
    if (!sobUnity(caminho)) continue;
    if (igualNaBase(caminho)) continue;
    if (caminho.endsWith(".meta") || extensoesSoltas.has(extensao(caminho))) soltos.push(caminho);
  }
}

if (faltam.length === 0 && soltos.length === 0) {
  console.log("✔ Arquivos do Unity da branch estão no commit.");
  process.exit(0);
}

if (faltam.length > 0) {
  console.error("Faltam .meta no commit (não escreva à mão):");
  for (const arquivo of faltam) console.error(`  ${arquivo}`);
  console.error("Script C# ou pasta nova: npm run unity:meta (não precisa do Unity).");
  console.error("Outros arquivos: com o editor fechado, neste worktree, npm run unity:importar.");
}
if (soltos.length > 0) {
  console.error("O Unity alterou arquivos que ficaram fora do commit:");
  for (const arquivo of soltos) console.error(`  ${arquivo}`);
  console.error("Inclua os da sua pasta. ProjectSettings continua de fora, salvo pedido da pessoa.");
}
process.exit(1);
