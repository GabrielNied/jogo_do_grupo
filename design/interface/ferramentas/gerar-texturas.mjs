// Gera as texturas da interface Asterra (molduras, botões, slots, barras, anéis) por código,
// sem programa de desenho: cada peça é uma função de distância com antisserrilhado. Saída em
// client/Assets/_Game/Core/UI/Visual/Resources/Lumora/Arte/Hud/. O Unity cria os .meta ao importar.
//
// Uso (da raiz): node design/interface/ferramentas/gerar-texturas.mjs
// As cores daqui são só da arte rasterizada; as cores da interface continuam em config-visual.json.

import { deflateSync } from "node:zlib";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
export const pastaSaida = resolve(raiz, "client/Assets/_Game/Core/UI/Visual/Resources/Lumora/Arte/Hud");

// ---------- PNG ----------
const tabelaCrc = new Uint32Array(256).map((_, n) => {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  return c >>> 0;
});
function crc32(buf) {
  let c = 0xffffffff;
  for (const b of buf) c = tabelaCrc[(c ^ b) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}
function bloco(tipo, dados) {
  const t = Buffer.from(tipo, "ascii");
  const tam = Buffer.alloc(4);
  tam.writeUInt32BE(dados.length);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(Buffer.concat([t, dados])));
  return Buffer.concat([tam, t, dados, crc]);
}
export function png(tela) {
  const { w, h, px } = tela;
  const bruto = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) {
    bruto[y * (w * 4 + 1)] = 0;
    for (let x = 0; x < w; x++) {
      const i = (y * w + x) * 4;
      const a = px[i + 3];
      const o = y * (w * 4 + 1) + 1 + x * 4;
      // Interno é pré-multiplicado; o PNG guarda alfa direto.
      bruto[o] = a > 0 ? Math.round(Math.min(1, px[i] / a) * 255) : 0;
      bruto[o + 1] = a > 0 ? Math.round(Math.min(1, px[i + 1] / a) * 255) : 0;
      bruto[o + 2] = a > 0 ? Math.round(Math.min(1, px[i + 2] / a) * 255) : 0;
      bruto[o + 3] = Math.round(Math.min(1, a) * 255);
    }
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0);
  ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    bloco("IHDR", ihdr),
    bloco("IDAT", deflateSync(bruto, { level: 9 })),
    bloco("IEND", Buffer.alloc(0)),
  ]);
}

// ---------- desenho ----------
export function tela(w, h) {
  return { w, h, px: new Float32Array(w * h * 4) };
}
export function cor(hex, a = 1) {
  const n = parseInt(hex.slice(1), 16);
  return [((n >> 16) & 255) / 255, ((n >> 8) & 255) / 255, (n & 255) / 255, a];
}
const mistura = (c1, c2, t) => c1.map((v, i) => v + (c2[i] - v) * t);
const limitar = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));

// Compõe "por cima" uma cor (alfa direto) com cobertura 0..1.
function pintar(t, x, y, c, cobertura) {
  const a = c[3] * cobertura;
  if (a <= 0) return;
  const i = (y * t.w + x) * 4;
  const p = t.px;
  p[i] = c[0] * a + p[i] * (1 - a);
  p[i + 1] = c[1] * a + p[i + 1] * (1 - a);
  p[i + 2] = c[2] * a + p[i + 2] * (1 - a);
  p[i + 3] = a + p[i + 3] * (1 - a);
}

// Distância (negativa dentro) a um retângulo arredondado.
function dRet(px, py, x, y, w, h, r) {
  const cx = x + w / 2;
  const cy = y + h / 2;
  const qx = Math.abs(px - cx) - (w / 2 - r);
  const qy = Math.abs(py - cy) - (h / 2 - r);
  return Math.hypot(Math.max(qx, 0), Math.max(qy, 0)) + Math.min(Math.max(qx, qy), 0) - r;
}
const dCirc = (px, py, cx, cy, r) => Math.hypot(px - cx, py - cy) - r;
function dLosango(px, py, cx, cy, r) {
  return (Math.abs(px - cx) + Math.abs(py - cy) - r) / Math.SQRT2;
}

