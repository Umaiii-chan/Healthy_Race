"""Gera a arte de cenário da arena: fundo em camadas, terreno, perigos e placa.

Uso:
    python tools/art/build_environment.py [--preview preview.png]

Saída em Assets/_Game/Art/Environment/Arena/ (256 px por unidade do jogo):
    SP_Sky.png        gradiente de céu (esticado pela câmera)
    SP_Clouds.png     3 nuvens, células de 512 x 256
    SP_HillsFar.png   morros distantes, ladrilhável na horizontal
    SP_HillsNear.png  morros próximos com copas de árvore, ladrilhável
    SP_Bushes.png     vegetação de primeiro plano do fundo, ladrilhável
    SP_Ground.png     chão com grama: bordas esquerda/direita + miolo (9-slice)
    SP_Stone.png      pedra das paredes, ladrilhável nos dois eixos
    SP_Plank.png      plataforma de madeira: pontas + miolo (9-slice)
    SP_Spikes.png     espinhos com pontas vermelhas e brilho, ladrilhável
    SP_Mud.png        lama com bolhas, ladrilhável
    SP_Sign.png       placa de madeira (9-slice) para instruções curtas

As bordas de 9-slice estão em BORDERS e são repetidas em
VisualAssetBuilder.Environment.cs. Mudou aqui, mude lá.
"""
import argparse
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

SS = 3
INK = (38, 26, 20, 255)
REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "Assets/_Game/Art/Environment/Arena"

# bordas (esquerda, baixo, direita, cima) em px, na convenção da Unity
BORDERS = {
    "SP_Ground": (96, 44, 96, 96),
    "SP_Plank": (88, 40, 88, 40),
    "SP_Sign": (44, 44, 44, 44),
}


def canvas(w, h, color=(0, 0, 0, 0)):
    return Image.new("RGBA", (w * SS, h * SS), color)


