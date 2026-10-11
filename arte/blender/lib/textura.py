"""Texturas pintadas por procedimento, calculadas em numpy no espaco 3D do modelo.

Como funciona: cada texel da textura e "rasterizado" a partir da UV, e ganha a posicao 3D,
a normal, o codigo da parte e o indice da face de onde veio (um G-buffer em espaco de UV).
A cor e uma funcao desses dados (ruido, listras de pelagem, degrade, luz pintada), entao a
pintura nao tem emenda entre ilhas de UV. Depois, a oclusao de ambiente (assada no Cycles)
escurece as dobras e as juntas, e as bordas das ilhas sao estendidas (margem) para o mipmap.
"""

import math

import bpy
import numpy as np

# Margem (em texels) estendida em volta de cada ilha de UV.
MARGEM_TEXELS = 12

# Oclusao de ambiente assada.
AO_AMOSTRAS = 48
AO_DISTANCIA = 0.35  # metros; o script da criatura pode trocar pela escala dela


# --------------------------------------------------------------------------------------------
# Cores


def hex_rgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i : i + 2], 16) / 255.0 for i in (0, 2, 4)], dtype=np.float32)


def misturar(a, b, t):
    """Mistura cores (N,3) ou (3,) por t (N,) ou escalar."""
    t = np.asarray(t, dtype=np.float32)
    if t.ndim == 1:
        t = t[:, None]
    return a * (1.0 - t) + b * t


def suave(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def satura(c, k):
    """k > 1 satura; k < 1 dessatura."""
    cinza = (c[..., 0:1] * 0.3 + c[..., 1:2] * 0.59 + c[..., 2:3] * 0.11)
    return np.clip(cinza + (c - cinza) * k, 0.0, 1.0)


# --------------------------------------------------------------------------------------------
# Ruido (valor 3D com interpolacao suave), estavel entre maquinas


def _hash(ix, iy, iz, semente):
    h = (ix * 374761393 + iy * 668265263 + iz * 1274126177 + semente * 1442695041) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF).astype(np.float32) / float(0xFFFFFF)


def ruido(p, escala=1.0, semente=0):
    """Ruido de valor 3D em [0, 1]. p: (N,3) em metros; escala: frequencia (1/m) ou (3,)."""
    q = p * np.asarray(escala, dtype=np.float64)
    i = np.floor(q).astype(np.int64)
    f = (q - i).astype(np.float32)
    u = f * f * (3.0 - 2.0 * f)
    x, y, z = i[:, 0], i[:, 1], i[:, 2]
    r = np.zeros(len(p), dtype=np.float32)
    for dx in (0, 1):
        wx = u[:, 0] if dx else 1.0 - u[:, 0]
        for dy in (0, 1):
            wy = u[:, 1] if dy else 1.0 - u[:, 1]
            for dz in (0, 1):
                wz = u[:, 2] if dz else 1.0 - u[:, 2]
                r += wx * wy * wz * _hash(x + dx, y + dy, z + dz, semente)
    return r


def fbm(p, escala=1.0, oitavas=4, semente=0, ganho=0.5):
    """Soma de oitavas de ruido, normalizada para [0, 1]."""
    total = np.zeros(len(p), dtype=np.float32)
    amp, soma = 1.0, 0.0
    esc = np.asarray(escala, dtype=np.float64)
    for o in range(oitavas):
        total += amp * ruido(p, esc * (2.0**o), semente + o * 31)
        soma += amp
        amp *= ganho
    return total / soma


def listras(p, direcao, ao_longo, atraves, semente=0, oitavas=2):
    """Listras de pelagem: ruido esticado na direcao (N,3) dos pelos.

    ao_longo: frequencia na direcao dos pelos (baixa = fios compridos);
    atraves: frequencia de lado (alta = fios finos).
    """
    d = direcao / np.maximum(np.linalg.norm(direcao, axis=1, keepdims=True), 1e-6)
    ao = np.sum(p * d, axis=1, keepdims=True)
    q = p * atraves + d * ao * (ao_longo - atraves)
    return fbm(q, 1.0, oitavas, semente)


def aleatorio_por_face(face, semente=0):
    """Um valor estavel em [0, 1] por face (variacao de facetas)."""
    f = face.astype(np.int64)
    return _hash(f, f * 3 + 1, f * 7 + 2, semente)


# --------------------------------------------------------------------------------------------
# G-buffer em espaco de UV