// Preenche a região d<0 com cor(x,y) dada por função.
export function preencher(t, dist, corEm) {
  for (let y = 0; y < t.h; y++)
    for (let x = 0; x < t.w; x++) {
      const d = dist(x + 0.5, y + 0.5);
      const cob = limitar(0.5 - d);
      if (cob > 0) pintar(t, x, y, corEm(x + 0.5, y + 0.5), cob);
    }
}
// Contorno de largura "l" do lado de dentro da borda (d entre -l e 0).
export function contornar(t, dist, l, corEm) {
  for (let y = 0; y < t.h; y++)
    for (let x = 0; x < t.w; x++) {
      const d = dist(x + 0.5, y + 0.5);
      const cob = limitar(0.5 - d) * limitar(0.5 + d + l);
      if (cob > 0) pintar(t, x, y, corEm(x + 0.5, y + 0.5), cob);
    }
}
const vertical = (c1, c2, y0, y1) => (_, y) => mistura(c1, c2, limitar((y - y0) / (y1 - y0)));
const fixa = (c) => () => c;

// Ruído determinístico para textura leve de couro/pedra.
function ruido(x, y, semente = 1) {
  const s = Math.sin(x * 12.9898 + y * 78.233 + semente * 37.719) * 43758.5453;
  return s - Math.floor(s);
}
function granular(t, intensidade, semente = 1) {
  for (let y = 0; y < t.h; y++)
    for (let x = 0; x < t.w; x++) {
      const i = (y * t.w + x) * 4;
      if (t.px[i + 3] <= 0) continue;
      const n = (ruido(x, y, semente) - 0.5) * intensidade * t.px[i + 3];
      t.px[i] = limitar(t.px[i] + n, 0, t.px[i + 3]);
      t.px[i + 1] = limitar(t.px[i + 1] + n, 0, t.px[i + 3]);
      t.px[i + 2] = limitar(t.px[i + 2] + n, 0, t.px[i + 3]);
    }
}

// ---------- paleta da arte ----------
const P = {
  preto: cor("#000000"),
  ouroClaro: cor("#E9CB84"),
  ouro: cor("#C99E52"),
  ouroEscuro: cor("#6E4F25"),
  bronze: cor("#8A6A3C"),
  corpoTopo: cor("#231B14"),
  corpoBase: cor("#110D0A"),
  linhaInterna: cor("#070504"),
};

// Moldura de janela: borda dourada dupla, cantos com losango e corpo em gradiente escuro.
function janela() {
  const t = tela(96, 96);
  const ret = (m, r) => (x, y) => dRet(x, y, m, m, 96 - 2 * m, 96 - 2 * m, r);
  preencher(t, ret(2, 6), vertical(cor("#231B14", 0.97), cor("#0F0C09", 0.97), 0, 96));
  granular(t, 0.035, 3);
  contornar(t, ret(0, 8), 1.2, fixa(cor("#000000", 0.85)));
  contornar(t, ret(1, 7), 2, vertical(P.ouroClaro, P.ouroEscuro, 0, 96));
  contornar(t, ret(3, 5), 1, fixa(cor("#050403", 0.9)));
  contornar(t, ret(6, 3), 1, fixa(cor("#C99E52", 0.28)));
  for (const [cx, cy] of [[8, 8], [88, 8], [8, 88], [88, 88]]) {
    preencher(t, (x, y) => dLosango(x, y, cx, cy, 6.5), fixa(cor("#1A130C")));
    contornar(t, (x, y) => dLosango(x, y, cx, cy, 6.5), 1.4, fixa(P.ouroClaro));
    preencher(t, (x, y) => dLosango(x, y, cx, cy, 2.2), fixa(P.ouro));
  }
  return t;
}

// Faixa do título da janela: um pouco mais clara, com filete dourado embaixo.
function cabecalho() {
  const t = tela(64, 48);
  preencher(t, (x, y) => dRet(x, y, 0, 0, 64, 46, 0), vertical(cor("#3A2C1E", 0.9), cor("#1A130D", 0.0), 0, 46));
  preencher(t, (x, y) => dRet(x, y, 0, 45, 64, 1.2, 0), fixa(cor("#C99E52", 0.95)));
  preencher(t, (x, y) => dRet(x, y, 0, 46.4, 64, 1, 0), fixa(cor("#000000", 0.7)));
  return t;
}

