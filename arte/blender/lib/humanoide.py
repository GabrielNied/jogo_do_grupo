"""Humanoide comum as cinco racas jogaveis: biotipo, malha, esqueleto, animacoes e pintura base.

Cada raca (arte/blender/personagens/) escolhe um Biotipo (proporcoes), uma paleta, os adornos
da malha e a pintura extra (marcas, fissuras, brilho). Aqui fica o que e igual em todas.

Esqueleto (pele rigida, cada parte segue um osso; E = esquerda do personagem = +X, D = direita):
  Raiz > Quadril > Tronco > Cabeca
  Tronco > Costas (encaixe das costas) ; Tronco > Braco_E/D > Antebraco_E/D > Mao_E/D
  Mao_D > Weapon.R (encaixe da mao direita = EncaixeMaoDireita no Unity)
  Quadril > Perna_E/D > Canela_E/D > Pe_E/D
Todos os ossos apontam para cima, entao os eixos de rotacao seguem esqueleto.py.
"""

import math
import sys

import numpy as np

from . import esqueleto
from .malha import Construtor, Secao
from .textura import aplicar_ao, fbm, hex_rgb, listras, luz_pintada, misturar, ruido, suave

ACAO_PARADO = "Idle"
ACAO_ANDAR = "Andar"
ACAO_CORRER = "Correr"
ACAO_ESQUIVAR = "Esquivar"
ACAO_ATACAR = "Atacar"
ACAO_MORRER = "Morrer"
ACAO_DANO = "Dano"
ACOES = [ACAO_PARADO, ACAO_ANDAR, ACAO_CORRER, ACAO_ESQUIVAR, ACAO_ATACAR, ACAO_MORRER, ACAO_DANO]

# Distancias da camera nas folhas de previa (multiplo da altura).
DISTANCIA_ROSTO = 0.5
DISTANCIA_VISTAS = 1.75


# --------------------------------------------------------------------------------------------
# Biotipo: proporcoes e roupa de cada raca


class Biotipo:
    """Proporcoes em metros de modelagem (a altura final vem da escala da receita)."""

    def __init__(self, **kw):
        self.largura = 1.0  # ombros, peito, quadril
        self.profundidade = 1.0  # barriga e costas
        self.braco = 1.0  # espessura dos bracos
        self.perna = 1.0  # espessura das pernas
        self.comp_perna = 1.0
        self.pescoco = 1.0
        self.cabeca = 1.0
        self.mao = 1.0
        self.manga = 1  # 0 = braco nu, 1 = manga curta, 2 = manga longa
        self.calca = 2  # 1 = bermuda (canela de fora), 2 = calca longa
        self.bota = 0.30  # altura do cano da bota (m)
        self.tunica = True  # False = torso nu (pele)
        self.lisa_pele = True  # False = pele facetada (pedra)
        self.cabeca_expo = 2.2  # 2 = redonda; maior = mais quadrada
        self.orelha = "humana"  # "humana", "longa" ou "nenhuma"
        for k, v in kw.items():
            setattr(self, k, v)
        self.calcular()

    def calcular(self):
        self.zq = 0.93 * self.comp_perna  # junta do quadril
        self.zc = self.zq + 0.10  # cintura
        self.zp = self.zc + 0.22  # peito
        self.zo = self.zp + 0.17  # ombros
        self.zh = self.zo + 0.12 + 0.06 * self.pescoco  # centro da cabeca
        self.zk = 0.08 + (self.zq - 0.08) * 0.52  # joelho
        k = self.zo / 1.45
        self.zcot = self.zo - 0.02 - 0.28 * k  # cotovelo
        self.zpul = self.zcot - 0.26 * k  # pulso
        self.sx = 0.20 * self.largura  # x do ombro
        self.hx = 0.085 * self.largura + 0.004  # x do quadril
        self.peito_rx = 0.160 * self.largura
        self.peito_ry = 0.105 * self.profundidade
        self.cint_rx = 0.130 * self.largura
        self.cint_ry = 0.088 * self.profundidade
        self.quad_rx = 0.150 * self.largura
        self.quad_ry = 0.098 * self.profundidade
        self.r_braco = 0.048 * self.braco
        self.r_ante = 0.040 * self.braco
        self.r_coxa = 0.078 * self.perna
        self.r_canela = 0.056 * self.perna
        self.r_pescoco = 0.046 * (0.5 + 0.5 * self.largura)


# --------------------------------------------------------------------------------------------
# Malha


def tubo(c, pts, lados, parte, osso, expo=2.2, **kw):
    """Loft vertical: pts = (x, y, z, rx, ry); rx e a meia largura (X) e ry a meia espessura (Y)."""
    secoes = [Secao((x, y, z), rx, ry, expo) for x, y, z, rx, ry in pts]
    c.loft(secoes, lados, parte, osso, ref=(0.0, -1.0, 0.0), **kw)


