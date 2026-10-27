"""
Descarga assets CC0 de Poly Haven y los deja listos para Unity URP.

Texturas: BaseColor (jpg), Normal OpenGL (jpg) y Mask (png: R=metálico, G=oclusión, A=suavidad) a partir del mapa ARM.
Modelos: glTF descargado a caché; Tools/Blender/convert_gltf.py los convierte a FBX y escribe un manifest.json.
Cada asset descargado se añade a Docs/Proyecto/REGISTRO_ASSETS.csv con autor y licencia (CC0 1.0).

Uso: python3 Tools/Art/fetch_polyhaven.py [ids...]
"""
import csv
import datetime
import io
import json
import os
import sys
import urllib.request

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CACHE = os.environ.get("BIWY_CACHE", "/tmp/claude-0/ph_cache")
TEX_OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Textures")
MODEL_CACHE = os.path.join(CACHE, "models")
REGISTER = os.path.join(ROOT, "Docs", "Proyecto", "REGISTRO_ASSETS.csv")
API = "https://api.polyhaven.com"

# Texturas de superficie (id, resolución). 2k para superficies grandes vistas de cerca.
TEXTURES = [
    ("exterior_wall_cladding_03", "2k"),
    ("grey_roof_01", "2k"),
    ("asphalt_02", "2k"),
    ("concrete_pavement_02", "2k"),
    ("brushed_concrete", "2k"),
    ("leafy_grass", "2k"),
    ("sparse_grass", "2k"),
    ("painted_plaster_wall", "2k"),
    ("white_plaster_02", "1k"),
    ("dark_wood", "1k"),
    ("weathered_plank_siding", "2k"),
    ("concrete_floor_02", "2k"),
    ("brown_mud_leaves_01", "2k"),
    ("rusty_metal_02", "1k"),
    ("brick_wall_02", "1k"),
    ("white_planks_clean", "1k"),
    # Interior.
    ("wood_floor", "2k"),
    ("floor_tiles_06", "2k"),
    ("floor_tiles_08", "1k"),
    ("grey_tiles", "1k"),
    ("beige_wall_001", "2k"),
    ("long_white_tiles", "1k"),
    ("kitchen_wood", "1k"),
    ("floral_jacquard", "1k"),
]

# HDRI del cielo nocturno (id, resolución).
HDRIS = [("moonlit_golf", "2k")]

# Modelos (id, resolución de texturas).
MODELS = [
    ("jacaranda_tree", "1k"),
    ("shrub_02", "1k"),
    ("shrub_03", "1k"),
    ("shrub_04", "1k"),
    ("potted_plant_02", "1k"),
    ("planter_pot_clay", "1k"),
    ("metal_trash_can", "1k"),
    ("exterior_aircon_unit", "1k"),
    ("garden_hose_wall_mounted_01", "1k"),
    ("utility_box_01", "1k"),
    ("water_manhole_cover", "1k"),
    ("fire_hydrant", "1k"),
    ("security_light", "1k"),
    ("outdoor_table_chair_set_01", "1k"),
    # Interior de la casa de Elena.
    ("sofa_03", "1k"),
    ("ArmChair_01", "1k"),
    ("Rockingchair_01", "1k"),
    ("television_02", "1k"),
    ("CoffeeTable_01", "1k"),
    ("side_table_01", "1k"),
    ("dining_table", "1k"),
    ("painted_wooden_chair_01", "1k"),
    ("wall_clock", "1k"),
    ("electric_stove", "1k"),
    ("vintage_microwave", "1k"),
    ("vintage_electric_kettle", "1k"),
    ("steel_frame_shelves_01", "1k"),
    ("painted_wooden_cabinet", "1k"),
    ("drawer_cabinet", "1k"),
    ("ClassicNightstand_01", "1k"),
    ("old_bed_frame", "1k"),
    ("standing_picture_frame_01", "1k"),
    ("standing_picture_frame_02", "1k"),
    ("hanging_picture_frame_01", "1k"),
    ("hanging_picture_frame_02", "1k"),
    ("throw_pillows_01", "1k"),
    ("ornate_mirror_01", "1k"),
    ("modern_ceiling_lamp_01", "1k"),
    ("lightbulb_01", "1k"),
    ("cardboard_box_01", "1k"),
    ("multi_cleaner_bottle", "1k"),
    ("bleach_bottle", "1k"),
    ("wicker_basket_01", "1k"),
    ("wooden_broom", "1k"),
    ("rubber_boots", "1k"),
    ("jug_01", "1k"),
    ("potted_plant_04", "1k"),
    ("book_encyclopedia_set_01", "1k"),
    # Refugio de Víctor en el cobertizo.
    ("russian_food_cans_01", "1k"),
    ("plastic_bottle_gallon", "1k"),
    ("long_life_food", "1k"),
    ("trashbag", "1k"),
]


def get(url, binary=True):
    request = urllib.request.Request(url, headers={"User-Agent": "BIWY-asset-fetch/1.0"})
    with urllib.request.urlopen(request, timeout=120) as response:
        data = response.read()
    return data if binary else data.decode("utf-8")


