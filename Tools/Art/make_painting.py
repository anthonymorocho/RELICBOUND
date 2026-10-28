"""
Cuadro de paisaje para la sala: recorte de un panorama CC0 de Poly Haven (meadow_2) tratado como óleo antiguo
(simplificación de color, pinceladas, barniz amarillento, viñeta). Uso: python3 Tools/Art/make_painting.py <meadow_2.jpg>
"""
import os
import sys

import numpy as np
from PIL import Image, ImageFilter, ImageEnhance

src = Image.open(sys.argv[1]).convert("RGB")
w, h = src.size
crop = src.crop((int(w * 0.30), int(h * 0.30), int(w * 0.30) + int(h * 0.36), int(h * 0.30) + int(h * 0.44)))
img = crop.resize((720, 880), Image.LANCZOS)
img = img.filter(ImageFilter.ModeFilter(7)).filter(ImageFilter.SMOOTH_MORE)
img = ImageEnhance.Color(img).enhance(0.75)
a = np.asarray(img).astype(np.float32) / 255
varnish = np.array([1.0, 0.93, 0.74], np.float32)
a = a * varnish
y, x = np.mgrid[0:a.shape[0], 0:a.shape[1]]
d = np.sqrt(((x / a.shape[1]) - 0.5) ** 2 + ((y / a.shape[0]) - 0.5) ** 2)
a *= (1 - np.clip((d - 0.3) * 1.4, 0, 0.45))[..., None]
rng = np.random.default_rng(5)
a *= (1 + rng.normal(0, 0.025, a.shape[:2]))[..., None]
out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Textures", "Paintings", "painting_meadow.jpg")
Image.fromarray(np.clip(a * 255, 0, 255).astype(np.uint8)).save(out, quality=90)
print(out)