def _mir(p, s):
    return (p[0] * s, p[1], p[2])


def cabeca_secoes(b):
    """(dz, rx, ry, dy) da cabeca, do queixo ao alto, em torno do centro (0, 0, zh)."""
    h = b.cabeca
    return [
        (-0.118 * h, 0.040 * h, 0.050 * h, -0.030 * h),
        (-0.095 * h, 0.072 * h, 0.080 * h, -0.018 * h),
        (-0.050 * h, 0.092 * h, 0.100 * h, -0.005 * h),
        (0.000, 0.100 * h, 0.112 * h, 0.0),
        (0.050 * h, 0.098 * h, 0.112 * h, 0.003 * h),
        (0.095 * h, 0.082 * h, 0.097 * h, 0.008 * h),
    ]


def construir_corpo(c, f):
    """Pernas, tronco, bracos, pescoco e cabeca (sem cabelo nem adornos)."""
    b = f.bio
    ls = b.lisa_pele
    h = b.cabeca

    # Pelve (calca) e tronco (tunica ou pele nua).
    tubo(
        c,
        [
            (0, 0, b.zq - 0.08, b.quad_rx * 0.78, b.quad_ry * 0.85),
            (0, 0, b.zq + 0.0, b.quad_rx, b.quad_ry),
            (0, 0, b.zc + 0.03, b.cint_rx, b.cint_ry),
        ],
        12,
        "calca",
        "Quadril",
        expo=2.4,
    )
    tubo(
        c,
        [
            (0, 0, b.zc - 0.02, b.cint_rx * 1.04, b.cint_ry * 1.04),
            (0, 0, b.zc + 0.07, b.cint_rx * 1.06, b.cint_ry * 1.06),
            (0, -0.004, b.zp, b.peito_rx, b.peito_ry),
            (0, -0.002, b.zo - 0.04, b.peito_rx * 1.02, b.peito_ry * 0.95),
            (0, 0, b.zo + 0.03, b.peito_rx * 0.55, b.peito_ry * 0.62),
        ],
        12,
        "tunica" if b.tunica else "pele",
        "Tronco",
        expo=2.5,
        lisa=b.tunica or ls,
    )
    # Cinto.
    tubo(
        c,
        [
            (0, 0, b.zc - 0.005, b.cint_rx * 1.10, b.cint_ry * 1.12),
            (0, 0, b.zc + 0.055, b.cint_rx * 1.10, b.cint_ry * 1.12),
        ],
        12,
        "couro",
        "Tronco",
        expo=2.4,
    )

    # Pescoco e cabeca.
    tubo(
        c,
        [
            (0, 0, b.zo - 0.01, b.r_pescoco * 1.08, b.r_pescoco * 1.1),
            (0, -0.006, b.zh - 0.085 * h, b.r_pescoco, b.r_pescoco * 1.05),
        ],
        8,
        "pele",
        "Cabeca",
        lisa=ls,
    )
    secoes = [Secao((0.0, dy, b.zh + dz), rx, ry, b.cabeca_expo) for dz, rx, ry, dy in cabeca_secoes(b)]
    c.loft(
        secoes,
        12,
        "rosto",
        "Cabeca",
        ref=(0.0, -1.0, 0.0),
        ponta_fim=(0.0, 0.01 * h, b.zh + 0.125 * h),
        lisa=ls,
    )
    # Nariz.
    c.cone(
        (0.0, -0.100 * h, b.zh - 0.018 * h),
        (0.0, -0.128 * h, b.zh - 0.034 * h),
        0.017 * h,
        4,
        "rosto",
        "Cabeca",
        ref=(1.0, 0.0, 0.0),
        achatado=0.8,
    )
    # Orelhas humanas (as longas ficam por conta da raca).
    if b.orelha == "humana":
        for s in (1, -1):
            c.esfera(
                (s * 0.099 * h, 0.004, b.zh - 0.002),
                0.026 * h,
                "rosto",
                "Cabeca",
                6,
                4,
                (0.35, 0.8, 1.15),
                lisa=False,
            )

    # Bracos.
    for s, suf in ((1, "_E"), (-1, "_D")):
        x = s * b.sx
        cobre = b.manga >= 1
        p_braco = "tunica" if cobre else "pele"
        p_ante = "tunica" if b.manga >= 2 else "pele"
        liso_b = cobre or ls
        liso_a = (b.manga >= 2) or ls
        ra, rf = b.r_braco, b.r_ante
        c.esfera((x, 0, b.zo - 0.025), ra * 1.28, p_braco, "Braco" + suf, 8, 5, lisa=liso_b)
        tubo(
            c,
            [
                (x, 0, b.zo - 0.025, ra * 1.12, ra * 1.12),
                (x, 0.003, (b.zo + b.zcot) / 2, ra * 1.0, ra * 1.0),
                (x, 0, b.zcot, ra * 0.86, ra * 0.86),
            ],
            8,
            p_braco,
            "Braco" + suf,
            lisa=liso_b,
        )
        c.esfera((x, 0, b.zcot), rf * 1.18, p_ante, "Antebraco" + suf, 8, 5, lisa=liso_a)
        tubo(
            c,
            [
                (x, 0, b.zcot, rf * 1.05, rf * 1.05),
                (x, -0.002, (b.zcot + b.zpul) / 2, rf * 0.97, rf * 0.97),
                (x, 0, b.zpul + 0.01, rf * 0.78, rf * 0.78),
            ],
            8,
            p_ante,
            "Antebraco" + suf,
            lisa=liso_a,
        )
        m = b.mao
        c.esfera(
            (x, -0.004, b.zpul - 0.062 * m),
            0.05 * m,
            "pele",
            "Mao" + suf,
            8,
            5,
            (0.8, 0.55, 1.5),
            lisa=ls,
        )
        c.cone(
            (x - s * 0.030 * m, -0.014 * m, b.zpul - 0.045 * m),
            (x - s * 0.040 * m, -0.048 * m, b.zpul - 0.105 * m),
            0.015 * m,
            5,
            "pele",
            "Mao" + suf,
            ref=(0.0, -1.0, 0.0),
        )

    # Pernas e botas.
    for s, suf in ((1, "_E"), (-1, "_D")):
        x = s * b.hx
        r, rc = b.r_coxa, b.r_canela
        longa = b.calca >= 2
        c.esfera((x, 0, b.zk), r * 0.78, "calca" if longa else "pele", "Canela" + suf, 8, 5, lisa=longa or ls)
        tubo(
            c,
            [
                (x, 0.0, b.zq + 0.03, r * 1.12, r * 1.10),
                (x, -0.002, (b.zq + b.zk) / 2, r * 1.0, r * 1.0),
                (x, -0.004, b.zk + 0.01, r * 0.78, r * 0.80),
            ],
            10,
            "calca",
            "Perna" + suf,
            expo=2.1,
        )

        def raio_canela(z, rc=rc):
            return float(np.interp(z, [0.09, b.zk], [rc * 0.72, rc * 1.15]))

        zm2 = (b.zk + 0.09) / 2
        tubo(
            c,
            [
                (x, -0.004, b.zk, raio_canela(b.zk), raio_canela(b.zk)),
                (x, 0.006, zm2, raio_canela(zm2) * 1.04, raio_canela(zm2) * 1.04),
                (x, 0.0, 0.09, raio_canela(0.09), raio_canela(0.09)),
            ],
            10,
            "calca" if longa else "pele",
            "Canela" + suf,
            lisa=longa or ls,
        )
        # Cano da bota (um pouco mais grosso que a canela).
        zt = min(b.bota, b.zk - 0.02)
        zm = (0.07 + zt) / 2
        tubo(
            c,
            [
                (x, 0.0, 0.07, raio_canela(0.07) + 0.014, raio_canela(0.07) + 0.014),
                (x, 0.005, zm, raio_canela(zm) + 0.012, raio_canela(zm) + 0.012),
                (x, 0.002, zt, raio_canela(zt) + 0.020, raio_canela(zt) + 0.020),
            ],
            10,
            "bota",
            "Canela" + suf,
            expo=2.2,
        )
        # Pe (bota), ao longo de -Y.
        pf = b.perna**0.6
        pts = [
            (x, 0.075, 0.050, 0.034 * pf, 0.045),
            (x, 0.0, 0.041, 0.044 * pf, 0.041),
            (x, -0.09, 0.033, 0.050 * pf, 0.034),
            (x, -0.17, 0.025, 0.040 * pf, 0.026),
        ]
        c.loft(
            [Secao((px, py, pz), ru, rv, 2.3) for px, py, pz, ru, rv in pts],
            8,
            "bota",
            "Pe" + suf,
            ref=(0.0, 0.0, 1.0),
            ponta_fim=(x, -0.205, 0.021),
        )


