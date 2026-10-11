"""Passo a passo comum a todas as criaturas: malha > UV > pintura > AO > esqueleto > FBX > previa."""

import os
import time

import numpy as np
from mathutils import Matrix

from . import cena, esqueleto, previa, quadrupede, textura


class Receita:
    """O que cada script de criatura informa ao pipeline.

    construir() -> malha.Construtor (em escala de modelagem);
    pintar(texels, ao) -> dict de mapas {"Cor": (N,3), "Brilho": (N,4)?, "Emissao": (N,3)?};
    medidas: quadrupede.Medidas (em escala de modelagem);
    escala: fator final (modelagem -> metros do jogo).
    """

    def __init__(self, nome, construir, pintar, medidas, escala=1.0, tamanho_textura=1024, ao_distancia=0.35):
        self.nome = nome
        self.construir = construir
        self.pintar = pintar
        self.medidas = medidas
        self.escala = escala
        self.tamanho_textura = tamanho_textura
        self.ao_distancia = ao_distancia


def _escalar_medidas(m, s):
    for campo in ("corpo", "cabeca", "rabo", "orelha", "perna_dianteira", "perna_traseira"):
        setattr(m, campo, tuple(c * s for c in getattr(m, campo)))
    m.altura_tombo *= s
    m.escala *= s
    return m


def gerar(receita):
    """Gera FBX e PNGs da receita. Linha de comando (depois de '--'):

    --saida <pasta>   onde gravar FBX e PNG (padrao: Features/Mobs/Art/Modelos do client)
    --previa <pasta>  tambem renderiza as folhas de conferencia nessa pasta
    """
    inicio = time.time()
    args = cena.argumentos()
    saida = args.get("--saida") or cena.pasta_modelos_unity()
    os.makedirs(saida, exist_ok=True)

    cena.limpar_cena()
    construtor = receita.construir()
    obj = construtor.criar()
    print("[%s] triangulos: %d" % (receita.nome, construtor.triangulos()))

    cena.desdobrar_uv(obj)
    tex = textura.rasterizar(obj, receita.tamanho_textura, construtor.partes)
    ao_img = textura.assar_ao(obj, receita.tamanho_textura, distancia=receita.ao_distancia)
    mapas = receita.pintar(tex, textura.ao_nos_texels(tex, ao_img))

    caminhos = {}
    for chave, valores in mapas.items():
        caminho = os.path.join(saida, "%s_%s.png" % (receita.nome, chave))
        fundo = (0.0, 0.0, 0.0, 0.0) if chave != "Cor" else (0.5, 0.5, 0.5)
        textura.salvar_png(textura.imagem_de_texels(tex, valores, fundo), caminho)
        caminhos[chave] = caminho
    cena.material_com_textura(
        obj,
        receita.nome,
        caminhos["Cor"],
        metal_png=caminhos.get("Brilho"),
        emissao_png=caminhos.get("Emissao"),
    )

    # Pes exatamente em Z = 0 (a variacao dos vertices pode deixar algo abaixo do chao).
    zs = np.array([v.co.z for v in obj.data.vertices])
    dz = -float(zs.min())
    obj.data.transform(Matrix.Translation((0.0, 0.0, dz)))
    for campo in ("corpo", "cabeca", "rabo", "orelha", "perna_dianteira", "perna_traseira"):
        x, y, z = getattr(receita.medidas, campo)
        setattr(receita.medidas, campo, (x, y, z + dz))

    s = receita.escala
    if s != 1.0:
        obj.data.transform(Matrix.Scale(s, 4))
        obj.data.update()
    m = _escalar_medidas(receita.medidas, s)
    obj.name = receita.nome + "_Malha"  # libera o nome para a armadura (raiz do FBX)
    arm = quadrupede.criar_esqueleto(receita.nome, m)
    esqueleto.vincular(obj, arm)
    quadrupede.animar(arm, m)

    fbx = os.path.join(saida, receita.nome + ".fbx")
    cena.exportar_fbx(fbx, arm, obj)

    zs = np.array([v.co.z for v in obj.data.vertices])
    altura = float(zs.max() - zs.min())
    print("[%s] altura: %.3f m (pes em %.3f)" % (receita.nome, altura, zs.min()))

    if args.get("--previa"):
        pasta = args["--previa"]
        os.makedirs(pasta, exist_ok=True)
        cam, alvo = previa.preparar(altura)
        centro = (0.0, 0.0, altura * 0.45)
        dist = altura * 2.6
        previa.vistas(os.path.join(pasta, receita.nome + "_vistas.png"), cam, alvo, centro, dist)
        rosto = (0.0, m.cabeca[1] - 0.2 * m.escala, m.cabeca[2] - 0.08 * m.escala)
        previa.vistas(
            os.path.join(pasta, receita.nome + "_rosto.png"),
            cam,
            alvo,
            rosto,
            altura * 1.1,
            [(30, 12), (-35, 20), (90, 5)],
        )
        previa.acoes(
            os.path.join(pasta, receita.nome + "_acoes.png"),
            cam,
            alvo,
            centro,
            dist,
            arm,
            quadrupede.ACOES,
        )
    print("[%s] pronto em %.1f s: %s" % (receita.nome, time.time() - inicio, fbx))
