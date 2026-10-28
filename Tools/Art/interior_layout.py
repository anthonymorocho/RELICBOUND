"""
Disposición de muebles y objetos del interior (fuente única para Unity y para los renders de comprobación).
Coordenadas del mundo de Unity (x este, y arriba, z norte); yaw en grados (0 = el frente mira al norte).
Genera Assets/_Game/Data/Layout_Interior.json.
"""
import json
import os

F = 0.45   # suelo de la casa
L = 0.27   # suelo del lavadero

# (modelo, variante o None, x, y, z, yaw, escala, colisión)
ITEMS = [
    # Sala: sofá contra el dormitorio 2, tele de tubo frente a él, butaca, mecedora junto a la ventana, librería.
    ("sofa_03", None, 6.85, F, 9.98, 180, 0.85, True),
    ("throw_pillows_01", None, 6.85, F + 0.42, 10.02, 180, 0.8, False),
    ("CoffeeTable_01", None, 6.85, F, 8.65, 0, 0.72, True),
    ("tea_set_01", "tea_set_01_A", 6.7, F + 0.375, 8.62, 20, 0.9, False),
    ("ceramic_vase_01", None, 7.15, F + 0.375, 8.7, 0, 0.8, False),
    ("side_table_01", None, 5.25, F, 6.6, 0, 1.0, True),
    ("television_02", None, 5.25, F + 0.55, 6.62, 0, 1.15, False),
    ("standing_picture_frame_01", None, 5.02, F + 0.55, 6.5, 20, 1.0, False),
    ("ArmChair_01", None, 8.95, F, 8.0, 250, 0.95, True),
    ("Rockingchair_01", None, 5.15, F, 9.05, 110, 1.0, True),
    ("wicker_basket_02", None, 5.6, F, 9.85, 30, 1.0, False),
    ("wooden_bookshelf_worn", None, 8.9, F, 10.12, 180, 0.9, True),
    ("mantel_clock_01", None, 8.9, F + 1.86, 10.05, 180, 1.0, False),
    ("cassette_player", None, 9.25, F + 1.86, 10.05, 200, 1.0, False),
    ("fancy_picture_frame_02", None, 6.85, F + 1.5, 10.40, 0, 0.75, False),
    ("book_encyclopedia_set_01", None, 8.9, F + 1.12, 10.15, 180, 1.0, False),
    ("book_encyclopedia_set_01", None, 8.75, F + 0.72, 10.15, 180, 1.0, False),
    ("ceramic_vase_01", None, 9.3, F + 0.32, 10.12, 0, 0.6, False),
    ("fancy_picture_frame_01", None, 4.71, F + 1.6, 9.6, 90, 0.7, False),
    ("ornate_mirror_01", None, 7.75, F + 1.35, 6.21, 0, 1.0, False),
    # Comedor.
    ("dining_table", None, 12.1, F, 8.25, 0, 0.8, True),
    ("painted_wooden_chair_01", None, 11.5, F, 7.55, 0, 1.0, True),
    ("painted_wooden_chair_01", None, 12.7, F, 7.6, 5, 1.0, True),
    ("painted_wooden_chair_01", None, 11.5, F, 8.95, 180, 1.0, True),
    ("painted_wooden_chair_01", None, 12.75, F, 9.1, 168, 1.0, True),
    ("wooden_bowl_01", None, 12.0, F + 0.705, 8.3, 0, 1.0, False),
    ("food_apple_01", None, 11.96, F + 0.73, 8.27, 0, 1.0, False),
    ("food_apple_01", None, 12.06, F + 0.73, 8.33, 120, 1.0, False),
    ("lemon", None, 12.0, F + 0.73, 8.38, 40, 1.0, False),
    ("jug_01", None, 12.45, F + 0.705, 8.15, 30, 1.0, False),
    ("drawer_cabinet", None, 14.04, F, 8.3, 270, 0.95, True),
    ("wall_clock", None, 12.1, F + 1.95, 6.21, 0, 1.0, False),
    ("hanging_picture_frame_03", None, 14.29, F + 1.6, 7.0, 270, 1.0, False),
    # Cocina.
    ("electric_stove", None, 13.97, F, 11.51, 270, 1.02, True),
    ("vintage_microwave", None, 14.02, F + 0.9, 14.35, 270, 0.48, False),
    ("vintage_electric_kettle", None, 14.02, F + 0.9, 11.0, 250, 0.75, False),
    ("painted_wooden_cabinet", None, 10.08, F, 13.4, 90, 1.0, True),
    ("vintage_telephone_wall_clock", None, 9.77, F + 1.6, 14.3, 90, 1.0, False),
    ("wicker_basket_01", None, 10.1, F + 1.18, 13.6, 70, 1.0, False),
    # Lavadero.
    ("steel_frame_shelves_01", None, 12.22, L, 16.65, 90, 0.1, True),
    ("multi_cleaner_bottle", None, 12.18, L + 0.98, 16.4, 30, 1.0, False),
    ("bleach_bottle", None, 12.2, L + 0.98, 16.85, 200, 1.0, False),
    ("cardboard_box_01", None, 12.55, L, 17.55, 23, 1.0, True),
    ("wicker_basket_01", None, 14.03, L + 0.85, 15.42, 10, 1.0, False),
    ("wooden_broom", None, 14.38, L, 17.7, 270, 1.0, False),
    ("rubber_boots", "rubber_boots_A", 13.95, L, 17.75, 200, 1.0, False),
    # Dormitorio de Elena: cama de hierro, mesita (cargador), cómoda (carpeta azul), fotos.
    ("old_bed_frame", None, 5.75, F, 16.99, 90, 1.0, True),
    ("ClassicNightstand_01", None, 4.98, F, 16.05, 90, 0.9, True),
    ("painted_wooden_cabinet", None, 6.3, F, 14.68, 0, 0.95, True),
    ("standing_picture_frame_02", None, 6.75, F + 1.12, 14.6, 340, 1.0, False),
    ("potted_plant_04", None, 5.85, F + 1.12, 14.6, 0, 1.0, False),
    ("hanging_picture_frame_03", None, 4.71, F + 1.55, 17.0, 90, 1.0, False),
    # Dormitorio 2 (costura y trastos).
    ("drawer_cabinet", None, 4.82, F, 12.6, 90, 0.95, True),
    ("cardboard_box_01", None, 7.6, F, 13.75, 8, 1.1, True),
    ("cardboard_box_01", None, 7.62, F + 0.34, 13.72, 352, 1.0, False),
    ("book_encyclopedia_set_01", None, 4.85, F + 1.79, 12.6, 90, 1.0, False),
    ("ArmChair_01", None, 7.4, F, 11.2, 220, 0.9, True),
    # Pasillo.
    ("hanging_picture_frame_02", None, 8.57, F + 1.5, 13.6, 90, 0.8, False),
    ("fancy_picture_frame_01", None, 9.63, F + 1.5, 13.4, 270, 0.6, False),
]

if __name__ == "__main__":
    out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Data", "Layout_Interior.json")
    data = {"items": [{"model": m, "variant": v or m, "position": [x, y, z], "yaw": yaw, "scale": sc, "collide": c}
                      for (m, v, x, y, z, yaw, sc, c) in ITEMS]}
    with open(out, "w") as fh:
        json.dump(data, fh, indent=1)
    print(len(ITEMS), "objetos ->", os.path.relpath(out))
