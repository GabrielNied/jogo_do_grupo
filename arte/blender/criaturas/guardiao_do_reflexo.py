"""Guardiao do Reflexo (criatura 100, chefe): fera de pedra azul-ardosia com cristais espelhados.

Tema "reflexo": o rosto e uma mascara de espelho (o jogador se ve nele), e os cristais das
costas, dos ombros e dos chifres sao metal polido que reflete o ambiente. Veias e olhos
brilham em ciano. Usa o mesmo esqueleto quadrupede do javali, mais pesado e lento.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/criaturas/guardiao_do_reflexo.py [-- --previa <pasta>]
"""

import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

import numpy as np  # noqa: E402

from lib import pipeline, quadrupede, textura  # noqa: E402
from lib.malha import Construtor, Secao  # noqa: E402
from lib.textura import hex_rgb, misturar, suave  # noqa: E402

# ---------------------------------------------------------------------------------------------
# Valores ajustaveis

NOME = "GuardiaoDoReflexo"
ALTURA_ALVO = 3.5  # metros, do chao a ponta do cristal mais alto
TAMANHO_TEXTURA = 1024
SEMENTE = 100
PESO = 1.6  # clipes mais lentos que os do javali

PALETA = {
    "pedra": "#36404e",
    "pedra_escura": "#1c222c",
    "pedra_clara": "#6b7a8c",
    "fenda": "#10141a",
    "veia": "#5fe8ff",
    "cristal_fundo": "#173f6b",
    "cristal": "#5aa7d8",
    "cristal_claro": "#d4f3ff",
    "espelho": "#cfdbe6",
    "espelho_borda": "#7d8fa3",
    "olho": "#e6fdff",
    "olho_halo": "#4fd6ff",
}

# Brilho (metal, lisura) de cada material no Unity (mapa _MetallicGlossMap).
METAL_CRISTAL, LISO_CRISTAL = 0.85, 0.88
METAL_ESPELHO, LISO_ESPELHO = 1.0, 0.95
METAL_PEDRA, LISO_PEDRA = 0.0, 0.18
# Emissao: intensidade das veias, do brilho interno dos cristais e dos olhos.
EMISSAO_VEIA = 0.9
EMISSAO_CRISTAL = 0.18
EMISSAO_OLHO = 1.0

# ---------------------------------------------------------------------------------------------
# Malha (escala de modelagem: ~1,2 de altura; o pipeline leva para ALTURA_ALVO)

CORPO = [
    (0.56, 0.58, 0.15, 0.17),
    (0.46, 0.59, 0.22, 0.24),
    (0.26, 0.61, 0.26, 0.28),
    (0.02, 0.65, 0.29, 0.33),
    (-0.18, 0.70, 0.32, 0.38),
    (-0.34, 0.69, 0.30, 0.35),
    (-0.44, 0.66, 0.20, 0.25),
]
CABECA = [
    (-0.30, 0.66, 0.19, 0.24),
    (-0.44, 0.66, 0.21, 0.25),
    (-0.58, 0.61, 0.19, 0.21),
    (-0.70, 0.55, 0.15, 0.16),
    (-0.78, 0.51, 0.12, 0.12),
]
PERNA_D = (0.21, -0.24)
PERNA_T = (0.19, 0.36)
OLHO = (0.14, -0.63, 0.68)
# Mascara de espelho sobre a testa e o focinho: (y, z, meia largura).
MASCARA = [(-0.40, 0.925, 0.13), (-0.50, 0.89, 0.17), (-0.62, 0.81, 0.15), (-0.72, 0.71, 0.12), (-0.79, 0.64, 0.085)]
# Chifres de cristal: saem das temporas, abrem para o lado e curvam para a frente.
CHIFRE = [(0.16, -0.46, 0.84), (0.30, -0.47, 0.92), (0.40, -0.58, 1.02), (0.40, -0.72, 1.09)]
PONTA_CHIFRE = (0.34, -0.88, 1.12)
RAIO_CHIFRE = 0.075


