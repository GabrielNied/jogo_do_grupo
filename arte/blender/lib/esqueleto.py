"""Esqueleto simples (ossos rigidos) e animacoes por keyframes.

Todos os ossos apontam para cima (+Z) com rolagem 0. Assim os eixos locais de rotacao sao os
mesmos em todo osso, e a animacao e escrita em termos do mundo:
- rot[0] (eixo X local = X do mundo): "inclinar" (positivo abaixa o focinho; numa perna
  pendurada, positivo leva o pe para tras);
- rot[1] (Y local = Z do mundo): "virar" (gira em volta do eixo vertical);
- rot[2] (Z local = -Y do mundo): "rolar" (tomba para o lado).
loc segue os mesmos eixos locais: loc[0] = lado (+X), loc[1] = cima, loc[2] = frente.
"""

import math

import bpy

QUADROS_POR_SEGUNDO = 30
COMPRIMENTO_OSSO = 0.1  # metros (so muda o desenho do osso, nao o movimento)


def criar_armadura(nome, ossos, escala_osso=1.0):
    """ossos: lista de (nome, pai ou None, cabeca (x,y,z)). Devolve o objeto de armadura."""
    dados = bpy.data.armatures.new(nome)
    obj = bpy.data.objects.new(nome, dados)
    bpy.context.scene.collection.objects.link(obj)
    for o in bpy.context.scene.objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    criados = {}
    for nome_osso, pai, cabeca in ossos:
        b = dados.edit_bones.new(nome_osso)
        b.head = cabeca
        b.tail = (cabeca[0], cabeca[1], cabeca[2] + COMPRIMENTO_OSSO * escala_osso)
        b.roll = 0.0
        if pai is not None:
            b.parent = criados[pai]
        criados[nome_osso] = b
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in obj.pose.bones:
        pb.rotation_mode = "XYZ"
    return obj


def vincular(malha, armadura):
    """Prende a malha na armadura (os grupos de vertices ja tem o nome dos ossos)."""
    malha.parent = armadura
    mod = malha.modifiers.new("Armadura", "ARMATURE")
    mod.object = armadura
    return mod


class Acao:
    """Cria uma acao (clipe) e grava keyframes nela.

    Uso:
        with Acao(armadura, "Andar", quadros=24, repete=True) as a:
            a.chave("Corpo", 0, loc=(0, 0.01, 0))
    Ossos sem chave ficam em repouso no clipe (a acao zera a pose antes).
    """

    def __init__(self, armadura, nome, quadros, repete=False):
        self.arm = armadura
        self.nome = nome
        self.quadros = quadros
        self.repete = repete

    def __enter__(self):
        if self.arm.animation_data is None:
            self.arm.animation_data_create()
        acao = bpy.data.actions.new(self.nome)
        acao.use_fake_user = True
        self.arm.animation_data.action = acao
        self.acao = acao
        for pb in self.arm.pose.bones:
            pb.location = (0.0, 0.0, 0.0)
            pb.rotation_euler = (0.0, 0.0, 0.0)
            pb.scale = (1.0, 1.0, 1.0)
        # Todo osso com chave de repouso no inicio (e no fim, se repete): o clipe sempre define
        # a pose inteira. Sem repetir, a ultima chave de cada osso segura ate o fim.
        for pb in self.arm.pose.bones:
            for q in (0, self.quadros) if self.repete else (0,):
                pb.keyframe_insert("location", frame=q, group=pb.name)
                pb.keyframe_insert("rotation_euler", frame=q, group=pb.name)
        return self

    def chave(self, osso, quadro, rot=None, loc=None):
        """rot em graus (inclinar, virar, rolar); loc em metros (lado, cima, frente)."""
        pb = self.arm.pose.bones[osso]
        if rot is not None:
            pb.rotation_euler = [math.radians(a) for a in rot]
            pb.keyframe_insert("rotation_euler", frame=quadro, group=osso)
        if loc is not None:
            pb.location = loc
            pb.keyframe_insert("location", frame=quadro, group=osso)

    def ciclo(self, osso, valores, rot=True):
        """Keyframes espalhados igualmente no clipe; o ultimo repete o primeiro (laco)."""
        n = len(valores)
        for i, v in enumerate(list(valores) + [valores[0]]):
            q = round(self.quadros * i / n)
            if rot:
                self.chave(osso, q, rot=v)
            else:
                self.chave(osso, q, loc=v)

    def __exit__(self, *exc):
        self.acao.frame_range = (0, self.quadros)
        self.acao.use_frame_range = True
        self.acao.use_cyclic = self.repete
        return False


def limpar_pose(armadura):
    for pb in armadura.pose.bones:
        pb.location = (0.0, 0.0, 0.0)
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
