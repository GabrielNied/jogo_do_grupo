"""Cinereo (herdeiros da brasa): pele de cinza e carvao, olhos de brasa, fissuras vermelhas,
placas de obsidiana nos ombros e chifres na testa.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/personagens/cinereo.py [-- --previa <pasta>]
"""

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

import numpy as np  # noqa: E402

from lib import humanoide, pipeline  # noqa: E402
from lib.humanoide import Biotipo, Forma, tubo, veios  # noqa: E402
from lib.textura import fbm, misturar, suave  # noqa: E402

# ---------------------------------------------------------------------------------------------
# Valores ajustaveis

NOME = "Cinereo"
ALTURA_ALVO = 1.8
SEMENTE = 59

PALETA = {
    "pele": "#58524f",
    "pele_sombra": "#2f2b2a",
    "pele_rubor": "#7a4a3c",
    "esclera": "#1d1412",
    "iris": "#ff7a2a",
    "pupila": "#fff0b0",
    "cabelo": "#1c1a1b",
    "cabelo_ponta": "#2a2425",
    "boca": "#3a1c18",
    "tunica": "#46312b",
    "tunica_escura": "#26191a",
    "calca": "#35323a",
    "calca_escura": "#1d1b21",
    "couro": "#563826",
    "couro_escura": "#2e1d14",
    "bota": "#2a2628",
    "bota_escura": "#161314",
    "obsidiana": "#232129",
    "brasa": "#ff4a1c",
}

BIOTIPO = Biotipo(
    largura=1.08,
    profundidade=1.0,
    braco=1.0,
    perna=1.0,
    comp_perna=1.05,
    pescoco=1.0,
    cabeca=1.0,
    manga=0,
    calca=2,
    bota=0.34,
    cabeca_expo=2.5,
)


def adornos(c, f):
    b = f.bio
    h = b.cabeca
    # Chifres de obsidiana e crista curta.
    for s in (1, -1):
        c.cone(
            (s * 0.05 * h, -0.06 * h, b.zh + 0.09 * h),
            (s * 0.085 * h, -0.02 * h, b.zh + 0.24 * h),
            0.024 * h,
            5,
            "obsidiana",
            "Cabeca",
            ref=(0, 1, 0),
        )
        c.cone(
            (s * 0.09 * h, 0.01 * h, b.zh + 0.06 * h),
            (s * 0.12 * h, 0.07 * h, b.zh + 0.17 * h),
            0.02 * h,
            5,
            "obsidiana",
            "Cabeca",
            ref=(0, 1, 0),
        )
    for i in range(4):
        y = -0.03 * h + i * 0.045 * h
        c.cone(
            (0, y, b.zh + 0.115 * h),
            (0, y + 0.02 * h, b.zh + 0.115 * h + 0.06 * h - i * 0.008),
            0.018 * h,
            4,
            "obsidiana",
            "Cabeca",
            ref=(1, 0, 0),
            achatado=0.7,
        )
    # Placas minerais nos ombros (cristais), antebracos, peito e joelhos.
    for s, suf in ((1, "_E"), (-1, "_D")):
        x = s * (b.sx + 0.03)
        c.cristal((x, 0, b.zo + 0.0), (x + s * 0.07, 0, b.zo + 0.19), 0.05, 5, "obsidiana", "Braco" + suf, ref=(0, 1, 0))
        c.cristal(
            (x - s * 0.03, 0.02, b.zo - 0.01),
            (x + s * 0.12, 0.03, b.zo + 0.11),
            0.04,
            5,
            "obsidiana",
            "Braco" + suf,
            ref=(0, 1, 0),
        )
        tubo(
            c,
            [
                (s * b.sx, 0, b.zpul + 0.01, b.r_ante * 1.25, b.r_ante * 1.25),
                (s * b.sx, 0, b.zpul + 0.14, b.r_ante * 1.35, b.r_ante * 1.35),
            ],
            6,
            "obsidiana",
            "Antebraco" + suf,
            expo=3.0,
            lisa=False,
        )
        c.esfera(
            (s * b.hx, -b.r_coxa * 0.78, b.zk), 0.05, "obsidiana", "Canela" + suf, 6, 4, (1.0, 0.5, 1.2), lisa=False
        )
    tubo(
        c,
        [
            (0, -b.peito_ry * 0.99, b.zp + 0.11, 0.065, 0.012),
            (0, -b.peito_ry * 1.04, b.zp - 0.03, 0.075, 0.016),
        ],
        6,
        "obsidiana",
        "Tronco",
        expo=3.0,
        lisa=False,
    )
    # Faixa rasgada de tecido na cintura (avental curto).
    tubo(
        c,
        [
            (-0.02, -b.quad_ry * 1.02, b.zc - 0.02, 0.07, 0.012),
            (-0.02, -b.quad_ry * 1.06, b.zq - 0.22, 0.065, 0.012),
        ],
        6,
        "tunica",
        "Quadril",
        expo=3.0,
    )


def extra(x):
    P, pal, b = x.P, x.pal, x.bio
    pele = x.pele
    cinza = fbm(P, 16.0, 3, x.sem + 60)
    # Cinza clara salpicada na pele e mais escura perto do chao.
    x.cor = misturar(x.cor, np.array([0.55, 0.52, 0.50], dtype=np.float32), suave(0.62, 0.8, cinza) * pele * 0.45)
    x.cor = misturar(x.cor, np.array([0.12, 0.10, 0.10], dtype=np.float32), suave(0.5, 0.0, P[:, 2]) * pele * 0.35)
    # Fissuras vermelhas que brilham.
    fis = np.clip(veios(P, 5.5, x.sem + 61, 0.018) + 0.5 * veios(P, 12.0, x.sem + 62, 0.012), 0, 1) * pele
    x.cor = misturar(x.cor, pal["brasa"], fis * 0.9)
    x.emi = x.emi + fis[:, None] * pal["brasa"] * 0.85
    # Obsidiana: textura vitrea com veios vermelhos pequenos.
    obs = x.m("obsidiana")
    ob = pal["obsidiana"] * (0.7 + 0.8 * fbm(P, 30.0, 2, x.sem + 63)[:, None])
    x.cor = misturar(x.cor, ob, obs * 1.0)
    brilho = veios(P, 14.0, x.sem + 64, 0.025) * obs * 0.8
    x.cor = misturar(x.cor, pal["brasa"], brilho)
    x.emi = x.emi + brilho[:, None] * pal["brasa"] * 0.6
    # Olhos de brasa.
    x.emi[x.iris] = pal["iris"] * 0.95
    # Barra da tunica queimada.
    tun = x.m("tunica")
    x.cor = misturar(x.cor, pal["tunica_escura"] * 0.5, suave(b.zc + 0.03, b.zc - 0.05, P[:, 2]) * tun * 0.8)
    brasas = suave(b.zc - 0.02, b.zc - 0.05, P[:, 2]) * tun * (fbm(P, 60.0, 2, x.sem + 65) > 0.7)
    x.cor = misturar(x.cor, pal["brasa"], brasas * 0.8)


def forma():
    return Forma(NOME, BIOTIPO, PALETA, SEMENTE, adornos, extra, ALTURA_ALVO)


if __name__ == "__main__":
    pipeline.gerar(humanoide.receita(forma()))
