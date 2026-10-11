"""Javali do Campo (criatura 1). Tambem e a base da Presa-Velha (presa_velha.py).

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/criaturas/javali.py [-- --previa <pasta>]
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

NOME = "JavaliDoCampo"
ALTURA_ALVO = 0.9  # metros, do chao ao topo da crina
TAMANHO_TEXTURA = 1024
SEMENTE = 11

PALETA = {
    "pelo": "#5a4030",
    "pelo_escuro": "#2e2019",
    "pelo_ponta": "#8c6c4e",
    "barriga": "#6e5a48",
    "bochecha": "#8a7a68",
    "pata": "#241a15",
    "crina": "#22170f",
    "crina_ponta": "#7a5a40",
    "focinho": "#6e4a46",
    "focinho_escuro": "#3a2422",
    "narina": "#140c0b",
    "presa": "#efe4c8",
    "presa_base": "#a8916a",
    "casco": "#2c2622",
    "casco_claro": "#4a423b",
    "olho": "#120a07",
    "iris": "#6a2e10",
    "brilho_olho": "#fff6e0",
    "orelha_dentro": "#7a504a",
    "grisalho": "#c9c3b8",
    "cicatriz": "#a5736a",
    "cicatriz_clara": "#d2a79c",
    "olho_cego": "#a9b4b8",
}


class Forma:
    """Medidas de modelagem (o javali do campo tem 0,9 m). A Presa-Velha muda estes campos."""

    def __init__(self):
        self.nome = NOME
        self.altura_alvo = ALTURA_ALVO
        self.semente = SEMENTE
        self.paleta = dict(PALETA)
        self.largura = 1.0  # engorda o corpo e a cabeca
        self.corcunda = 0.0  # sobe o cangote (metros de modelagem)
        self.presa_comprimento = 1.0
        self.presa_raio = 1.0
        self.presa_curva = 1.0
        self.crina_altura = 1.0
        self.grisalho = 0.0  # 0 a 1: pelos brancos (velhice)
        self.cicatriz = False
        self.orelha_rasgada = False


# ---------------------------------------------------------------------------------------------
# Malha

# Corpo: (y, z do centro, meia largura, meia altura), de tras para a frente.
CORPO = [
    (0.57, 0.53, 0.15, 0.17),
    (0.47, 0.535, 0.205, 0.225),
    (0.28, 0.54, 0.225, 0.25),
    (0.05, 0.55, 0.235, 0.275),
    (-0.17, 0.565, 0.24, 0.29),
    (-0.33, 0.57, 0.225, 0.275),
    (-0.42, 0.57, 0.15, 0.19),
]
# Cabeca: (y, z, meia largura, meia altura).
CABECA = [
    (-0.28, 0.585, 0.17, 0.22),
    (-0.38, 0.60, 0.18, 0.222),
    (-0.49, 0.59, 0.162, 0.195),
    (-0.61, 0.55, 0.13, 0.15),
    (-0.73, 0.495, 0.095, 0.108),
    (-0.83, 0.452, 0.074, 0.08),
    (-0.87, 0.44, 0.07, 0.072),
]
OLHO = (0.118, -0.595, 0.64)
RAIO_OLHO = 0.024
FOCINHO_Y = (-0.86, -0.905)
FOCINHO_Z = 0.438
PRESA_RAIZ = (0.058, -0.765, 0.425)
PERNA_D = (0.135, -0.25)  # x, y da dianteira esquerda
PERNA_T = (0.13, 0.36)


def _secoes(lista, largura, corcunda=0.0, expoente=2.15):
    r = []
    for y, z, rx, rz in lista:
        sobe = corcunda * suave(0.2, -0.3, y) * suave(-0.6, -0.35, y) if corcunda else 0.0
        r.append(Secao((0.0, y, z + sobe), rx * largura, rz + sobe * 0.5, expoente))
    return r


def _topo_do_corpo(y, forma):
    """Altura (z) do topo do corpo em y, interpolando as secoes."""
    ys = [c[0] for c in CORPO][::-1]
    zs = [c[1] + c[3] for c in CORPO][::-1]
    z = float(np.interp(y, ys, zs))
    yc = [c[0] for c in CABECA][::-1]
    zc = [c[1] + c[3] for c in CABECA][::-1]
    if yc[0] <= y <= yc[-1]:
        z = max(z, float(np.interp(y, yc, zc)) + forma.corcunda * 0.6)
    if forma.corcunda:
        z += forma.corcunda * 1.5 * float(suave(0.2, -0.3, y) * suave(-0.6, -0.35, y))
    return z


def _presa(c, lado, forma):
    comp = 0.16 * forma.presa_comprimento
    curva = forma.presa_curva
    raio = 0.019 * forma.presa_raio
    x0, y0, z0 = PRESA_RAIZ
    pontos = []
    n = 6
    # Arco no plano frente/cima: sai para a frente, sobe e curva para tras; abre para o lado.
    total = math.radians(105) * curva
    r_arco = comp / total
    for i in range(n):
        t = i / (n - 1)
        ang = math.radians(25) + t * total
        pontos.append(
            (
                lado * (x0 + comp * 0.45 * t),
                y0 - r_arco * (math.sin(ang) - math.sin(math.radians(25))),
                z0 + r_arco * (math.cos(math.radians(25)) - math.cos(ang)),
            )
        )
    secoes = [Secao(p, raio * (1.0 - 0.75 * i / (n - 1))) for i, p in enumerate(pontos[:-1])]
    c.loft(secoes, 6, "presa", "Cabeca", ref=(0, -1, 0), tampa_ini=True, ponta_fim=pontos[-1])


def construir(forma):
    c = Construtor(forma.nome, semente=forma.semente)
    lg = forma.largura

    # Corpo, com o traseiro arredondado.
    corpo = _secoes(CORPO, lg, forma.corcunda)
    c.loft(corpo, 14, "corpo", "Corpo", ponta_ini=(0.0, 0.63, 0.53), variacao=0.006)

    # Cabeca em cunha, mais a mandibula.
    c.loft(_secoes(CABECA, lg, forma.corcunda * 0.6, 2.5), 10, "cabeca", "Cabeca", tampa_ini=False, variacao=0.004)
    mand = [
        Secao((0.0, -0.50, 0.47), 0.11 * lg, 0.07),
        Secao((0.0, -0.64, 0.43), 0.085 * lg, 0.055),
        Secao((0.0, -0.78, 0.405), 0.055 * lg, 0.034),
    ]
    c.loft(mand, 8, "cabeca", "Cabeca", ponta_fim=(0.0, -0.83, 0.405))

    # Focinho (disco).
    y0, y1 = FOCINHO_Y
    c.loft(
        [
            Secao((0.0, y0, FOCINHO_Z), 0.07 * lg, 0.068, 2.2),
            Secao((0.0, y1 + 0.012, FOCINHO_Z - 0.003), 0.08 * lg, 0.077, 2.2),
            Secao((0.0, y1, FOCINHO_Z - 0.004), 0.074 * lg, 0.071, 2.2),
        ],
        12,
        "focinho",
        "Cabeca",
        tampa_ini=False,
    )

    # Olhos (um pouco para fora da cabeca) e sobrancelhas.
    for lado in (1, -1):
        ox, oy, oz = OLHO
        c.esfera((lado * ox * lg, oy, oz), RAIO_OLHO, "olho", "Cabeca", 8, 6, (0.8, 1.0, 0.85))
        c.loft(
            [
                Secao((lado * (ox * lg - 0.014), oy + 0.03, oz + 0.026), 0.014, 0.008),
                Secao((lado * (ox * lg - 0.002), oy - 0.005, oz + 0.024), 0.018, 0.009),
                Secao((lado * (ox * lg - 0.012), oy - 0.04, oz + 0.012), 0.016, 0.01),
            ],
            6,
            "cabeca",
            "Cabeca",
            ref=(lado, 0, 1),
        )

    # Presas.
    for lado in (1, -1):
        _presa(c, lado, forma)

    # Orelhas: folha achatada, para cima e para tras.
    def orelha(lado, osso):
        base = (lado * 0.085 * lg, -0.46, 0.745 + forma.corcunda * 0.3)
        meio = (lado * 0.118 * lg, -0.43, 0.80 + forma.corcunda * 0.3)
        ponta = (lado * 0.15 * lg, -0.39, 0.875 + forma.corcunda * 0.3)
        if forma.orelha_rasgada and lado < 0:
            ponta = (lado * 0.135 * lg, -0.405, 0.835 + forma.corcunda * 0.3)
        c.loft(
            [Secao(base, 0.04, 0.016), Secao(meio, 0.045, 0.012)],
            6,
            "orelha",
            osso,
            ref=(0, -1, 0.2),
            ponta_fim=ponta,
        )

    c.espelhar_x(orelha, "Orelha_E", "Orelha_D")

    # Crina: duas fileiras de cerdas (cones achatados) no cangote e no lombo.
    n = 16
    for i in range(n):
        t = i / (n - 1)
        y = -0.50 + t * 0.80
        h = (0.03 + 0.06 * math.sin(math.pi * min(1.0, t * 1.4 + 0.1))) * forma.crina_altura
        for fila, dx in enumerate((-0.022, 0.022)):
            r = math.sin((i * 2 + fila) * 12.9898) * 43758.5453
            r = r - math.floor(r)
            yy = y + (0.025 if fila else 0.0)
            hh = h * (0.75 + 0.5 * r)
            osso = "Cabeca" if yy < -0.40 else "Corpo"
            z = _topo_do_corpo(yy, forma)
            base = (dx * lg, yy, z - 0.035)
            ponta = (dx * (1.5 + r) * lg, yy + 0.06 + hh * 0.6, z + hh)
            c.cone(base, ponta, 0.015, 4, "crina", osso, ref=(1, 0, 0), achatado=2.2)

    # Rabo com tufo.
    c.loft(
        [Secao((0.0, 0.585, 0.63), 0.022), Secao((0.0, 0.645, 0.56), 0.016), Secao((0.0, 0.665, 0.46), 0.012)],
        5,
        "corpo",
        "Rabo",
    )
    c.cone((0.0, 0.665, 0.48), (0.0, 0.672, 0.36), 0.026, 5, "crina", "Rabo", ref=(1, 0, 0), achatado=0.7)

    # Pernas: dianteiras retas, traseiras com o pernil e o jarrete para tras.
    def perna(lado, osso, x, y, traseira):
        x *= lado
        if traseira:
            pts = [
                (x, y, 0.53, 0.10, 0.15),
                (x * 1.03, y + 0.02, 0.36, 0.085, 0.115),
                (x, y + 0.045, 0.22, 0.048, 0.056),
                (x, y + 0.015, 0.10, 0.037, 0.041),
                (x, y + 0.008, 0.075, 0.039, 0.043),
            ]
        else:
            pts = [
                (x, y, 0.50, 0.085, 0.11),
                (x * 1.03, y, 0.35, 0.07, 0.085),
                (x, y + 0.008, 0.21, 0.045, 0.05),
                (x, y, 0.10, 0.037, 0.041),
                (x, y - 0.002, 0.075, 0.039, 0.043),
            ]
        sec = [Secao((px, py, pz), ru * lg ** 0.5, rv) for px, py, pz, ru, rv in pts]
        c.loft(sec, 8, "pata", osso, ref=(0, -1, 0), tampa_fim=False, variacao=0.003)
        yc = pts[-1][1]
        c.loft(
            [
                Secao((x, yc, 0.085), 0.04, 0.046, 2.6),
                Secao((x, yc - 0.006, 0.03), 0.046, 0.056, 2.6),
                Secao((x, yc - 0.008, 0.0), 0.047, 0.058, 2.6),
            ],
            8,
            "casco",
            osso,
            ref=(0, -1, 0),
            lisa=False,
        )

    c.espelhar_x(lambda s, o: perna(s, o, PERNA_D[0], PERNA_D[1], False), "Perna_DianteiraE", "Perna_DianteiraD")
    c.espelhar_x(lambda s, o: perna(s, o, PERNA_T[0], PERNA_T[1], True), "Perna_TraseiraE", "Perna_TraseiraD")
    return c


def medidas(forma):
    return quadrupede.Medidas(
        corpo=(0.0, 0.0, 0.55),
        cabeca=(0.0, -0.40, 0.62 + forma.corcunda * 0.5),
        rabo=(0.0, 0.585, 0.63),
        orelha=(0.09 * forma.largura, -0.46, 0.75 + forma.corcunda * 0.3),
        perna_dianteira=(PERNA_D[0], PERNA_D[1], 0.47),
        perna_traseira=(PERNA_T[0], PERNA_T[1], 0.49),
        escala=1.0,
        peso=1.0,
        altura_tombo=0.24 * forma.largura,
    )


# ---------------------------------------------------------------------------------------------
# Pintura


def _linha(py, pz, a, b):
    """Distancia de (py, pz) ao segmento a-b no plano YZ."""
    ay, az = a
    by, bz = b
    dy, dz = by - ay, bz - az
    t = np.clip(((py - ay) * dy + (pz - az) * dz) / (dy * dy + dz * dz), 0.0, 1.0)
    return np.hypot(py - (ay + t * dy), pz - (az + t * dz))


def pintar(forma):
    pal = {k: hex_rgb(v) for k, v in forma.paleta.items()}
    sem = forma.semente

    def f(tex, ao):
        P, N = tex.pos, tex.normal
        n = len(P)
        ax = np.abs(P[:, 0])
        cor = np.zeros((n, 3), dtype=np.float32)

        pelo = tex.mascara("corpo", "cabeca", "pata", "orelha")
        crina = tex.mascara("crina")

        # Direcao dos pelos: para tras no lombo, para baixo nos flancos e nas patas.
        d = np.zeros_like(P)
        d[:, 1] = 1.0 - np.abs(N[:, 0]) * 0.6
        d[:, 2] = -np.abs(N[:, 0]) * 0.9 - 0.1
        pata = tex.mascara("pata")
        d[pata] = (0.0, 0.1, -1.0)
        cab = tex.mascara("cabeca")
        d[cab] = np.stack([np.zeros(cab.sum()), np.ones(cab.sum()), 0.25 + 0.0 * P[cab, 2]], axis=1)

        grande = textura.fbm(P, 3.0, 4, sem)
        fios = textura.listras(P, d, 5.0, 130.0, sem + 1)
        finos = textura.listras(P, d, 9.0, 300.0, sem + 2, oitavas=1)

        base = misturar(pal["pelo"], pal["pelo_escuro"], suave(0.3, 0.75, grande) * 0.75)
        # Lombo mais escuro, barriga um pouco mais clara.
        dorso = suave(0.35, 0.9, N[:, 2]) * suave(0.2, 0.02, ax)
        base = misturar(base, pal["pelo_escuro"], dorso * 0.75)
        barriga = suave(-0.2, -0.85, N[:, 2]) * tex.mascara("corpo")
        base = misturar(base, pal["barriga"], barriga * 0.55)
        # Bochechas acinzentadas.
        boch = cab * suave(0.45, 0.85, np.abs(N[:, 0])) * suave(0.66, 0.52, P[:, 2]) * suave(-0.45, -0.55, P[:, 1])
        base = misturar(base, pal["bochecha"], boch * (0.35 + 0.35 * grande))
        # Patas escurecem para baixo.
        base = misturar(base, pal["pata"], suave(0.36, 0.12, P[:, 2]) * (pata | tex.mascara("corpo")))

        # Pelagem: fios claros e escuros.
        k = 0.72 + 0.5 * fios + 0.18 * (finos - 0.5)
        pel = base * k[:, None]
        pel = misturar(pel, pal["pelo_ponta"], suave(0.62, 0.9, fios) * 0.35 + suave(0.6, 0.85, finos) * 0.15)
        if forma.grisalho > 0:
            # Velhice: o pelo inteiro acinzenta (mais no rosto e no lombo) e ganha fios brancos.
            onde = (0.4 + 0.6 * suave(0.35, 0.7, textura.fbm(P, 4.0, 3, sem + 9))) * np.clip(0.35 + 0.5 * dorso + 0.6 * cab, 0, 1)
            cinza = textura.satura(pel, 0.35) * 1.25
            pel = misturar(pel, cinza, np.clip(onde * forma.grisalho, 0, 1))
            pel = misturar(pel, pal["grisalho"], np.clip(suave(0.66, 0.92, fios) * onde * forma.grisalho * 0.45, 0, 1))
        cor[pelo] = pel[pelo]

        # Orelha por dentro.
        orelha = tex.mascara("orelha")
        dentro = orelha & (N[:, 1] < -0.55)
        cor[dentro] = misturar(pal["orelha_dentro"], pal["pelo_escuro"], fios[dentro] * 0.5)

        # Crina: escura com pontas claras.
        h = P[:, 2] - np.array([_topo_do_corpo(y, forma) for y in P[:, 1]]) if crina.any() else None
        if crina.any():
            pontas = suave(0.0, 0.07 * forma.crina_altura, h[crina])
            cc = misturar(pal["crina"], pal["crina_ponta"], pontas * 0.8)
            cc *= (0.8 + 0.4 * fios[crina])[:, None]
            if forma.grisalho > 0:
                cc = misturar(cc, pal["grisalho"], pontas * forma.grisalho * 0.9)
            cor[crina] = cc
        # Tufo do rabo (tambem e "crina") fica escuro.
        tufo = crina & (P[:, 1] > 0.6)
        cor[tufo] = (pal["crina"] * (0.8 + 0.4 * fios[tufo])[:, None])

        # Focinho com narinas.
        foc = tex.mascara("focinho")
        fc = misturar(pal["focinho"], pal["focinho_escuro"], textura.fbm(P, 30.0, 3, sem + 3) * 0.6)
        frente = suave(-0.6, -0.9, N[:, 1])
        fc = misturar(fc, pal["focinho_escuro"], (1 - frente) * 0.35)
        lg = forma.largura
        for lado in (1, -1):
            dn = np.hypot((P[:, 0] - lado * 0.028 * lg) / 1.2, (P[:, 2] - FOCINHO_Z + 0.004) / 1.6)
            fc = misturar(fc, pal["narina"], suave(0.016, 0.011, dn) * frente)
        cor[foc] = fc[foc]

        # Presas: marfim, mais escuras na raiz, com aneis.
        presa = tex.mascara("presa")
        raiz = np.array(PRESA_RAIZ)
        dist = np.hypot(np.hypot(ax - raiz[0], P[:, 1] - raiz[1]), P[:, 2] - raiz[2])
        t = np.clip(dist / (0.16 * forma.presa_comprimento), 0, 1)
        pc = misturar(pal["presa_base"], pal["presa"], suave(0.0, 0.7, t))
        aneis = 0.92 + 0.08 * np.sin(dist * 260.0)
        pc *= aneis[:, None]
        cor[presa] = pc[presa]

        # Cascos com fenda na frente.
        casco = tex.mascara("casco")
        cs = misturar(pal["casco"], pal["casco_claro"], textura.listras(P, np.tile([0, 0, 1.0], (n, 1)), 8.0, 120.0, sem + 4))
        xs = np.where(P[:, 1] < 0.0, PERNA_D[0], PERNA_T[0])
        fenda = suave(0.006, 0.002, np.abs(ax - xs)) * suave(-0.2, -0.6, N[:, 1])
        cs = misturar(cs, pal["narina"], fenda)
        cs = misturar(cs, pal["pata"], suave(0.06, 0.085, P[:, 2]))
        cor[casco] = cs[casco]

        # Olhos: iris escura avermelhada, pupila e brilho.
        olho = tex.mascara("olho")
        if olho.any():
            ox, oy, oz = OLHO
            lado = np.sign(P[:, 0])
            centro = np.stack([lado * ox * lg, np.full(n, oy), np.full(n, oz)], axis=1)
            v = P - centro
            v /= np.maximum(np.linalg.norm(v, axis=1, keepdims=True), 1e-6)
            eixo = np.stack([lado * 0.85, np.full(n, -0.5), np.full(n, 0.15)], axis=1)
            eixo /= np.linalg.norm(eixo, axis=1, keepdims=True)
            dd = np.sum(v * eixo, axis=1)
            oc = misturar(pal["olho"], pal["iris"], suave(0.55, 0.75, dd) * 0.9)
            oc = misturar(oc, pal["olho"], suave(0.9, 0.95, dd))
            luz = np.stack([lado * 0.5, np.full(n, -0.55), np.full(n, 0.67)], axis=1)
            luz /= np.linalg.norm(luz, axis=1, keepdims=True)
            oc = misturar(oc, pal["brilho_olho"], suave(0.95, 0.975, np.sum(v * luz, axis=1)))
            if forma.cicatriz:
                cego = lado > 0
                oc[cego] = misturar(oc[cego], pal["olho_cego"], 0.75 * suave(0.3, 0.6, dd[cego]))
            cor[olho] = oc[olho]

        # Cicatrizes (Presa-Velha): no rosto, sobre o olho esquerdo, e tres riscos no flanco direito.
        if forma.cicatriz:
            rosto = cab & (P[:, 0] > 0.02)
            dl = _linha(P[:, 1], P[:, 2], (-0.50, 0.73), (-0.69, 0.52)) + (textura.ruido(P, 90.0, sem + 5) - 0.5) * 0.006
            marca = suave(0.012, 0.006, dl) * rosto
            cor = misturar(cor, pal["cicatriz"], marca)
            cor = misturar(cor, pal["cicatriz_clara"], suave(0.005, 0.002, dl) * rosto)
            flanco = tex.mascara("corpo") & (P[:, 0] < -0.1)
            for i in range(3):
                off = i * 0.05
                dl = _linha(P[:, 1], P[:, 2], (-0.05 + off, 0.68), (0.12 + off, 0.47)) + (textura.ruido(P, 70.0, sem + 6 + i) - 0.5) * 0.008
                marca = suave(0.01, 0.005, dl) * flanco
                cor = misturar(cor, pal["cicatriz"], marca)
                cor = misturar(cor, pal["cicatriz_clara"], suave(0.004, 0.0015, dl) * flanco)

        # Luz pintada e oclusao.
        cor *= textura.luz_pintada(tex, 0.28, 0.25, 0.95)[:, None]
        cor = textura.aplicar_ao(cor, ao, forca=0.7, gama=1.2)
        return {"Cor": np.clip(cor, 0, 1)}

    return f


def receita(forma):
    c = construir(forma)  # so para medir a altura de modelagem
    zs = [v[2] for v in c.verts]
    escala = forma.altura_alvo / (max(zs) - min(zs))
    return pipeline.Receita(
        forma.nome,
        lambda: construir(forma),
        pintar(forma),
        medidas(forma),
        escala=escala,
        tamanho_textura=TAMANHO_TEXTURA,
        ao_distancia=0.3,
    )


if __name__ == "__main__":
    pipeline.gerar(receita(Forma()))