class Texels:
    """Dados de cada texel coberto pela UV (vetores do mesmo tamanho N)."""

    def __init__(self, tamanho, linhas, colunas, pos, normal, parte, face, partes):
        self.tamanho = tamanho
        self.linhas = linhas
        self.colunas = colunas
        self.pos = pos
        self.normal = normal
        self.parte = parte
        self.face = face
        self.partes = partes  # nome -> codigo

    def mascara(self, *nomes):
        m = np.zeros(len(self.parte), dtype=bool)
        for n in nomes:
            if n in self.partes:
                m |= self.parte == self.partes[n]
        return m


def rasterizar(obj, tamanho, partes):
    """Rasteriza a malha (pose de repouso) no espaco de UV e devolve Texels."""
    malha = obj.data
    malha.calc_loop_triangles()
    uv = np.zeros(len(malha.loops) * 2, dtype=np.float32)
    malha.uv_layers.active.data.foreach_get("uv", uv)
    uv = uv.reshape(-1, 2) * tamanho
    co = np.zeros(len(malha.vertices) * 3, dtype=np.float32)
    malha.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    cn = np.zeros(len(malha.loops) * 3, dtype=np.float32)
    malha.corner_normals.foreach_get("vector", cn)
    cn = cn.reshape(-1, 3)
    loop_vert = np.zeros(len(malha.loops), dtype=np.int64)
    malha.loops.foreach_get("vertex_index", loop_vert)
    parte_face = np.zeros(len(malha.polygons), dtype=np.int64)
    malha.attributes["parte"].data.foreach_get("value", parte_face)

    n_tri = len(malha.loop_triangles)
    tri_loops = np.zeros(n_tri * 3, dtype=np.int64)
    malha.loop_triangles.foreach_get("loops", tri_loops)
    tri_loops = tri_loops.reshape(-1, 3)
    tri_poly = np.zeros(n_tri, dtype=np.int64)
    malha.loop_triangles.foreach_get("polygon_index", tri_poly)

    dono = np.full((tamanho, tamanho), -1, dtype=np.int64)
    bary = np.zeros((tamanho, tamanho, 3), dtype=np.float32)
    for t in range(n_tri):
        a, b, c = uv[tri_loops[t]]
        x0 = int(max(math.floor(min(a[0], b[0], c[0])), 0))
        x1 = int(min(math.ceil(max(a[0], b[0], c[0])), tamanho - 1))
        y0 = int(max(math.floor(min(a[1], b[1], c[1])), 0))
        y1 = int(min(math.ceil(max(a[1], b[1], c[1])), tamanho - 1))
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(den) < 1e-12:
            continue
        w0 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / den
        w1 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / den
        w2 = 1.0 - w0 - w1
        dentro = (w0 >= -0.02) & (w1 >= -0.02) & (w2 >= -0.02)
        if not dentro.any():
            continue
        yy = (ys[dentro] - 0.5).astype(np.int64)
        xx = (xs[dentro] - 0.5).astype(np.int64)
        dono[yy, xx] = t
        bary[yy, xx, 0] = w0[dentro]
        bary[yy, xx, 1] = w1[dentro]
        bary[yy, xx, 2] = w2[dentro]

    linhas, colunas = np.nonzero(dono >= 0)
    tri = dono[linhas, colunas]
    w = bary[linhas, colunas]
    w = np.clip(w, 0.0, None)
    w /= np.maximum(w.sum(axis=1, keepdims=True), 1e-6)
    lp = tri_loops[tri]
    pos = sum(co[loop_vert[lp[:, k]]] * w[:, k : k + 1] for k in range(3))
    nor = sum(cn[lp[:, k]] * w[:, k : k + 1] for k in range(3))
    nor /= np.maximum(np.linalg.norm(nor, axis=1, keepdims=True), 1e-6)
    poly = tri_poly[tri]
    return Texels(tamanho, linhas, colunas, pos, nor, parte_face[poly], poly, partes)


# --------------------------------------------------------------------------------------------
# Imagens


def imagem_de_texels(tex, cor, fundo=(0.5, 0.5, 0.5)):
    """Monta a imagem (H,W,3) com as cores dos texels e estende as bordas das ilhas."""
    img = np.zeros((tex.tamanho, tex.tamanho, cor.shape[1]), dtype=np.float32)
    img[...] = np.asarray(fundo[: cor.shape[1]], dtype=np.float32)
    cheio = np.zeros((tex.tamanho, tex.tamanho), dtype=bool)
    img[tex.linhas, tex.colunas] = cor
    cheio[tex.linhas, tex.colunas] = True
    return dilatar(img, cheio, MARGEM_TEXELS)