def cabelo_curto(c, b, volume=1.0, osso="Cabeca", parte="cabelo"):
    """Calota de cabelo sobre o alto da cabeca."""
    h = b.cabeca
    v = 1.0 + 0.09 * volume
    pts = [
        (0.015 * h, 0.104 * h * v, 0.118 * h * v, 0.010 * h),
        (0.050 * h, 0.104 * h * v, 0.118 * h * v, 0.013 * h),
        (0.095 * h, 0.090 * h * v, 0.104 * h * v, 0.013 * h),
        (0.125 * h, 0.060 * h * v, 0.075 * h * v, 0.013 * h),
    ]
    c.loft(
        [Secao((0.0, dy, b.zh + dz), rx, ry, b.cabeca_expo) for dz, rx, ry, dy in pts],
        12,
        parte,
        osso,
        ref=(0.0, -1.0, 0.0),
        ponta_fim=(0.0, 0.012 * h, b.zh + 0.152 * h * v),
    )


def cabelo_longo(c, b, comprimento=0.25, largura=1.0, parte="cabelo"):
    """Massa de cabelo atras da cabeca, caindo ate 'comprimento' metros abaixo do queixo."""
    h = b.cabeca
    base = b.zh + 0.05 * h
    fim = b.zh - 0.12 * h - comprimento
    meio = (base + fim) / 2
    pts = [
        (0, 0.060 * h, base, 0.100 * h * largura, 0.050 * h),
        (0, 0.090 * h, b.zh - 0.04 * h, 0.108 * h * largura, 0.055 * h),
        (0, 0.100 * h, meio, 0.100 * h * largura, 0.050 * h),
        (0, 0.104 * h, fim, 0.070 * h * largura, 0.036 * h),
    ]
    tubo(c, pts, 10, parte, "Cabeca", expo=2.4)


