"""Renders de conferencia (vistas e quadros de cada animacao) montados numa folha so."""

import math
import os
import tempfile

import bpy
import numpy as np

from . import textura

TAMANHO_QUADRO = 384
COR_FUNDO = (0.42, 0.47, 0.52)
COR_CHAO = (0.33, 0.36, 0.30)

# (azimute, elevacao) em graus; azimute 0 = de frente para a criatura.
VISTAS_PADRAO = [(35, 18), (90, 5), (150, 22), (0, 8), (-60, 40), (180, 10)]


def _motor(cena):
    for motor in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "BLENDER_WORKBENCH"):
        try:
            cena.render.engine = motor
            return motor
        except TypeError:
            continue
    return cena.render.engine


def preparar(altura):
    """Luzes, mundo, chao e camera para uma criatura de 'altura' metros."""
    cena = bpy.context.scene
    _motor(cena)
    cena.render.resolution_x = TAMANHO_QUADRO
    cena.render.resolution_y = TAMANHO_QUADRO
    cena.render.film_transparent = False
    cena.view_settings.view_transform = "Standard"
    if cena.world is None:
        cena.world = bpy.data.worlds.new("Mundo")
    cena.world.use_nodes = True
    # Ceu em degrade (claro em cima, escuro embaixo): da o que refletir aos metais.
    nos = cena.world.node_tree.nodes
    fundo = nos.get("Background")
    fundo.inputs[1].default_value = 0.9
    coord = nos.new("ShaderNodeTexCoord")
    sep = nos.new("ShaderNodeSeparateXYZ")
    rampa = nos.new("ShaderNodeValToRGB")
    rampa.color_ramp.elements[0].position = 0.35
    rampa.color_ramp.elements[0].color = (*COR_CHAO, 1.0)
    rampa.color_ramp.elements[1].position = 0.75
    rampa.color_ramp.elements[1].color = (0.75, 0.82, 0.92, 1.0)
    mapa = nos.new("ShaderNodeMapRange")
    mapa.inputs["From Min"].default_value = -1.0
    ligar = cena.world.node_tree.links.new
    ligar(coord.outputs["Generated"], sep.inputs[0])
    ligar(sep.outputs["Z"], mapa.inputs["Value"])
    ligar(mapa.outputs["Result"], rampa.inputs["Fac"])
    ligar(rampa.outputs["Color"], fundo.inputs[0])

    sol = bpy.data.lights.new("Sol", "SUN")
    sol.energy = 3.2
    sol.angle = math.radians(8)
    o = bpy.data.objects.new("Sol", sol)
    o.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    cena.collection.objects.link(o)

    m = bpy.data.meshes.new("Chao")
    s = altura * 6
    m.from_pydata([(-s, -s, 0), (s, -s, 0), (s, s, 0), (-s, s, 0)], [], [(0, 1, 2, 3)])
    chao = bpy.data.objects.new("Chao", m)
    mat = bpy.data.materials.new("Chao")
    mat.use_nodes = True
    mat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (*COR_CHAO, 1)
    mat.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value = 1.0
    m.materials.append(mat)
    cena.collection.objects.link(chao)

    cam_dados = bpy.data.cameras.new("Camera")
    cam_dados.lens = 50
    cam = bpy.data.objects.new("Camera", cam_dados)
    cena.collection.objects.link(cam)
    cena.camera = cam
    alvo = bpy.data.objects.new("Alvo", None)
    cena.collection.objects.link(alvo)
    c = cam.constraints.new("TRACK_TO")
    c.target = alvo
    c.track_axis = "TRACK_NEGATIVE_Z"
    c.up_axis = "UP_Y"
    return cam, alvo


def _posicionar(cam, alvo, centro, distancia, azimute, elevacao):
    az, el = math.radians(azimute), math.radians(elevacao)
    alvo.location = centro
    cam.location = (
        centro[0] + distancia * math.sin(az) * math.cos(el),
        centro[1] - distancia * math.cos(az) * math.cos(el),
        centro[2] + distancia * math.sin(el),
    )


def _render(caminho):
    cena = bpy.context.scene
    cena.render.filepath = caminho
    cena.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    return textura.carregar_png(caminho)[..., :3]


def _folha(quadros, colunas):
    h, w, _ = quadros[0].shape
    linhas = (len(quadros) + colunas - 1) // colunas
    folha = np.ones((linhas * h, colunas * w, 3), dtype=np.float32)
    for i, q in enumerate(quadros):
        r, c = divmod(i, colunas)
        # A imagem do Blender comeca embaixo: a primeira linha da folha fica no topo.
        r = linhas - 1 - r
        folha[r * h : (r + 1) * h, c * w : (c + 1) * w] = q
    return folha


def vistas(caminho, cam, alvo, centro, distancia, lista=None, colunas=3):
    tmp = tempfile.mkdtemp()
    quadros = []
    for i, (az, el) in enumerate(lista or VISTAS_PADRAO):
        _posicionar(cam, alvo, centro, distancia, az, el)
        quadros.append(_render(os.path.join(tmp, "v%d.png" % i)))
    textura.salvar_png(_folha(quadros, colunas), caminho)


def acoes(caminho, cam, alvo, centro, distancia, armadura, nomes, por_acao=6, azimute=60, elevacao=15):
    """Uma linha por acao, com 'por_acao' quadros igualmente espacados."""
    tmp = tempfile.mkdtemp()
    cena = bpy.context.scene
    _posicionar(cam, alvo, centro, distancia, azimute, elevacao)
    quadros = []
    for nome in nomes:
        acao = bpy.data.actions[nome]
        armadura.animation_data.action = acao
        if hasattr(armadura.animation_data, "action_slot") and acao.slots:
            armadura.animation_data.action_slot = acao.slots[0]
        ini, fim = acao.frame_range
        for k in range(por_acao):
            q = ini + (fim - ini) * k / (por_acao - 1)
            cena.frame_set(int(round(q)))
            quadros.append(_render(os.path.join(tmp, "%s_%d.png" % (nome, k))))
    armadura.animation_data.action = None
    textura.salvar_png(_folha(quadros, por_acao), caminho)
