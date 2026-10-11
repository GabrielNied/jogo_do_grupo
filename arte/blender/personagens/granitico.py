"""Granitico (filhos da montanha): corpo robusto de granito cinza, fissuras que brilham em ambar,
ombros de pedra e crista de rocha.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/personagens/granitico.py [-- --previa <pasta>]
"""

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

import numpy as np  # noqa: E402

from lib import humanoide, pipeline  # noqa: E402
from lib.humanoide import Biotipo, Forma, tubo, veios  # noqa: E402
from lib.malha import Secao  # noqa: E402
from lib.textura import aleatorio_por_face, fbm, misturar, suave  # noqa: E402

# ---------------------------------------------------------------------------------------------
# Valores ajustaveis

NOME = "Granitico"
ALTURA_ALVO = 1.8
SEMENTE = 47

PALETA = {
    "pele": "#8f9298",
    "pele_sombra": "#5c5f66",
    "pele_rubor": "#8f9298",
    "esclera": "#241c16",
    "iris": "#ffb243",
    "pupila": "#ffe2a0",
    "cabelo": "#3c3e44",
    "cabelo_ponta": "#2c2e33",
    "boca": "#3c3e44",
    "calca": "#6a5238",
    "calca_escura": "#3e2e1e",
    "couro": "#47321f",
    "couro_escura": "#2a1d12",
    "bota": "#3c3631",
    "bota_escura": "#221e1b",
    "pedra": "#5a5d64",
    "pedra_escura": "#34363b",
    "fissura": "#ff9a38",
}

BIOTIPO = Biotipo(
    largura=1.35,
    profundidade=1.3,
    braco=1.45,
    perna=1.3,
    comp_perna=0.88,
    pescoco=0.35,
    cabeca=0.95,
    mao=1.4,
    manga=0,
    calca=1,
    bota=0.24,
    tunica=False,
    lisa_pele=False,
    cabeca_expo=3.0,
    orelha="nenhuma",
)


def adornos(c, f):
    b = f.bio
    h = b.cabeca
    # Testa pesada.
    c.loft(
        [
            Secao((-0.10 * h, -0.072 * h, b.zh + 0.035 * h), 0.02 * h, 0.016 * h),
            Secao((0.0, -0.108 * h, b.zh + 0.034 * h), 0.028 * h, 0.021 * h),
            Secao((0.10 * h, -0.072 * h, b.zh + 0.035 * h), 0.02 * h, 0.016 * h),
        ],
        6,
        "rosto",
        "Cabeca",
        ref=(0, 0, 1),
        lisa=False,
    )
    # Crista de pedra no alto da cabeca.
    for i in range(5):
        t = i / 4.0
        y = -0.05 * h + t * 0.11 * h
        alt = 0.07 * h * (1.0 - abs(t - 0.45) * 1.1)
        c.cone(
            (0, y, b.zh + 0.105 * h),
            (0, y + 0.01 * h, b.zh + 0.105 * h + alt),
            0.026 * h,
            4,
            "pedra_escura",
            "Cabeca",
            ref=(1, 0, 0),
            achatado=0.7,
        )
    # Ombros de pedra com espinhos e punhos de pedra.
    for s, suf in ((1, "_E"), (-1, "_D")):
        x = s * (b.sx + 0.015)
        c.esfera((x, 0, b.zo + 0.015), 0.125, "pedra", "Braco" + suf, 6, 4, (1.15, 1.0, 0.8), lisa=False)
        for dx, dy, alt in ((0.02, 0.0, 0.13), (0.09, 0.03, 0.09), (-0.06, -0.03, 0.1)):
            c.cone(
                (x + s * dx, dy, b.zo + 0.07),
                (x + s * (dx * 1.8 + 0.02), dy, b.zo + 0.07 + alt),
                0.035,
                4,
                "pedra_escura",
                "Braco" + suf,
                ref=(0, 1, 0),
            )
        tubo(
            c,
            [
                (s * b.sx, 0, b.zpul + 0.01, b.r_ante * 1.25, b.r_ante * 1.25),
                (s * b.sx, 0, b.zpul + 0.13, b.r_ante * 1.4, b.r_ante * 1.4),
            ],
            6,
            "pedra",
            "Antebraco" + suf,
            lisa=False,
        )
    # Fivela de pedra no cinto e avental de couro.
    tubo(
        c,
        [
            (0, -b.cint_ry * 1.1, b.zc + 0.0, 0.05, 0.012),
            (0, -b.cint_ry * 1.12, b.zc + 0.06, 0.05, 0.016),
        ],
        6,
        "pedra",
        "Tronco",
        expo=3.0,
        lisa=False,
    )
    tubo(
        c,
        [
            (0, -b.quad_ry * 1.02, b.zc - 0.02, 0.075, 0.012),
            (0, -b.quad_ry * 1.05, b.zq - 0.20, 0.07, 0.012),
        ],
        6,
        "calca",
        "Quadril",
        expo=3.0,
    )


def extra(x):
    P, N, pal, b = x.P, x.N, x.pal, x.bio
    pele = x.pele
    rocha = pele | x.m("pedra", "pedra_escura")
    # Granito: pintas, facetas e manchas.
    pintas = fbm(P, 70.0, 2, x.sem + 50)
    fac = aleatorio_por_face(x.tex.face, x.sem)
    vari = 0.80 + 0.18 * fac + 0.3 * (pintas - 0.5)
    x.cor = np.where(pele[:, None], x.cor * vari[:, None], x.cor)
    musgo = suave(0.62, 0.8, fbm(P, 9.0, 3, x.sem + 51)) * pele * (N[:, 2] > 0.3)
    x.cor = misturar(x.cor, np.array([0.40, 0.45, 0.33], dtype=np.float32), musgo * 0.45)
    x.cor = misturar(x.cor, pal["pedra"] * (0.7 + 0.5 * pintas[:, None]), x.m("pedra") * 1.0)
    x.cor = misturar(x.cor, pal["pedra_escura"] * (0.7 + 0.5 * pintas[:, None]), x.m("pedra_escura") * 1.0)
    # Fissuras de ambar que brilham discretamente (pele e pedra).
    fiss = np.clip(veios(P, 5.0, x.sem + 52, 0.02) * rocha + veios(P, 11.0, x.sem + 53, 0.014) * pele * 0.6, 0, 1)
    x.cor = misturar(x.cor, pal["fissura"], fiss * 0.85)
    x.emi = x.emi + fiss[:, None] * pal["fissura"] * 0.55
    # Olhos de ambar acesos.
    x.emi[x.iris] = pal["iris"] * 0.8
    # Faixa de couro cruzada no peito.
    a = np.array([b.sx * 0.5, b.zo - 0.03])
    d = np.array([-b.cint_rx, b.zc + 0.06]) - a
    t = np.clip(((P[:, 0] - a[0]) * d[0] + (P[:, 2] - a[1]) * d[1]) / (d @ d), 0.0, 1.0)
    dist = np.hypot(P[:, 0] - (a[0] + t * d[0]), P[:, 2] - (a[1] + t * d[1]))
    faixa = suave(0.030, 0.024, dist) * x.m("pele") * (np.abs(N[:, 1]) > 0.3) * (P[:, 2] > b.zc + 0.03)
    x.cor = misturar(x.cor, pal["couro"] * (0.8 + 0.4 * pintas[:, None]), faixa)
    x.emi = x.emi * (1.0 - faixa)[:, None]


def forma():
    return Forma(NOME, BIOTIPO, PALETA, SEMENTE, adornos, extra, ALTURA_ALVO)


if __name__ == "__main__":
    pipeline.gerar(humanoide.receita(forma()))