# --------------------------------------------------------------------------------------------
# Esqueleto


class Medidas:
    """Posicao das juntas do lado esquerdo (+X), em metros; escala e deslocamento acompanham a receita."""

    def __init__(self, bio):
        self.bio = bio
        b = bio
        self.juntas = {
            "Quadril": (0.0, 0.0, b.zq),
            "Tronco": (0.0, 0.0, b.zc),
            "Cabeca": (0.0, 0.0, b.zo + 0.03),
            "Costas": (0.0, 0.09, b.zp),
            "Braco": (b.sx, 0.0, b.zo - 0.025),
            "Antebraco": (b.sx, 0.0, b.zcot),
            "Mao": (b.sx, 0.0, b.zpul),
            "Perna": (b.hx, 0.0, b.zq),
            "Canela": (b.hx, 0.0, b.zk),
            "Pe": (b.hx, 0.0, 0.08),
            "Weapon": (b.sx, -0.015, b.zpul - 0.05 * b.mao),
            "Face": (0.0, 0.0, b.zh),
        }
        self.escala = 1.0

    @property
    def cabeca(self):
        return self.juntas["Face"]

    def escalar(self, s):
        self.juntas = {k: (v[0] * s, v[1] * s, v[2] * s) for k, v in self.juntas.items()}
        self.escala *= s

    def deslocar(self, dz):
        self.juntas = {k: (v[0], v[1], v[2] + dz) for k, v in self.juntas.items()}

    @property
    def altura_quadril(self):
        return self.juntas["Quadril"][2]


def centro_rosto(m):
    return m.juntas["Face"]


def ossos(m):
    j = m.juntas
    lista = [
        ("Raiz", None, (0.0, 0.0, 0.0)),
        ("Quadril", "Raiz", j["Quadril"]),
        ("Tronco", "Quadril", j["Tronco"]),
        ("Cabeca", "Tronco", j["Cabeca"]),
        ("Costas", "Tronco", j["Costas"]),
    ]
    for suf, s in (("_E", 1), ("_D", -1)):
        lista += [
            ("Braco" + suf, "Tronco", _mir(j["Braco"], s)),
            ("Antebraco" + suf, "Braco" + suf, _mir(j["Antebraco"], s)),
            ("Mao" + suf, "Antebraco" + suf, _mir(j["Mao"], s)),
            ("Perna" + suf, "Quadril", _mir(j["Perna"], s)),
            ("Canela" + suf, "Perna" + suf, _mir(j["Canela"], s)),
            ("Pe" + suf, "Canela" + suf, _mir(j["Pe"], s)),
        ]
    lista.append(("Weapon.R", "Mao_D", _mir(j["Weapon"], -1)))
    return lista


def criar_esqueleto(nome, m):
    return esqueleto.criar_armadura(nome, ossos(m), escala_osso=m.escala)


# --------------------------------------------------------------------------------------------
# Animacoes. Rotacao (inclinar, virar, rolar) em graus; loc (lado, cima, frente) em metros.
# Inclinar positivo: o alto do osso vai para a frente (perna ou braco pendurado: o pe/mao vai para tras).
# Rolar positivo no lado esquerdo afasta a mao do corpo; no direito, o negativo afasta.

QUADROS_IDLE = 64
QUADROS_ANDAR = 28
QUADROS_CORRER = 20
QUADROS_ESQUIVAR = 30
QUADROS_ATACAR = 26
QUADROS_MORRER = 44
QUADROS_DANO = 14
AMOSTRAS_CICLO = 8


def animar(arm, m):
    _parado(arm, m)
    _caminhada(arm, ACAO_ANDAR, QUADROS_ANDAR, passo=26, joelho=60, braco=22, cotovelo=22, inclina=2, quica=0.022, giro=6)
    _caminhada(arm, ACAO_CORRER, QUADROS_CORRER, passo=48, joelho=105, braco=55, cotovelo=85, inclina=12, quica=0.05, giro=10)
    _esquivar(arm, m)
    _atacar(arm)
    _morrer(arm, m)
    _dano(arm)


