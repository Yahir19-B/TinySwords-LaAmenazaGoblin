#!/usr/bin/env python3
"""Arma el ícono de la app con el Caballero Azul (necesita Pillow y el pack Tiny Swords en Assets/images).

Genera en Assets/Icono: fondo.png y frente.png (ícono adaptable), icono.png (completo) y redondo.png.
"""
import os
from PIL import Image, ImageDraw, ImageFilter
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
SRC = os.path.join(ROOT, "Assets/images/Tiny Swords (Update 010)/Factions/Knights/Troops/Warrior/Blue/Warrior_Blue.png")
OUT = os.path.join(ROOT, "Assets/Icono")
N = 432


def knight(height):
    frame = Image.open(SRC).convert("RGBA").crop((0, 0, 192, 192))
    frame = frame.crop(frame.getbbox())
    k = max(1, round(height * 4 / frame.height))          # se agranda en múltiplos enteros y luego se reduce: pixeles limpios
    big = frame.resize((frame.width * k, frame.height * k), Image.NEAREST)
    return big.resize((round(big.width * height / big.height), height), Image.LANCZOS)


def background(n=N):
    img = Image.new("RGB", (n, n))
    px = img.load()
    for y in range(n):
        for x in range(n):
            d = min(1, ((x - n / 2) ** 2 + (y - n * .52) ** 2) ** .5 / (n * .62))
            a, b = (238, 178, 74), (38, 20, 34)           # fuego al centro, noche en las orillas
            m = d ** .8
            px[x, y] = tuple(round(a[i] * (1 - m) + b[i] * m) for i in range(3))
    return img.convert("RGBA")


def foreground(height):
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    k = knight(height)
    pos = ((N - k.width) // 2, (N - k.height) // 2)
    glow = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    glow.paste(Image.new("RGBA", k.size, (20, 8, 16, 255)), pos, k.getchannel("A"))
    alpha = glow.getchannel("A").filter(ImageFilter.MaxFilter(13)).filter(ImageFilter.GaussianBlur(1.5))
    outline = Image.new("RGBA", (N, N), (28, 12, 22, 0))
    outline.putalpha(alpha)
    img.alpha_composite(outline)
    img.alpha_composite(k, pos)
    return img


os.makedirs(OUT, exist_ok=True)
bg, fg = background(), foreground(235)                     # cabe en la zona segura (66 %) del ícono adaptable
bg.save(os.path.join(OUT, "fondo.png"))
fg.save(os.path.join(OUT, "frente.png"))
full = background()
full.alpha_composite(foreground(340))


def masked(shape):
    m = Image.new("L", (N * 4, N * 4), 0)
    d = ImageDraw.Draw(m)
    box = (0, 0, N * 4 - 1, N * 4 - 1)
    d.ellipse(box, fill=255) if shape == "round" else d.rounded_rectangle(box, N * 4 * .2, fill=255)
    r = full.copy()
    r.putalpha(m.resize((N, N), Image.LANCZOS))
    return r


masked("square").save(os.path.join(OUT, "icono.png"))
masked("round").save(os.path.join(OUT, "redondo.png"))
full.convert("RGB").resize((1024, 1024), Image.LANCZOS).save(os.path.join(OUT, "completo.png"))   # iPhone
