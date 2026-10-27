"""Utilidades de geometría para el kit de RELICBOUND (1 unidad = 1 m, Z arriba, exterior hacia -Y)."""
import math
import bmesh
import bpy
from mathutils import Vector

TILE_METERS = 2.0  # cada textura cubre 2 x 2 m (rb_textures.py)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _link(obj, collection=None):
    (collection or bpy.context.scene.collection).objects.link(obj)
    return obj


def mesh_object(name, bm, material=None, collection=None):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    if material:
        mesh.materials.append(material)
    return _link(obj, collection)


def box(name, size, location, material=None, bevel=0.04, collection=None, taper=0.0):
    """Caja con base en location.z. taper estrecha la parte superior (talud) en metros por lado."""
    sx, sy, sz = size
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        top = v.co.z > 0
        shrink = taper if top else 0.0
        v.co.x = v.co.x * sx - math.copysign(shrink, v.co.x)
        v.co.y = v.co.y * sy - math.copysign(shrink, v.co.y)
        v.co.z = (v.co.z + 0.5) * sz
    obj = mesh_object(name, bm, material, collection)
    obj.location = location
    if bevel > 0:
        add_bevel(obj, bevel)
    return obj


def cylinder(name, radius, height, location, material=None, segments=24, bevel=0.03, collection=None,
             top_radius=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments,
                          radius1=radius, radius2=top_radius if top_radius is not None else radius, depth=height)
    for v in bm.verts:
        v.co.z += height / 2
    obj = mesh_object(name, bm, material, collection)
    obj.location = location
    if bevel > 0:
        add_bevel(obj, bevel)
    return obj


def cone(name, radius, height, location, material=None, segments=24, collection=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=True, segments=segments, radius1=radius, radius2=0.0,
                          depth=height)
    for v in bm.verts:
        v.co.z += height / 2
    obj = mesh_object(name, bm, material, collection)
    obj.location = location
    return obj


def pyramid(name, size_x, size_y, height, location, material=None, collection=None):
    bm = bmesh.new()
    hx, hy = size_x / 2, size_y / 2
    base = [bm.verts.new((x, y, 0)) for x, y in ((-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy))]
    apex = bm.verts.new((0, 0, height))
    bm.faces.new(list(reversed(base)))
    for i in range(4):
        bm.faces.new((base[i], base[(i + 1) % 4], apex))
    obj = mesh_object(name, bm, material, collection)
    obj.location = location
    return obj


def hip_roof(name, size_x, size_y, height, location, material=None, collection=None):
    """Tejado a cuatro aguas con cumbrera a lo largo de X."""
    bm = bmesh.new()
    hx, hy = size_x / 2, size_y / 2
    ridge = max(0.0, hx - hy)
    b = [bm.verts.new((x, y, 0)) for x, y in ((-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy))]
    r0 = bm.verts.new((-ridge, 0, height))
    r1 = bm.verts.new((ridge, 0, height))
    bm.faces.new(list(reversed(b)))
    bm.faces.new((b[0], b[1], r1, r0))
    bm.faces.new((b[1], b[2], r1))
    bm.faces.new((b[2], b[3], r0, r1))
    bm.faces.new((b[3], b[0], r0))
    obj = mesh_object(name, bm, material, collection)
    obj.location = location
    return obj


def add_bevel(obj, width, segments=2):
    modifier = obj.modifiers.new("Bevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = math.radians(35)
    modifier.harden_normals = True
    return modifier


def cut(target, cutter):
    """Resta cutter de target (booleano exacto) y elimina el cortador."""
    modifier = target.modifiers.new("Cut", "BOOLEAN")
    modifier.operation = "DIFFERENCE"
    modifier.solver = "EXACT"
    modifier.object = cutter
    # El booleano debe aplicarse antes que el bisel.
    bpy.context.view_layer.objects.active = target
    while target.modifiers[0] != modifier:
        bpy.ops.object.modifier_move_up(modifier=modifier.name)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def apply_all(obj):
    bpy.context.view_layer.objects.active = obj
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)


def world_box_uv(obj, tile=TILE_METERS):
    """UV por proyección de caja en coordenadas de mundo: densidad de texel uniforme en todo el kit."""
    apply_all(obj)
    matrix = obj.matrix_world
    normal_matrix = matrix.to_3x3().inverted().transposed()
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    uv_layer = bm.loops.layers.uv.verify()
    for face in bm.faces:
        n = (normal_matrix @ face.normal).normalized()
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for loop in face.loops:
            p = matrix @ loop.vert.co
            if az >= ax and az >= ay:
                u, v = p.x, p.y
            elif ax >= ay:
                u, v = p.y * (1 if n.x > 0 else -1), p.z
            else:
                u, v = p.x * (-1 if n.y > 0 else 1), p.z
            loop[uv_layer].uv = (u / tile, v / tile)
    bm.to_mesh(obj.data)
    bm.free()


def radial_uv(obj, tile=TILE_METERS, center=(0.0, 0.0)):
    """UV cilíndrica en metros para torres y tejados cónicos: U = arco, V = altura/inclinación."""
    apply_all(obj)
    matrix = obj.matrix_world
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    uv_layer = bm.loops.layers.uv.verify()
    for face in bm.faces:
        centroid = matrix @ face.calc_center_median()
        face_angle = math.atan2(centroid.y - center[1], centroid.x - center[0])
        for loop in face.loops:
            p = matrix @ loop.vert.co
            angle = math.atan2(p.y - center[1], p.x - center[0])
            # Evita la costura: mantiene el ángulo del vértice cerca del de su cara.
            while angle - face_angle > math.pi:
                angle -= 2 * math.pi
            while face_angle - angle > math.pi:
                angle += 2 * math.pi
            radius = math.hypot(p.x - center[0], p.y - center[1])
            loop[uv_layer].uv = (angle * max(radius, 0.5) / tile, p.z / tile)
    bm.to_mesh(obj.data)
    bm.free()


def cone_uv(obj, apex_height, base_radius, base_z, tile=TILE_METERS, center=(0.0, 0.0)):
    """UV para tejados cónicos: V = distancia a lo largo de la pendiente desde el alero."""
    apply_all(obj)
    matrix = obj.matrix_world
    slant = math.hypot(base_radius, apex_height)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    uv_layer = bm.loops.layers.uv.verify()
    for face in bm.faces:
        centroid = matrix @ face.calc_center_median()
        face_angle = math.atan2(centroid.y - center[1], centroid.x - center[0])
        for loop in face.loops:
            p = matrix @ loop.vert.co
            angle = math.atan2(p.y - center[1], p.x - center[0])
            while angle - face_angle > math.pi:
                angle -= 2 * math.pi
            while face_angle - angle > math.pi:
                angle += 2 * math.pi
            t = (p.z - base_z) / apex_height
            along = t * slant
            radius_at = base_radius * (1 - t)
            loop[uv_layer].uv = (angle * max(radius_at, 0.15) / tile, along / tile)
    bm.to_mesh(obj.data)
    bm.free()


def join(objects, name):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    result = bpy.context.view_layer.objects.active
    result.name = name
    result.data.name = name
    return result


def set_origin_to_ground(obj):
    """Pivote en (0,0,0) del mundo: las piezas se diseñan centradas, con la base en Z=0."""
    bpy.context.scene.cursor.location = Vector((0, 0, 0))
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
