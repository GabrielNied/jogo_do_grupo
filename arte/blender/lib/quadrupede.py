"""Esqueleto e animacoes de quadrupede (Idle, Andar, Atacar, Morrer), reaproveitados pelas criaturas.

Ossos: Raiz (no chao) > Corpo > Cabeca > Orelha_E/Orelha_D; Corpo > Rabo; Corpo > 4 pernas.
E = esquerda da criatura (+X), D = direita (-X). A criatura olha para -Y.
"""

from . import esqueleto

ACAO_PARADO = "Idle"
ACAO_ANDAR = "Andar"
ACAO_ATACAR = "Atacar"
ACAO_MORRER = "Morrer"
ACOES = [ACAO_PARADO, ACAO_ANDAR, ACAO_ATACAR, ACAO_MORRER]

# Clipes de preparo do chefe (Medidas.ataques_do_chefe): um por tipo de ataque do servidor.
ACAO_INVESTIDA = "AtacarInvestida"
ACAO_RAIOS = "AtacarRaios"
ACAO_PISADA = "AtacarPisada"
ACOES_DO_CHEFE = [ACAO_INVESTIDA, ACAO_RAIOS, ACAO_PISADA]

# Fracao (0 a 1) do clipe em que fica a pose de contato, o instante do golpe. Os mesmos valores
# estao em ContatosDosClipes (client/.../Mobs/Scripts), que o client usa para casar o contato
# com o fim do aviso; mude nos dois lugares e gere de novo.
CONTATO_ATACAR = 0.71
CONTATO_INVESTIDA = 0.85
CONTATO_RAIOS = 0.85
CONTATO_PISADA = 0.85

PERNAS = ["Perna_DianteiraE", "Perna_DianteiraD", "Perna_TraseiraE", "Perna_TraseiraD"]


class Medidas:
    """Pontos de articulacao (metros, pose de repouso) e o jeito de mexer.

    escala: multiplica os deslocamentos das animacoes (1 = javali de 0,9 m).
    peso: > 1 deixa os clipes mais lentos (criatura pesada).
    """

    def __init__(
        self,
        corpo,
        cabeca,
        rabo,
        orelha,
        perna_dianteira,
        perna_traseira,
        escala=1.0,
        peso=1.0,
        altura_tombo=0.22,
        ataques_do_chefe=False,
    ):
        self.corpo = corpo  # (x, y, z)
        self.cabeca = cabeca
        self.rabo = rabo
        self.orelha = orelha  # lado esquerdo; o direito espelha X
        self.perna_dianteira = perna_dianteira  # lado esquerdo
        self.perna_traseira = perna_traseira
        self.escala = escala
        self.peso = peso
        self.altura_tombo = altura_tombo  # altura do centro do corpo deitado de lado
        self.ataques_do_chefe = ataques_do_chefe  # gera os 3 clipes de preparo do chefe


def _esp(p):
    return (-p[0], p[1], p[2])


def ossos(m):
    return [
        ("Raiz", None, (0.0, 0.0, 0.0)),
        ("Corpo", "Raiz", m.corpo),
        ("Cabeca", "Corpo", m.cabeca),
        ("Orelha_E", "Cabeca", m.orelha),
        ("Orelha_D", "Cabeca", _esp(m.orelha)),
        ("Rabo", "Corpo", m.rabo),
        ("Perna_DianteiraE", "Corpo", m.perna_dianteira),
        ("Perna_DianteiraD", "Corpo", _esp(m.perna_dianteira)),
        ("Perna_TraseiraE", "Corpo", m.perna_traseira),
        ("Perna_TraseiraD", "Corpo", _esp(m.perna_traseira)),
    ]


def criar_esqueleto(nome, m):
    return esqueleto.criar_armadura(nome, ossos(m), escala_osso=m.escala)


def _q(segundos, m):
    return max(1, round(segundos * m.peso * esqueleto.QUADROS_POR_SEGUNDO))


def animar(arm, m):
    s = m.escala
    _parado(arm, m, s)
    _andar(arm, m, s)
    _atacar(arm, m, s)
    _morrer(arm, m, s)
    if m.ataques_do_chefe:
        _investida(arm, m, s)
        _raios(arm, m, s)
        _pisada(arm, m, s)


