"""Malha low poly montada por partes (lofts, cones, esferas), sem operadores do Blender.

Convencao de eixos (a mesma de todos os modelos): Blender em metros, Z para cima, a criatura
olha para -Y (a vista "Front" do Blender ve o rosto) e os pes ficam em Z = 0. O exportador
(exportar.py) converte para o Unity (Y para cima, frente +Z).

Cada parte guarda o nome do osso que a move (pele rigida: peso 1 num osso so) e um codigo de
parte, gravado na face, que a pintura (textura.py) usa para escolher a cor.
"""

import math

import bmesh
import bpy

# Variacao aleatoria padrao dos vertices (em metros), para o low poly nao parecer "de CAD".
VARIACAO_PADRAO = 0.0

# Angulo acima do qual uma aresta fica dura no sombreamento suave (graus).
ANGULO_SUAVE = 50.0


def _norm(v):
    c = math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2])
    if c < 1e-9:
        return (0.0, 0.0, 0.0)
    return (v[0] / c, v[1] / c, v[2] / c)


def _cruz(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def _sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def _soma(a, b, k=1.0):
    return (a[0] + b[0] * k, a[1] + b[1] * k, a[2] + b[2] * k)


def _dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _rand(i, semente):
    """Numero pseudoaleatorio estavel em [-1, 1] (sem depender do random do Python)."""
    x = math.sin(i * 12.9898 + semente * 78.233) * 43758.5453
    return (x - math.floor(x)) * 2.0 - 1.0


class Secao:
    """Uma secao do loft: centro, raios nos dois eixos da secao e o expoente da forma.

    expoente 2 = elipse; maior que 2 = mais quadrada; menor = losango.
    """

    def __init__(self, centro, raio_u, raio_v=None, expoente=2.0):
        self.centro = tuple(centro)
        self.raio_u = raio_u
        self.raio_v = raio_u if raio_v is None else raio_v
        self.expoente = expoente


class Construtor:
    """Acumula partes e no fim cria um unico objeto de malha com grupos de vertices por osso."""

    def __init__(self, nome, semente=1):
        self.nome = nome
        self.semente = semente
        self.verts = []
        self.faces = []
        self.face_parte = []
        self.face_lisa = []
        self.vert_osso = []
        self.partes = {}  # nome da parte -> codigo inteiro

    def codigo(self, parte):
        if parte not in self.partes:
            self.partes[parte] = len(self.partes)
        return self.partes[parte]

    def _add(self, verts, faces, parte, osso, lisa, variacao):
        base = len(self.verts)
        for i, v in enumerate(verts):
            if variacao > 0.0:
                k = base + i
                v = (
                    v[0] + _rand(k, self.semente) * variacao,
                    v[1] + _rand(k + 7919, self.semente) * variacao,
                    v[2] + _rand(k + 104729, self.semente) * variacao,
                )
            self.verts.append(v)
            self.vert_osso.append(osso)
        cod = self.codigo(parte)
        for f in faces:
            self.faces.append(tuple(base + i for i in f))
            self.face_parte.append(cod)
            self.face_lisa.append(lisa)

    def loft(
        self,
        secoes,
        lados,
        parte,
        osso,
        ref=(0.0, 0.0, 1.0),
        tampa_ini=True,
        tampa_fim=True,
        ponta_ini=None,
        ponta_fim=None,
        lisa=True,
        variacao=VARIACAO_PADRAO,
        giro=0.0,
    ):
        """Tubo que passa pelos centros das secoes.

        ref: direcao que vira o eixo "v" da secao (o "cima" da secao). O eixo "u" e
        perpendicular ao caminho e a ref. ponta_ini/ponta_fim: ponto onde o tubo fecha em
        bico (cone); sem ponta, a tampa e plana.
        giro: rotacao (em voltas) do primeiro vertice de cada anel.
        """
        n = len(secoes)
        verts = []
        for i, s in enumerate(secoes):
            if n == 1:
                tang = (0.0, 1.0, 0.0)
            elif i == 0:
                tang = _sub(secoes[1].centro, s.centro)
            elif i == n - 1:
                tang = _sub(s.centro, secoes[i - 1].centro)
            else:
                tang = _sub(secoes[i + 1].centro, secoes[i - 1].centro)
            t = _norm(tang)
            r = ref
            if abs(_dot(t, _norm(r))) > 0.98:
                r = (1.0, 0.0, 0.0) if abs(t[0]) < 0.9 else (0.0, 1.0, 0.0)
            u = _norm(_cruz(t, r))
            v = _norm(_cruz(u, t))
            for k in range(lados):
                a = 2.0 * math.pi * (k / lados + giro)
                ca, sa = math.cos(a), math.sin(a)
                e = 2.0 / s.expoente
                cu = math.copysign(abs(ca) ** e, ca) * s.raio_u
                cv = math.copysign(abs(sa) ** e, sa) * s.raio_v
                verts.append(_soma(_soma(s.centro, u, cu), v, cv))
        faces = []
        for i in range(n - 1):
            for k in range(lados):
                a = i * lados + k
                b = i * lados + (k + 1) % lados
                c = (i + 1) * lados + (k + 1) % lados
                d = (i + 1) * lados + k
                faces.append((a, b, c, d))
        if tampa_ini:
            if ponta_ini is not None:
                p = len(verts)
                verts.append(tuple(ponta_ini))
                for k in range(lados):
                    faces.append(((k + 1) % lados, k, p))
            else:
                faces.append(tuple(reversed(range(lados))))
        if tampa_fim:
            o = (n - 1) * lados
            if ponta_fim is not None:
                p = len(verts)
                verts.append(tuple(ponta_fim))
                for k in range(lados):
                    faces.append((o + k, o + (k + 1) % lados, p))
            else:
                faces.append(tuple(o + k for k in range(lados)))
        self._add(verts, faces, parte, osso, lisa, variacao)

    def cone(self, base, ponta, raio, lados, parte, osso, ref=(0.0, 0.0, 1.0), achatado=1.0, lisa=False):
        """Cone simples (espinho, chifre reto, tufo). achatado < 1 achata no eixo u."""
        t = _norm(_sub(ponta, base))
        r = ref
        if abs(_dot(t, _norm(r))) > 0.98:
            r = (1.0, 0.0, 0.0) if abs(t[0]) < 0.9 else (0.0, 1.0, 0.0)
        u = _norm(_cruz(t, r))
        v = _norm(_cruz(u, t))
        verts = []
        for k in range(lados):
            a = 2.0 * math.pi * k / lados
            verts.append(_soma(_soma(base, u, math.cos(a) * raio * achatado), v, math.sin(a) * raio))
        verts.append(tuple(ponta))
        p = lados
        faces = [tuple(reversed(range(lados)))]
        for k in range(lados):
            faces.append((k, (k + 1) % lados, p))
        self._add(verts, faces, parte, osso, lisa, 0.0)

    def esfera(self, centro, raio, parte, osso, segmentos=8, aneis=6, escala=(1.0, 1.0, 1.0), lisa=True):
        """Esfera UV low poly (olhos, juntas)."""
        verts = [(centro[0], centro[1], centro[2] + raio * escala[2])]
        for i in range(1, aneis):
            fi = math.pi * i / aneis
            for k in range(segmentos):
                a = 2.0 * math.pi * k / segmentos
                verts.append(
                    (
                        centro[0] + math.sin(fi) * math.cos(a) * raio * escala[0],
                        centro[1] + math.sin(fi) * math.sin(a) * raio * escala[1],
                        centro[2] + math.cos(fi) * raio * escala[2],
                    )
                )
        verts.append((centro[0], centro[1], centro[2] - raio * escala[2]))
        faces = []
        for k in range(segmentos):
            faces.append((0, 1 + k, 1 + (k + 1) % segmentos))
        for i in range(aneis - 2):
            for k in range(segmentos):
                a = 1 + i * segmentos + k
                b = 1 + i * segmentos + (k + 1) % segmentos
                faces.append((a, a + segmentos, b + segmentos, b))
        ult = len(verts) - 1
        o = 1 + (aneis - 2) * segmentos
        for k in range(segmentos):
            faces.append((o + (k + 1) % segmentos, o + k, ult))
        self._add(verts, faces, parte, osso, lisa, 0.0)

    def cristal(self, base, ponta, raio, lados, parte, osso, ombro=0.7, ref=(0.0, 0.0, 1.0), achatado=1.0):
        """Cristal facetado: prisma que fecha em ponta (sempre sombreado chapado)."""
        meio = _soma(base, _sub(ponta, base), ombro)
        self.loft(
            [Secao(base, raio * achatado, raio, 2.0), Secao(meio, raio * achatado * 1.08, raio * 1.08, 2.0)],
            lados,
            parte,
            osso,
            ref=ref,
            ponta_fim=ponta,
            lisa=False,
            giro=0.5 / lados,
        )

    def espelhar_x(self, funcao, osso_esq, osso_dir):
        """Chama funcao(sinal, osso) para o lado esquerdo (+X) e o direito (-X)."""
        funcao(1.0, osso_esq)
        funcao(-1.0, osso_dir)

    def criar(self, colecao=None):
        """Cria o objeto de malha, com faces marcadas (atributo 'parte'), lisas/chapadas e grupos."""
        malha = bpy.data.meshes.new(self.nome)
        malha.from_pydata(self.verts, [], self.faces)
        malha.validate(clean_customdata=False)
        # Normais para fora em cada parte fechada (a ordem dos vertices das tampas varia).
        bm = bmesh.new()
        bm.from_mesh(malha)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(malha)
        bm.free()
        malha.update()
        attr = malha.attributes.new("parte", "INT", "FACE")
        attr.data.foreach_set("value", self.face_parte)
        malha.polygons.foreach_set("use_smooth", self.face_lisa)
        obj = bpy.data.objects.new(self.nome, malha)
        (colecao or bpy.context.scene.collection).objects.link(obj)
        grupos = {}
        for i, osso in enumerate(self.vert_osso):
            if osso not in grupos:
                grupos[osso] = obj.vertex_groups.new(name=osso)
            grupos[osso].add([i], 1.0, "REPLACE")
        malha.set_sharp_from_angle(angle=math.radians(ANGULO_SUAVE))
        # set_sharp_from_angle mexe no sombreamento das faces: as chapadas voltam a ser chapadas.
        malha.polygons.foreach_set("use_smooth", self.face_lisa)
        malha.update()
        return obj

    def triangulos(self):
        return sum(len(f) - 2 for f in self.faces)
