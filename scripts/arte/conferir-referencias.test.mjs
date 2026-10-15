import { test } from "node:test";
import assert from "node:assert/strict";
import { conferir, lerImagem } from "./conferir-referencias.mjs";

function png(largura, altura) {
  const buf = Buffer.alloc(24);
  Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]).copy(buf);
  buf.writeUInt32BE(largura, 16);
  buf.writeUInt32BE(altura, 20);
  return buf;
}

function jpeg(largura, altura) {
  // SOI, APP0 vazio (tamanho 2), SOF0 com altura e largura
  return Buffer.from([0xff, 0xd8, 0xff, 0xe0, 0x00, 0x02, 0xff, 0xc0, 0x00, 0x11, 0x08, altura >> 8, altura & 255, largura >> 8, largura & 255, 0x03]);
}

const entrada = (extra = {}) => ({
  id: "x",
  filename: "x.png",
  relative_path: "docs/arte/referencias-visuais/materiais/x.png",
  game: "Jogo",
  category: "materiais",
  source_url: "https://exemplo.com/x.png",
  retrieved_at: "2026-10-14T00:00:00Z",
  width: 10,
  height: 20,
  format: "png",
  artistic_principles: [],
  usage_notes: "",
  license_note: "desconhecido",
  verification_status: "verificado",
  ...extra,
});

test("lerImagem lê PNG e JPEG e recusa HTML", () => {
  assert.deepEqual(lerImagem(png(10, 20)), { format: "png", width: 10, height: 20 });
  assert.deepEqual(lerImagem(jpeg(1920, 1080)), { format: "jpeg", width: 1920, height: 1080 });
  assert.equal(lerImagem(Buffer.from("<html>erro</html>")), null);
});

test("manifesto em dia com o disco não tem problema", () => {
  const e = entrada();
  assert.deepEqual(conferir({ imagens: [e] }, () => png(10, 20), () => [e.relative_path]), []);
});

test("aponta dimensão errada, arquivo faltando e imagem fora do manifesto", () => {
  const e = entrada();
  assert.match(conferir({ imagens: [e] }, () => png(11, 20), () => [])[0], /11x20/);
  assert.match(conferir({ imagens: [e] }, () => null, () => [])[0], /não existe/);
  const sobra = conferir({ imagens: [e] }, () => png(10, 20), () => [e.relative_path, "docs/arte/referencias-visuais/a.png"]);
  assert.deepEqual(sobra, ["docs/arte/referencias-visuais/a.png: imagem fora do manifesto"]);
});

test("imagem externa exige source_url e pasta da categoria", () => {
  const e = entrada({ source_url: null, relative_path: "docs/arte/referencias-visuais/luz-cor/x.png" });
  const erros = conferir({ imagens: [e] }, () => png(10, 20), () => [e.relative_path]);
  assert.ok(erros.some((m) => /source_url/.test(m)));
  assert.ok(erros.some((m) => /deveria estar em/.test(m)));
});
