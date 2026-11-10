"""Gera a arte das fases em labirinto: estufa (caminho longo) e lanchonete (curto).

Uso:
    python tools/art/build_greenhouse.py

Saída em Assets/_Game/Art/Environment/Greenhouse/. Ladrilhos a 256 px por unidade
do jogo, exceto o fundo (32 px por unidade, esticado sobre a fase inteira).
Bordas de 9-slice: SP_GH_Lift (32, 16, 32, 16), repetidas em
VisualAssetBuilder.Greenhouse.cs.
"""
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

SS = 3
REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "Assets/_Game/Art/Environment/Greenhouse"
INK = (30, 46, 34, 255)


def canvas(w, h, color=(0, 0, 0, 0)):
    return Image.new("RGBA", (w * SS, h * SS), color)


def down(image):
    return image.resize((image.width // SS, image.height // SS), Image.LANCZOS)


def p(v):
    return int(round(v * SS))


def wall():
    """Parede da estufa: gradiente, caixilhos de vidro e folhagem desfocada."""
    w, h = 2048, 1152
    image = Image.new("RGBA", (w, h))
    top, bottom = (226, 240, 222), (196, 224, 200)
    draw = ImageDraw.Draw(image)
    for y in range(h):
        t = y / (h - 1)
        draw.line([(0, y), (w, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    leaves = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ld = ImageDraw.Draw(leaves)
    rng = random.Random(4)
    for _ in range(70):
        cx, cy, r = rng.uniform(0, w), rng.uniform(0, h), rng.uniform(30, 80)
        ld.ellipse([cx - r, cy - r * 0.6, cx + r, cy + r * 0.6], fill=(166, 204, 164, 110))
        ld.ellipse([cx - r * 0.4, cy - r * 1.1, cx + r * 0.5, cy - r * 0.2], fill=(156, 198, 156, 110))
    image.alpha_composite(leaves.filter(ImageFilter.GaussianBlur(14)))
    # caixilhos: a cada 4 unidades na horizontal e 6 na vertical (128 e 192 px)
    for x in range(0, w + 1, 128):
        draw.line([(x, 0), (x, h)], fill=(176, 208, 182), width=4)
    for y in range(0, h + 1, 192):
        draw.line([(0, y), (w, y)], fill=(176, 208, 182), width=4)
    for x in range(64, w, 128):
        draw.line([(x, 0), (x, h)], fill=(188, 216, 192), width=2)
    return image


def leaves():
    """Folhagem de fundo para o parallax: manchas suaves que ladrilham sem emenda
    (desenhadas também do outro lado das bordas). 1024 px a 128 px/unidade = 8 × 8."""
    s = 1024
    image = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    rng = random.Random(9)
    for _ in range(18):
        cx, cy, r = rng.uniform(0, s), rng.uniform(0, s), rng.uniform(60, 130)
        tone = rng.choice(((120, 176, 120), (104, 162, 108), (136, 188, 130)))
        for ox in (-s, 0, s):
            for oy in (-s, 0, s):
                x, y = cx + ox, cy + oy
                d.ellipse([x - r, y - r * 0.55, x + r, y + r * 0.55], fill=tone + (42,))
                d.ellipse([x - r * 0.45, y - r, x + r * 0.5, y - r * 0.15], fill=tone + (36,))
    return image.filter(ImageFilter.GaussianBlur(16))


def brick():
    """Bloco da estrutura: tijolo verde-escuro que ladrilha sem emenda."""
    s = 256
    image = canvas(s, s, (58, 88, 66, 255))
    d = ImageDraw.Draw(image)
    mortar = (46, 72, 54, 255)
    rng = random.Random(2)
    for row in range(2):
        y0 = row * 128
        shift = 0 if row == 0 else 64
        for col in range(-1, 3):
            x0 = col * 128 + shift
            tone = rng.choice(((62, 94, 70), (56, 86, 64), (66, 98, 74)))
            d.rectangle([p(x0 + 4), p(y0 + 4), p(x0 + 124), p(y0 + 124)], fill=tone + (255,))
            d.rectangle([p(x0 + 10), p(y0 + 10), p(x0 + 118), p(y0 + 20)],
                        fill=tuple(min(255, c + 12) for c in tone) + (255,))
        d.line([(0, p(y0)), (p(s), p(y0))], fill=mortar, width=p(6))
    return down(image)


def brick_top():
    """Faixa clara no topo dos blocos expostos (ladrilha na horizontal)."""
    w, h = 256, 48
    image = canvas(w, h)
    d = ImageDraw.Draw(image)
    d.rectangle([0, p(8), p(w), p(h)], fill=(96, 136, 102, 255))
    d.rectangle([0, 0, p(w), p(9)], fill=INK)
    d.rectangle([0, p(14), p(w), p(20)], fill=(124, 164, 120, 255))
    return down(image)


def ramp():
    """Rampa '/' (sobe para a direita): tijolo no triângulo e borda clara."""
    s = 256
    base = brick().resize((s * SS, s * SS), Image.NEAREST)
    mask = Image.new("L", base.size, 0)
    ImageDraw.Draw(mask).polygon([(0, p(s)), (p(s), p(s)), (p(s), 0)], fill=255)
    image = Image.new("RGBA", base.size, (0, 0, 0, 0))
    image.paste(base, (0, 0), mask)
    d = ImageDraw.Draw(image)
    d.line([(0, p(s)), (p(s), 0)], fill=(96, 136, 102, 255), width=p(30))
    d.line([(0, p(s)), (p(s), 0)], fill=INK, width=p(9))
    # o traço grosso vaza do triângulo: recorta de novo
    alpha = Image.composite(image.getchannel("A"), Image.new("L", image.size, 0), mask)
    image.putalpha(alpha)
    return down(image)


def fast_floor():
    """Piso da lanchonete: ladrilho vermelho e branco, um por unidade."""
    w, h = 512, 128
    image = canvas(w, h)
    d = ImageDraw.Draw(image)
    d.rounded_rectangle([0, 0, p(w), p(h)], radius=p(10), fill=INK)
    for i, color in enumerate(((214, 64, 52), (250, 244, 236))):
        x0 = i * 256
        d.rectangle([p(x0 + 6), p(10), p(x0 + 250), p(h - 10)], fill=color + (255,))
        d.rectangle([p(x0 + 14), p(16), p(x0 + 242), p(28)],
                    fill=tuple(min(255, c + 24) for c in color) + (255,))
    return down(image)


def fast_sign():
    w, h = 256, 96
    image = canvas(w, h)
    d = ImageDraw.Draw(image)
    d.rounded_rectangle([0, 0, p(w), p(h)], radius=p(28), fill=INK)
    d.rounded_rectangle([p(6), p(6), p(w - 6), p(h - 6)], radius=p(24), fill=(214, 64, 52, 255))
    # letras em blocos simples (sem fonte embutida): F A S T
    white = (250, 244, 236, 255)
    x, y0, y1, t = 40, 24, 72, 9

    def bar(a, b, c, e):
        d.rectangle([p(a), p(b), p(c), p(e)], fill=white)

    bar(x, y0, x + t, y1); bar(x, y0, x + 30, y0 + t); bar(x, 44, x + 24, 44 + t)
    x = 86
    bar(x, y0, x + t, y1); bar(x + 26, y0, x + 26 + t, y1); bar(x, y0, x + 35, y0 + t)
    bar(x, 46, x + 35, 46 + t)
    x = 136
    bar(x, y0, x + 34, y0 + t); bar(x, y0, x + t, 48); bar(x, 44, x + 34, 44 + t)
    bar(x + 25, 44, x + 34, y1); bar(x, y1 - t, x + 34, y1)
    x = 184
    bar(x, y0, x + 34, y0 + t); bar(x + 13, y0, x + 13 + t, y1)
    return down(image)


def pot():
    s = 256
    image = canvas(s, s)
    d = ImageDraw.Draw(image)
    leaf, leaf_light = (92, 168, 84, 255), (130, 200, 110, 255)
    for cx, cy, r in ((96, 104, 52), (160, 96, 50), (128, 60, 56), (120, 130, 40), (150, 140, 40)):
        d.ellipse([p(cx - r - 7), p(cy - r - 7), p(cx + r + 7), p(cy + r + 7)], fill=INK)
    for cx, cy, r in ((96, 104, 52), (160, 96, 50), (128, 60, 56), (120, 130, 40), (150, 140, 40)):
        d.ellipse([p(cx - r), p(cy - r), p(cx + r), p(cy + r)], fill=leaf)
        d.ellipse([p(cx - r * 0.6), p(cy - r * 0.75), p(cx), p(cy - r * 0.2)], fill=leaf_light)
    d.polygon([(p(70), p(160)), (p(186), p(160)), (p(170), p(250)), (p(86), p(250))], fill=INK)
    d.polygon([(p(78), p(166)), (p(178), p(166)), (p(164), p(244)), (p(92), p(244))],
              fill=(186, 104, 64, 255))
    d.rectangle([p(66), p(154), p(190), p(178)], fill=INK)
    d.rectangle([p(72), p(158), p(184), p(174)], fill=(206, 124, 80, 255))
    return down(image)


def vine():
    w, h = 128, 256
    image = canvas(w, h)
    d = ImageDraw.Draw(image)
    stem = (64, 132, 70, 255)
    pts = [(64 + math.sin(t / 40) * 10, t) for t in range(0, 230, 6)]
    d.line([(p(x), p(y)) for x, y in pts], fill=INK, width=p(12), joint="curve")
    d.line([(p(x), p(y)) for x, y in pts], fill=stem, width=p(6), joint="curve")
    for i, (x, y) in enumerate(pts[3::5]):
        side = 1 if i % 2 else -1
        cx, cy = x + side * 18, y + 6
        d.ellipse([p(cx - 20), p(cy - 12), p(cx + 20), p(cy + 12)], fill=INK)
        d.ellipse([p(cx - 16), p(cy - 9), p(cx + 16), p(cy + 9)], fill=(104, 180, 92, 255))
    return down(image)


def lift():
    w, h = 256, 64
    image = canvas(w, h)
    d = ImageDraw.Draw(image)
    d.rounded_rectangle([0, 0, p(w), p(h)], radius=p(14), fill=INK)
    d.rounded_rectangle([p(5), p(5), p(w - 5), p(h - 5)], radius=p(10), fill=(150, 168, 190, 255))
    d.rectangle([p(16), p(12), p(w - 16), p(22)], fill=(196, 210, 226, 255))
    for x in range(40, w - 20, 44):
        d.polygon([(p(x), p(h - 10)), (p(x + 22), p(h - 10)), (p(x + 30), p(30)), (p(x + 8), p(30))],
                  fill=(246, 196, 60, 255))
    return down(image)


def gate():
    w, h = 64, 256
    image = canvas(w, h)
    d = ImageDraw.Draw(image)
    d.rectangle([0, 0, p(w), p(h)], fill=INK)
    d.rectangle([p(6), 0, p(w - 6), p(h)], fill=(36, 176, 160, 255))
    d.rectangle([p(14), 0, p(24), p(h)], fill=(96, 214, 196, 255))
    for y in (48, 176):
        d.rectangle([0, p(y), p(w), p(y + 14)], fill=INK)
    return down(image)


def neutral(image, reference):
    """Versão em tons de cinza claros para colorir no jogo pelo tema da fase.
    `reference` é a cor que vira branco; o resto fica proporcional a ela."""
    ref = 0.299 * reference[0] + 0.587 * reference[1] + 0.114 * reference[2]
    rgba = image.convert("RGBA")
    luma = rgba.convert("L").point(lambda v: min(255, int(round(v / ref * 255))))
    return Image.merge("RGBA", (luma, luma, luma, rgba.getchannel("A")))


# Cores que reproduzem a Fase 1 com a arte neutra; o [tema] de cada fase troca.
# Repetidas em LevelBootstrap (DefaultBrickTint, DefaultWallTint, DefaultFoliageTint).
BRICK_TINT = (69, 105, 79)
WALL_TINT = (226, 240, 222)
FOLIAGE_TINT = (124, 180, 124)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    images = {
        "SP_GH_Wall": neutral(wall(), WALL_TINT),
        "SP_GH_Leaves": neutral(leaves(), (136, 188, 130)),
        "SP_GH_Brick": neutral(brick(), (70, 100, 78)),
        "SP_GH_BrickTop": neutral(brick_top(), (124, 164, 120)),
        "SP_GH_Ramp": neutral(ramp(), (70, 100, 78)),
        "SP_GH_FastFloor": fast_floor(),
        "SP_GH_FastSign": fast_sign(),
        "SP_GH_Pot": pot(),
        "SP_GH_Vine": vine(),
        "SP_GH_Lift": lift(),
        "SP_GH_Gate": gate(),
    }
    for name, image in images.items():
        image.save(OUT / f"{name}.png", optimize=True)
        print(name, image.size)


if __name__ == "__main__":
    main()