def down(image):
    return image.resize((image.width // SS, image.height // SS), Image.LANCZOS)


def p(v):
    return v * SS


def periodic(x, width, terms):
    """Soma de senos com período múltiplo de `width`: a faixa emenda nas pontas."""
    return sum(a * math.sin(2 * math.pi * k * x / width + ph) for a, k, ph in terms)


def sky():
    w, h = 8, 512
    image = Image.new("RGBA", (w, h))
    top, bottom = (86, 178, 244), (214, 240, 255)
    for y in range(h):
        t = (y / (h - 1)) ** 1.25
        row = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        for x in range(w):
            image.putpixel((x, y), row)
    return image


def clouds():
    sheet = canvas(512, 768)
    draw = ImageDraw.Draw(sheet)
    rng = random.Random(7)
    for i in range(3):
        cx, cy = 256, 128 + i * 256
        blobs = [(cx + dx, cy + 20 - abs(dx) * 0.18 - rng.uniform(0, 26), r)
                 for dx, r in ((-150, 52), (-90, 78), (-20, 96), (60, 84), (130, 60), (180, 40))]
        # barriga azulada por baixo, corpo branco por cima
        for x, y, r in blobs:
            draw.ellipse([p(x - r), p(y - r + 14), p(x + r), p(y + r + 14)],
                         fill=(214, 232, 248, 255))
        for x, y, r in blobs:
            draw.ellipse([p(x - r), p(y - r), p(x + r), p(y + r)], fill=(255, 255, 255, 255))
        draw.rounded_rectangle([p(cx - 200), p(cy + 40), p(cx + 220), p(cy + 96)], radius=p(28),
                               fill=(255, 255, 255, 255))
        draw.rounded_rectangle([p(cx - 200), p(cy + 70), p(cx + 220), p(cy + 104)], radius=p(17),
                               fill=(214, 232, 248, 255))
    return down(sheet)


def hills(width, height, base, terms, fill, rim, trees=None):
    image = canvas(width, height)
    draw = ImageDraw.Draw(image)

    def top(x):
        return base + periodic(x, width, terms)

    step = 4
    line = [(p(x), p(top(x))) for x in range(0, width + step, step)]
    draw.polygon(line + [(p(width), p(height)), (0, p(height))], fill=fill)
    # faixa clara acompanhando o topo: dá volume sem contorno escuro
    draw.line(line, fill=rim, width=p(10), joint="curve")
    draw.polygon(
        [(x, y + p(8)) for x, y in line] + [(p(width), p(height)), (0, p(height))], fill=fill
    )
    if trees:
        color, light, count, seed = trees
        rng = random.Random(seed)
        for i in range(count):
            x = (i + 0.5) * width / count + rng.uniform(-40, 40)
            y = top(x % width)
            r = rng.uniform(26, 44)
            for offset in (-width, 0, width):  # copas na emenda aparecem dos dois lados
                cx = x + offset
                draw.rectangle([p(cx - 5), p(y - r), p(cx + 5), p(y + 12)], fill=color)
                for dx, dy, k in ((-0.6, 0.1, 0.75), (0.6, 0.1, 0.75), (0, -0.45, 1.0)):
                    draw.ellipse(
                        [p(cx + dx * r - r * k), p(y - r * 1.5 + dy * r - r * k),
                         p(cx + dx * r + r * k), p(y - r * 1.5 + dy * r + r * k)],
                        fill=color,
                    )
                draw.ellipse([p(cx - r * 0.75), p(y - r * 2.55), p(cx + r * 0.1), p(y - r * 1.8)],
                             fill=light)
    return down(image)


def bushes():
    width, height = 2048, 256
    image = canvas(width, height)
    draw = ImageDraw.Draw(image)
    rng = random.Random(11)
    dark, mid, light = (52, 132, 78, 255), (70, 158, 92, 255), (104, 190, 112, 255)
    x = 0.0
    lumps = []
    while x < width:
        r = rng.uniform(46, 96)
        lumps.append((x, height - r * rng.uniform(0.35, 0.8), r))
        x += r * rng.uniform(0.8, 1.2)
    for color, shift, scale in ((dark, 0, 1.0), (mid, 10, 0.86), (light, 26, 0.45)):
        for cx, cy, r in lumps:
            for offset in (-width, 0, width):
                rr = r * scale
                draw.ellipse(
                    [p(cx + offset - rr), p(cy + shift - rr), p(cx + offset + rr), p(cy + shift + rr)],
                    fill=color,
                )
    draw.rectangle([0, p(height - 40), p(width), p(height)], fill=dark)
    return down(image)


def outlined(draw_shape, w, h, outline=9):
    """Desenha a forma em tinta e depois a mesma forma encolhida em cor."""
    image = canvas(w, h)
    draw = ImageDraw.Draw(image)
    draw_shape(draw, 0, INK)
    return image, draw, outline


def ground():
    # 768 x 192: pontas de 96 px e miolo de 576 px (12 arcos de grama de 48 px),
    # para o recorte da grama emendar quando o miolo se repete
    w, h = 768, 192
    image = canvas(w, h)
    draw = ImageDraw.Draw(image)
    o = 9
    dirt, dirt_dark, pebble = (160, 106, 68, 255), (128, 82, 52, 255), (196, 150, 108, 255)
    grass, grass_light, grass_dark = (116, 210, 72, 255), (170, 238, 112, 255), (72, 160, 52, 255)
    draw.rounded_rectangle([0, 0, p(w), p(h)], radius=p(40), fill=INK)
    draw.rounded_rectangle([p(o), p(o), p(w - o), p(h - o)], radius=p(32), fill=dirt)
    rng = random.Random(3)
    for _ in range(34):
        x, y = rng.uniform(40, w - 40), rng.uniform(104, h - 26)
        rx, ry = rng.uniform(8, 18), rng.uniform(4, 9)
        draw.ellipse([p(x - rx), p(y - ry), p(x + rx), p(y + ry)],
                     fill=dirt_dark if rng.random() < 0.7 else pebble)
    scallop = 48
    for x in range(-scallop, w + scallop, scallop):
        draw.ellipse([p(x - 4), p(46), p(x + scallop + 4), p(46 + 46)], fill=INK)
    draw.rounded_rectangle([p(o), p(o), p(w - o), p(72)], radius=p(30), fill=grass)
    for x in range(-scallop, w + scallop, scallop):
        draw.ellipse([p(x + 3), p(44), p(x + scallop - 3), p(44 + 38)], fill=grass)
        draw.ellipse([p(x + 13), p(62), p(x + scallop - 13), p(62 + 14)], fill=grass_dark)
    draw.rounded_rectangle([p(o + 14), p(o + 7), p(w - o - 14), p(o + 19)], radius=p(6),
                           fill=grass_light)
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, p(w), p(h)], radius=p(40), fill=255)
    image.putalpha(Image.composite(image.getchannel("A"), Image.new("L", image.size, 0), mask))
    return down(image)