def _ciclo(a, osso, fn, rot=True, n=AMOSTRAS_CICLO):
    a.ciclo(osso, [fn(2.0 * math.pi * k / n) for k in range(n)], rot=rot)


def _parado(arm, m):
    with esqueleto.Acao(arm, ACAO_PARADO, QUADROS_IDLE, repete=True) as a:
        _ciclo(a, "Quadril", lambda p: (0.0, -0.006 * (1 - math.cos(p)) / 2, 0.0), rot=False, n=4)
        _ciclo(a, "Tronco", lambda p: (1.2 * math.sin(p), 0.8 * math.sin(p), 0.0), n=4)
        _ciclo(a, "Cabeca", lambda p: (-1.0 * math.sin(p), 3.0 * math.sin(p + 1.0), 0.0), n=4)
        _ciclo(a, "Braco_E", lambda p: (1.5 * math.sin(p), 0.0, 3.0 + 1.5 * math.sin(p)), n=4)
        _ciclo(a, "Braco_D", lambda p: (1.5 * math.sin(p), 0.0, -3.0 - 1.5 * math.sin(p)), n=4)
        _ciclo(a, "Antebraco_E", lambda p: (-6.0 - 1.5 * math.sin(p), 0.0, 0.0), n=4)
        _ciclo(a, "Antebraco_D", lambda p: (-6.0 - 1.5 * math.sin(p), 0.0, 0.0), n=4)


def _caminhada(arm, nome, quadros, passo, joelho, braco, cotovelo, inclina, quica, giro):
    with esqueleto.Acao(arm, nome, quadros, repete=True) as a:
        for suf, sg in (("_E", 1.0), ("_D", -1.0)):
            # Perna: sinal positivo = perna para tras. O joelho dobra quando a perna passa para a frente.
            _ciclo(a, "Perna" + suf, lambda p, sg=sg: (passo * sg * math.sin(p), 0.0, 0.0))
            _ciclo(
                a,
                "Canela" + suf,
                lambda p, sg=sg: (
                    joelho * max(0.0, -sg * math.cos(p)) ** 1.2 + 0.1 * joelho,
                    0.0,
                    0.0,
                ),
            )
            # Braco oposto a perna do mesmo lado.
            _ciclo(a, "Braco" + suf, lambda p, sg=sg: (-braco * sg * math.sin(p), 0.0, 0.0))
            _ciclo(
                a,
                "Antebraco" + suf,
                lambda p, sg=sg: (-(cotovelo + 0.1 * braco * (1 + sg * math.sin(p))), 0.0, 0.0),
            )
        _ciclo(a, "Quadril", lambda p: (inclina * 0.4, giro * 0.6 * math.sin(p), 2.0 * math.sin(p)))
        _ciclo(a, "Quadril", lambda p: (0.0, quica * math.cos(2.0 * p) - quica * 0.5, 0.0), rot=False)
        _ciclo(a, "Tronco", lambda p: (inclina * 0.6, -giro * math.sin(p), -1.5 * math.sin(p)))
        _ciclo(a, "Cabeca", lambda p: (-inclina * 0.8, giro * 0.4 * math.sin(p), 0.0))


def _esquivar(arm, m):
    k = m.altura_quadril / 0.93  # proporcao do quadril (pernas curtas ou longas)
    baixa = m.altura_quadril - 0.55 * m.escala
    with esqueleto.Acao(arm, ACAO_ESQUIVAR, QUADROS_ESQUIVAR, repete=False) as a:

        def encolhido(q, grau, perna, canela, braco, ante, tronco, cabeca):
            for suf, sg in (("_E", 1), ("_D", -1)):
                a.chave("Perna" + suf, q, rot=(perna * grau, 0, 0))
                a.chave("Canela" + suf, q, rot=(canela * grau, 0, 0))
                a.chave("Braco" + suf, q, rot=(braco * grau, 0, -10 * sg if braco < -60 else 0))
                a.chave("Antebraco" + suf, q, rot=(ante * grau, 0, 0))
            a.chave("Tronco", q, rot=(tronco * grau, 0, 0))
            a.chave("Cabeca", q, rot=(cabeca * grau, 0, 0))

        # Agacha, rola 360 graus para a frente (o quadril desce e gira, tudo encolhido) e levanta.
        a.chave("Quadril", 4, rot=(0, 0, 0), loc=(0, -0.25 * k, 0))
        encolhido(4, 1.0, -65, 85, -35, -60, 28, -12)
        for q, ang, grau in ((7, 0.0, 1.0), (14, 180.0, 1.2), (22, 360.0, 1.0)):
            a.chave("Quadril", q, rot=(ang, 0, 0), loc=(0, -baixa, 0.0))
            encolhido(q, grau, -100, 125, -70, -115, 55, 28)
        # Aterrissa agachado (o quadril termina em 360 = pose de pe) e levanta.
        a.chave("Quadril", 26, rot=(360.0, 0, 0), loc=(0, -0.26 * k, 0.0))
        encolhido(26, 1.0, -60, 80, -30, -55, 25, -10)
        a.chave("Quadril", QUADROS_ESQUIVAR, rot=(360.0, 0, 0), loc=(0, 0, 0))
        encolhido(QUADROS_ESQUIVAR, 0.0, 0, 0, 0, 0, 0, 0)


