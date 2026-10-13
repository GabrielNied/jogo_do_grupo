"""Lobo do Campo (criatura 2): esguio e rapido, pernas longas, orelhas em pe, cauda cheia.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/criaturas/lobo.py [-- --previa <pasta>]
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

NOME = "LoboDoCampo"
ALTURA_ALVO = 0.85  # metros, do chao a ponta das orelhas
TAMANHO_TEXTURA = 1024
SEMENTE = 31
PESO_DOS_CLIPES = 0.7  # o golpe sai em 0,7 s (aviso do Lobo em docs/features/mobs.md)

PALETA = {
    "pelo": "#6b6258",
    "pelo_escuro": "#2f2b28",
    "pelo_ponta": "#a39886",
    "barriga": "#bdb2a0",
    "focinho": "#9d9282",
    "nariz": "#101010",
    "pata": "#3a332e",
    "garra": "#d8d0bf",
    "olho": "#120c06",
    "iris": "#d6a026",
    "brilho_olho": "#fff6e0",
    "orelha_dentro": "#8a6e66",
    "boca": "#4a2a2a",
}

# Corpo: (y, z do centro, meia largura, meia altura), de tras para a frente.
CORPO = [
    (0.50, 0.60, 0.07, 0.09),
    (0.36, 0.62, 0.10, 0.135),
    (0.15, 0.625, 0.115, 0.155),
    (-0.08, 0.64, 0.125, 0.175),
    (-0.24, 0.66, 0.13, 0.19),
    (-0.33, 0.67, 0.10, 0.15),
]
# Cabeca: (y, z, meia largura, meia altura); focinho longo e fino.
CABECA = [
    (-0.28, 0.69, 0.10, 0.15),
    (-0.40, 0.70, 0.098, 0.125),
    (-0.52, 0.69, 0.085, 0.10),
    (-0.63, 0.655, 0.057, 0.066),
    (-0.75, 0.635, 0.042, 0.047),
    (-0.83, 0.63, 0.037, 0.04),
]
OLHO = (0.062, -0.555, 0.725)
PERNA_D = (0.075, -0.23)
PERNA_T = (0.078, 0.40)


def _secoes(lista, expoente=2.15):
    return [Secao((0.0, y, z), rx, rz, expoente) for y, z, rx, rz in lista]


def construir():
    c = Construtor(NOME, semente=SEMENTE)
    c.loft(_secoes(CORPO), 12, "corpo", "Corpo", ponta_ini=(0.0, 0.56, 0.59), variacao=0.004)
    c.loft(_secoes(CABECA, 2.4), 10, "cabeca", "Cabeca", tampa_ini=False, variacao=0.003)
    # Mandibula e nariz.
    c.loft(
        [
            Secao((0.0, -0.52, 0.60), 0.07, 0.05),
            Secao((0.0, -0.66, 0.595), 0.045, 0.032),
            Secao((0.0, -0.80, 0.605), 0.03, 0.02),
        ],
        8,
        "cabeca",
        "Cabeca",
        ponta_fim=(0.0, -0.83, 0.605),
    )
    c.esfera((0.0, -0.835, 0.637), 0.026, "nariz", "Cabeca", 8, 6, (1.0, 0.8, 0.8))
    for lado in (1, -1):
        ox, oy, oz = OLHO
        c.esfera((lado * ox, oy, oz), 0.02, "olho", "Cabeca", 8, 6, (0.8, 1.0, 0.8))

    # Orelhas em pe, pontudas.
    def orelha(lado, osso):
        c.loft(
            [
                Secao((lado * 0.062, -0.46, 0.79), 0.042, 0.018),
                Secao((lado * 0.07, -0.445, 0.86), 0.03, 0.012),
            ],
            6,
            "orelha",
            osso,
            ref=(0, -1, 0.2),
            ponta_fim=(lado * 0.074, -0.435, 0.935),
        )

    c.espelhar_x(orelha, "Orelha_E", "Orelha_D")

    # Gola de pelos no pescoco e cangote (cones baixos), para a silhueta nao ficar de cachorro liso.
    for i in range(10):
        t = i / 9
        y = -0.40 + t * 0.55
        r = math.sin(i * 12.9898) * 43758.5453
        r = r - math.floor(r)
        z = 0.80 + 0.015 * math.sin(math.pi * t) + 0.02 * (1 - t)
        h = 0.022 + 0.018 * r
        osso = "Cabeca" if y < -0.28 else "Corpo"
        for dx in (-0.03, 0.03):
            c.cone((dx, y, z - 0.03), (dx * 1.3, y + 0.04, z + h), 0.014, 4, "pelo_ponta", osso, ref=(1, 0, 0), achatado=2.0)
    # Peito cheio.
    c.loft(
        [Secao((0.0, -0.33, 0.58), 0.07, 0.07), Secao((0.0, -0.40, 0.56), 0.05, 0.05)],
        6,
        "corpo",
        "Corpo",
        ponta_fim=(0.0, -0.43, 0.55),
    )

    # Cauda cheia, caida e curva.
    c.loft(
        [
            Secao((0.0, 0.52, 0.62), 0.03),
            Secao((0.0, 0.64, 0.57), 0.05, 0.045),
            Secao((0.0, 0.76, 0.46), 0.058, 0.05),
            Secao((0.0, 0.82, 0.34), 0.04, 0.035),
        ],
        8,
        "cauda",
        "Rabo",
        ref=(1, 0, 0),
        ponta_fim=(0.0, 0.83, 0.25),
    )

    # Pernas longas e finas; as traseiras com o jarrete dobrado.
    def perna(lado, osso, x, y, traseira):
        x *= lado
        if traseira:
            pts = [
                (x, y, 0.58, 0.075, 0.115),
                (x * 1.03, y + 0.03, 0.40, 0.055, 0.085),
                (x, y + 0.075, 0.24, 0.032, 0.04),
                (x, y + 0.03, 0.12, 0.026, 0.03),
                (x, y + 0.022, 0.05, 0.027, 0.03),
            ]
        else:
            pts = [
                (x, y, 0.58, 0.058, 0.09),
                (x * 1.03, y, 0.40, 0.045, 0.06),
                (x, y + 0.006, 0.24, 0.03, 0.034),
                (x, y, 0.12, 0.026, 0.03),
                (x, y - 0.002, 0.05, 0.027, 0.03),
            ]
        sec = [Secao((px, py, pz), ru, rv) for px, py, pz, ru, rv in pts]
        c.loft(sec, 7, "pata", osso, ref=(0, -1, 0), tampa_fim=False, variacao=0.002)
        yc = pts[-1][1]
        # Pata: oval comprido com garras.
        c.loft(
            [
                Secao((x, yc + 0.01, 0.045), 0.032, 0.04, 2.4),
                Secao((x, yc - 0.03, 0.018), 0.036, 0.05, 2.4),
                Secao((x, yc - 0.05, 0.003), 0.03, 0.03, 2.4),
            ],
            8,
            "pata",
            osso,
            ref=(0, -1, 0),
            lisa=True,
            ponta_fim=(x, yc - 0.075, 0.002),
        )

    c.espelhar_x(lambda s, o: perna(s, o, PERNA_D[0], PERNA_D[1], False), "Perna_DianteiraE", "Perna_DianteiraD")
    c.espelhar_x(lambda s, o: perna(s, o, PERNA_T[0], PERNA_T[1], True), "Perna_TraseiraE", "Perna_TraseiraD")
    return c


def medidas():
    return quadrupede.Medidas(
        corpo=(0.0, 0.0, 0.62),
        cabeca=(0.0, -0.33, 0.67),
        rabo=(0.0, 0.52, 0.62),
        orelha=(0.062, -0.46, 0.79),
        perna_dianteira=(PERNA_D[0], PERNA_D[1], 0.58),
        perna_traseira=(PERNA_T[0], PERNA_T[1], 0.58),
        escala=1.0,
        peso=PESO_DOS_CLIPES,
        altura_tombo=0.17,
    )


def pintar():
    pal = {k: hex_rgb(v) for k, v in PALETA.items()}
    sem = SEMENTE

    def f(tex, ao):
        P, N = tex.pos, tex.normal
        n = len(P)
        ax = np.abs(P[:, 0])
        cor = np.zeros((n, 3), dtype=np.float32)

        pelo = tex.mascara("corpo", "cabeca", "pata", "orelha", "cauda")
        d = np.zeros_like(P)
        d[:, 1] = 1.0 - np.abs(N[:, 0]) * 0.5
        d[:, 2] = -np.abs(N[:, 0]) * 0.8 - 0.1
        pata = tex.mascara("pata")
        d[pata] = (0.0, 0.1, -1.0)
        cab = tex.mascara("cabeca")
        d[cab] = np.stack([np.zeros(cab.sum()), np.ones(cab.sum()), 0.2 + 0.0 * P[cab, 2]], axis=1)

        grande = textura.fbm(P, 3.0, 4, sem)
        fios = textura.listras(P, d, 5.0, 140.0, sem + 1)
        finos = textura.listras(P, d, 9.0, 320.0, sem + 2, oitavas=1)

        base = misturar(pal["pelo"], pal["pelo_escuro"], suave(0.3, 0.75, grande) * 0.6)
        # Manto escuro no dorso, barriga e peito claros.
        dorso = suave(0.3, 0.9, N[:, 2]) * suave(0.16, 0.02, ax)
        base = misturar(base, pal["pelo_escuro"], dorso * 0.7)
        barriga = suave(-0.1, -0.85, N[:, 2]) * (tex.mascara("corpo") | tex.mascara("cauda"))
        base = misturar(base, pal["barriga"], barriga * 0.75)
        # Focinho e bochechas claros, mascara escura sobre o nariz.
        focinho = cab * suave(-0.5, -0.8, P[:, 1])
        base = misturar(base, pal["focinho"], focinho * 0.7)
        base = misturar(base, pal["pelo_escuro"], cab * suave(0.04, 0.0, ax) * suave(-0.55, -0.7, P[:, 1]) * suave(0.6, 0.67, P[:, 2]) * 0.6)
        # Patas: dianteiras escuras na frente.
        base = misturar(base, pal["pata"], pata * suave(0.34, 0.1, P[:, 2]) * 0.8)
        # Ponta da cauda escura.
        cauda = tex.mascara("cauda")
        base = misturar(base, pal["pelo_escuro"], cauda * suave(0.72, 0.82, P[:, 1]) * 0.9)

        k = 0.72 + 0.5 * fios + 0.18 * (finos - 0.5)
        pel = base * k[:, None]
        pel = misturar(pel, pal["pelo_ponta"], suave(0.62, 0.9, fios) * 0.4 + suave(0.6, 0.85, finos) * 0.15)
        cor[pelo] = pel[pelo]

        orelha = tex.mascara("orelha")
        dentro = orelha & (N[:, 1] < -0.5)
        cor[dentro] = misturar(pal["orelha_dentro"], pal["pelo_escuro"], fios[dentro] * 0.5)
        fora = orelha & ~dentro
        cor[fora] = pal["pelo_escuro"] * (0.8 + 0.4 * fios[fora])[:, None]

        pp = tex.mascara("pelo_ponta")
        if pp.any():
            cor[pp] = misturar(pal["pelo"], pal["pelo_ponta"], 0.45) * (0.8 + 0.4 * fios[pp])[:, None]

        nariz = tex.mascara("nariz")
        cor[nariz] = pal["nariz"]

        olho = tex.mascara("olho")
        if olho.any():
            ox, oy, oz = OLHO
            lado = np.sign(P[:, 0])
            centro = np.stack([lado * ox, np.full(n, oy), np.full(n, oz)], axis=1)
            v = P - centro
            v /= np.maximum(np.linalg.norm(v, axis=1, keepdims=True), 1e-6)
            eixo = np.stack([lado * 0.8, np.full(n, -0.55), np.full(n, 0.1)], axis=1)
            eixo /= np.linalg.norm(eixo, axis=1, keepdims=True)
            dd = np.sum(v * eixo, axis=1)
            oc = misturar(pal["olho"], pal["iris"], suave(0.5, 0.7, dd))
            oc = misturar(oc, pal["olho"], suave(0.93, 0.97, dd))
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
        ao_distancia=0.25,
    )


if __name__ == "__main__":
    pipeline.gerar(receita())