def stone():
    # ladrilho de pedra sem contorno externo: repete nos dois eixos sem emenda
    w = h = 256
    image = canvas(w, h, (74, 83, 96, 255))
    draw = ImageDraw.Draw(image)
    rng = random.Random(9)
    for row in range(2):
        y0 = row * 128
        shift = 0 if row == 0 else 64
        for col in range(-1, 3):
            x0 = col * 128 + shift
            tone = rng.choice(((142, 151, 163), (130, 140, 153), (152, 160, 171)))
            draw.rounded_rectangle([p(x0 + 5), p(y0 + 5), p(x0 + 123), p(y0 + 123)], radius=p(14),
                                   fill=(*tone, 255))
            draw.rounded_rectangle([p(x0 + 14), p(y0 + 12), p(x0 + 114), p(y0 + 24)], radius=p(6),
                                   fill=(*[min(255, c + 22) for c in tone], 255))
    return down(image)


def plank():
    w, h = 768, 128
    image = canvas(w, h)
    draw = ImageDraw.Draw(image)
    o = 8
    wood, wood_light, wood_dark = (212, 152, 88, 255), (238, 190, 126, 255), (160, 106, 58, 255)
    draw.rounded_rectangle([0, 0, p(w), p(h)], radius=p(30), fill=INK)
    draw.rounded_rectangle([p(o), p(o), p(w - o), p(h - o)], radius=p(23), fill=wood)
    draw.rounded_rectangle([p(o + 10), p(o + 6), p(w - o - 10), p(o + 20)], radius=p(7),
                           fill=wood_light)
    draw.rounded_rectangle([p(o + 6), p(h - o - 26), p(w - o - 6), p(h - o - 4)], radius=p(10),
                           fill=wood_dark)
    for x in (192, 384, 576):  # emendas das tábuas
        draw.line([p(x), p(o + 4), p(x), p(h - o - 4)], fill=wood_dark, width=p(6))
    for x in (44, w - 44):  # pregos nas pontas
        draw.ellipse([p(x - 10), p(54), p(x + 10), p(74)], fill=INK)
        draw.ellipse([p(x - 5), p(57), p(x + 1), p(63)], fill=(190, 190, 190, 255))
    return down(image)


def spikes():
    w, h = 256, 192
    # brilho vermelho rente ao chão: uma faixa uniforme na horizontal (ladrilha
    # sem emenda), borrada só na vertical
    glow = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    band = Image.new("L", (1, h * SS), 0)
    for y in range(h * SS):
        t = y / (h * SS - 1)
        band.putpixel((0, y), int(120 * max(0.0, (t - 0.35) / 0.65) ** 1.5))
    glow.paste((255, 64, 64, 255), [0, 0, w * SS, h * SS], band.resize((w * SS, h * SS)))
    image = canvas(w, h)
    draw = ImageDraw.Draw(image)
    steel, shade, tip = (226, 232, 240, 255), (150, 164, 180, 255), (255, 78, 78, 255)
    base_y, top_y = h - 8, 22
    for i in range(3):
        cx = (i + 0.5) * w / 3
        half = w / 6 - 5
        left, right, apex = (cx - half, base_y), (cx + half, base_y), (cx, top_y)
        draw.polygon([(p(left[0] - 7), p(base_y + 6)), (p(right[0] + 7), p(base_y + 6)),
                      (p(cx), p(top_y - 16))], fill=INK)
        draw.polygon([(p(left[0]), p(base_y)), (p(right[0]), p(base_y)), (p(cx), p(top_y))],
                     fill=steel)
        draw.polygon([(p(cx), p(base_y)), (p(right[0]), p(base_y)), (p(cx), p(top_y))], fill=shade)
        # ponta vermelha: lê como perigo mesmo sem o brilho
        t = 0.36
        lx, rx = cx - half * t, cx + half * t
        ty = top_y + (base_y - top_y) * t
        draw.polygon([(p(lx), p(ty)), (p(rx), p(ty)), (p(cx), p(top_y))], fill=tip)
    # o brilho vaza para os lados; corta nas bordas para a faixa ladrilhar limpa
    merged = Image.alpha_composite(glow, image)
    return down(merged)