_OSSOS_ATAQUE = (
    "Braco_D",
    "Antebraco_D",
    "Mao_D",
    "Tronco",
    "Quadril",
    "Cabeca",
    "Braco_E",
    "Antebraco_E",
    "Perna_E",
    "Canela_E",
    "Perna_D",
    "Canela_D",
)


def _atacar(arm):
    with esqueleto.Acao(arm, ACAO_ATACAR, QUADROS_ATACAR, repete=False) as a:
        # Preparacao (quadro 7): braco direito para o alto e para tras, tronco girado.
        a.chave("Braco_D", 7, rot=(-150, 0, -22))
        a.chave("Antebraco_D", 7, rot=(-70, 0, 0))
        a.chave("Mao_D", 7, rot=(-20, 0, 0))
        a.chave("Tronco", 7, rot=(-8, -26, 0))
        a.chave("Quadril", 7, rot=(0, -12, 0), loc=(0, -0.05, -0.04))
        a.chave("Cabeca", 7, rot=(4, 14, 0))
        a.chave("Braco_E", 7, rot=(-25, 0, 18))
        a.chave("Antebraco_E", 7, rot=(-55, 0, 0))
        a.chave("Perna_E", 7, rot=(-24, 0, 0))
        a.chave("Canela_E", 7, rot=(20, 0, 0))
        a.chave("Perna_D", 7, rot=(18, 0, 0))
        a.chave("Canela_D", 7, rot=(30, 0, 0))
        # Golpe (quadro 11): o braco desce na diagonal, o corpo se joga para a frente.
        a.chave("Braco_D", 11, rot=(-40, 0, -6))
        a.chave("Antebraco_D", 11, rot=(-12, 0, 0))
        a.chave("Mao_D", 11, rot=(15, 0, 0))
        a.chave("Tronco", 11, rot=(18, 22, 0))
        a.chave("Quadril", 11, rot=(0, 14, 0), loc=(0, -0.08, 0.10))
        a.chave("Cabeca", 11, rot=(-8, -10, 0))
        a.chave("Braco_E", 11, rot=(15, 0, 28))
        a.chave("Antebraco_E", 11, rot=(-30, 0, 0))
        a.chave("Perna_E", 11, rot=(-34, 0, 0))
        a.chave("Canela_E", 11, rot=(34, 0, 0))
        a.chave("Perna_D", 11, rot=(24, 0, 0))
        a.chave("Canela_D", 11, rot=(24, 0, 0))
        # Segue o golpe (quadro 15) e volta ao repouso.
        a.chave("Braco_D", 15, rot=(-12, 0, -8))
        a.chave("Tronco", 15, rot=(12, 16, 0))
        a.chave("Quadril", 15, rot=(0, 10, 0), loc=(0, -0.07, 0.09))
        for osso in _OSSOS_ATAQUE:
            a.chave(osso, QUADROS_ATACAR, rot=(0, 0, 0))
        a.chave("Quadril", QUADROS_ATACAR, loc=(0, 0, 0))


def _morrer(arm, m):
    h = m.altura_quadril / 0.93
    with esqueleto.Acao(arm, ACAO_MORRER, QUADROS_MORRER, repete=False) as a:
        # Cambaleia, cai de joelhos e tomba de costas.
        etapas = [
            # quadro, (inclinar, baixar e recuar o quadril), pernas, canelas, tronco, cabeca, abertura dos bracos
            (6, (-10, 0.12, 0.05), (-20, 10), (40, 30), -10, -15, 25),
            (16, (-25, 0.45, 0.12), (-85, -60), (95, 100), 5, -20, 35),
            (30, (-70, 0.72, 0.22), (-60, -20), (80, 40), -10, -25, 45),
            (QUADROS_MORRER, (-90, 0.80, 0.10), (-8, -3), (18, 10), -6, -10, 55),
        ]
        for q, (inc, baixa, dz), (pe_e, pe_d), (ca_e, ca_d), tr, cab, ab in etapas:
            final = q == QUADROS_MORRER
            a.chave("Quadril", q, rot=(inc, 0, 0), loc=(0, -baixa * h, -dz))
            a.chave("Perna_E", q, rot=(pe_e, 0, 6 if final else 0))
            a.chave("Perna_D", q, rot=(pe_d, 0, -6 if final else 0))
            a.chave("Canela_E", q, rot=(ca_e, 0, 0))
            a.chave("Canela_D", q, rot=(ca_d, 0, 0))
            a.chave("Tronco", q, rot=(tr, 0, 0))
            a.chave("Cabeca", q, rot=(cab, 8 if final else 0, 0))
            a.chave("Braco_E", q, rot=(-10, 0, ab))
            a.chave("Braco_D", q, rot=(-10, 0, -ab))
            a.chave("Antebraco_E", q, rot=(-15, 0, 0))
            a.chave("Antebraco_D", q, rot=(-10, 0, 0))