def dilatar(img, cheio, passos):
    """Estende as cores das ilhas para os texels vazios vizinhos (margem para o mipmap)."""
    img = img.copy()
    cheio = cheio.copy()
    for _ in range(passos):
        soma = np.zeros_like(img)
        cont = np.zeros(cheio.shape, dtype=np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
            m = np.roll(np.roll(cheio, dy, 0), dx, 1)
            v = np.roll(np.roll(img, dy, 0), dx, 1)
            soma += v * m[..., None]
            cont += m
        novo = (~cheio) & (cont > 0)
        if not novo.any():
            break
        img[novo] = soma[novo] / cont[novo][:, None]
        cheio |= novo
    return img


def salvar_png(img, caminho, nome=None):
    """Grava (H,W,3) ou (H,W,4) em [0,1] como PNG (linha 0 = base da imagem, como a UV)."""
    h, w, c = img.shape
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[..., :c] = np.clip(img, 0.0, 1.0)
    nome = nome or bpy.path.basename(caminho)
    im = bpy.data.images.get(nome) or bpy.data.images.new(nome, w, h, alpha=(c == 4))
    if im.size[0] != w or im.size[1] != h:
        im.scale(w, h)
    im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw = caminho
    im.file_format = "PNG"
    im.save()
    return im


def carregar_png(caminho):
    im = bpy.data.images.load(caminho, check_existing=False)
    w, h = im.size
    px = np.zeros(w * h * 4, dtype=np.float32)
    im.pixels.foreach_get(px)
    bpy.data.images.remove(im)
    return px.reshape(h, w, 4)


# --------------------------------------------------------------------------------------------
# Oclusao de ambiente assada no Cycles


def assar_ao(obj, tamanho, distancia=AO_DISTANCIA, amostras=AO_AMOSTRAS, chao=True):
    """Assa a oclusao de ambiente do objeto (pose de repouso) e devolve (H,W) em [0,1].

    chao: poe um plano em Z=0 durante o bake, para a barriga e as patas escurecerem.
    """
    cena = bpy.context.scene
    motor = cena.render.engine
    cena.render.engine = "CYCLES"
    cena.cycles.device = "CPU"
    cena.cycles.samples = amostras
    if cena.world is None:
        cena.world = bpy.data.worlds.new("Mundo")
    cena.world.light_settings.distance = distancia

    img = bpy.data.images.new("_ao", tamanho, tamanho, alpha=False, float_buffer=True)
    img.colorspace_settings.name = "Non-Color"
    mat = bpy.data.materials.new("_bake_ao")
    mat.use_nodes = True
    no = mat.node_tree.nodes.new("ShaderNodeTexImage")
    no.image = img
    mat.node_tree.nodes.active = no
    antigos = list(obj.data.materials)
    obj.data.materials.clear()
    obj.data.materials.append(mat)

    plano = None
    if chao:
        m = bpy.data.meshes.new("_chao")
        s = 50.0
        m.from_pydata([(-s, -s, 0), (s, -s, 0), (s, s, 0), (-s, s, 0)], [], [(0, 1, 2, 3)])
        plano = bpy.data.objects.new("_chao", m)
        cena.collection.objects.link(plano)

    for o in cena.objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.bake(type="AO", margin=MARGEM_TEXELS, use_clear=True)

    px = np.zeros(tamanho * tamanho * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    ao = px.reshape(tamanho, tamanho, 4)[..., 0].copy()

    obj.data.materials.clear()
    for a in antigos:
        obj.data.materials.append(a)
    bpy.data.materials.remove(mat)
    bpy.data.images.remove(img)
    if plano is not None:
        bpy.data.objects.remove(plano)
        bpy.data.meshes.remove(m)
    cena.render.engine = motor
    return ao


def ao_nos_texels(tex, ao):
    return ao[tex.linhas, tex.colunas]


# --------------------------------------------------------------------------------------------
# Luz pintada (o "sombreado" desenhado na textura)


def luz_pintada(tex, forca_cima=0.35, forca_baixo=0.3, base=0.95):
    """Clareia o que olha para cima e escurece o que olha para baixo."""
    nz = tex.normal[:, 2]
    return base + forca_cima * np.clip(nz, 0.0, 1.0) - forca_baixo * np.clip(-nz, 0.0, 1.0)


def aplicar_ao(cor, ao, forca=0.6, gama=1.0):
    k = 1.0 - forca + forca * np.clip(ao, 0.0, 1.0) ** gama
    return cor * k[:, None]
