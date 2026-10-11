"""Silvano (guardioes das trilhas): esguio, orelhas alongadas, cabelo de folhas, linhas de casca,
coroa de folhas, galhos e saiote de folhas.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/personagens/silvano.py [-- --previa <pasta>]
"""

import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

import numpy as np  # noqa: E402

from lib import humanoide, pipeline  # noqa: E402
from lib.humanoide import Biotipo, Forma, tubo, veios  # noqa: E402
from lib.malha import Secao  # noqa: E402
from lib.textura import fbm, misturar, suave  # noqa: E402

# ---------------------------------------------------------------------------------------------
# Valores ajustaveis

NOME = "Silvano"
ALTURA_ALVO = 1.8
SEMENTE = 71

PALETA = {
    "pele": "#d2c08f",
    "pele_sombra": "#a89468",
    "pele_rubor": "#d99a72",
    "esclera": "#f0eedc",
    "iris": "#5fb04a",
    "pupila": "#10200c",
    "cabelo": "#4f8a36",
    "cabelo_ponta": "#9fcf5c",
    "boca": "#9a5848",
    "tunica": "#4f7a3c",
    "tunica_escura": "#33542a",
    "calca": "#6b5538",
    "calca_escura": "#44361f",
    "couro": "#7a5a36",
    "couro_escura": "#47331d",
    "bota": "#5e4730",
    "bota_escura": "#382a1b",
    "folha": "#6fa84a",
    "casca": "#6b4f35",
}

BIOTIPO = Biotipo(
    largura=0.86,
    profundidade=0.9,
    braco=0.8,
    perna=0.85,
    comp_perna=1.10,
    pescoco=1.2,
    cabeca=0.95,
    mao=0.9,
    manga=0,
    calca=2,
    bota=0.26,
    cabeca_expo=2.0,
    orelha="longa",
)


def adornos(c, f):
    b = f.bio
    h = b.cabeca
    humanoide.cabelo_curto(c, b, volume=1.2)
    humanoide.cabelo_longo(c, b, comprimento=0.24, largura=0.9)
    # Orelhas alongadas, para tras e para cima.
    for s in (1, -1):
        c.loft(
            [
                Secao((s * 0.096 * h, 0.004, b.zh - 0.004 * h), 0.02 * h, 0.03 * h),
                Secao((s * 0.16 * h, 0.03 * h, b.zh + 0.05 * h), 0.016 * h, 0.026 * h),
            ],
            5,
            "rosto",
            "Cabeca",
            ref=(0, -1, 0),
            ponta_fim=(s * 0.27 * h, 0.075 * h, b.zh + 0.15 * h),
            lisa=False,
        )
    # Coroa de folhas e galhos.
    for k in range(8):
        a = 2.0 * math.pi * (k + 0.5) / 8
        bx, by = math.sin(a) * 0.108 * h, -math.cos(a) * 0.122 * h
        c.cone(
            (bx, by, b.zh + 0.07 * h),
            (bx * 1.25, by * 1.25, b.zh + 0.15 * h),
            0.022 * h,
            4,
            "folha",
            "Cabeca",
            ref=(math.sin(a), -math.cos(a), 0),
            achatado=1.7,
        )
    for s in (1, -1):
        c.cone((s * 0.045 * h, 0, b.zh + 0.10 * h), (s * 0.095 * h, 0, b.zh + 0.27 * h), 0.012 * h, 4, "casca", "Cabeca", ref=(0, 1, 0))
        c.cone((s * 0.07 * h, 0, b.zh + 0.17 * h), (s * 0.16 * h, 0, b.zh + 0.25 * h), 0.009 * h, 4, "casca", "Cabeca", ref=(0, 1, 0))
    # Saiote de folhas na cintura.
    for k in range(12):
        a = 2.0 * math.pi * k / 12
        ca, sa = math.cos(a), math.sin(a)
        bx, by = ca * b.cint_rx * 1.12, sa * b.cint_ry * 1.15
        c.cone(
            (bx, by, b.zc + 0.01),
            (bx * 1.18, by * 1.3, b.zc - 0.17 - 0.03 * (k % 2)),
            0.032,
            4,
            "folha",
            "Quadril",
            ref=(ca, sa, 0),
            achatado=1.8,
        )
    # Ombreira de folhas no ombro esquerdo.
    for dx, dz in ((0.03, 0.12), (0.09, 0.09), (0.13, 0.03)):
        c.cone((b.sx, 0, b.zo + 0.0), (b.sx + dx, 0.0, b.zo + dz), 0.03, 4, "folha", "Braco_E", ref=(0, 1, 0), achatado=1.4)
    # Bracadeiras de casca.
    for s, suf in ((1, "_E"), (-1, "_D")):
        x = s * b.sx
        tubo(
            c,
            [
                (x, 0, b.zpul + 0.015, b.r_ante * 1.2, b.r_ante * 1.2),
                (x, 0, b.zpul + 0.12, b.r_ante * 1.25, b.r_ante * 1.25),
            ],
            6,
            "casca",
            "Antebraco" + suf,
            lisa=False,
        )


def extra(x):
    P, N, pal, b = x.P, x.N, x.pal, x.bio
    pele = x.pele
    # Linhas de casca na pele (bracos, pescoco e laterais do rosto) e leve toque de musgo.
    casca = np.clip(veios(P, 7.0, x.sem + 70, 0.05) + 0.5 * veios(P, 16.0, x.sem + 71, 0.03), 0, 1) * pele
    x.cor = misturar(x.cor, pal["pele_sombra"] * 0.8, casca * 0.65)
    verde = suave(0.55, 0.8, fbm(P, 8.0, 3, x.sem + 72)) * pele
    x.cor = misturar(x.cor, np.array([0.55, 0.66, 0.38], dtype=np.float32), verde * 0.3)
    # Folhas: degrade de verde; galhos e bracadeiras: casca.
    fol = x.m("folha")
    gr = misturar(pal["folha"], pal["cabelo_ponta"], suave(0.3, 0.8, fbm(P, 25.0, 2, x.sem + 73)) * 0.7)
    x.cor = misturar(x.cor, gr * (0.8 + 0.4 * np.abs(N[:, 2]))[:, None], fol * 1.0)
    cas = x.m("casca")
    cc = pal["casca"] * (0.7 + 0.6 * suave(0.3, 0.7, fbm(P, np.array([60.0, 60.0, 8.0]), 2, x.sem + 74)))[:, None]
    x.cor = misturar(x.cor, cc, cas * 1.0)
    # Barra da tunica mais clara, como folhas novas.
    tun = x.m("tunica")
    x.cor = misturar(x.cor, pal["folha"], suave(b.zc + 0.01, b.zc - 0.03, P[:, 2]) * tun * 0.5)


def forma():
    return Forma(NOME, BIOTIPO, PALETA, SEMENTE, adornos, extra, ALTURA_ALVO)


if __name__ == "__main__":
    pipeline.gerar(humanoide.receita(forma()))