def _dano(arm):
    with esqueleto.Acao(arm, ACAO_DANO, QUADROS_DANO, repete=False) as a:
        a.chave("Quadril", 3, rot=(-6, 4, 0), loc=(0, -0.03, -0.06))
        a.chave("Tronco", 3, rot=(-14, 10, 0))
        a.chave("Cabeca", 3, rot=(-16, 12, 0))
        a.chave("Braco_E", 3, rot=(20, 0, 30))
        a.chave("Braco_D", 3, rot=(20, 0, -30))
        a.chave("Antebraco_E", 3, rot=(-20, 0, 0))
        a.chave("Antebraco_D", 3, rot=(-20, 0, 0))
        a.chave("Perna_E", 3, rot=(-8, 0, 0))
        a.chave("Perna_D", 3, rot=(10, 0, 0))
        for osso in ("Tronco", "Cabeca", "Braco_E", "Braco_D", "Antebraco_E", "Antebraco_D", "Perna_E", "Perna_D", "Quadril"):
            a.chave(osso, QUADROS_DANO, rot=(0, 0, 0))
        a.chave("Quadril", QUADROS_DANO, loc=(0, 0, 0))


# --------------------------------------------------------------------------------------------
# Pintura


class Contexto:
    """Dados de pintura entregues as funcoes de cada raca (extras)."""

    def __init__(self, tex, ao, pal, sem, bio):
        self.tex = tex
        self.ao = ao
        self.pal = pal
        self.sem = sem
        self.bio = bio
        self.P = tex.pos
        self.N = tex.normal
        self.n = len(self.P)
        self.cor = np.zeros((self.n, 3), dtype=np.float32)
        self.emi = np.zeros((self.n, 3), dtype=np.float32)
        self.iris = np.zeros(self.n, dtype=bool)
        self.pele = tex.mascara("pele", "rosto")

    def m(self, *nomes):
        return self.tex.mascara(*nomes)


def veios(P, escala, sem, largura=0.05, oitavas=3):
    """Linhas finas e tortuosas (curvas de nivel de um ruido): fissuras, veias, marcas de casca."""
    v = fbm(P, escala, oitavas, sem)
    return suave(largura, 0.0, np.abs(v - 0.5))


def _tecido(P, f=420.0):
    return 0.93 + 0.07 * np.sin(P[:, 2] * f) * np.sin((P[:, 0] + P[:, 1]) * f)


