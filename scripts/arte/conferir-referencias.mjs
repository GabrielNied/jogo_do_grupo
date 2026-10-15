// Confere docs/arte/referencias-visuais/manifesto-referencias.json contra o disco:
// cada entrada existe, é PNG/JPEG de verdade, com o formato e as dimensões do manifesto,
// e toda imagem da pasta está no manifesto. Uso: npm run arte:referencias
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const RAIZ = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const PASTA = "docs/arte/referencias-visuais";
const CAMPOS = [
  "id",
  "filename",
  "relative_path",
  "game",
  "category",
  "source_url",
  "retrieved_at",
  "width",
  "height",
  "format",
  "artistic_principles",
  "usage_notes",
  "license_note",
  "verification_status",
];
const EXTENSOES = new Set([".png", ".jpg", ".jpeg", ".webp", ".gif"]);

/** Lê formato e dimensões do cabeçalho de um PNG ou JPEG; null se não for nenhum dos dois. */
export function lerImagem(buf) {
  const png = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
  if (buf.length >= 24 && buf.subarray(0, 8).equals(png))
    return { format: "png", width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) };
  if (buf.length < 4 || buf[0] !== 0xff || buf[1] !== 0xd8) return null;
  let i = 2;
  while (i + 9 < buf.length) {
    if (buf[i] !== 0xff) return null;
    const m = buf[i + 1];
    if (m >= 0xc0 && m <= 0xcf && m !== 0xc4 && m !== 0xc8 && m !== 0xcc)
      return { format: "jpeg", height: buf.readUInt16BE(i + 5), width: buf.readUInt16BE(i + 7) };
    i += 2 + buf.readUInt16BE(i + 2);
  }
  return null;
}

/** Devolve a lista de problemas do manifesto; `ler(caminho)` dá o Buffer ou null, `listar()` os arquivos da pasta. */
export function conferir(manifesto, ler, listar) {
  const erros = [];
  const ids = new Set();
  const caminhos = new Set();
  for (const e of manifesto.imagens ?? []) {
    const nome = e.id ?? JSON.stringify(e).slice(0, 40);
    for (const c of CAMPOS) if (!(c in e)) erros.push(`${nome}: falta o campo ${c}`);
    if (ids.has(e.id)) erros.push(`${nome}: id repetido`);
    ids.add(e.id);
    caminhos.add(e.relative_path);
    if (path.posix.basename(e.relative_path ?? "") !== e.filename)
      erros.push(`${nome}: filename não bate com relative_path`);
    const externa = e.category !== "assets-do-projeto";
    if (externa) {
      if (e.relative_path !== `${PASTA}/${e.category}/${e.filename}`)
        erros.push(`${nome}: deveria estar em ${PASTA}/${e.category}/`);
      if (!/^https:\/\//.test(e.source_url ?? "")) erros.push(`${nome}: source_url ausente`);
      if (Number.isNaN(Date.parse(e.retrieved_at ?? ""))) erros.push(`${nome}: retrieved_at não é ISO 8601`);
      if (!e.license_note) erros.push(`${nome}: license_note vazio (use "desconhecido")`);
    }
    const buf = ler(e.relative_path);
    if (!buf) {
      erros.push(`${nome}: arquivo não existe (${e.relative_path}); rode npm run arte:baixar-referencias`);
      continue;
    }
    const img = lerImagem(buf);
    if (!img) {
      erros.push(`${nome}: não é PNG nem JPEG válido (ponteiro LFS? rode git lfs pull)`);
      continue;
    }
    if (img.format !== e.format || img.width !== e.width || img.height !== e.height)
      erros.push(
        `${nome}: manifesto diz ${e.format} ${e.width}x${e.height}, arquivo é ${img.format} ${img.width}x${img.height}`
      );
  }
  for (const arq of listar())
    if (!caminhos.has(arq)) erros.push(`${arq}: imagem fora do manifesto`);
  return erros;
}

function listarImagens(dir) {
  const saida = [];
  for (const ent of fs.readdirSync(path.join(RAIZ, dir), { withFileTypes: true })) {
    const rel = `${dir}/${ent.name}`;
    if (ent.isDirectory()) saida.push(...listarImagens(rel));
    else if (EXTENSOES.has(path.extname(ent.name).toLowerCase())) saida.push(rel);
  }
  return saida;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const manifesto = JSON.parse(fs.readFileSync(path.join(RAIZ, PASTA, "manifesto-referencias.json"), "utf8"));
  const ler = (rel) => {
    const abs = path.join(RAIZ, rel ?? "");
    return rel && fs.existsSync(abs) ? fs.readFileSync(abs) : null;
  };
  const erros = conferir(manifesto, ler, () => listarImagens(PASTA));
  const porCategoria = {};
  for (const e of manifesto.imagens) porCategoria[e.category] = (porCategoria[e.category] ?? 0) + 1;
  console.log(Object.entries(porCategoria).map(([c, n]) => `${c}: ${n}`).join(", "));
  if (erros.length) {
    console.error(`✖ ${erros.length} problema(s):\n- ${erros.join("\n- ")}`);
    process.exit(1);
  }
  console.log(`✔ ${manifesto.imagens.length} imagens conferidas com o disco.`);
}
