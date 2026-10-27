"""Texturas 9-slice de la interfaz de RELICBOUND (paleta de docs/04).

Uso: python3 Tools/UI/make_ui_textures.py [carpeta_salida]
Se dibujan a 4x y se reducen con Lanczos para bordes suaves. Cada textura documenta su slice en SLICES.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1] if len(sys.argv) > 1 else "Assets/Relicbound/UI/Textures"
S = 4  # supermuestreo

NIGHT = (20, 33, 61)
NIGHT_LIGHT = (34, 52, 88)
STONE = (43, 42, 46)
GOLD = (201, 165, 76)
GOLD_LIGHT = (233, 204, 120)
GOLD_DARK = (128, 98, 40)
GREEN = (78, 154, 81)
GREEN_LIGHT = (112, 186, 98)
GREEN_DARK = (46, 98, 52)
RED = (181, 64, 58)

SLICES = {}


def vertical_gradient(size, top, bottom, alpha=255):
    w, h = size
    image = Image.new("RGBA", size)
    draw = ImageDraw.Draw(image)
    for y in range(h):
        t = y / max(1, h - 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        draw.line([(0, y), (w, y)], fill=(*color, alpha))
    return image


def rounded_mask(size, radius, inset=0):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([inset, inset, size[0] - 1 - inset, size[1] - 1 - inset], radius=radius, fill=255)
    return mask


def save(image, name, size, slice_px):
    image = image.resize(size, Image.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    image.save(os.path.join(OUT, f"{name}.png"))
    SLICES[name] = slice_px
    print(f"[ui] {name} {size} slice={slice_px}")


def framed(name, size, radius, fill_top, fill_bottom, fill_alpha, border, border_width, inner_line=None,
           ornaments=False, slice_px=None, highlight=True):
    w, h = size[0] * S, size[1] * S
    r = radius * S
    image = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    # Sombra exterior suave.
    shadow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle([2 * S, 4 * S, w - 2 * S, h - 1 * S], radius=r, fill=(0, 0, 0, 110))
    image.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(2 * S)))
    # Borde.
    edge = vertical_gradient((w, h), border[0], border[1])
    image.paste(edge, (0, 0), rounded_mask((w, h), r, 1 * S))
    # Relleno.
    bw = border_width * S
    fill = vertical_gradient((w, h), fill_top, fill_bottom, fill_alpha)
    fill_mask = rounded_mask((w, h), max(1, r - bw), 1 * S + bw)
    image.paste(fill, (0, 0), fill_mask)
    draw = ImageDraw.Draw(image)
    if highlight:
        # Brillo superior sutil.
        # Brillo superior que se desvanece hacia abajo (sin corte duro), recortado a la forma del relleno.
        gloss = Image.new("RGBA", (w, h), (255, 255, 255, 0))
        gloss_draw = ImageDraw.Draw(gloss)
        top = int(h * 0.55)
        for y in range(top):
            gloss_draw.line([(0, y), (w, y)], fill=(255, 255, 255, int(30 * (1 - y / top) ** 1.6)))
        clipped = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        clipped.paste(gloss, (0, 0), fill_mask)
        image.alpha_composite(clipped)
    if inner_line:
        inset = (1 + border_width + 3) * S
        draw.rounded_rectangle([inset, inset, w - 1 - inset, h - 1 - inset], radius=max(1, r - inset),
                               outline=inner_line, width=1 * S)
    if ornaments:
        d = 4 * S
        for cx, cy in ((r * 0.7, r * 0.7), (w - r * 0.7, r * 0.7), (r * 0.7, h - r * 0.7), (w - r * 0.7, h - r * 0.7)):
            draw.polygon([(cx, cy - d), (cx + d, cy), (cx, cy + d), (cx - d, cy)], fill=(*GOLD_LIGHT, 255))
    save(image, name, size, slice_px or radius + border_width + 4)


def main():
    # Panel principal: azul noche con doble filete dorado y rombos en las esquinas.
    framed("ui_panel", (96, 96), 14, NIGHT_LIGHT, NIGHT, 240, (GOLD_LIGHT, GOLD_DARK), 2,
           inner_line=(*GOLD, 70), ornaments=True, slice_px=28)
    # Panel secundario (tarjetas dentro de paneles).
    framed("ui_card", (64, 64), 10, (38, 40, 48), STONE, 235, ((90, 84, 70), (60, 56, 48)), 1, slice_px=16)
    # Botón principal (verde, acción positiva) y su estado pulsado.
    framed("ui_button_primary", (96, 64), 12, GREEN_LIGHT, GREEN_DARK, 255, (GOLD_LIGHT, GOLD_DARK), 2, slice_px=20)
    framed("ui_button_primary_pressed", (96, 64), 12, GREEN_DARK, GREEN, 255, (GOLD_DARK, GOLD), 2, slice_px=20,
           highlight=False)
    framed("ui_button_disabled", (96, 64), 12, (88, 92, 98), (60, 62, 68), 255, ((120, 116, 104), (80, 78, 70)), 2,
           slice_px=20)
    # Botón secundario (azul noche con borde dorado).
    framed("ui_button_secondary", (96, 64), 12, NIGHT_LIGHT, NIGHT, 255, (GOLD_LIGHT, GOLD_DARK), 2, slice_px=20)
    # Botón redondo de icono (ajustes, perfil).
    framed("ui_button_round", (72, 72), 36, NIGHT_LIGHT, NIGHT, 245, (GOLD_LIGHT, GOLD_DARK), 2, slice_px=0)
    # Píldora de recurso.
    framed("ui_pill", (96, 48), 24, (26, 30, 42), (14, 17, 26), 225, ((150, 122, 62), (90, 70, 34)), 2, slice_px=24,
           highlight=False)
    # Campo de texto.
    framed("ui_field", (64, 56), 8, (12, 16, 26), (20, 26, 40), 255, ((150, 122, 62), (90, 70, 34)), 2, slice_px=14,
           highlight=False)
    framed("ui_field_focus", (64, 56), 8, (12, 16, 26), (20, 26, 40), 255, (GOLD_LIGHT, GOLD), 2, slice_px=14,
           highlight=False)
    framed("ui_field_error", (64, 56), 8, (26, 14, 16), (34, 18, 20), 255, ((214, 96, 86), RED), 2, slice_px=14,
           highlight=False)
    # Barra de experiencia: fondo y relleno.
    framed("ui_bar_bg", (64, 20), 10, (8, 10, 16), (18, 22, 32), 255, ((110, 90, 48), (70, 56, 30)), 1, slice_px=10,
           highlight=False)
    framed("ui_bar_fill", (64, 20), 10, (240, 214, 130), (176, 132, 52), 255, (GOLD_LIGHT, GOLD_DARK), 1, slice_px=10)
    # Divisor ornamental (línea dorada que se desvanece con rombo central).
    w, h = 512 * S, 24 * S
    divider = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(divider)
    for x in range(w):
        t = 1 - abs(x - w / 2) / (w / 2)
        draw.line([(x, h / 2 - S), (x, h / 2 + S)], fill=(*GOLD, int(255 * t ** 0.7)))
    d = 7 * S
    draw.polygon([(w / 2, h / 2 - d), (w / 2 + d, h / 2), (w / 2, h / 2 + d), (w / 2 - d, h / 2)], fill=(*GOLD_LIGHT, 255))
    draw.polygon([(w / 2, h / 2 - d / 2), (w / 2 + d / 2, h / 2), (w / 2, h / 2 + d / 2), (w / 2 - d / 2, h / 2)],
                 fill=(*NIGHT, 255))
    save(divider, "ui_divider", (512, 24), 0)
    # Sombra inferior: legibilidad del texto de la introducción sobre la escena.
    shade = Image.new("RGBA", (8, 256), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade)
    for y in range(256):
        sd.line([(0, y), (8, y)], fill=(5, 8, 15, int(215 * (y / 255) ** 1.8)))
    save(shade, "ui_shade_bottom", (8, 256), 0)
    # Viñeta para pantallas de fondo (perfil, intro): oscurece bordes.
    w, h = 256, 256
    vignette = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vignette)
    for i in range(128):
        alpha = int(200 * (1 - i / 128) ** 2.2)
        vd.ellipse([i - 60, i - 60, w - i + 60, h - i + 60], outline=(10, 14, 26, alpha), width=2)
    save(vignette.filter(ImageFilter.GaussianBlur(6)), "ui_vignette", (256, 256), 0)


main()
