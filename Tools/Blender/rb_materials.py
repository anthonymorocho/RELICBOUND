"""Materiales procedurales de RELICBOUND (paleta de docs/02).

Las coordenadas usan UV: 1 unidad UV = 2 m, para que los patrones encajen al hornear en tile.
Todos los patrones repiten un número entero de veces por tile para que sean tileables.
"""
import bpy


def _new(name):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    material.node_tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return material, nodes, material.node_tree.links, bsdf


def _hex(value):
    value = value.lstrip("#")
    srgb = [int(value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (*lin, 1.0)


def _uv(nodes, links, scale):
    coord = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (scale, scale, scale)
    links.new(coord.outputs["UV"], mapping.inputs["Vector"])
    return mapping.outputs["Vector"]


def _ramp(nodes, stops):
    ramp = nodes.new("ShaderNodeValToRGB")
    elements = ramp.color_ramp.elements
    elements[0].position, elements[0].color = stops[0][0], _hex(stops[0][1])
    elements[1].position, elements[1].color = stops[-1][0], _hex(stops[-1][1])
    for position, color in stops[1:-1]:
        element = elements.new(position)
        element.color = _hex(color)
    return ramp


def _torus(nodes, links, vector, radius=1.0, tube=0.5):
    """Convierte UV 0..1 en un punto de un toroide: el ruido 3D muestreado ahí repite en U y V."""
    sep = nodes.new("ShaderNodeSeparateXYZ")
    links.new(vector, sep.inputs["Vector"])

    def math(op, a, b=None):
        node = nodes.new("ShaderNodeMath")
        node.operation = op
        for index, value in enumerate((a, b)):
            if value is None:
                continue
            if isinstance(value, (int, float)):
                node.inputs[index].default_value = value
            else:
                links.new(value, node.inputs[index])
        return node.outputs["Value"]

    angle_u = math("MULTIPLY", sep.outputs["X"], 6.283185307)
    angle_v = math("MULTIPLY", sep.outputs["Y"], 6.283185307)
    ring = math("ADD", radius, math("MULTIPLY", math("COSINE", angle_v), tube))
    combine = nodes.new("ShaderNodeCombineXYZ")
    links.new(math("MULTIPLY", ring, math("COSINE", angle_u)), combine.inputs["X"])
    links.new(math("MULTIPLY", ring, math("SINE", angle_u)), combine.inputs["Y"])
    links.new(math("MULTIPLY", math("SINE", angle_v), tube), combine.inputs["Z"])
    return combine.outputs["Vector"]


def _noise(nodes, links, vector, scale, detail=6.0, roughness=0.6):
    """Ruido tileable en el tile UV 0..1 (escala aproximada en repeticiones por tile)."""
    vector = _torus(nodes, links, vector)
    scale = scale / 6.283185307
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = detail
    noise.inputs["Roughness"].default_value = roughness
    links.new(vector, noise.inputs["Vector"])
    return noise


def stone_wall():
    """Sillería: bloques de 1 x 0,5 m (legibles desde la cámara de juego), tono por bloque y juntas hundidas."""
    material, nodes, links, bsdf = _new("RB_StoneWall")
    vector = _uv(nodes, links, 1.0)
    brick = nodes.new("ShaderNodeTexBrick")
    brick.offset = 0.5
    brick.inputs["Scale"].default_value = 2.0       # 2 bloques de ancho por tile de 2 m
    brick.inputs["Brick Width"].default_value = 1.0
    brick.inputs["Row Height"].default_value = 0.5  # 4 hiladas por tile
    brick.inputs["Mortar Size"].default_value = 0.012
    brick.inputs["Mortar Smooth"].default_value = 0.6
    brick.inputs["Bias"].default_value = 0.0
    brick.squash = 0.5          # una hilada de cada dos con bloques de la mitad de ancho
    brick.squash_frequency = 2
    brick.inputs["Color1"].default_value = (1, 1, 1, 1)
    brick.inputs["Color2"].default_value = (0.7, 0.7, 0.7, 1)
    brick.inputs["Mortar"].default_value = (0, 0, 0, 1)
    links.new(vector, brick.inputs["Vector"])

    grime = _noise(nodes, links, vector, 3.0)
    tone = _ramp(nodes, [(0.0, "#857E71"), (0.5, "#ABA494"), (0.8, "#BDB6A6"), (1.0, "#D2CCBC")])
    # Tono por bloque (Brick Color) + mancha grande tileable para romper la repetición.
    blotch = _noise(nodes, links, vector, 2.0, 3.0, 0.5)
    tone_input = nodes.new("ShaderNodeMath")
    tone_input.operation = "MULTIPLY_ADD"
    tone_input.inputs[1].default_value = 0.6
    links.new(blotch.outputs["Fac"], tone_input.inputs[0])
    links.new(brick.outputs["Color"], tone_input.inputs[2])
    tone_shift = nodes.new("ShaderNodeMath")
    tone_shift.operation = "SUBTRACT"
    tone_shift.inputs[1].default_value = 0.3
    links.new(tone_input.outputs["Value"], tone_shift.inputs[0])
    links.new(tone_shift.outputs["Value"], tone.inputs["Fac"])

    dirt = nodes.new("ShaderNodeMix")
    dirt.data_type = "RGBA"
    dirt.blend_type = "MULTIPLY"
    dirt.inputs["Factor"].default_value = 0.35
    dirt_ramp = _ramp(nodes, [(0.35, "#7A7468"), (0.7, "#FFFFFF")])
    links.new(grime.outputs["Fac"], dirt_ramp.inputs["Fac"])
    links.new(tone.outputs["Color"], dirt.inputs["A"])
    links.new(dirt_ramp.outputs["Color"], dirt.inputs["B"])

    mortar_color = nodes.new("ShaderNodeMix")
    mortar_color.data_type = "RGBA"
    links.new(brick.outputs["Fac"], mortar_color.inputs["Factor"])
    links.new(dirt.outputs["Result"], mortar_color.inputs["A"])
    mortar_color.inputs["B"].default_value = _hex("#6F6A62")
    links.new(mortar_color.outputs["Result"], bsdf.inputs["Base Color"])

    height = nodes.new("ShaderNodeMath")
    height.operation = "SUBTRACT"
    height.inputs[0].default_value = 1.0
    links.new(brick.outputs["Fac"], height.inputs[1])
    chip = _noise(nodes, links, vector, 16.0, 8.0, 0.7)
    combined = nodes.new("ShaderNodeMath")
    combined.operation = "MULTIPLY_ADD"
    combined.inputs[1].default_value = 0.25
    links.new(chip.outputs["Fac"], combined.inputs[0])
    links.new(height.outputs["Value"], combined.inputs[2])
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.8
    bump.inputs["Distance"].default_value = 0.02
    links.new(combined.outputs["Value"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.85
    return material


def wood_planks():
    """Tablones de 0,25 m con veta, separación y tono variable."""
    material, nodes, links, bsdf = _new("RB_Wood")
    vector = _uv(nodes, links, 1.0)
    planks = nodes.new("ShaderNodeTexBrick")
    planks.offset = 0.37
    planks.inputs["Scale"].default_value = 1.0
    planks.inputs["Brick Width"].default_value = 1.0
    planks.inputs["Row Height"].default_value = 0.125  # 8 tablones por tile
    planks.inputs["Mortar Size"].default_value = 0.006
    planks.inputs["Color1"].default_value = (1, 1, 1, 1)
    planks.inputs["Color2"].default_value = (0.6, 0.6, 0.6, 1)
    links.new(vector, planks.inputs["Vector"])

    stretch = nodes.new("ShaderNodeMapping")
    stretch.inputs["Scale"].default_value = (1.0, 16.0, 1.0)
    links.new(vector, stretch.inputs["Vector"])
    grain = nodes.new("ShaderNodeTexWave")
    grain.wave_type = "BANDS"
    grain.bands_direction = "Y"
    grain.inputs["Scale"].default_value = 2.0
    grain.inputs["Distortion"].default_value = 6.0
    grain.inputs["Detail"].default_value = 4.0
    links.new(stretch.outputs["Vector"], grain.inputs["Vector"])

    mix = nodes.new("ShaderNodeMath")
    mix.operation = "MULTIPLY"
    links.new(planks.outputs["Color"], mix.inputs[0])
    links.new(grain.outputs["Fac"], mix.inputs[1])
    tone = _ramp(nodes, [(0.0, "#4A3220"), (0.4, "#6B4A2C"), (1.0, "#8E6A43")])
    links.new(mix.outputs["Value"], tone.inputs["Fac"])
    gap = nodes.new("ShaderNodeMix")
    gap.data_type = "RGBA"
    links.new(planks.outputs["Fac"], gap.inputs["Factor"])
    links.new(tone.outputs["Color"], gap.inputs["A"])
    gap.inputs["B"].default_value = _hex("#24170E")
    links.new(gap.outputs["Result"], bsdf.inputs["Base Color"])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.6
    bump.inputs["Distance"].default_value = 0.01
    inverted = nodes.new("ShaderNodeMath")
    inverted.operation = "SUBTRACT"
    inverted.inputs[0].default_value = 1.0
    links.new(planks.outputs["Fac"], inverted.inputs[1])
    links.new(inverted.outputs["Value"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.75
    return material


def roof_tiles(color_dark="#2C4566", color_mid="#3E5F8A", color_light="#5B7FAD", name="RB_RoofBlue"):
    """Tejas en escama (pizarra) con borde inferior oscuro; color de equipo en docs/02."""
    material, nodes, links, bsdf = _new(name)
    vector = _uv(nodes, links, 1.0)
    tiles = nodes.new("ShaderNodeTexBrick")
    tiles.offset = 0.5
    tiles.inputs["Scale"].default_value = 8.0       # tejas de 0,25 m
    tiles.inputs["Brick Width"].default_value = 1.0
    tiles.inputs["Row Height"].default_value = 0.5   # 16 hiladas exactas por tile
    tiles.inputs["Mortar Size"].default_value = 0.025
    tiles.inputs["Color1"].default_value = (1, 1, 1, 1)
    tiles.inputs["Color2"].default_value = (0.7, 0.7, 0.7, 1)
    links.new(vector, tiles.inputs["Vector"])

    # Degradado vertical dentro de cada hilada: la parte baja de cada teja queda en sombra.
    sep = nodes.new("ShaderNodeSeparateXYZ")
    links.new(vector, sep.inputs["Vector"])
    rows = nodes.new("ShaderNodeMath")
    rows.operation = "MULTIPLY"
    rows.inputs[1].default_value = 16.0
    links.new(sep.outputs["Y"], rows.inputs[0])
    frac = nodes.new("ShaderNodeMath")
    frac.operation = "FRACT"
    links.new(rows.outputs["Value"], frac.inputs[0])

    # 1 en el borde inferior (expuesto a la luz) y 0 arriba, donde la cubre la teja superior.
    exposed = nodes.new("ShaderNodeMath")
    exposed.operation = "POWER"
    exposed.inputs[1].default_value = 0.6
    inverse = nodes.new("ShaderNodeMath")
    inverse.operation = "SUBTRACT"
    inverse.inputs[0].default_value = 1.0
    links.new(frac.outputs["Value"], inverse.inputs[1])
    links.new(inverse.outputs["Value"], exposed.inputs[0])
    shade = nodes.new("ShaderNodeMath")
    shade.operation = "MULTIPLY"
    links.new(tiles.outputs["Color"], shade.inputs[0])
    links.new(exposed.outputs["Value"], shade.inputs[1])
    tone = _ramp(nodes, [(0.0, color_dark), (0.45, color_mid), (1.0, color_light)])
    links.new(shade.outputs["Value"], tone.inputs["Fac"])
    links.new(tone.outputs["Color"], bsdf.inputs["Base Color"])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.9
    bump.inputs["Distance"].default_value = 0.03
    links.new(inverse.outputs["Value"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.6
    return material


def roof_red():
    return roof_tiles("#5E2725", "#8A3E3A", "#A9574F", "RB_RoofRed")


def iron():
    """Hierro forjado oscuro con manchas: herrajes, rastrillo, abrazaderas."""
    material, nodes, links, bsdf = _new("RB_Iron")
    vector = _uv(nodes, links, 1.0)
    spots = _noise(nodes, links, vector, 10.0, 8.0, 0.65)
    tone = _ramp(nodes, [(0.3, "#3A3A40"), (0.6, "#55545A"), (0.85, "#6E5440")])
    links.new(spots.outputs["Fac"], tone.inputs["Fac"])
    links.new(tone.outputs["Color"], bsdf.inputs["Base Color"])
    # Metallic 0 al hornear: Cycles oscurece el albedo difuso de los metales.
    # La metalicidad (0,7) se aplica en el material de Unity.
    bsdf.inputs["Metallic"].default_value = 0.0
    bsdf.inputs["Roughness"].default_value = 0.55
    return material


FAMILIES = {
    "StoneWall": stone_wall,
    "Wood": wood_planks,
    "RoofBlue": roof_tiles,
    "RoofRed": roof_red,
    "Iron": iron,
}
