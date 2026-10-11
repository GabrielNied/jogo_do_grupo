"""Presa-Velha (criatura 200, chefe raro): o javali velho, maior, grisalho, com presas enormes,
cicatrizes no rosto e no flanco, olho esquerdo cego e orelha direita rasgada.

Rodar (na raiz do repo):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --factory-startup \
      --python arte/blender/criaturas/presa_velha.py [-- --previa <pasta>]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

import javali  # noqa: E402
from lib import pipeline  # noqa: E402

# ---------------------------------------------------------------------------------------------
# Valores ajustaveis (o resto vem do javali)

NOME = "PresaVelha"
ALTURA_ALVO = 1.5  # metros
SEMENTE = 23
LARGURA = 1.16
CORCUNDA = 0.05
PRESA_COMPRIMENTO = 1.75
PRESA_RAIO = 1.45
PRESA_CURVA = 1.4
CRINA_ALTURA = 1.45
GRISALHO = 0.8

PALETA = {
    "pelo": "#4a3d35",
    "pelo_escuro": "#251e1a",
    "pelo_ponta": "#9a8b7a",
    "barriga": "#5e544b",
    "bochecha": "#a39a8e",
    "crina_ponta": "#bdb4a6",
    "presa": "#f2e9d2",
    "presa_base": "#8f7955",
}


def forma():
    f = javali.Forma()
    f.nome = NOME
    f.altura_alvo = ALTURA_ALVO
    f.semente = SEMENTE
    f.paleta.update(PALETA)
    f.largura = LARGURA
    f.corcunda = CORCUNDA
    f.presa_comprimento = PRESA_COMPRIMENTO
    f.presa_raio = PRESA_RAIO
    f.presa_curva = PRESA_CURVA
    f.crina_altura = CRINA_ALTURA
    f.grisalho = GRISALHO
    f.cicatriz = True
    f.orelha_rasgada = True
    return f


if __name__ == "__main__":
    receita = javali.receita(forma())
    receita.medidas.peso = 1.25  # mais pesado: clipes um pouco mais lentos
    pipeline.gerar(receita)