def _parado(arm, m, s):
    with esqueleto.Acao(arm, ACAO_PARADO, _q(2.4, m), repete=True) as a:
        a.ciclo("Corpo", [(0, 0, 0.0), (0, 0.010 * s, 0), (0, 0, 0), (0, 0.004 * s, 0)], rot=False)
        a.ciclo("Cabeca", [(0, 0, 0), (-3, 4, 1), (2, 0, 0), (-2, -5, -1)])
        a.ciclo("Rabo", [(0, 18, 0), (5, -18, 0)])
        # Orelha esquerda da uma sacudida rapida no meio do clipe.
        f = a.quadros
        a.chave("Orelha_E", int(f * 0.50), rot=(0, 0, 0))
        a.chave("Orelha_E", int(f * 0.55), rot=(-20, 0, -15))
        a.chave("Orelha_E", int(f * 0.60), rot=(5, 0, 5))
        a.chave("Orelha_E", int(f * 0.66), rot=(0, 0, 0))
        a.ciclo("Orelha_D", [(0, 0, 0), (4, 0, 3), (0, 0, 0)])


def _andar(arm, m, s):
    amp = 24
    with esqueleto.Acao(arm, ACAO_ANDAR, _q(0.8, m), repete=True) as a:
        # Pares diagonais em fase: dianteira E com traseira D.
        frente = [(-amp, 0, 0), (0, 0, 0), (amp, 0, 0), (0, 0, 0)]
        tras = [(amp, 0, 0), (0, 0, 0), (-amp, 0, 0), (0, 0, 0)]
        a.ciclo("Perna_DianteiraE", frente)
        a.ciclo("Perna_TraseiraD", frente)
        a.ciclo("Perna_DianteiraD", tras)
        a.ciclo("Perna_TraseiraE", tras)
        # Pe levanta quando a perna volta para a frente.
        sobe = (0, 0.035 * s, 0)
        zero = (0, 0, 0)
        a.ciclo("Perna_DianteiraE", [zero, zero, zero, sobe], rot=False)
        a.ciclo("Perna_TraseiraD", [zero, zero, zero, sobe], rot=False)
        a.ciclo("Perna_DianteiraD", [zero, sobe, zero, zero], rot=False)
        a.ciclo("Perna_TraseiraE", [zero, sobe, zero, zero], rot=False)
        a.ciclo("Corpo", [(0, 0.012 * s, 0), (0, -0.006 * s, 0)] * 2, rot=False)
        a.ciclo("Cabeca", [(3, 3, 0), (-2, 0, 0), (3, -3, 0), (-2, 0, 0)])
        a.ciclo("Rabo", [(0, 15, 0), (0, -15, 0)])
        a.ciclo("Orelha_E", [(4, 0, 0), (-4, 0, 0)])
        a.ciclo("Orelha_D", [(-4, 0, 0), (4, 0, 0)])