// Botões: gradiente, borda e brilho fino no topo.
function botao(topo, base, borda, brilho = 0.16, raio = 4) {
  const t = tela(48, 48);
  const forma = (m, r) => (x, y) => dRet(x, y, m, m, 48 - 2 * m, 48 - 2 * m, r);
  preencher(t, forma(0.5, raio + 1), fixa(cor("#000000", 0.75)));
  preencher(t, forma(1.5, raio), vertical(cor(topo), cor(base), 2, 46));
  granular(t, 0.025, 7);
  contornar(t, forma(1.5, raio), 1.2, fixa(cor(borda)));
  contornar(t, forma(2.7, raio - 1), 1, (_, y) => cor("#FFF1D0", y < 12 ? brilho : brilho * 0.15));
  return t;
}

// Slot de item/ação: fundo fundo, chanfro claro em cima/esquerda e escuro embaixo/direita.
function slot(bordaClara, bordaEscura, centro = "#1B1612") {
  const t = tela(64, 64);
  preencher(t, (x, y) => dRet(x, y, 0, 0, 64, 64, 4), fixa(cor("#000000", 0.92)));
  preencher(t, (x, y) => dRet(x, y, 1.5, 1.5, 61, 61, 3), (x, y) => {
    const d = Math.hypot(x - 32, y - 28) / 40;
    return mistura(cor(centro), cor("#070605"), limitar(d));
  });
  contornar(t, (x, y) => dRet(x, y, 1.5, 1.5, 61, 61, 3), 2, (x, y) => mistura(cor(bordaClara), cor(bordaEscura), limitar((x + y - 20) / 90)));
  contornar(t, (x, y) => dRet(x, y, 3.5, 3.5, 57, 57, 2), 1, fixa(cor("#000000", 0.8)));
  return t;
}

// Fundo das barras de recurso.
function barraFundo() {
  const t = tela(32, 16);
  preencher(t, (x, y) => dRet(x, y, 0, 0, 32, 16, 3), fixa(cor("#000000", 0.95)));
  preencher(t, (x, y) => dRet(x, y, 1, 1, 30, 14, 2), vertical(cor("#050403"), cor("#17120E"), 1, 15));
  contornar(t, (x, y) => dRet(x, y, 1, 1, 30, 14, 2), 1, fixa(cor("#5A4630", 0.9)));
  return t;
}

// Preenchimento branco com volume, para ser tingido por -unity-background-image-tint-color.
function barraPreenchimento() {
  const t = tela(8, 32);
  preencher(t, (x, y) => dRet(x, y, 0, 0, 8, 32, 0), (_, y) => {
    const v = y < 4 ? 1 : y < 16 ? 0.92 - (y - 4) * 0.015 : 0.74 - (y - 16) * 0.012;
    return [v, v, v, 1];
  });
  preencher(t, (x, y) => dRet(x, y, 0, 3, 8, 1.2, 0), fixa(cor("#FFFFFF", 0.55)));
  return t;
}

// Anel do retrato: ouro com sombra interna; centro transparente.
function anel(lado, raioExterno, espessura, enfeites) {
  const t = tela(lado, lado);
  const c = lado / 2;
  const fora = (x, y) => dCirc(x, y, c, c, raioExterno);
  const dentro = (x, y) => -dCirc(x, y, c, c, raioExterno - espessura);
  const faixa = (x, y) => Math.max(fora(x, y), dentro(x, y));
  preencher(t, (x, y) => Math.max(dCirc(x, y, c, c, raioExterno + 1.5), -dCirc(x, y, c, c, raioExterno - espessura - 2)), fixa(cor("#000000", 0.9)));
  preencher(t, faixa, (x, y) => mistura(P.ouroClaro, P.ouroEscuro, limitar((y - (c - raioExterno)) / (2 * raioExterno))));
  contornar(t, (x, y) => dCirc(x, y, c, c, raioExterno - espessura * 0.45), 1, fixa(cor("#3A2810", 0.8)));
  contornar(t, (x, y) => dCirc(x, y, c, c, raioExterno - 0.5), 1, fixa(cor("#FFF0C8", 0.5)));
  if (enfeites) {
    for (const ang of [0, 90, 180, 270]) {
      const r = (ang * Math.PI) / 180;
      const ex = c + Math.sin(r) * (raioExterno - espessura / 2);
      const ey = c - Math.cos(r) * (raioExterno - espessura / 2);
      const tam = ang === 0 ? espessura * 0.95 : espessura * 0.7;
      preencher(t, (x, y) => dLosango(x, y, ex, ey, tam), fixa(cor("#1A130C")));
      contornar(t, (x, y) => dLosango(x, y, ex, ey, tam), 1.6, fixa(P.ouroClaro));
      preencher(t, (x, y) => dLosango(x, y, ex, ey, tam * 0.35), fixa(ang === 0 ? cor("#D94A32") : P.ouro));
    }
  }
  return t;
}