def mud():
    w, h = 256, 128
    image = canvas(w, h)
    draw = ImageDraw.Draw(image)
    body, dark, light = (112, 70, 40, 255), (78, 46, 26, 255), (160, 110, 70, 255)

    def top(x):
        return 34 + periodic(x, w, ((7, 2, 0.4), (4, 5, 1.7)))

    line = [(p(x), p(top(x))) for x in range(0, w + 4, 4)]
    draw.polygon([(x, y - p(8)) for x, y in line] + [(p(w), p(h)), (0, p(h))], fill=INK)
    draw.polygon(line + [(p(w), p(h)), (0, p(h))], fill=body)
    draw.line([(x, y + p(9)) for x, y in line], fill=light, width=p(6), joint="curve")
    rng = random.Random(5)
    for _ in range(9):
        x, y, r = rng.uniform(20, w - 20), rng.uniform(64, h - 16), rng.uniform(6, 13)
        draw.ellipse([p(x - r), p(y - r * 0.6), p(x + r), p(y + r * 0.6)], fill=dark)
    for x, y, r in ((60, 30, 11), (150, 22, 8), (206, 34, 13)):  # bolhas na superfície
        draw.ellipse([p(x - r - 5), p(y - r - 5), p(x + r + 5), p(y + r + 5)], fill=INK)
        draw.ellipse([p(x - r), p(y - r), p(x + r), p(y + r)], fill=light)
        draw.ellipse([p(x - r * 0.5), p(y - r * 0.6), p(x - r * 0.1), p(y - r * 0.2)],
                     fill=(230, 200, 170, 255))
    return down(image)


def sign():
    w, h = 256, 256
    image = canvas(w, h)
    draw = ImageDraw.Draw(image)
    o = 9
    board, light, dark = (74, 50, 36, 255), (110, 78, 56, 255), (48, 32, 22, 255)
    draw.rounded_rectangle([0, 0, p(w), p(h)], radius=p(40), fill=INK)
    draw.rounded_rectangle([p(o), p(o), p(w - o), p(h - o)], radius=p(32), fill=board)
    draw.rounded_rectangle([p(o + 8), p(o + 8), p(w - o - 8), p(h - o - 8)], radius=p(25),
                           outline=light, width=p(5))
    for x, y in ((30, 30), (w - 30, 30), (30, h - 30), (w - 30, h - 30)):
        draw.ellipse([p(x - 7), p(y - 7), p(x + 7), p(y + 7)], fill=dark)
    return down(image)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)

    far_terms = ((70, 1, 0.0), (36, 2, 1.3), (16, 5, 0.6))
    near_terms = ((56, 1, 2.1), (40, 3, 0.2), (14, 7, 1.1))
    images = {
        "SP_Sky": sky(),
        "SP_Clouds": clouds(),
        "SP_HillsFar": hills(2048, 512, 210, far_terms, (158, 212, 196, 255), (182, 228, 212, 255)),
        "SP_HillsNear": hills(
            2048, 512, 250, near_terms, (112, 190, 104, 255), (146, 214, 126, 255),
            trees=((78, 156, 86, 255), (110, 186, 108, 255), 9, 21),
        ),
        "SP_Bushes": bushes(),
        "SP_Ground": ground(),
        "SP_Stone": stone(),
        "SP_Plank": plank(),
        "SP_Spikes": spikes(),
        "SP_Mud": mud(),
        "SP_Sign": sign(),
    }
    for name, image in images.items():
        image.save(OUT / f"{name}.png", optimize=True)
        print(name, image.size)

    if args.preview:
        board = Image.new("RGBA", (1600, 900), (0, 0, 0, 255))
        board.paste(images["SP_Sky"].resize((1600, 900)), (0, 0))
        for name, y, scale in (("SP_HillsFar", 330, 0.9), ("SP_HillsNear", 470, 0.9)):
            layer = images[name].resize((int(2048 * scale), int(512 * scale)))
            board.alpha_composite(layer, (0, y))
        board.alpha_composite(images["SP_Bushes"].resize((1600, 200)), (0, 700))
        board.alpha_composite(images["SP_Clouds"].crop((0, 0, 512, 256)), (120, 40))
        board.alpha_composite(images["SP_Clouds"].crop((0, 256, 512, 512)), (980, 110))
        board.alpha_composite(images["SP_Ground"], (60, 600))
        board.alpha_composite(images["SP_Plank"], (60, 430))
        board.alpha_composite(images["SP_Spikes"], (900, 610))
        board.alpha_composite(images["SP_Spikes"], (1156, 610))
        board.alpha_composite(images["SP_Mud"], (900, 480))
        board.alpha_composite(images["SP_Mud"], (1156, 480))
        board.alpha_composite(images["SP_Sign"].resize((180, 180)), (1340, 280))
        board.alpha_composite(images["SP_Stone"], (1340, 20))
        board.save(args.preview)
        print("preview", args.preview)


if __name__ == "__main__":
    main()
