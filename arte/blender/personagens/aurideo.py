"""Aurideo (portadores da alvorada): pele quente, olhos claros, marcas douradas que brilham,
diadema de sol e mantelete azul.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/personagens/aurideo.py [-- --previa <pasta>]
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

NOME = "Aurideo"
ALTURA_ALVO = 1.8
SEMENTE = 33

PALETA = {
    "pele": "#e3b58a",
    "pele_sombra": "#c48f66",
    "pele_rubor": "#ee9a78",
    "esclera": "#f4f1e8",
    "iris": "#9fd6f2",
    "pupila": "#16222c",
    "cabelo": "#e8dcae",
    "cabelo_ponta": "#fff4cc",
    "boca": "#b0584a",
    "tunica": "#eee6cc",
    "tunica_escura": "#cfc3a0",
    "calca": "#b9a98a",
    "calca_escura": "#8c7d62",
    "couro": "#8a6638",
    "couro_escura": "#5a4122",
    "bota": "#8a6c48",
    "bota_escura": "#5a452c",
    "ouro": "#e6b73c",
    "capa": "#35567f",
    "marca": "#ffd35a",
}

BIOTIPO = Biotipo(
    largura=0.93,
    profundidade=0.92,
    braco=0.88,
    perna=0.9,
    comp_perna=1.04,
    pescoco=1.1,
    cabeca=0.96,
    manga=0,
    calca=2,
    bota=0.28,
    cabeca_expo=2.1,
)


def adornos(c, f):
    b = f.bio
    h = b.cabeca
    humanoide.cabelo_curto(c, b, volume=1.1)
    humanoide.cabelo_longo(c, b, comprimento=0.30, largura=0.95)
    # Diadema de ouro com a gema solar na testa.
    tubo(
        c,
        [
            (0, 0.005 * h, b.zh + 0.050 * h, 0.108 * h, 0.122 * h),
            (0, 0.005 * h, b.zh + 0.082 * h, 0.106 * h, 0.120 * h),
        ],
        12,
        "ouro",
        "Cabeca",
        expo=2.2,
    )
    c.esfera((0, -0.122 * h, b.zh + 0.068 * h), 0.022 * h, "ouro", "Cabeca", 6, 4, (1.0, 0.7, 1.0))
    for s in (1, -1):
        c.cone(
            (s * 0.03 * h, -0.115 * h, b.zh + 0.08 * h),
            (s * 0.075 * h, -0.10 * h, b.zh + 0.17 * h),
            0.012 * h,
            4,
            "ouro",
            "Cabeca",
            ref=(0, 1, 0),
        )
    c.cone(
        (0, -0.115 * h, b.zh + 0.085 * h),
        (0, -0.105 * h, b.zh + 0.19 * h),
        0.014 * h,
        4,
        "ouro",
        "Cabeca",
        ref=(1, 0, 0),
    )
    # Gola de ouro sobre os ombros e raios de sol.
    tubo(
        c,
        [
            (0, 0, b.zo - 0.005, b.peito_rx * 1.12, b.peito_ry * 1.25),
            (0, 0, b.zo + 0.035, b.peito_rx * 0.85, b.peito_ry * 1.1),
        ],
        12,
        "ouro",
        "Tronco",
        expo=2.4,
    )
    for s in (1, -1):
        for dx, dz in ((0.09, 0.10), (0.14, 0.06)):
            c.cone(
                (s * b.peito_rx * 0.75, 0, b.zo + 0.02),
                (s * (b.peito_rx * 0.75 + dx), 0, b.zo + 0.02 + dz),
                0.022,
                4,
                "ouro",
                "Tronco",
                ref=(0, 1, 0),
            )
    # Mantelete azul nas costas.
    tubo(
        c,
        [
            (0, b.peito_ry + 0.012, b.zo - 0.01, b.peito_rx * 0.8, 0.013),
            (0, b.peito_ry + 0.03, b.zp - 0.05, b.peito_rx * 0.85, 0.014),
            (0, b.peito_ry + 0.045, b.zc - 0.10, b.peito_rx * 0.9, 0.014),
        ],
        8,
        "capa",
        "Tronco",
        expo=4.0,
    )
    # Bracadeiras e faixa de ouro.
    for s, suf in ((1, "_E"), (-1, "_D")):
        x = s * b.sx
        tubo(
            c,
            [
                (x, 0, b.zpul + 0.015, b.r_ante * 1.25, b.r_ante * 1.25),
                (x, 0, b.zpul + 0.11, b.r_ante * 1.3, b.r_ante * 1.3),
            ],
            8,
            "ouro",
            "Antebraco" + suf,
        )
    tubo(
        c,
        [
            (0, 0, b.zc - 0.01, b.cint_rx * 1.13, b.cint_ry * 1.15),
            (0, 0, b.zc + 0.03, b.cint_rx * 1.13, b.cint_ry * 1.15),
        ],
        12,
        "ouro",
        "Tronco",
        expo=2.4,
    )


def extra(x):
    P, N, pal, b = x.P, x.N, x.pal, x.bio
    h = b.cabeca
    # Marcas douradas: linhas finas que correm pela pele (bracos, pescoco e rosto).
    marca = veios(P, 6.5, x.sem + 40, 0.028) * x.m("pele")
    rosto = x.m("rosto")
    marca_rosto = (
        veios(P, 12.0, x.sem + 41, 0.03)
        * rosto
        * (P[:, 2] < b.zh - 0.02 * h)
        * (P[:, 2] > b.zh - 0.09 * h)
        * suave(0.03 * h, 0.06 * h, np.abs(P[:, 0]))
        * (N[:, 1] < -0.2)
    )
    brilho = np.clip(marca + marca_rosto, 0, 1)
    x.cor = misturar(x.cor, pal["marca"], brilho * 0.9)
    x.emi = x.emi + brilho[:, None] * pal["marca"] * 0.9
    ouro = x.m("ouro")
    x.cor = misturar(x.cor, pal["ouro"] * (0.8 + 0.5 * fbm(P, 40.0, 2, x.sem)[:, None]), ouro * 1.0)
    x.emi = x.emi + (ouro * 0.18)[:, None] * pal["ouro"]
    capa = x.m("capa")
    x.cor = misturar(x.cor, pal["capa"] * (0.75 + 0.5 * suave(b.zc - 0.12, b.zo, P[:, 2]))[:, None], capa * 1.0)
    # Bordado de ouro na barra da tunica.
    tun = x.m("tunica")
    barra = suave(b.zc + 0.10, b.zc + 0.07, P[:, 2]) * suave(b.zc - 0.05, b.zc - 0.02, P[:, 2]) * tun
    x.cor = misturar(x.cor, pal["ouro"], barra * 0.7)


def forma():
    return Forma(NOME, BIOTIPO, PALETA, SEMENTE, adornos, extra, ALTURA_ALVO)


if __name__ == "__main__":
    pipeline.gerar(humanoide.receita(forma()))