def _pintar_base(x):
    P, N, pal, sem, b = x.P, x.N, x.pal, x.sem, x.bio
    z = P[:, 2]
    grande = fbm(P, 4.0, 3, sem)
    medio = fbm(P, 18.0, 3, sem + 1)
    fino = fbm(P, 90.0, 2, sem + 2)
    dobras = ruido(P, np.array([20.0, 20.0, 3.0]), sem + 3)
    h = b.cabeca

    # Pele.
    pele = x.pele
    if pele.any():
        base = misturar(pal["pele"], pal["pele_sombra"], suave(0.3, 0.8, grande) * 0.55)
        base = base * (0.93 + 0.14 * medio)[:, None]
        x.cor[pele] = base[pele]

    # Cabelo, em fios.
    cabelo = x.m("cabelo")
    if cabelo.any():
        d = np.zeros_like(P)
        d[:, 1] = 0.35
        d[:, 2] = -1.0
        fios = listras(P, d, 6.0, 150.0, sem + 5)
        fino2 = listras(P, d, 10.0, 320.0, sem + 6, oitavas=1)
        base = misturar(pal["cabelo"], pal["cabelo_ponta"], suave(0.55, 0.9, fios) * 0.6 + suave(0.5, 0.9, fino2) * 0.2)
        base = base * (0.7 + 0.5 * fios + 0.15 * (fino2 - 0.5))[:, None]
        x.cor[cabelo] = base[cabelo]

    # Tecidos e couros.
    for nome, tecido in (("tunica", True), ("calca", True), ("couro", False), ("bota", False)):
        mk = x.m(nome)
        if not mk.any():
            continue
        if tecido:
            base = misturar(pal[nome], pal[nome + "_escura"], suave(0.3, 0.8, dobras) * 0.6)
            base = base * (_tecido(P) * (0.82 + 0.3 * medio))[:, None]
        else:
            base = misturar(pal[nome], pal[nome + "_escura"], suave(0.35, 0.8, grande) * 0.6)
            base = base * (0.8 + 0.4 * fino + 0.15 * (medio - 0.5))[:, None]
        if nome in ("calca", "bota"):
            base = misturar(base, pal[nome + "_escura"], suave(0.40, 0.0, z) * 0.5)
        x.cor[mk] = base[mk]

    # Qualquer outra parte com cor propria na paleta (adornos): mancha leve.
    for nome in x.tex.partes:
        if nome in ("pele", "rosto", "cabelo", "tunica", "calca", "couro", "bota") or nome not in pal:
            continue
        mk = x.m(nome)
        if mk.any():
            base = pal[nome] * (0.82 + 0.3 * medio + 0.1 * (fino - 0.5))[:, None]
            x.cor[mk] = base[mk]

    # Rosto: bochechas, olhos, sobrancelhas e boca.
    rosto = x.m("rosto")
    if rosto.any():
        frente = (N[:, 1] < -0.25) & (P[:, 1] < -0.05 * h) & rosto
        ax = np.abs(P[:, 0])
        zh = b.zh
        boch = suave(0.04 * h, 0.012 * h, np.hypot(ax - 0.066 * h, z - (zh - 0.032 * h))) * frente
        x.cor = misturar(x.cor, pal["pele_rubor"] + 0.0 * x.cor, boch * 0.45)
        ex, ez = 0.040 * h, zh + 0.012 * h
        de = np.hypot((ax - ex) / (0.021 * h), (z - ez) / (0.0125 * h))
        olho = frente & (de < 1.0)
        anel = frente & (de >= 1.0) & (de < 1.45)
        x.cor[anel] = misturar(x.cor[anel], pal["pele_sombra"] * 0.55, 0.85)
        branco = misturar(pal["esclera"], pal["pele_sombra"], suave(0.6, 1.0, de) * 0.6)
        x.cor[olho] = branco[olho]
        di = np.hypot((ax - ex) / (0.0105 * h), (z - ez) / (0.0115 * h))
        ir = frente & (di < 1.0)
        cor_ir = misturar(pal["iris"], pal["iris"] * 0.45, suave(0.4, 1.0, di))
        x.cor[ir] = cor_ir[ir]
        pup = frente & (di < 0.45)
        x.cor[pup] = pal["pupila"]
        x.iris |= ir
        # Sobrancelhas (levemente inclinadas) e boca.
        zb = ez + 0.030 * h + 0.18 * (ax - ex)
        sobr = frente & (np.abs(z - zb) < 0.0042 * h) & (ax > 0.016 * h) & (ax < 0.064 * h)
        x.cor[sobr] = misturar(x.cor[sobr], pal["cabelo_ponta"] * 0.6, 0.85)
        boca = frente & (np.abs(z - (zh - 0.067 * h)) < 0.0035 * h) & (ax < 0.028 * h)
        x.cor[boca] = misturar(x.cor[boca], pal["boca"], 0.9)


def pintar(forma):
    pal = {k: hex_rgb(v) for k, v in forma.paleta.items()}
    sem = forma.semente

    def f(tex, ao):
        x = Contexto(tex, ao, pal, sem, forma.bio)
        _pintar_base(x)
        if forma.extra is not None:
            forma.extra(x)
        cor = x.cor * luz_pintada(tex, 0.25, 0.25, 0.95)[:, None]
        cor = aplicar_ao(cor, ao, forca=0.7, gama=1.2)
        mapas = {"Cor": np.clip(cor, 0, 1)}
        if x.emi.any():
            mapas["Emissao"] = np.clip(x.emi, 0, 1)
        return mapas

    return f


def receita(forma):
    from . import pipeline

    c = forma.construir()
    zs = [v[2] for v in c.verts]
    escala = forma.altura_alvo / (max(zs) - min(zs))
    return pipeline.Receita(
        forma.nome,
        forma.construir,
        pintar(forma),
        Medidas(forma.bio),
        escala=escala,
        tamanho_textura=1024,
        ao_distancia=0.22,
        rig=sys.modules[__name__],
        feature="Personagem",
    )


class Forma:
    """Tudo que uma raca informa: proporcoes, paleta, adornos e pintura extra."""

    def __init__(self, nome, bio, paleta, semente, adornos=None, extra=None, altura_alvo=1.8):
        self.nome = nome
        self.bio = bio
        self.paleta = paleta
        self.semente = semente
        self.adornos = adornos
        self.extra = extra
        self.altura_alvo = altura_alvo

    def construir(self):
        c = Construtor(self.nome, semente=self.semente)
        construir_corpo(c, self)
        if self.adornos is not None:
            self.adornos(c, self)
        return c
