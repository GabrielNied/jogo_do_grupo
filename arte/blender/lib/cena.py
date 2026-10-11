"""Cena, UV, materiais e exportacao FBX para o Unity."""

import math
import os
import sys

import bpy

# Margem entre ilhas de UV (fracao da textura).
MARGEM_UV = 0.012
# Angulo do Smart UV Project (graus): maior = ilhas maiores e menos emendas.
ANGULO_UV = 60.0


def limpar_cena():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    cena = bpy.context.scene
    cena.unit_settings.system = "METRIC"
    cena.unit_settings.scale_length = 1.0
    cena.render.fps = 30
    return cena


def argumentos():
    """Argumentos depois de '--' na linha do Blender, como dicionario {--chave: valor}."""
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    r = {}
    i = 0
    while i < len(args):
        if args[i].startswith("--"):
            if i + 1 < len(args) and not args[i + 1].startswith("--"):
                r[args[i]] = args[i + 1]
                i += 2
                continue
            r[args[i]] = True
        i += 1
    return r


def raiz_do_repo():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))


def pasta_modelos_unity(feature="Mobs"):
    return os.path.join(
        raiz_do_repo(), "client", "Assets", "_Game", "Features", feature, "Art", "Modelos"
    )


def desdobrar_uv(obj):
    """Smart UV Project + empacotamento, numa camada 'UV'."""
    if not obj.data.uv_layers:
        obj.data.uv_layers.new(name="UV")
    for o in bpy.context.scene.objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(
        angle_limit=math.radians(ANGULO_UV),
        island_margin=MARGEM_UV,
        area_weight=0.0,
        correct_aspect=True,
        scale_to_bounds=False,
    )
    bpy.ops.uv.pack_islands(margin=MARGEM_UV, rotate=True)
    bpy.ops.object.mode_set(mode="OBJECT")


def material_com_textura(obj, nome, cor_png, rugosidade=0.85, metal_png=None, emissao_png=None):
    """Material Principled com a textura de cor (para a previa; o Unity cria o seu)."""
    mat = bpy.data.materials.new(nome)
    mat.use_nodes = True
    nos = mat.node_tree.nodes
    bsdf = nos.get("Principled BSDF")
    tex = nos.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(cor_png, check_existing=True)
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = rugosidade
    if metal_png:
        m = nos.new("ShaderNodeTexImage")
        m.image = bpy.data.images.load(metal_png, check_existing=True)
        m.image.colorspace_settings.name = "Non-Color"
        sep = nos.new("ShaderNodeSeparateColor")
        mat.node_tree.links.new(m.outputs["Color"], sep.inputs["Color"])
        mat.node_tree.links.new(sep.outputs[0], bsdf.inputs["Metallic"])
        inv = nos.new("ShaderNodeMath")
        inv.operation = "SUBTRACT"
        inv.inputs[0].default_value = 1.0
        mat.node_tree.links.new(m.outputs["Alpha"], inv.inputs[1])
        mat.node_tree.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    if emissao_png:
        e = nos.new("ShaderNodeTexImage")
        e.image = bpy.data.images.load(emissao_png, check_existing=True)
        mat.node_tree.links.new(e.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 1.5
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return mat


def exportar_fbx(caminho, armadura, malha):
    """FBX para o Unity: 1 unidade = 1 m, Y para cima, frente +Z, um clipe por acao."""
    for o in bpy.context.scene.objects:
        o.select_set(False)
    armadura.select_set(True)
    malha.select_set(True)
    bpy.context.view_layer.objects.active = armadura
    armadura.animation_data.action = None
    for pb in armadura.pose.bones:  # a pose de repouso e a do modelo exportado
        pb.location = (0.0, 0.0, 0.0)
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()
    os.makedirs(os.path.dirname(caminho), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=caminho,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=False,
        mesh_smooth_type="FACE",
        use_tspace=False,
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_armature_deform_only=False,
        armature_nodetype="NULL",
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="STRIP",
        embed_textures=False,
    )
