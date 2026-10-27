"""Texturas de interfaz de BEFORE IT WAS YOU (degradados y viñetas). Uso: python3 Tools/UI/make_ui_textures.py"""
import os
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "UI", "Textures")


def save(name, rgba):
    rgba = np.clip(rgba, 0, 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(os.path.join(OUT, name))
    print(name, rgba.shape)


def smooth(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def highlight():
    # Barra roja del elemento seleccionado: intensa a la izquierda y desvanecida hacia la derecha (moodboard).
    w, h = 1024, 96
    x = np.linspace(0, 1, w)[None, :].repeat(h, 0)
    y = np.linspace(-1, 1, h)[:, None].repeat(w, 1)
    alpha = (1 - smooth(x / 0.95)) ** 1.35 * 0.92
    alpha *= 1 - 0.12 * y ** 2
    r = 120 + 30 * (1 - x)
    g = 18 + 6 * (1 - x)
    b = 20 + 4 * (1 - x)
    save("ui_menu_highlight.png", np.dstack([r, g, b, alpha * 255]))


def accent_line():
    # Subrayado rojo bajo el título: más brillante al inicio, se apaga hacia el final.
    w, h = 1024, 8
    x = np.linspace(0, 1, w)[None, :].repeat(h, 0)
    alpha = (1 - smooth((x - 0.55) / 0.45)) * 255
    save("ui_accent_line.png", np.dstack([np.full_like(x, 178), np.full_like(x, 30), np.full_like(x, 30), alpha]))


def scrim_left():
    # Oscurece el lado del menú para que el texto se lea sobre cualquier fotograma de la escena.
    w, h = 1024, 256
    x = np.linspace(0, 1, w)[None, :].repeat(h, 0)
    alpha = (1 - smooth(x / 0.62)) ** 1.6 * 0.86 * 255
    save("ui_scrim_left.png", np.dstack([np.full_like(x, 6), np.full_like(x, 8), np.full_like(x, 11), alpha]))


def scrim_bottom():
    w, h = 256, 512
    y = np.linspace(0, 1, h)[:, None].repeat(w, 1)
    alpha = smooth((y - 0.35) / 0.65) ** 1.4 * 0.8 * 255
    save("ui_scrim_bottom.png", np.dstack([np.full_like(y, 5), np.full_like(y, 6), np.full_like(y, 8), alpha]))


def vignette():
    w, h = 1024, 576
    x = np.linspace(-1, 1, w)[None, :].repeat(h, 0)
    y = np.linspace(-1, 1, h)[:, None].repeat(w, 1)
    d = np.sqrt((x * 0.92) ** 2 + (y * 1.05) ** 2)
    alpha = smooth((d - 0.55) / 0.85) * 0.72 * 255
    save("ui_vignette.png", np.dstack([np.zeros_like(x), np.zeros_like(x), np.zeros_like(x), alpha]))


def panel():
    # Panel translúcido para pausa/opciones: casi negro con un leve degradado vertical.
    w, h = 64, 512
    y = np.linspace(0, 1, h)[:, None].repeat(w, 1)
    c = 12 + 6 * (1 - y)
    save("ui_panel.png", np.dstack([c, c + 1, c + 4, np.full_like(y, 0.9 * 255)]))


def grain():
    # Grano fino de película para el fondo del prólogo y pantallas negras (sutil, sin efecto VHS).
    rng = np.random.default_rng(7)
    n = rng.normal(0, 1, (256, 256))
    a = np.clip(np.abs(n) * 18, 0, 40)
    v = np.where(n > 0, 255, 0)
    save("ui_grain.png", np.dstack([v, v, v, a]))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    highlight()
    accent_line()
    scrim_left()
    scrim_bottom()
    vignette()
    panel()
    grain()