// Dica (tooltip): quase preta, borda fina.
function dica() {
  const t = tela(48, 48);
  preencher(t, (x, y) => dRet(x, y, 0, 0, 48, 48, 4), fixa(cor("#000000", 0.9)));
  preencher(t, (x, y) => dRet(x, y, 1, 1, 46, 46, 3), vertical(cor("#17120E", 0.97), cor("#0B0907", 0.97), 0, 48));
  contornar(t, (x, y) => dRet(x, y, 1, 1, 46, 46, 3), 1, fixa(cor("#7A6545")));
  return t;
}

// Abas: topo arredondado.
function aba(ativa) {
  const t = tela(48, 40);
  const forma = (x, y) => dRet(x, y, 1, 1, 46, 46, 5);
  preencher(t, forma, vertical(cor(ativa ? "#4A3826" : "#251C14", 0.98), cor(ativa ? "#21180F" : "#120E0A", 0.98), 0, 40));
  contornar(t, forma, ativa ? 1.4 : 1, fixa(cor(ativa ? "#D8B266" : "#5E4A33")));
  if (ativa) preencher(t, (x, y) => dRet(x, y, 6, 2.2, 36, 1.2, 0), fixa(cor("#FFE7B0", 0.5)));
  return t;
}

// Vinheta radial para telas de menu.
function vinheta() {
  const t = tela(256, 256);
  preencher(t, (x, y) => dRet(x, y, 0, 0, 256, 256, 0), (x, y) => {
    const d = Math.hypot((x - 128) / 128, (y - 128) / 128) / Math.SQRT2;
    return cor("#000000", limitar((d - 0.35) / 0.65) ** 1.6 * 0.92);
  });
  return t;
}

// Separador decorativo: filete que some nas pontas e losango no meio.
function separador() {
  const t = tela(256, 16);
  preencher(t, (x, y) => dRet(x, y, 0, 7.4, 256, 1.2, 0), (x) => cor("#C99E52", limitar(1 - Math.abs(x - 128) / 128) ** 0.8));
  preencher(t, (x, y) => dLosango(x, y, 128, 8, 5), fixa(cor("#1A130C")));
  contornar(t, (x, y) => dLosango(x, y, 128, 8, 5), 1.3, fixa(P.ouroClaro));
  return t;
}

// Placa da bússola: faixa escura que esmaece nas pontas.
function bussola() {
  const t = tela(256, 32);
  preencher(t, (x, y) => dRet(x, y, 0, 3, 256, 26, 0), (x, y) => {
    const borda = limitar(1 - Math.abs(x - 128) / 128) ** 0.6;
    return cor(y < 16 ? "#16110C" : "#0D0A07", 0.78 * borda);
  });
  preencher(t, (x, y) => dRet(x, y, 0, 2, 256, 1, 0), (x) => cor("#C99E52", 0.8 * limitar(1 - Math.abs(x - 128) / 128) ** 0.7));
  preencher(t, (x, y) => dRet(x, y, 0, 29, 256, 1, 0), (x) => cor("#C99E52", 0.8 * limitar(1 - Math.abs(x - 128) / 128) ** 0.7));
  return t;
}

