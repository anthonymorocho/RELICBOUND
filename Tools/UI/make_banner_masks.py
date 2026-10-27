"""Máscaras de figuras heráldicas para el paño de los estandartes (shader Relicbound/Banner).

Canal R = torre, G = cheurón, B = cruz. Alfa = recorte del paño (cola en punta). 512x1024, UV: v=1 arriba.
Uso: python3 Tools/UI/make_banner_masks.py [salida.png]
"""
import sys

from PIL import Image, ImageDraw

OUT = sys.argv[1] if len(sys.argv) > 1 else "Assets/Relicbound/Art/Textures/T_BannerMasks.png"
W, H, S = 512, 1024, 2
w, h = W * S, H * S


def layer(draw_fn):
    image = Image.new("L", (w, h), 0)
    draw_fn(ImageDraw.Draw(image))
    return image.resize((W, H), Image.LANCZOS)


def tower(d):
    cx, top, bottom, tw = w / 2, h * 0.30, h * 0.66, w * 0.44
    left = cx - tw / 2
    m = tw / 5
    d.rectangle([left, top, left + tw, bottom], fill=255)
    for i in range(0, 5, 2):
        d.rectangle([left + m * i, top - m * 0.9, left + m * (i + 1), top], fill=255)
    d.rectangle([left - tw * 0.08, bottom, left + tw * 1.08, bottom + h * 0.02], fill=255)
    door_w = m * 1.2
    d.rectangle([cx - door_w / 2, bottom - h * 0.11, cx + door_w / 2, bottom], fill=0)
    d.ellipse([cx - door_w / 2, bottom - h * 0.11 - door_w / 2, cx + door_w / 2, bottom - h * 0.11 + door_w / 2], fill=0)


def chevron(d):
    d.polygon([(w * 0.12, h * 0.62), (w / 2, h * 0.34), (w * 0.88, h * 0.62), (w * 0.88, h * 0.72),
               (w / 2, h * 0.46), (w * 0.12, h * 0.72)], fill=255)


def cross(d):
    arm = w * 0.09
    d.rectangle([w / 2 - arm, h * 0.22, w / 2 + arm, h * 0.74], fill=255)
    d.rectangle([w * 0.16, h * 0.38 - arm, w * 0.84, h * 0.38 + arm], fill=255)


def cloth(d):
    # Paño rectangular con cola en punta y borde metálico implícito en el shader.
    d.polygon([(0, 0), (w, 0), (w, h * 0.86), (w / 2, h), (0, h * 0.86)], fill=255)


r, g, b, a = layer(tower), layer(chevron), layer(cross), layer(cloth)
# Las figuras de la máscara están dibujadas con y hacia abajo; Unity muestrea con v hacia arriba.
image = Image.merge("RGBA", (r, g, b, a)).transpose(Image.FLIP_TOP_BOTTOM)
image.save(OUT)
print(f"[ui] {OUT}")
