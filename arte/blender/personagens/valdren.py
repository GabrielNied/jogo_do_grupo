"""Valdren (humanos da fronteira): tecido grosso, couro gasto, cachecol ocre e marcas de viagem.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/personagens/valdren.py [-- --previa <pasta>]
"""

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

import numpy as np  # noqa: E402

from lib import humanoide, pipeline  # noqa: E402
from lib.humanoide import Biotipo, Forma, tubo  # noqa: E402
from lib.textura import misturar, suave  # noqa: E402

# ---------------------------------------------------------------------------------------------
# Valores ajustaveis

NOME = "Valdren"
ALTURA_ALVO = 1.8  # metros, do chao ao alto da cabeca
SEMENTE = 21

PALETA = {
    "pele": "#c99872",
    "pele_sombra": "#a8714f",
    "pele_rubor": "#d98a6a",
    "esclera": "#ece6da",
    "iris": "#4a2e1c",
    "pupila": "#120a06",
    "cabelo": "#4a3020",
    "cabelo_ponta": "#7a5638",
    "boca": "#8d4a3f",
    "tunica": "#b8a98a",
    "tunica_escura": "#8a7b60",
    "calca": "#55584a",
    "calca_escura": "#383a2f",
    "couro": "#6b4426",
    "couro_escura": "#3f2616",
    "bota": "#4b3322",
    "bota_escura": "#2a1c13",
    "cachecol": "#b4532a",
    "remendo": "#7d6a4a",
}

BIOTIPO = Biotipo(manga=1, calca=2, bota=0.34, cabeca_expo=2.5)


def adornos(c, f):
    b = f.bio
    humanoide.cabelo_curto(c, b, volume=1.3)
    humanoide.cabelo_longo(c, b, comprimento=0.07, largura=0.8)
    # Cachecol ocre e a ponta caida no peito.
    tubo(
        c,
        [
            (0, 0, b.zo + 0.002, b.r_pescoco * 1.55, b.r_pescoco * 1.6),
            (0, 0, b.zo + 0.06, b.r_pescoco * 1.4, b.r_pescoco * 1.45),
        ],
        10,
        "cachecol",
        "Tronco",
    )
    ry = b.peito_ry
    tubo(
        c,
        [
            (0.06, -ry * 1.0, b.zo + 0.0, 0.038, 0.012),
            (0.065, -ry * 1.02, b.zp + 0.04, 0.034, 0.012),
            (0.07, -ry * 1.03, b.zp - 0.10, 0.032, 0.011),
        ],
        6,
        "cachecol",
        "Tronco",
        expo=3.0,
    )
    # Bolsa no quadril esquerdo, bracadeiras de couro e ombreira direita.
    tubo(
        c,
        [
            (b.quad_rx + 0.05, 0.01, b.zc - 0.20, 0.03, 0.07),
            (b.quad_rx + 0.055, 0.01, b.zc - 0.12, 0.034, 0.08),
            (b.quad_rx + 0.05, 0.01, b.zc - 0.03, 0.03, 0.07),
        ],
        8,
        "couro",
        "Quadril",
        expo=3.5,
    )
    for s, suf in ((1, "_E"), (-1, "_D")):
        x = s * b.sx
        tubo(
            c,
            [
                (x, 0, b.zpul + 0.015, b.r_ante * 1.2, b.r_ante * 1.2),
                (x, 0, b.zpul + 0.12, b.r_ante * 1.3, b.r_ante * 1.3),
            ],
            8,
            "couro",
            "Antebraco" + suf,
        )
    c.esfera((-b.sx, 0.0, b.zo + 0.012), 0.078, "couro", "Braco_D", 8, 5, (1.0, 1.0, 0.62))


def extra(x):
    P, N, pal, b = x.P, x.N, x.pal, x.bio
    tun = x.m("tunica")
    # Correia diagonal de couro, do ombro esquerdo ao quadril direito.
    a = np.array([b.sx * 0.6, b.zo - 0.02])
    d = np.array([-b.cint_rx * 1.0, b.zc + 0.05]) - a
    t = np.clip(((P[:, 0] - a[0]) * d[0] + (P[:, 2] - a[1]) * d[1]) / (d @ d), 0.0, 1.0)
    dist = np.hypot(P[:, 0] - (a[0] + t * d[0]), P[:, 2] - (a[1] + t * d[1]))
    correia = suave(0.020, 0.016, dist) * tun * (np.abs(N[:, 1]) > 0.3) * (P[:, 2] > b.zc + 0.04)
    x.cor = misturar(x.cor, pal["couro"] * (0.8 + 0.4 * suave(0.3, 0.7, np.sin(P[:, 2] * 300))[:, None]), correia)
    # Barra da tunica suja de estrada e remendos nos joelhos.
    sujo = suave(b.zc + 0.04, b.zc - 0.02, P[:, 2]) * tun
    x.cor = misturar(x.cor, pal["tunica_escura"] * 0.7, sujo * 0.7)
    calca = x.m("calca")
    for lado in (1, -1):
        ex = lado * b.hx
        quadro = (np.abs(P[:, 0] - ex) < 0.032) & (np.abs(P[:, 2] - (b.zk + 0.07)) < 0.034) & (N[:, 1] < -0.2) & calca
        x.cor = misturar(x.cor, pal["remendo"] * (0.85 + 0.2 * suave(0.2, 0.8, np.sin(P[:, 0] * 500))[:, None]), quadro * 1.0)
    # Cicatriz fina na bochecha esquerda do rosto.
    rosto = x.m("rosto") & (N[:, 1] < -0.2)
    h = b.cabeca
    t2 = np.clip(((P[:, 2] - (b.zh - 0.05 * h)) / (0.07 * h)), 0, 1)
    dc = np.abs(P[:, 0] - (0.078 * h - 0.025 * h * t2))
    cic = suave(0.004, 0.0015, dc) * rosto * (P[:, 2] < b.zh + 0.02 * h) * (P[:, 2] > b.zh - 0.05 * h)
    x.cor = misturar(x.cor, pal["pele_rubor"] * 1.15, cic * 0.8)


def forma():
    return Forma(NOME, BIOTIPO, PALETA, SEMENTE, adornos, extra, ALTURA_ALVO)


if __name__ == "__main__":
    pipeline.gerar(humanoide.receita(forma()))
