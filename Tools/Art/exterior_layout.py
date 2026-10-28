"""
Disposición de vegetación y objetos exteriores (fuente única para Unity y para los renders de comprobación).
Coordenadas del mundo de Unity; yaw en grados. Genera Assets/_Game/Data/Layout_Exterior.json.
"""
import json
import os

F = 0.45

ITEMS = [
    # Parterre de la fachada: arbustos variados, nunca en fila perfecta.
    ("shrub_02", "shrub_02_A", 8.65, 0.04, 5.4, 20, 0.62, False),
    ("shrub_02", "shrub_02_D", 10.1, 0.04, 5.45, 160, 0.72, False),
    ("shrub_02", "shrub_02_B", 12.3, 0.04, 5.45, 75, 0.66, False),
    ("shrub_02", "shrub_02_C", 14.05, 0.04, 5.35, 300, 0.52, False),
    ("shrub_02", "shrub_02_D", 5.1, 0.04, 5.35, 10, 0.55, False),
    ("shrub_03", "shrub_03_A", 11.25, 0.04, 5.15, 200, 1.6, False),
    ("shrub_04", "shrub_04_C", 9.35, 0.04, 5.2, 40, 2.2, False),
    ("shrub_03", "shrub_03_C", 13.2, 0.04, 5.2, 120, 1.8, False),
    ("shrub_02", "shrub_02_A", -0.4, 0.0, 8.5, 80, 0.9, False),
    ("shrub_02", "shrub_02_C", -0.5, 0.0, 10.6, 140, 0.8, False),
    ("shrub_02", "shrub_02_B", 17.3, 0.0, 6.2, 30, 0.85, False),
    ("shrub_02", "shrub_02_D", 0.2, 0.0, 24.8, 200, 0.9, False),
    # Árboles: uno grande en el jardín delantero, otro donde el vecino y uno detrás.
    ("jacaranda_tree", None, 15.6, 0.0, 2.2, 35, 0.52, True),
    ("jacaranda_tree", None, -6.0, 0.0, 3.5, 210, 0.46, True),
    ("jacaranda_tree", None, 9.0, 0.0, 30.5, 120, 0.6, False),
    # Porche, entrada de coches, patio y calle.
    ("potted_plant_02", "potted_plant_02_A", 5.45, F, 4.85, 30, 1.0, False),
    ("planter_pot_clay", None, 7.75, F, 4.8, 0, 1.2, False),
    ("metal_trash_can", "metal_trash_can_A", 4.05, 0.02, 10.2, 95, 1.0, True),
    ("metal_trash_can", "metal_trash_can_B", 4.0, 0.02, 11.0, 260, 1.0, True),
    ("exterior_aircon_unit", "exterior_aircon_unit_A", 14.92, 0.03, 13.2, 270, 1.0, True),
    ("garden_hose_wall_mounted_01", None, 4.47, 0.55, 9.2, 270, 1.0, False),
    ("utility_box_01", None, -2.2, 0.0, -0.9, 180, 1.0, True),
    ("fire_hydrant", "fire_hydrant_A", 19.6, 0.0, -1.55, 200, 1.0, True),
    ("water_manhole_cover", None, 8.6, -0.148, -5.4, 0, 1.0, False),
    ("outdoor_table_chair_set_01", None, 12.4, 0.03, 19.4, 15, 1.0, True),
    ("security_light", None, 14.32, 2.2, 21.5, 180, 0.8, False),
]

if __name__ == "__main__":
    out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Data", "Layout_Exterior.json")
    data = {"items": [{"model": m, "variant": v or m, "position": [x, y, z], "yaw": yaw, "scale": sc, "collide": c}
                      for (m, v, x, y, z, yaw, sc, c) in ITEMS]}
    with open(out, "w") as fh:
        json.dump(data, fh, indent=1)
    print(len(ITEMS), "objetos ->", os.path.relpath(out))
