"""
Utilidades de geometría para el arte de BEFORE IT WAS YOU (Blender 4.2, sin interfaz).

Convención: se modela en coordenadas del mundo de Unity leídas como Blender (x = este, y = norte, z = arriba).
La exportación usa axis_forward='Z', axis_up='Y', de modo que un punto (x, y, z) de Blender aparece en Unity en
(x, z, y): las coordenadas del plano técnico se escriben tal cual.
UVs en metros (u, v = distancias reales); Unity ajusta el tiling con el tamaño físico de cada textura.
"""
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


_materials = {}

# Materiales cuya textura debe ir girada 90° (tablas verticales con una textura de tablas horizontales).
UV_ROTATION = {"M_Fence": math.pi / 2, "M_ShedWall": math.pi / 2, "M_ShedWallIn": math.pi / 2, "M_ShedTrim": math.pi / 2,
               "M_PoleWood": math.pi / 2, "M_DoorServicePaint": math.pi / 2}


def material(name):
    if name not in _materials:
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        _materials[name] = m
    return _materials[name]


class Builder:
    """Acumula caras con material y UV métricas en un bmesh; produce un objeto al final."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.mats = []

    def mat_index(self, mat_name):
        if mat_name not in self.mats:
            self.mats.append(mat_name)
        return self.mats.index(mat_name)

    def face(self, points, mat, uv_mode="auto", uv_origin=(0, 0, 0), uv_rot=None, flip=False):
        verts = [self.bm.verts.new(Vector(p)) for p in points]
        if flip:
            verts.reverse()
        try:
            f = self.bm.faces.new(verts)
        except ValueError:
            return None
        f.material_index = self.mat_index(mat)
        f.normal_update()
        self.apply_uv(f, uv_mode, Vector(uv_origin), uv_rot)
        return f

    def apply_uv(self, f, mode, origin, rot=None):
        if rot is None:
            rot = UV_ROTATION.get(self.mats[f.material_index], 0.0)
        n = f.normal
        if mode == "auto":
            if abs(n.z) > 0.85:
                t = Vector((1, 0, 0))
                b = Vector((0, 1, 0))
            else:
                t = Vector((0, 0, 1)).cross(n)
                if t.length < 1e-6:
                    t = Vector((1, 0, 0))
                t.normalize()
                b = n.cross(t)
                b.normalize()
        elif mode == "x":
            t, b = Vector((0, 1, 0)), Vector((0, 0, 1))
        elif mode == "y":
            t, b = Vector((1, 0, 0)), Vector((0, 0, 1))
        else:
            t, b = Vector((1, 0, 0)), Vector((0, 1, 0))
        c, s = math.cos(rot), math.sin(rot)
        for loop in f.loops:
            p = loop.vert.co - origin
            u, v = p.dot(t), p.dot(b)
            loop[self.uv].uv = (u * c - v * s, u * s + v * c)

    def box(self, x0, y0, z0, x1, y1, z1, mat, faces="all", mats=None):
        """Caja alineada a ejes. faces: subconjunto de 'nsewtb' (norte, sur, este, oeste, arriba, abajo)."""
        if faces == "all":
            faces = "nsewtb"
        m = mats or {}
        p = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        if "b" in faces:
            self.face([p[0], p[3], p[2], p[1]], m.get("b", mat))
        if "t" in faces:
            self.face([p[4], p[5], p[6], p[7]], m.get("t", mat))
        if "s" in faces:
            self.face([p[0], p[1], p[5], p[4]], m.get("s", mat))
        if "n" in faces:
            self.face([p[2], p[3], p[7], p[6]], m.get("n", mat))
        if "w" in faces:
            self.face([p[3], p[0], p[4], p[7]], m.get("w", mat))
        if "e" in faces:
            self.face([p[1], p[2], p[6], p[5]], m.get("e", mat))

    def prism(self, base, height, mat, top_mat=None, z0=0.0, bottom=False):
        """Extruye un polígono (lista de (x, y), antihorario visto desde arriba) entre z0 y z0 + height."""
        n = len(base)
        for i in range(n):
            a, b = base[i], base[(i + 1) % n]
            self.face([(a[0], a[1], z0), (b[0], b[1], z0), (b[0], b[1], z0 + height), (a[0], a[1], z0 + height)], mat)
        self.face([(x, y, z0 + height) for x, y in base], top_mat or mat)
        if bottom:
            self.face([(x, y, z0) for x, y in reversed(base)], mat)

    def cylinder(self, cx, cy, z0, z1, r, mat, segments=16, caps=True, r_top=None):
        r_top = r if r_top is None else r_top
        ring0 = [(cx + r * math.cos(2 * math.pi * i / segments), cy + r * math.sin(2 * math.pi * i / segments)) for i in range(segments)]
        ring1 = [(cx + r_top * math.cos(2 * math.pi * i / segments), cy + r_top * math.sin(2 * math.pi * i / segments)) for i in range(segments)]
        for i in range(segments):
            j = (i + 1) % segments
            self.face([(*ring0[i], z0), (*ring0[j], z0), (*ring1[j], z1), (*ring1[i], z1)], mat)
        if caps:
            self.face([(x, y, z1) for x, y in ring1], mat)
            self.face([(x, y, z0) for x, y in reversed(ring0)], mat)

    def object(self, collection=None, smooth_angle=None):
        mesh = bpy.data.meshes.new(self.name)
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0005)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        obj = bpy.data.objects.new(self.name, mesh)
        (collection or bpy.context.scene.collection).objects.link(obj)
        if smooth_angle is not None:
            for poly in mesh.polygons:
                poly.use_smooth = True
            mod = obj.modifiers.new("Smooth", "EDGE_SPLIT")
            mod.split_angle = math.radians(smooth_angle)
        return obj


def wall_with_openings(b, axis, fixed, a0, a1, z0, z1, openings, thickness, outward, mat_out, mat_in=None, mat_reveal=None, inner=True):
    """
    Muro plano con huecos rectangulares.
    axis: 'x' (muro que corre en X, cara en y = fixed) o 'y' (corre en Y, cara en x = fixed).
    outward: +1/-1, sentido de la cara exterior sobre el eje perpendicular.
    openings: lista de (a_start, a_end, z_start, z_end).
    Se trocea en rectángulos sin solapes con los huecos y se añaden mochetas (jambas, dintel y alféizar interiores).
    """
    mat_in = mat_in or mat_out
    mat_reveal = mat_reveal or mat_out
    cuts_a = sorted({a0, a1, *[min(max(o[0], a0), a1) for o in openings], *[min(max(o[1], a0), a1) for o in openings]})
    cuts_z = sorted({z0, z1, *[min(max(o[2], z0), z1) for o in openings], *[min(max(o[3], z0), z1) for o in openings]})

    def P(a, z, off):
        d = fixed + off
        return (a, d, z) if axis == "x" else (d, a, z)

    def inside(ac, zc):
        return any(o[0] <= ac <= o[1] and o[2] <= zc <= o[3] for o in openings)

    out_off = 0.0
    in_off = -outward * thickness
    for i in range(len(cuts_a) - 1):
        for j in range(len(cuts_z) - 1):
            a_lo, a_hi, z_lo, z_hi = cuts_a[i], cuts_a[i + 1], cuts_z[j], cuts_z[j + 1]
            if a_hi - a_lo < 1e-5 or z_hi - z_lo < 1e-5:
                continue
            if inside((a_lo + a_hi) / 2, (z_lo + z_hi) / 2):
                continue
            quad = [P(a_lo, z_lo, out_off), P(a_hi, z_lo, out_off), P(a_hi, z_hi, out_off), P(a_lo, z_hi, out_off)]
            f = b.face(quad, mat_out)
            orient(f, axis, outward, b)
            if inner:
                quad_in = [P(a_lo, z_lo, in_off), P(a_lo, z_hi, in_off), P(a_hi, z_hi, in_off), P(a_hi, z_lo, in_off)]
                f = b.face(quad_in, mat_in)
                orient(f, axis, -outward, b)
    # Mochetas de cada hueco.
    for (o_a0, o_a1, o_z0, o_z1) in openings:
        o_z0, o_z1 = max(o_z0, z0), min(o_z1, z1)
        if o_z1 - o_z0 < 1e-4:
            continue
        sides = [
            ([P(o_a0, o_z0, out_off), P(o_a0, o_z0, in_off), P(o_a0, o_z1, in_off), P(o_a0, o_z1, out_off)], "+a"),
            ([P(o_a1, o_z0, out_off), P(o_a1, o_z1, out_off), P(o_a1, o_z1, in_off), P(o_a1, o_z0, in_off)], "-a"),
            ([P(o_a0, o_z1, out_off), P(o_a0, o_z1, in_off), P(o_a1, o_z1, in_off), P(o_a1, o_z1, out_off)], "-z"),
        ]
        if o_z0 > z0 + 1e-4:
            sides.append(([P(o_a0, o_z0, out_off), P(o_a1, o_z0, out_off), P(o_a1, o_z0, in_off), P(o_a0, o_z0, in_off)], "+z"))
        for quad, want in sides:
            f = b.face(quad, mat_reveal)
            if f is None:
                continue
            n = f.normal
            comp = (n.x if axis == "x" else n.y) if want.endswith("a") else n.z
            if (comp > 0) != want.startswith("+"):
                f.normal_flip()
                b.apply_uv(f, "auto", Vector((0, 0, 0)))


def orient(f, axis, outward, b):
    if f is None:
        return
    comp = f.normal.y if axis == "x" else f.normal.x
    if comp * outward < 0:
        f.normal_flip()
        b.apply_uv(f, "auto", Vector((0, 0, 0)))


def export_fbx(objects, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="Z", axis_up="Y", mesh_smooth_type="FACE", use_tspace=True, path_mode="STRIP",
                             bake_space_transform=True, add_leaf_bones=False, use_mesh_modifiers=True,
                             object_types={"MESH", "EMPTY"})
    print(f"[biwy] export {path}: {len(objects)} objetos")
