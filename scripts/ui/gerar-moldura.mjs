// Gera as texturas 9-slice da interface (arte nossa, sem pacote de terceiro).
// Rode na raiz: node scripts/ui/gerar-moldura.mjs
// Saída: client/Assets/_Game/Core/UI/Resources/UI/*.png
// O Unity cria os .meta ao importar. Não escreva .meta à mão.

import { deflateSync } from "node:zlib";
import { writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { crc32 } from "node:zlib";

const pasta = join(
  dirname(fileURLToPath(import.meta.url)),
  "../../client/Assets/_Game/Core/UI/Resources/UI",
);

const ouro = [212, 168, 84];
const ouroClaro = [244, 214, 140];
const ouroEscuro = [120, 84, 36];
const fundo = [22, 16, 12];
const fundoCampo = [10, 8, 6];
const sombra = [6, 4, 3];

function imagem(largura, altura, pintar) {
  const pixels = new Uint8Array(largura * altura * 4);
  for (let y = 0; y < altura; y++) {
    for (let x = 0; x < largura; x++) {
      const cor = pintar(x, y, largura, altura);
      const i = (y * largura + x) * 4;
      pixels[i] = cor[0];
      pixels[i + 1] = cor[1];
      pixels[i + 2] = cor[2];
      pixels[i + 3] = cor[3] ?? 255;
    }
  }
  return png(largura, altura, pixels);
}

function misturar(a, b, t) {
  return [
    Math.round(a[0] + (b[0] - a[0]) * t),
    Math.round(a[1] + (b[1] - a[1]) * t),
    Math.round(a[2] + (b[2] - a[2]) * t),
    255,
  ];
}

function distanciaDaBorda(x, y, largura, altura) {
  return Math.min(x, y, largura - 1 - x, altura - 1 - y);
}

function canto(x, y, largura, altura, margem) {
  const esquerda = x < margem;
  const direita = x >= largura - margem;
  const cima = y < margem;
  const baixo = y >= altura - margem;
  return (esquerda || direita) && (cima || baixo);
}

function moldura(x, y, largura, altura) {
  const d = distanciaDaBorda(x, y, largura, altura);
  const noCanto = canto(x, y, largura, altura, 32);
  if (d === 0) return [...sombra, 255];
  if (d < 3) return misturar(ouroEscuro, sombra, d === 1 ? 0.35 : 0);
  if (d < 8) {
    const t = (d - 3) / 5;
    const base = y < altura * 0.45 ? ouroClaro : ouro;
    return misturar(base, ouroEscuro, t * 0.55);
  }
  if (d < 10) return [...sombra, 255];
  if (d < 13) return misturar(ouroEscuro, fundo, 0.45);
  if (noCanto && d < 22) {
    const cx = x < largura / 2 ? 16 : largura - 17;
    const cy = y < altura / 2 ? 16 : altura - 17;
    const dist = Math.abs(x - cx) + Math.abs(y - cy);
    if (dist < 7) return dist < 3 ? ouroClaro.concat(255) : ouro.concat(255);
  }
  const luz = 1 - y / altura;
  return misturar(fundo, [34, 26, 18], luz * 0.35);
}

function botao(x, y, largura, altura, claro) {
  const d = distanciaDaBorda(x, y, largura, altura);
  const preenchimento = claro ? [112, 76, 28] : [58, 42, 24];
  const topo = claro ? [168, 112, 40] : [78, 56, 32];
  if (d === 0) return [...sombra, 255];
  if (d < 3) return y < 3 ? ouroClaro.concat(255) : ouroEscuro.concat(255);
  if (d < 6) return (y < altura / 2 ? ouro : ouroEscuro).concat(255);
  const t = y / (altura - 1);
  return misturar(topo, preenchimento, t);
}

function campo(x, y, largura, altura) {
  const d = distanciaDaBorda(x, y, largura, altura);
  if (d === 0) return [...ouroEscuro, 255];
  if (d < 3) return [...ouro, 255];
  if (d < 5) return [...sombra, 255];
  return [...fundoCampo, 255];
}

function slot(x, y, largura, altura) {
  const d = distanciaDaBorda(x, y, largura, altura);
  if (d === 0) return [...ouroEscuro, 255];
  if (d < 2) return [...ouro, 255];
  if (d < 4) return [...sombra, 255];
  return [28, 22, 16, 255];
}

function png(largura, altura, rgba) {
  const bruto = Buffer.alloc((largura * 4 + 1) * altura);
  for (let y = 0; y < altura; y++) {
    const linha = y * (largura * 4 + 1);
    bruto[linha] = 0;
    rgba.subarray(y * largura * 4, (y + 1) * largura * 4).forEach((valor, i) => {
      bruto[linha + 1 + i] = valor;
    });
  }
  const comprimido = deflateSync(bruto);
  const assinatura = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);
  return Buffer.concat([
    assinatura,
    pedaco("IHDR", ihdr(largura, altura)),
    pedaco("IDAT", comprimido),
    pedaco("IEND", Buffer.alloc(0)),
  ]);
}

function ihdr(largura, altura) {
  const buffer = Buffer.alloc(13);
  buffer.writeUInt32BE(largura, 0);
  buffer.writeUInt32BE(altura, 4);
  buffer[8] = 8;
  buffer[9] = 6;
  return buffer;
}

function pedaco(tipo, dados) {
  const nome = Buffer.from(tipo);
  const tamanho = Buffer.alloc(4);
  tamanho.writeUInt32BE(dados.length, 0);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(Buffer.concat([nome, dados])) >>> 0, 0);
  return Buffer.concat([tamanho, nome, dados, crc]);
}

mkdirSync(pasta, { recursive: true });
writeFileSync(join(pasta, "moldura.png"), imagem(128, 128, moldura));
writeFileSync(join(pasta, "botao.png"), imagem(64, 48, (x, y, w, h) => botao(x, y, w, h, false)));
writeFileSync(
  join(pasta, "botao-principal.png"),
  imagem(64, 48, (x, y, w, h) => botao(x, y, w, h, true)),
);
writeFileSync(join(pasta, "campo.png"), imagem(32, 32, campo));
writeFileSync(join(pasta, "slot.png"), imagem(40, 40, slot));
console.log("Texturas gravadas em", pasta);
