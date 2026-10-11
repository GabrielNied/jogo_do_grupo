// Gera os .meta que faltam para scripts C# e pastas, sem abrir o Unity.
//
// Para esses dois casos o .meta não tem nada que dependa do Unity: é o texto fixo abaixo
// mais um guid novo. É o mesmo conteúdo que o Unity grava neste projeto, então dá para
// gerar em qualquer máquina (inclusive sem Unity instalado).
//
// Qualquer outro arquivo (prefab, material, textura, .asmdef, .uss...) tem configuração de
// importação que só o Unity sabe escrever: o script só avisa e continua valendo
// npm run unity:importar.
//
// Nunca altera um .meta que já existe: trocar o guid quebra as referências.
//
// Uso:
//   npm run unity:meta               gera os que faltam
//   npm run unity:meta -- --conferir só lista; sai 1 se falta algum

import { randomUUID } from "node:crypto";
import { existsSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const RAIZ = resolve(dirname(fileURLToPath(import.meta.url)), "../..");

/** Pastas que o Unity importa. Os .meta das próprias raízes não entram no repo. */
export const RAIZES_UNITY = ["client/Assets", "shared/Package"];

export function novoGuid() {
  return randomUUID().replaceAll("-", "");
}

/** O Unity grava o .meta de script sem quebra de linha no fim. */
export function metaDeScript(guid) {
  return `fileFormatVersion: 2\nguid: ${guid}`;
}

export function metaDePasta(guid) {
  return [
    "fileFormatVersion: 2",
    `guid: ${guid}`,
    "folderAsset: yes",
    "DefaultImporter:",
    "  externalObjects: {}",
    "  userData: ",
    "  assetBundleName: ",
    "  assetBundleVariant: ",
    "",
  ].join("\n");
}

/** O Unity não importa (e não cria .meta para) nomes que começam com ponto ou terminam com ~. */
export function ignoradoPeloUnity(nome) {
  return nome.startsWith(".") || nome.endsWith("~") || nome === "cvs" || nome.endsWith(".tmp");
}

/**
 * Procura o que está sem .meta dentro de `raiz`.
 * Devolve { gerar: [{ caminho, tipo }], soUnity: [caminho] }, com caminhos absolutos do .meta
 * em `gerar` e do arquivo em `soUnity`.
 */
export function procurarSemMeta(raiz) {
  const gerar = [];
  const soUnity = [];
  if (!existsSync(raiz)) return { gerar, soUnity };

  const visitar = (pasta) => {
    for (const item of readdirSync(pasta, { withFileTypes: true })) {
      if (item.name.endsWith(".meta") || ignoradoPeloUnity(item.name)) continue;
      const caminho = join(pasta, item.name);
      const meta = `${caminho}.meta`;
      if (item.isDirectory()) {
        if (!existsSync(meta)) gerar.push({ caminho: meta, tipo: "pasta" });
        visitar(caminho);
      } else if (!existsSync(meta)) {
        if (item.name.toLowerCase().endsWith(".cs")) gerar.push({ caminho: meta, tipo: "script" });
        else soUnity.push(caminho);
      }
    }
  };
  visitar(raiz);
  return { gerar, soUnity };
}

/** Grava os .meta. A flag "wx" recusa sobrescrever se outro processo criou o arquivo antes. */
export function gravar(gerar) {
  for (const { caminho, tipo } of gerar) {
    const texto = tipo === "pasta" ? metaDePasta(novoGuid()) : metaDeScript(novoGuid());
    writeFileSync(caminho, texto, { encoding: "utf8", flag: "wx" });
  }
}

function principal() {
  const conferir = process.argv.includes("--conferir");
  const gerar = [];
  const soUnity = [];
  for (const raiz of RAIZES_UNITY) {
    const achado = procurarSemMeta(join(RAIZ, raiz));
    gerar.push(...achado.gerar);
    soUnity.push(...achado.soUnity);
  }
  const mostrar = (caminho) => relative(RAIZ, caminho).replaceAll("\\", "/");

  if (soUnity.length > 0) {
    console.error("Sem .meta, e só o Unity gera (npm run unity:importar, editor fechado):");
    for (const caminho of soUnity) console.error(`  ${mostrar(caminho)}`);
  }
  if (gerar.length === 0) {
    console.log("✔ Nenhum .meta de script ou pasta faltando.");
    process.exit(soUnity.length > 0 ? 1 : 0);
  }
  if (conferir) {
    console.error("Faltam .meta de script ou pasta (npm run unity:meta gera):");
    for (const { caminho } of gerar) console.error(`  ${mostrar(caminho)}`);
    process.exit(1);
  }
  gravar(gerar);
  console.log(`✔ ${gerar.length} .meta gerado(s). Inclua no commit:`);
  for (const { caminho } of gerar) console.log(`  ${mostrar(caminho)}`);
  process.exit(soUnity.length > 0 ? 1 : 0);
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) principal();