// Placa do quadro de unidade (retrato + barras).
function placa() {
  const t = tela(96, 64);
  const forma = (x, y) => dRet(x, y, 1, 1, 94, 62, 6);
  preencher(t, forma, vertical(cor("#1E1711", 0.9), cor("#0C0907", 0.9), 0, 64));
  contornar(t, forma, 1.2, vertical(cor("#B8924F"), cor("#5C4220"), 0, 64));
  contornar(t, (x, y) => dRet(x, y, 2.2, 2.2, 91.6, 59.6, 5), 1, fixa(cor("#000000", 0.7)));
  return t;
}

// Fundo da barra de ação: placa larga com acabamento dourado em cima.
function placaAcao() {
  const t = tela(96, 64);
  const forma = (x, y) => dRet(x, y, 1, 1, 94, 70, 8);
  preencher(t, forma, vertical(cor("#1E1711", 0.88), cor("#0A0806", 0.95), 0, 64));
  contornar(t, forma, 1.4, vertical(cor("#D2AC62"), cor("#5C4220", 0.4), 0, 64));
  preencher(t, (x, y) => dRet(x, y, 10, 3.2, 76, 1, 0), fixa(cor("#FFE2A0", 0.25)));
  return t;
}

// Moldura do alvo (inimigo comum/elite/chefe): placa com chifres simples nas pontas.
function placaAlvo(corBorda) {
  const t = tela(128, 48);
  const forma = (x, y) => dRet(x, y, 8, 8, 112, 32, 5);
  preencher(t, forma, vertical(cor("#1E1711", 0.92), cor("#0C0907", 0.92), 8, 40));
  contornar(t, forma, 1.4, fixa(cor(corBorda)));
  for (const cx of [8, 120]) {
    preencher(t, (x, y) => dLosango(x, y, cx, 24, 7), fixa(cor("#1A130C")));
    contornar(t, (x, y) => dLosango(x, y, cx, 24, 7), 1.4, fixa(cor(corBorda)));
    preencher(t, (x, y) => dLosango(x, y, cx, 24, 2.5), fixa(cor(corBorda)));
  }
  return t;
}

export const texturas = {
  "janela.png": janela,
  "cabecalho.png": cabecalho,
  "botao.png": () => botao("#3B2F22", "#1E1711", "#8E6E3C"),
  "botao-hover.png": () => botao("#56432E", "#2C2117", "#D8B266", 0.22),
  "botao-ativo.png": () => botao("#17120D", "#2D2318", "#D8B266", 0.05),
  "botao-desabilitado.png": () => botao("#2A2622", "#1A1815", "#4A443C", 0.04),
  "botao-principal.png": () => botao("#A3321F", "#5A140B", "#E2BC6A", 0.22),
  "botao-principal-hover.png": () => botao("#C2432B", "#741C10", "#F6DA92", 0.3),
  "botao-principal-ativo.png": () => botao("#5A140B", "#8E2818", "#F6DA92", 0.05),
  "slot.png": () => slot("#6E5A42", "#241C14"),
  "slot-hover.png": () => slot("#F1D38A", "#8A6A3C", "#2A2118"),
  "slot-vazio.png": () => slot("#3E3328", "#18130F", "#120F0C"),
  "barra-fundo.png": barraFundo,
  "barra-preenchimento.png": barraPreenchimento,
  "retrato.png": () => anel(128, 62, 9, false),
  "minimapa.png": () => anel(256, 126, 13, true),
  "dica.png": dica,
  "aba.png": () => aba(false),
  "aba-ativa.png": () => aba(true),
  "vinheta.png": vinheta,
  "separador.png": separador,
  "bussola.png": bussola,
  "placa.png": placa,
  "placa-acao.png": placaAcao,
  "placa-alvo.png": () => placaAlvo("#9C7B45"),
  "placa-alvo-elite.png": () => placaAlvo("#E7C36F"),
  "placa-alvo-chefe.png": () => placaAlvo("#D9573C"),
};

export function gerarTudo(pasta = pastaSaida) {
  mkdirSync(pasta, { recursive: true });
  for (const [nome, fazer] of Object.entries(texturas)) writeFileSync(resolve(pasta, nome), png(fazer()));
  return Object.keys(texturas);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const destino = process.argv[2] ? resolve(process.argv[2]) : pastaSaida;
  console.log(`Texturas geradas em ${destino}: ${gerarTudo(destino).join(", ")}`);
}