def _topo(y):
    ys = [c[0] for c in CORPO][::-1]
    zs = [c[1] + c[3] for c in CORPO][::-1]
    return float(np.interp(y, ys, zs))


def construir():
    c = Construtor(NOME, semente=SEMENTE)
    c.loft(
        [Secao((0.0, y, z), rx, rz, 2.4) for y, z, rx, rz in CORPO],
        14,
        "pedra",
        "Corpo",
        ponta_ini=(0.0, 0.62, 0.58),
        variacao=0.01,
    )
    c.loft([Secao((0.0, y, z), rx, rz, 2.6) for y, z, rx, rz in CABECA], 10, "pedra", "Cabeca", tampa_ini=False, variacao=0.006)
    # Mandibula pesada.
    c.loft(
        [Secao((0.0, -0.50, 0.50), 0.15, 0.09, 2.6), Secao((0.0, -0.68, 0.45), 0.12, 0.07, 2.6), Secao((0.0, -0.80, 0.43), 0.08, 0.05, 2.6)],
        8,
        "pedra",
        "Cabeca",
        ponta_fim=(0.0, -0.84, 0.43),
    )
    # Mascara de espelho: placa facetada na frente da cabeca, da testa ao focinho.
    c.loft(
        [Secao((0.0, y, z), rx, 0.03, 2.0) for y, z, rx in MASCARA],
        6,
        "espelho",
        "Cabeca",
        ref=(0, -1, 0.8),
        lisa=False,
        giro=1 / 12,
    )
    # Olhos: brasas ciano dos lados da mascara.
    for lado in (1, -1):
        ox, oy, oz = OLHO
        c.esfera((lado * ox, oy, oz), 0.032, "olho", "Cabeca", 8, 6, (0.8, 1.0, 0.7))
    # Chifres de cristal: saem das temporas, para fora e para a frente.
    for lado in (1, -1):
        sec = [
            Secao((lado * x, y, z), RAIO_CHIFRE * (1 - 0.22 * i), RAIO_CHIFRE * (1 - 0.22 * i), 2.0)
            for i, (x, y, z) in enumerate(CHIFRE)
        ]
        ponta = (lado * PONTA_CHIFRE[0], PONTA_CHIFRE[1], PONTA_CHIFRE[2])
        c.loft(sec, 6, "cristal", "Cabeca", ref=(0, 0, 1), ponta_fim=ponta, lisa=False)

    # Barbatanas de cristal no lugar das orelhas.
    def barbatana(lado, osso):
        c.cristal((lado * 0.17, -0.40, 0.76), (lado * 0.30, -0.30, 0.86), 0.04, 4, "cristal", osso, achatado=0.5, ref=(0, 1, 0))

    c.espelhar_x(barbatana, "Orelha_E", "Orelha_D")

    # Cristais das costas: grandes na corcunda, menores para tras.
    costas = [(-0.30, 0.26, 0.07, 0.0), (-0.16, 0.36, 0.09, 0.06), (-0.02, 0.30, 0.08, -0.05), (0.12, 0.22, 0.07, 0.04), (0.26, 0.16, 0.06, -0.03), (0.38, 0.11, 0.05, 0.02)]
    for i, (y, h, r, dx) in enumerate(costas):
        z = _topo(y) - 0.05
        c.cristal((dx, y, z), (dx * 2.5, y + h * 0.45, z + h), r, 5, "cristal", "Corpo", ombro=0.65)
        if i < 4:
            for lado in (1, -1):
                hx = h * 0.55
                c.cristal(
                    (lado * 0.12 + dx, y + 0.05, z - 0.04),
                    (lado * (0.12 + hx * 0.6) + dx, y + 0.05 + hx * 0.4, z - 0.04 + hx),
                    r * 0.6,
                    4,
                    "cristal",
                    "Corpo",
                )
    # Ombreiras de cristal.
    for lado in (1, -1):
        for k, (dy, h, ang) in enumerate(((-0.02, 0.20, 50), (0.08, 0.15, 65), (-0.12, 0.13, 40))):
            a = math.radians(ang)
            base = (lado * 0.27, -0.22 + dy, 0.82)
            ponta = (lado * (0.27 + h * math.sin(a)), -0.22 + dy + 0.03, 0.82 + h * math.cos(a))
            c.cristal(base, ponta, 0.045, 5, "cristal", "Corpo")

    # Rabo curto com ponta de cristal.
    c.loft([Secao((0.0, 0.55, 0.66), 0.06), Secao((0.0, 0.68, 0.58), 0.045), Secao((0.0, 0.76, 0.48), 0.035)], 6, "pedra", "Rabo")
    c.cristal((0.0, 0.76, 0.50), (0.0, 0.86, 0.36), 0.045, 5, "cristal", "Rabo")

    # Pernas: colunas grossas, com garras de cristal.
    def perna(lado, osso, x, y, traseira):
        x *= lado
        if traseira:
            pts = [(x, y, 0.58, 0.14, 0.19), (x * 1.04, y + 0.03, 0.40, 0.12, 0.15), (x, y + 0.05, 0.22, 0.085, 0.09), (x, y + 0.02, 0.08, 0.09, 0.095), (x, y + 0.01, 0.0, 0.10, 0.11)]
        else:
            pts = [(x, y, 0.58, 0.13, 0.16), (x * 1.05, y, 0.40, 0.115, 0.13), (x, y + 0.01, 0.22, 0.085, 0.09), (x, y, 0.08, 0.09, 0.095), (x, y - 0.01, 0.0, 0.10, 0.11)]
        c.loft([Secao((px, py, pz), ru, rv, 2.4) for px, py, pz, ru, rv in pts], 8, "pedra", osso, ref=(0, -1, 0), variacao=0.006)
        yc = pts[-1][1]
        for k in (-1, 0, 1):
            base = (x + k * 0.055, yc - 0.07, 0.035)
            c.cristal(base, (x + k * 0.07, yc - 0.17, 0.0), 0.028, 4, "cristal", osso, ombro=0.5)

    c.espelhar_x(lambda s, o: perna(s, o, PERNA_D[0], PERNA_D[1], False), "Perna_DianteiraE", "Perna_DianteiraD")
    c.espelhar_x(lambda s, o: perna(s, o, PERNA_T[0], PERNA_T[1], True), "Perna_TraseiraE", "Perna_TraseiraD")
    return c


