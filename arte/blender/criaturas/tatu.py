"""Tatu Rochoso (criatura 3): baixo e largo, carapaca de placas de pedra com cristas, focinho
curto e cauda grossa. Lento e duro, o oposto do Lobo (lobo.py).

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/criaturas/tatu.py [-- --previa <pasta>]
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

NOME = "TatuRochoso"
ALTURA_ALVO = 0.5  # metros, do chao ao topo das cristas
TAMANHO_TEXTURA = 1024
SEMENTE = 47
PESO_DOS_CLIPES = 1.4  # o golpe sai em 1,4 s (aviso do Tatu em docs/features/mobs.md)
ESCALA_DOS_MOVIMENTOS = 0.8  # corpo baixo: os saltos dos clipes sao menores

PALETA = {
    "pedra": "#6d675e",
    "pedra_escura": "#37332e",
    "pedra_clara": "#9a9183",
    "fenda": "#1c1916",
    "pele": "#8a7566",
    "pele_escura": "#4b3e36",
    "cristal": "#c7b58c",
    "cristal_base": "#7c6a4c",
    "garra": "#d6ccb6",
    "olho": "#100c08",
    "iris": "#b8641c",
    "brilho_olho": "#fff6e0",
}

# Carapaca: faixas ao longo de y, dome mais baixo que largo.
CASCO_Y0 = 0.40
CASCO_PASSO = 0.05
CASCO_SECOES = 15
CASCO_CENTRO_Y = 0.05
CASCO_META = 0.42  # meio comprimento para o perfil
CASCO_Z = 0.25
CASCO_RX = 0.31
CASCO_RZ = 0.195
PERNA_D = (0.20, -0.15)
PERNA_T = (0.20, 0.23)


def _perfil(y):
    s = (y - CASCO_CENTRO_Y) / CASCO_META
    return math.sqrt(max(1.0 - s * s, 0.12))


def construir():
    c = Construtor(NOME, semente=SEMENTE)

    # Carapaca em faixas (crista e fenda alternadas), sombreamento chapado.
    secoes = []
    for j in range(CASCO_SECOES):
        y = CASCO_Y0 - CASCO_PASSO * j
        k = _perfil(y) * (1.06 if j % 2 == 0 else 0.95)
        secoes.append(Secao((0.0, y, CASCO_Z), CASCO_RX * k, CASCO_RZ * k, 2.5))
    c.loft(secoes, 12, "carapaca", "Corpo", ponta_ini=(0.0, CASCO_Y0 + 0.05, CASCO_Z - 0.01), lisa=False)

    # Cabeca: escudo largo que afina num focinho curto.
    cab = [
        (-0.28, 0.215, 0.15, 0.115),
        (-0.40, 0.19, 0.13, 0.10),
        (-0.53, 0.155, 0.09, 0.075),
        (-0.64, 0.125, 0.052, 0.05),
        (-0.72, 0.108, 0.03, 0.03),
    ]
    c.loft(
        [Secao((0.0, y, z), rx, rz, 2.3) for y, z, rx, rz in cab],
        10,
        "cabeca",
        "Cabeca",
        tampa_ini=False,
        ponta_fim=(0.0, -0.75, 0.105),
        variacao=0.003,
    )
    # Placa de pedra sobre a cabeca.
    c.loft(
        [
            Secao((0.0, -0.31, 0.275), 0.12, 0.05, 2.6),
            Secao((0.0, -0.43, 0.25), 0.095, 0.045, 2.6),
            Secao((0.0, -0.53, 0.205), 0.06, 0.04, 2.6),
        ],
        8,
        "carapaca",
        "Cabeca",
        lisa=False,
        ponta_fim=(0.0, -0.58, 0.19),
    )
    for lado in (1, -1):
        c.esfera((lado * 0.085, -0.455, 0.2), 0.018, "olho", "Cabeca", 8, 6, (0.8, 1.0, 0.8))

    def orelha(lado, osso):
        c.cone((lado * 0.1, -0.36, 0.255), (lado * 0.125, -0.35, 0.33), 0.034, 6, "pele", osso, ref=(0, -1, 0), achatado=0.7, lisa=True)

    c.espelhar_x(orelha, "Orelha_E", "Orelha_D")

    # Cristas de pedra no dorso (uma fileira central e duas laterais, de tamanhos variados).
    fileiras = [(0.0, 0.0, 0.0), (0.12, 0.05, 0.03), (-0.12, 0.05, 0.03)]
    for fi, (dx, dy, perda) in enumerate(fileiras):
        n = 5 if fi == 0 else 4
        for i in range(n):
            y = 0.28 - i * 0.12 - dy
            k = _perfil(y)
            r = math.sin((i * 3 + fi) * 12.9898) * 43758.5453
            r = r - math.floor(r)
            alt = (0.085 + 0.05 * r) * (1.0 if fi == 0 else 0.75)
            topo = CASCO_Z + CASCO_RZ * k * (1.05 - 0.25 * abs(dx) / 0.12 if dx else 1.05)
            x = dx * k
            c.cristal((x, y, topo - 0.03), (x * 1.1, y - 0.02, topo + alt), 0.034, 5, "cristal", "Corpo", ombro=0.55, achatado=1.0)

    # Cauda grossa de placas.
    cauda = []
    for j, (y, z, r) in enumerate([(0.36, 0.21, 0.085), (0.45, 0.19, 0.082), (0.55, 0.15, 0.07), (0.65, 0.115, 0.052), (0.74, 0.085, 0.032)]):
        cauda.append(Secao((0.0, y, z), r * (1.07 if j % 2 == 0 else 0.96), r * 0.9 * (1.07 if j % 2 == 0 else 0.96), 2.4))
    c.loft(cauda, 8, "carapaca", "Rabo", ponta_fim=(0.0, 0.8, 0.07), lisa=False)

    # Pernas curtas e grossas, com garras.
    def perna(lado, osso, x, y, traseira):
        x *= lado
        pts = [
            (x, y, 0.20, 0.075, 0.08),
            (x * 1.02, y, 0.12, 0.062, 0.066),
            (x, y - 0.005, 0.05, 0.058, 0.06),
        ]
        c.loft([Secao((px, py, pz), ru, rv) for px, py, pz, ru, rv in pts], 8, "pata", osso, ref=(0, -1, 0), tampa_fim=False)
        yc = pts[-1][1]
        c.loft(
            [Secao((x, yc, 0.045), 0.056, 0.058, 2.4), Secao((x, yc - 0.015, 0.015), 0.062, 0.07, 2.4), Secao((x, yc - 0.02, 0.0), 0.06, 0.07, 2.4)],
            8,
            "pata",
            osso,
            ref=(0, -1, 0),
        )
        for dx in (-0.032, 0.0, 0.032):
            c.cone((x + dx, yc - 0.05, 0.022), (x + dx * 1.3, yc - 0.105, 0.006), 0.014, 4, "garra", osso, ref=(1, 0, 0), achatado=0.9)

    c.espelhar_x(lambda s, o: perna(s, o, PERNA_D[0], PERNA_D[1], False), "Perna_DianteiraE", "Perna_DianteiraD")
    c.espelhar_x(lambda s, o: perna(s, o, PERNA_T[0], PERNA_T[1], True), "Perna_TraseiraE", "Perna_TraseiraD")
    return c


def medidas():
    return quadrupede.Medidas(
        corpo=(0.0, 0.0, 0.25),
        cabeca=(0.0, -0.28, 0.2),
        rabo=(0.0, 0.38, 0.2),
        orelha=(0.1, -0.36, 0.26),
        perna_dianteira=(PERNA_D[0], PERNA_D[1], 0.2),
        perna_traseira=(PERNA_T[0], PERNA_T[1], 0.2),
        escala=ESCALA_DOS_MOVIMENTOS,
        peso=PESO_DOS_CLIPES,
        altura_tombo=0.17,
    )


def pintar():
    pal = {k: hex_rgb(v) for k, v in PALETA.items()}
    sem = SEMENTE

    def f(tex, ao):
        P, N = tex.pos, tex.normal
        n = len(P)
        cor = np.zeros((n, 3), dtype=np.float32)

        # Pele (barriga, pernas, orelhas, laterais da cabeca): rugosa e quente.
        pele = tex.mascara("pata", "pele", "cabeca")
        rug = textura.fbm(P, 18.0, 3, sem + 1)
        pl = misturar(pal["pele"], pal["pele_escura"], suave(0.3, 0.8, rug) * 0.7)
        pl = misturar(pl, pal["pele_escura"], suave(0.2, 0.0, N[:, 2]) * 0.5)
        cor[pele] = pl[pele]

        # Pedra: manchas, fendas por ruido em crista e faixas escuras entre as placas.
        pedra = tex.mascara("carapaca")
        grande = textura.fbm(P, 5.0, 4, sem)
        fino = textura.fbm(P, 24.0, 3, sem + 2)
        rach = suave(0.035, 0.0, np.abs(textura.fbm(P, 9.0, 3, sem + 3) - 0.5))
        pc = misturar(pal["pedra"], pal["pedra_escura"], suave(0.35, 0.75, grande) * 0.8)
        pc = misturar(pc, pal["pedra_clara"], suave(0.55, 0.85, fino) * 0.5)
        faixa = 0.5 + 0.5 * np.cos(2.0 * math.pi * (P[:, 1] - CASCO_Y0) / (2 * CASCO_PASSO))
        pc = misturar(pc, pal["pedra_clara"], suave(0.75, 1.0, faixa) * 0.35)
        pc = misturar(pc, pal["fenda"], suave(0.0, 0.3, 1.0 - faixa) * 0.55)
        pc = misturar(pc, pal["fenda"], rach * 0.8)
        # Musgo seco/ocre no topo das placas.
        pc = misturar(pc, pal["cristal_base"], suave(0.6, 0.95, N[:, 2]) * suave(0.5, 0.8, fino) * 0.25)
        # Barriga (face para baixo) volta a ser pele.
        pc = misturar(pc, pal["pele"] * 0.7, suave(-0.15, -0.6, N[:, 2]))
        cor[pedra] = pc[pedra]

        cristal = tex.mascara("cristal")
        if cristal.any():
            alt = suave(0.28, 0.5, P[:, 2])
            cc = misturar(pal["cristal_base"], pal["cristal"], alt)
            cc *= (0.85 + 0.3 * textura.fbm(P, 30.0, 2, sem + 4))[:, None]
            cor[cristal] = cc[cristal]

        garra = tex.mascara("garra")
        cor[garra] = pal["garra"]

        olho = tex.mascara("olho")
        if olho.any():
            lado = np.sign(P[:, 0])
            centro = np.stack([lado * 0.085, np.full(n, -0.455), np.full(n, 0.2)], axis=1)
            v = P - centro
            v /= np.maximum(np.linalg.norm(v, axis=1, keepdims=True), 1e-6)
            eixo = np.stack([lado * 0.8, np.full(n, -0.55), np.full(n, 0.1)], axis=1)
            eixo /= np.linalg.norm(eixo, axis=1, keepdims=True)
            dd = np.sum(v * eixo, axis=1)
            oc = misturar(pal["olho"], pal["iris"], suave(0.5, 0.7, dd))
            oc = misturar(oc, pal["olho"], suave(0.92, 0.96, dd))
            luz = np.stack([lado * 0.5, np.full(n, -0.55), np.full(n, 0.67)], axis=1)
            luz /= np.linalg.norm(luz, axis=1, keepdims=True)
            oc = misturar(oc, pal["brilho_olho"], suave(0.95, 0.975, np.sum(v * luz, axis=1)))
            cor[olho] = oc[olho]

        cor *= textura.luz_pintada(tex, 0.28, 0.25, 0.95)[:, None]
        cor = textura.aplicar_ao(cor, ao, forca=0.7, gama=1.2)
        return {"Cor": np.clip(cor, 0, 1)}

    return f


def receita():
    c = construir()
    zs = [v[2] for v in c.verts]
    escala = ALTURA_ALVO / (max(zs) - min(zs))
    return pipeline.Receita(
        NOME,
        construir,
        pintar(),
        medidas(),
        escala=escala,
        tamanho_textura=TAMANHO_TEXTURA,
        ao_distancia=0.2,
    )


if __name__ == "__main__":
    pipeline.gerar(receita())