def _atacar(arm, m, s):
    # Preparo de ~1 s (o aviso do golpe do servidor), investida e volta.
    f = _q(1.4, m)
    golpe = round(f * CONTATO_ATACAR)
    preparo = round(f * 0.55)
    with esqueleto.Acao(arm, ACAO_ATACAR, f, repete=False) as a:
        a.chave("Corpo", 0, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Corpo", preparo, loc=(0, -0.05 * s, -0.10 * s), rot=(6, 0, 0))
        a.chave("Corpo", golpe, loc=(0, 0.02 * s, 0.22 * s), rot=(-6, 0, 0))
        a.chave("Corpo", f, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Cabeca", 0, rot=(0, 0, 0))
        a.chave("Cabeca", preparo, rot=(24, 6, 0))
        a.chave("Cabeca", golpe - 2, rot=(18, 0, 0))
        a.chave("Cabeca", golpe, rot=(-30, -8, 0))
        a.chave("Cabeca", f, rot=(0, 0, 0))
        # A dianteira direita cava o chao duas vezes no preparo.
        p = "Perna_DianteiraD"
        a.chave(p, 0, rot=(0, 0, 0), loc=(0, 0, 0))
        for i, t in enumerate((0.12, 0.24, 0.36, 0.48)):
            q = round(f * t)
            if i % 2 == 0:
                a.chave(p, q, rot=(-30, 0, 0), loc=(0, 0.05 * s, 0))
            else:
                a.chave(p, q, rot=(25, 0, 0), loc=(0, 0, 0))
        a.chave(p, preparo, rot=(0, 0, 0), loc=(0, 0, 0))
        a.chave(p, golpe, rot=(-20, 0, 0))
        a.chave(p, f, rot=(0, 0, 0))
        for perna, sinal in (("Perna_DianteiraE", -1), ("Perna_TraseiraE", 1), ("Perna_TraseiraD", 1)):
            a.chave(perna, preparo, rot=(10 * sinal, 0, 0))
            a.chave(perna, golpe, rot=(-25 * sinal if sinal < 0 else 30, 0, 0))
            a.chave(perna, f, rot=(0, 0, 0))
        for o in ("Orelha_E", "Orelha_D"):
            a.chave(o, preparo, rot=(25, 0, 0))
            a.chave(o, f, rot=(0, 0, 0))
        a.chave("Rabo", preparo, rot=(-40, 0, 0))
        a.chave("Rabo", f, rot=(0, 0, 0))


def _morrer(arm, m, s):
    f = _q(1.2, m)
    queda = round(f * 0.55)
    descer = m.corpo[2] - m.altura_tombo
    with esqueleto.Acao(arm, ACAO_MORRER, f, repete=False) as a:
        a.chave("Corpo", 0, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Corpo", round(f * 0.15), loc=(0, 0.04 * s, -0.03 * s), rot=(-8, 0, -4))
        a.chave("Corpo", round(f * 0.35), loc=(0.05 * s, -descer * 0.35, 0), rot=(4, 0, 30))
        a.chave("Corpo", queda, loc=(0.12 * s, -descer - 0.015 * s, 0), rot=(0, 0, 92))
        a.chave("Corpo", round(f * 0.70), loc=(0.12 * s, -descer + 0.02 * s, 0), rot=(0, 0, 86))
        a.chave("Corpo", f, loc=(0.12 * s, -descer, 0), rot=(0, 0, 90))
        a.chave("Cabeca", 0, rot=(0, 0, 0))
        a.chave("Cabeca", round(f * 0.15), rot=(-25, 0, 0))
        a.chave("Cabeca", queda, rot=(10, 0, 12))
        a.chave("Cabeca", f, rot=(18, 0, 20))
        for perna, ang in zip(PERNAS, (-25, -35, 20, 30)):
            a.chave(perna, 0, rot=(0, 0, 0))
            a.chave(perna, round(f * 0.3), rot=(ang * 0.4, 0, 0))
            a.chave(perna, queda, rot=(ang, 0, 6))
            a.chave(perna, round(f * 0.8), rot=(ang * 1.1, 0, 2))
            a.chave(perna, f, rot=(ang * 1.05, 0, 3))
        for o in ("Orelha_E", "Orelha_D"):
            a.chave(o, queda, rot=(30, 0, 0))
        a.chave("Rabo", queda, rot=(40, 0, 0))


def _investida(arm, m, s):
    # Agacha para tras, cava o chao com a pata e, no contato, o corpo sai para a frente de
    # cabeca baixa. Depois do contato, so o amortecimento.
    f = _q(2.0, m)
    golpe = round(f * CONTATO_INVESTIDA)
    agacha = round(f * 0.60)
    with esqueleto.Acao(arm, ACAO_INVESTIDA, f, repete=False) as a:
        a.chave("Corpo", 0, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Corpo", agacha, loc=(0, -0.10 * s, -0.16 * s), rot=(10, 0, 0))
        a.chave("Corpo", golpe, loc=(0, 0.06 * s, 0.34 * s), rot=(-10, 0, 0))
        a.chave("Corpo", f, loc=(0, 0.02 * s, 0.10 * s), rot=(-3, 0, 0))
        a.chave("Cabeca", 0, rot=(0, 0, 0))
        a.chave("Cabeca", agacha, rot=(30, 0, 0))
        a.chave("Cabeca", golpe, rot=(38, 0, 0))
        a.chave("Cabeca", f, rot=(8, 0, 0))
        p = "Perna_DianteiraD"
        a.chave(p, 0, rot=(0, 0, 0), loc=(0, 0, 0))
        for i, t in enumerate((0.10, 0.22, 0.34, 0.46)):
            q = round(f * t)
            if i % 2 == 0:
                a.chave(p, q, rot=(-35, 0, 0), loc=(0, 0.06 * s, 0))
            else:
                a.chave(p, q, rot=(25, 0, 0), loc=(0, 0, 0))
        a.chave(p, agacha, rot=(0, 0, 0), loc=(0, 0, 0))
        a.chave(p, golpe, rot=(-30, 0, 0))
        a.chave(p, f, rot=(0, 0, 0))
        for perna, sinal in (("Perna_DianteiraE", -1), ("Perna_TraseiraE", 1), ("Perna_TraseiraD", 1)):
            a.chave(perna, agacha, rot=(14 * sinal, 0, 0))
            a.chave(perna, golpe, rot=(-30 if sinal < 0 else 35, 0, 0))
            a.chave(perna, f, rot=(0, 0, 0))
        for o in ("Orelha_E", "Orelha_D"):
            a.chave(o, agacha, rot=(30, 0, 0))
            a.chave(o, f, rot=(0, 0, 0))
        a.chave("Rabo", agacha, rot=(-45, 0, 0))
        a.chave("Rabo", golpe, rot=(30, 0, 0))
        a.chave("Rabo", f, rot=(0, 0, 0))


def _raios(arm, m, s):
    # Empina o peito e joga a cabeca para cima enquanto o corpo treme (os cristais carregam);
    # no contato o peito esta no alto, e depois a cabeca cai de uma vez para soltar os raios.
    f = _q(2.0, m)
    golpe = round(f * CONTATO_RAIOS)
    with esqueleto.Acao(arm, ACAO_RAIOS, f, repete=False) as a:
        a.chave("Corpo", 0, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Corpo", round(f * 0.45), loc=(0, 0.02 * s, 0.04 * s), rot=(-12, 0, 0))
        # Tremor do corpo na carga.
        for i, t in enumerate((0.50, 0.58, 0.66, 0.74)):
            a.chave("Corpo", round(f * t), rot=(-14 - 3 * (i % 2), 0, 3 if i % 2 == 0 else -3))
        a.chave("Corpo", golpe, loc=(0, 0.03 * s, 0.06 * s), rot=(-20, 0, 0))
        a.chave("Corpo", f, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Cabeca", 0, rot=(0, 0, 0))
        a.chave("Cabeca", round(f * 0.45), rot=(-30, 0, 0))
        a.chave("Cabeca", golpe, rot=(-48, 0, 0))
        a.chave("Cabeca", f, rot=(14, 0, 0))
        for perna in ("Perna_DianteiraE", "Perna_DianteiraD"):
            a.chave(perna, round(f * 0.45), rot=(-22, 0, 0))
            a.chave(perna, golpe, rot=(-34, 0, 0))
            a.chave(perna, f, rot=(0, 0, 0))
        for perna in ("Perna_TraseiraE", "Perna_TraseiraD"):
            a.chave(perna, golpe, rot=(14, 0, 0))
            a.chave(perna, f, rot=(0, 0, 0))
        for o in ("Orelha_E", "Orelha_D"):
            a.chave(o, golpe, rot=(-30, 0, 0))
            a.chave(o, f, rot=(0, 0, 0))
        a.chave("Rabo", golpe, rot=(-55, 0, 0))
        a.chave("Rabo", f, rot=(0, 0, 0))


def _pisada(arm, m, s):
    # Empina sobre as patas de tras, levanta as duas dianteiras e, no contato, as patas
    # batem no chao com o corpo caindo junto (o impacto).
    f = _q(2.0, m)
    golpe = round(f * CONTATO_PISADA)
    alto = round(f * 0.62)
    with esqueleto.Acao(arm, ACAO_PISADA, f, repete=False) as a:
        a.chave("Corpo", 0, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Corpo", alto, loc=(0, 0.02 * s, 0.14 * s), rot=(-22, 0, 0))
        a.chave("Corpo", golpe, loc=(0, 0.02 * s, -0.08 * s), rot=(8, 0, 0))
        a.chave("Corpo", f, loc=(0, 0, 0), rot=(0, 0, 0))
        a.chave("Cabeca", 0, rot=(0, 0, 0))
        a.chave("Cabeca", alto, rot=(-18, 0, 0))
        a.chave("Cabeca", golpe, rot=(26, 0, 0))
        a.chave("Cabeca", f, rot=(0, 0, 0))
        for perna in ("Perna_DianteiraE", "Perna_DianteiraD"):
            a.chave(perna, alto, rot=(-70, 0, 0), loc=(0, 0.10 * s, 0))
            a.chave(perna, golpe, rot=(-12, 0, 0), loc=(0, -0.04 * s, 0))
            a.chave(perna, f, rot=(0, 0, 0), loc=(0, 0, 0))
        for perna in ("Perna_TraseiraE", "Perna_TraseiraD"):
            a.chave(perna, alto, rot=(20, 0, 0))
            a.chave(perna, golpe, rot=(-8, 0, 0))
            a.chave(perna, f, rot=(0, 0, 0))
        for o in ("Orelha_E", "Orelha_D"):
            a.chave(o, golpe, rot=(35, 0, 0))
            a.chave(o, f, rot=(0, 0, 0))
        a.chave("Rabo", alto, rot=(-35, 0, 0))
        a.chave("Rabo", golpe, rot=(30, 0, 0))
        a.chave("Rabo", f, rot=(0, 0, 0))
