"""Formas orgánicas compartidas (follaje, rocas, cargas de heno)."""
import bmesh
from mathutils import Vector, noise

import rb_geo as G


def blob(name, radius, location, seed, material, roughness=0.35, subdivisions=3, squash=0.85, scale=(1.0, 1.0, 1.0)):
    """Esfera deformada por ruido: masa de follaje o roca."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=radius)
    offset = Vector((seed * 13.1, seed * 7.7, seed * 3.3))
    for v in bm.verts:
        direction = v.co.normalized()
        n = noise.noise(direction * 1.6 + offset)
        n2 = noise.noise(direction * 4.0 + offset * 2)
        v.co = direction * radius * (1 + roughness * n + roughness * 0.35 * n2)
        v.co.z *= squash
        v.co.x *= scale[0]
        v.co.y *= scale[1]
        v.co.z *= scale[2]
    obj = G.mesh_object(name, bm, material)
    obj.location = location
    return obj