def cached(url, path):
    if not os.path.exists(path):
        os.makedirs(os.path.dirname(path), exist_ok=True)
        data = get(url)
        with open(path, "wb") as f:
            f.write(data)
    return path


def info(asset_id):
    return json.loads(get(f"{API}/info/{asset_id}", False))


def files(asset_id):
    return json.loads(get(f"{API}/files/{asset_id}", False))


def pack_mask(arm_path, out_path, size=None):
    """ARM (R=AO, G=rugosidad, B=metálico) -> máscara URP (R=metálico, G=oclusión, B=0, A=suavidad)."""
    arm = Image.open(arm_path).convert("RGB")
    if size:
        arm = arm.resize((size, size), Image.LANCZOS)
    a = np.asarray(arm).astype(np.float32)
    out = np.zeros(a.shape[:2] + (4,), np.uint8)
    out[..., 0] = a[..., 2]
    out[..., 1] = a[..., 0]
    out[..., 3] = 255 - a[..., 1]
    Image.fromarray(out, "RGBA").save(out_path, optimize=True)


def register(asset_id, kind, data, location):
    authors = "; ".join(sorted(data.get("authors", {}).keys()))
    url = f"https://polyhaven.com/a/{asset_id}"
    rows = []
    if os.path.exists(REGISTER):
        with open(REGISTER, newline="", encoding="utf-8") as f:
            rows = list(csv.reader(f))
    asset_key = f"PH-{asset_id}"
    if any(r and r[0] == asset_key for r in rows):
        return
    rows.append([
        asset_key, f"{data.get('name', asset_id)} ({kind})", url, authors, "CC0 1.0 Universal (Poly Haven)",
        datetime.date.today().isoformat(), f"https://api.polyhaven.com/info/{asset_id}", "No requerida (se acredita en créditos)",
        location, "integrado",
    ])
    with open(REGISTER, "w", newline="", encoding="utf-8") as f:
        csv.writer(f).writerows(rows)


def fetch_texture(asset_id, res):
    data = info(asset_id)
    f = files(asset_id)
    out = os.path.join(TEX_OUT, asset_id)
    os.makedirs(out, exist_ok=True)
    diff = cached(f["Diffuse"][res]["jpg"]["url"], os.path.join(CACHE, "tex", asset_id, f"diff_{res}.jpg"))
    nor = cached(f["nor_gl"][res]["jpg"]["url"], os.path.join(CACHE, "tex", asset_id, f"nor_{res}.jpg"))
    arm = cached(f["arm"][res]["jpg"]["url"], os.path.join(CACHE, "tex", asset_id, f"arm_{res}.jpg"))
    Image.open(diff).convert("RGB").save(os.path.join(out, f"{asset_id}_BaseColor.jpg"), quality=92)
    Image.open(nor).convert("RGB").save(os.path.join(out, f"{asset_id}_Normal.jpg"), quality=95)
    pack_mask(arm, os.path.join(out, f"{asset_id}_Mask.png"), 1024)
    meta = {"id": asset_id, "name": data.get("name"), "dimensions_mm": data.get("dimensions"), "authors": list(data.get("authors", {}).keys())}
    with open(os.path.join(out, "texture.json"), "w") as fh:
        json.dump(meta, fh, indent=2)
    register(asset_id, "textura PBR", data, f"Assets/_Game/Art/Textures/{asset_id}")
    print("textura", asset_id, data.get("dimensions"))


def fetch_hdri(asset_id, res):
    data = info(asset_id)
    f = files(asset_id)
    out = os.path.join(ROOT, "Assets", "_Game", "Art", "Sky")
    os.makedirs(out, exist_ok=True)
    src = cached(f["hdri"][res]["hdr"]["url"], os.path.join(CACHE, "hdri", f"{asset_id}_{res}.hdr"))
    import shutil
    shutil.copy(src, os.path.join(out, f"{asset_id}_{res}.hdr"))
    register(asset_id, "HDRI", data, f"Assets/_Game/Art/Sky/{asset_id}_{res}.hdr")
    print("hdri", asset_id)


def fetch_model(asset_id, res):
    data = info(asset_id)
    f = files(asset_id)
    gltf = f["gltf"][res]["gltf"]
    folder = os.path.join(MODEL_CACHE, asset_id)
    cached(gltf["url"], os.path.join(folder, os.path.basename(gltf["url"])))
    for rel, entry in gltf.get("include", {}).items():
        cached(entry["url"], os.path.join(folder, rel))
    with open(os.path.join(folder, "info.json"), "w") as fh:
        json.dump(data, fh, indent=2)
    register(asset_id, "modelo 3D", data, f"Assets/_Game/Art/Models/PolyHaven/{asset_id}")
    print("modelo", asset_id, data.get("polycount"))


if __name__ == "__main__":
    wanted = set(sys.argv[1:])
    for asset_id, res in TEXTURES:
        if not wanted or asset_id in wanted:
            fetch_texture(asset_id, res)
    for asset_id, res in HDRIS:
        if not wanted or asset_id in wanted:
            fetch_hdri(asset_id, res)
    for asset_id, res in MODELS:
        if not wanted or asset_id in wanted:
            fetch_model(asset_id, res)