def medidas():
    return quadrupede.Medidas(
        corpo=(0.0, 0.0, 0.64),
        cabeca=(0.0, -0.40, 0.70),
        rabo=(0.0, 0.55, 0.66),
        orelha=(0.17, -0.40, 0.76),
        perna_dianteira=(PERNA_D[0], PERNA_D[1], 0.56),
        perna_traseira=(PERNA_T[0], PERNA_T[1], 0.56),
        escala=1.0,
        peso=PESO,
        altura_tombo=0.30,
        ataques_do_chefe=True,
    )


# ---------------------------------------------------------------------------------------------
# Pintura


def pintar(tex, ao):
    pal = {k: hex_rgb(v) for k, v in PALETA.items()}
    P, N = tex.pos, tex.normal
    n = len(P)
    cor = np.zeros((n, 3), dtype=np.float32)
    brilho = np.zeros((n, 4), dtype=np.float32)
    emis = np.zeros((n, 3), dtype=np.float32)

    # Pedra: placas grandes, fendas escuras, bordas claras e veias ciano.
    pedra = tex.mascara("pedra")
    g = textura.fbm(P, 4.0, 5, SEMENTE)
    placas = textura.fbm(P, 9.0, 2, SEMENTE + 1)
    fenda = np.abs(placas - 0.5)
    pc = misturar(pal["pedra"], pal["pedra_escura"], suave(0.35, 0.75, g) * 0.8)
    pc = misturar(pc, pal["pedra_clara"], suave(0.55, 0.85, N[:, 2]) * 0.35 + suave(0.6, 0.8, g) * 0.2)
    pc *= (0.85 + 0.3 * textura.fbm(P, 40.0, 2, SEMENTE + 2))[:, None]
    pc = misturar(pc, pal["fenda"], suave(0.025, 0.008, fenda) * 0.9)
    veia_d = np.abs(textura.fbm(P, 3.0, 3, SEMENTE + 3) - 0.5)
    veia = suave(0.009, 0.002, veia_d) * suave(0.5, 0.62, textura.ruido(P, 2.5, SEMENTE + 4))
    pc = misturar(pc, pal["veia"], veia * 0.85)
    pc *= textura.luz_pintada(tex, 0.3, 0.3, 0.95)[:, None]
    cor[pedra] = pc[pedra]
    brilho[pedra] = (METAL_PEDRA, 0, 0, LISO_PEDRA)
    brilho[pedra, 3] += veia[pedra] * 0.5
    emis[pedra] = (pal["veia"] * (veia * EMISSAO_VEIA)[:, None])[pedra]

    # Cristais: fundo azul profundo na base, claro nas pontas, cada faceta com um tom.
    cristal = tex.mascara("cristal")
    faceta = textura.aleatorio_por_face(tex.face, SEMENTE + 5)
    altura = suave(0.5, 1.15, P[:, 2]) * 0.6 + suave(-0.3, 0.9, N[:, 2]) * 0.4
    cc = misturar(pal["cristal_fundo"], pal["cristal"], suave(0.0, 0.7, altura * 0.6 + faceta * 0.5))
    cc = misturar(cc, pal["cristal_claro"], suave(0.75, 1.0, faceta) * 0.6 + suave(0.7, 1.0, altura) * 0.3)
    estrias = textura.listras(P, np.tile([0.0, 0.0, 1.0], (n, 1)), 3.0, 60.0, SEMENTE + 6, 1)
    cc *= (0.85 + 0.3 * estrias)[:, None]
    cor[cristal] = cc[cristal]
    brilho[cristal] = (METAL_CRISTAL, 0, 0, LISO_CRISTAL)
    emis[cristal] = (cc * EMISSAO_CRISTAL)[cristal]

    # Mascara de espelho: prata azulada, borda mais escura e rachaduras finas.
    esp = tex.mascara("espelho")
    borda = suave(0.75, 0.97, np.abs(P[:, 0]) / 0.16)
    ec = misturar(pal["espelho"], pal["espelho_borda"], borda * 0.8 + faceta * 0.15)
    racha = np.abs(textura.fbm(P, 12.0, 2, SEMENTE + 7) - 0.5)
    ec = misturar(ec, pal["espelho_borda"], suave(0.012, 0.004, racha) * 0.9)
    cor[esp] = ec[esp]
    brilho[esp] = (METAL_ESPELHO, 0, 0, LISO_ESPELHO)
    brilho[esp, 3] -= (borda * 0.3)[esp]

    # Olhos que brilham.
    olho = tex.mascara("olho")
    cor[olho] = pal["olho"]
    brilho[olho] = (0, 0, 0, 0.9)
    emis[olho] = pal["olho_halo"] * EMISSAO_OLHO

    # Halo ciano em volta dos olhos, pintado na pedra.
    for lado in (1, -1):
        d = np.linalg.norm(P - np.array([lado * OLHO[0], OLHO[1], OLHO[2]]), axis=1)
        halo = suave(0.055, 0.032, d) * pedra
        cor = misturar(cor, pal["olho_halo"], halo * 0.45)
        emis = emis + pal["olho_halo"][None, :] * (halo * 0.35)[:, None]

    cor = textura.aplicar_ao(cor, ao, forca=0.65, gama=1.2)
    return {"Cor": np.clip(cor, 0, 1), "Brilho": np.clip(brilho, 0, 1), "Emissao": np.clip(emis, 0, 1)}


def receita():
    c = construir()
    zs = [v[2] for v in c.verts]
    return pipeline.Receita(
        NOME,
        construir,
        pintar,
        medidas(),
        escala=ALTURA_ALVO / (max(zs) - min(zs)),
        tamanho_textura=TAMANHO_TEXTURA,
        ao_distancia=0.35,
    )


if __name__ == "__main__":
    pipeline.gerar(receita())
