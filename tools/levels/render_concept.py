"""Imagem de conceito de uma fase a partir do mapa em grade.

Uso:
    python tools/levels/render_concept.py pasta_de_saida

Gera `fase-01-conceito.png` (a fase inteira, reduzida para 1920 de largura) e
`fase-01-detalhe.png` (uma tela de 1920 x 1080 em tamanho real, como a câmera
mostra de perto). É uma prévia para aprovação, não o jogo: usa a mesma grade e os
sprites de alimentos, caixa e personagem do atlas, num tema de estufa.
"""
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import fase01_map as level  # noqa: E402

W, H = level.W, level.H
REPO = Path(__file__).resolve().parents[2]
ATLAS = REPO / "Assets/_Game/Art/VisualAtlas/SP_VisualAtlas.png"
T = 60

WALL_TOP, WALL_BOTTOM = (226, 240, 222), (196, 224, 200)
GLASS = (182, 212, 186)
STRUCT, STRUCT_LIGHT, STRUCT_DARK, BRICK = (58, 88, 66), (96, 136, 102), (30, 48, 36), (52, 80, 60)
WOOD, WOOD_LIGHT, WOOD_DARK, INK = (190, 126, 66), (226, 166, 98), (138, 84, 40), (40, 28, 20)
FAST_BACK = (246, 206, 190)
FAST_RED, FAST_WHITE = (214, 64, 52), (250, 244, 236)
MUD, MUD_LIGHT = (120, 76, 42), (168, 118, 74)
P1_COLOR, P2_COLOR = (49, 142, 240), (192, 106, 235)
LEVER_COLOR, LIFT_COLOR = (36, 176, 160), (130, 150, 176)

HEALTHY = ["apple", "banana", "carrot", "broccoli", "water", "strawberry", "orange", "grapes",
           "nuts", "tomato"]
FAST = ["burger", "pizza", "soda", "fries", "donut", "hotdog", "icecream", "chocolate"]
ATLAS_NAMES = [
    "VitaHealthy", "VitaStable", "VitaTired", "VitaCritical", "Salo", "Tree",
    "apple", "banana", "orange", "grapes", "strawberry", "carrot",
    "broccoli", "tomato", "water", "nuts", "burger", "pizza",
    "soda", "fries", "donut", "hotdog", "icecream", "chocolate",
    "Ground", "Platform", "Wall", "Shrub", "Checkpoint", "Finish",
    "Box", "PressureButton", "Lever", "Door", "Spikes", "Mud",
]
XS = [0, 211, 420, 627, 837, 1045, 1254]
YS = [0, 278, 475, 677, 883, 1054, 1254]


def font(size):
    return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", size)


def atlas():
    sheet = Image.open(ATLAS).convert("RGBA")
    sprites = {}
    for i, name in enumerate(ATLAS_NAMES):
        row, col = divmod(i, 6)
        cell = sheet.crop((XS[col], YS[row], XS[col + 1], YS[row + 1]))
        sprites[name] = cell.crop(cell.getbbox())
    return sprites


class Canvas:
    def __init__(self):
        self.image = Image.new("RGBA", (W * T, H * T), (0, 0, 0, 255))
        self.draw = ImageDraw.Draw(self.image)

    def px(self, x, y):
        return (x * T, (H - y) * T)

    def rect(self, x0, y0, x1, y1, fill, outline=None, width=0, radius=0):
        a, b = self.px(x0, y1), self.px(x1, y0)
        self.draw.rounded_rectangle([a, b], radius=radius, fill=fill, outline=outline, width=width)

    def poly(self, points, fill, outline=None, width=0):
        pts = [self.px(x, y) for x, y in points]
        self.draw.polygon(pts, fill=fill)
        if outline:
            self.draw.line(pts + [pts[0]], fill=outline, width=width, joint="curve")

    def line(self, points, fill, width):
        self.draw.line([self.px(x, y) for x, y in points], fill=fill, width=int(width), joint="curve")

    def sprite(self, image, cx, by, height, flip=False):
        scale = height * T / image.height
        sized = image.resize((max(1, int(image.width * scale)), max(1, int(image.height * scale))),
                             Image.LANCZOS)
        if flip:
            sized = sized.transpose(Image.FLIP_LEFT_RIGHT)
        x, y = self.px(cx, by)
        self.image.alpha_composite(sized, (int(x - sized.width / 2), int(y - sized.height)))

    def text(self, cx, cy, value, size, fill):
        self.draw.multiline_text(self.px(cx, cy), value, font=font(size), fill=fill, anchor="mm",
                                 align="center", spacing=2)


def background(c):
    w, h = c.image.size
    for py in range(h):
        t = py / h
        color = tuple(int(WALL_TOP[i] + (WALL_BOTTOM[i] - WALL_TOP[i]) * t) for i in range(3))
        c.draw.line([(0, py), (w, py)], fill=color)
    for x in range(0, W + 1, 4):
        c.line([(x, 0), (x, H)], GLASS, 3)
    for y in range(0, H + 1, 6):
        c.line([(0, y), (W, y)], GLASS, 3)
    leaves = Image.new("RGBA", c.image.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(leaves)
    rng = random.Random(4)
    for _ in range(40):
        cx, cy, r = rng.uniform(2, W - 2), rng.uniform(1, H - 1), rng.uniform(1.2, 2.4)
        x, y = c.px(cx, cy)
        rr = r * T
        ld.ellipse([x - rr, y - rr * 0.6, x + rr, y + rr * 0.6], fill=(168, 204, 166, 100))
    c.image.alpha_composite(leaves.filter(ImageFilter.GaussianBlur(18)))


def fast_corridors(c):
    """O corredor curto de cada andar tem cara de lanchonete: fundo quente e letreiros."""
    for k in range(level.FLOORS):
        s = level.short_row(k) + 1
        top = level.long_row(k) + 0.5
        x0, x1 = 4, 61
        mask = Image.new("RGBA", c.image.size, (0, 0, 0, 0))
        md = ImageDraw.Draw(mask)
        md.rectangle([c.px(x0, top), c.px(x1, s)], fill=FAST_BACK + (150,))
        c.image.alpha_composite(mask)
        for x in range(8, 58, 10):
            cx = x + 0.5 if k % 2 == 0 else W - 1 - x - 0.5
            c.rect(cx - 0.55, s + 0.95, cx + 0.55, s + 1.35, FAST_RED, radius=8)
            c.text(cx, s + 1.15, "FAST", 13, FAST_WHITE)


def solid_at(g, x, y):
    return 0 <= x < W and 0 <= y < H and g[y][x] in "#/\\"


def structure(c, g):
    for y in range(H):
        for x in range(W):
            ch = g[y][x]
            if ch == "#":
                c.rect(x, y, x + 1, y + 1, STRUCT)
                offset = 0.5 if y % 2 else 0.0
                c.line([(x, y + 0.5), (x + 1, y + 0.5)], BRICK, 2)
                c.line([(x + offset, y), (x + offset, y + 0.5)], BRICK, 2)
                c.line([(x + 0.5 - offset, y + 0.5), (x + 0.5 - offset, y + 1)], BRICK, 2)
            elif ch == "/":
                c.poly([(x, y), (x + 1, y), (x + 1, y + 1)], STRUCT)
            elif ch == "\\":
                c.poly([(x, y), (x + 1, y), (x, y + 1)], STRUCT)
    for y in range(H):
        for x in range(W):
            ch = g[y][x]
            if ch == "#":
                if y + 1 < H and not solid_at(g, x, y + 1):
                    c.rect(x, y + 0.82, x + 1, y + 1, STRUCT_LIGHT)
                    c.line([(x, y + 1), (x + 1, y + 1)], STRUCT_DARK, 3)
                for nx, a, b in ((x - 1, (x, y), (x, y + 1)), (x + 1, (x + 1, y), (x + 1, y + 1))):
                    if 0 <= nx < W and not solid_at(g, nx, y):
                        c.line([a, b], STRUCT_DARK, 3)
                if y > 0 and not solid_at(g, x, y - 1):
                    c.line([(x, y), (x + 1, y)], STRUCT_DARK, 3)
            elif ch in "/\\":
                a, b = ((x, y), (x + 1, y + 1)) if ch == "/" else ((x, y + 1), (x + 1, y))
                c.line([a, b], STRUCT_LIGHT, 10)
                c.line([a, b], STRUCT_DARK, 3)
    # piso de lanchonete no chão do corredor curto do andar 0
    for x in range(1, 61):
        if g[1][x] in ".fCE":
            color = FAST_RED if x % 2 else FAST_WHITE
            c.rect(x, 0.82, x + 1, 1.0, color)


def planks(c, g):
    short_rows = {level.short_row(k) for k in range(1, level.FLOORS)}
    for y in range(H):
        x = 0
        while x < W:
            kind = g[y][x]
            if kind not in "=^~w":
                x += 1
                continue
            start = x
            while x < W and g[y][x] == kind:
                x += 1
            if kind == "=" and y in short_rows:
                # piso de ladrilho vermelho e branco do corredor curto
                c.rect(start, y + 0.5, x, y + 1, INK, radius=6)
                for tx in range(start, x):
                    color = FAST_RED if tx % 2 else FAST_WHITE
                    c.rect(tx + 0.03, y + 0.56, tx + 0.97, y + 0.94, color)
            elif kind == "=":
                c.rect(start, y + 0.5, x, y + 1, INK, radius=12)
                c.rect(start + 0.05, y + 0.56, x - 0.05, y + 0.94, WOOD, radius=9)
                c.rect(start + 0.15, y + 0.84, x - 0.15, y + 0.91, WOOD_LIGHT, radius=3)
                for seam in range(start + 2, x, 2):
                    c.line([(seam, y + 0.58), (seam, y + 0.92)], WOOD_DARK, 3)
            elif kind == "w":
                for tx in range(start, x):
                    c.rect(tx - 0.2, y + 0.5, tx + 1.2, y + 1, INK, radius=10)
                    c.rect(tx - 0.15, y + 0.56, tx + 1.15, y + 0.94, (214, 170, 110), radius=8)
                    c.line([(tx + 0.3, y + 0.94), (tx + 0.45, y + 0.72), (tx + 0.62, y + 0.86),
                            (tx + 0.7, y + 0.56)], INK, 3)
            elif kind == "~":
                c.rect(start, y + 0.45, x, y + 1, MUD)
                c.line([(start, y + 1), (x, y + 1)], MUD_LIGHT, 5)
                for tx in range(start, x):
                    c.draw.ellipse([c.px(tx + 0.25, y + 0.92), c.px(tx + 0.4, y + 0.77)],
                                   fill=MUD_LIGHT, outline=INK, width=1)
            else:
                c.rect(start, y + 0.5, x, y + 1, (70, 74, 84), radius=6)
                glow = Image.new("RGBA", c.image.size, (0, 0, 0, 0))
                ImageDraw.Draw(glow).rectangle([c.px(start, y + 1.6), c.px(x, y + 0.95)],
                                               fill=(255, 70, 70, 120))
                c.image.alpha_composite(glow.filter(ImageFilter.GaussianBlur(14)))
                for i in range((x - start) * 3):
                    sx = start + (i + 0.5) / 3
                    c.poly([(sx - 0.15, y + 1), (sx + 0.15, y + 1), (sx, y + 1.5)],
                           (226, 232, 240), INK, 2)
                    c.poly([(sx - 0.05, y + 1.33), (sx + 0.05, y + 1.33), (sx, y + 1.5)],
                           (255, 70, 70))


def decor(c, g):
    """Vasos nos caminhos longos e trepadeiras penduradas, só onde não atrapalham."""
    rng = random.Random(11)
    for k in range(level.FLOORS):
        l = level.long_row(k) + 1
        mirrored = k % 2 == 1
        spots = [x for x in range(7, 58)
                 if all(g[l][x + d] == "." and g[l - 1][x + d] == "=" for d in (-1, 0, 1))
                 and g[l + 1][x] == "."]
        for x in rng.sample(spots, min(3, len(spots))):
            c.rect(x + 0.25, l, x + 0.75, l + 0.35, (176, 98, 60), INK, 2, radius=4)
            for dx, dy, r in ((-0.12, 0.62, 0.22), (0.14, 0.7, 0.2), (0.0, 0.85, 0.2)):
                c.draw.ellipse([c.px(x + 0.5 + dx - r, l + dy + r), c.px(x + 0.5 + dx + r, l + dy - r)],
                               fill=(92, 168, 84), outline=INK, width=2)
        if k + 1 < level.FLOORS:
            ceiling = level.short_row(k + 1) + 0.5
            for x in rng.sample(range(8, 56), 4):
                gx = W - 1 - x if mirrored else x
                length = rng.uniform(0.5, 0.9)
                c.line([(gx + 0.5, ceiling), (gx + 0.45, ceiling - length)], (64, 132, 70), 4)
                for i in range(3):
                    ly = ceiling - length * (i + 1) / 3.2
                    c.draw.ellipse([c.px(gx + 0.38, ly + 0.08), c.px(gx + 0.6, ly - 0.08)],
                                   fill=(104, 180, 92))


def mechanisms(c, g, sprites):
    healthy = iter(HEALTHY * 10)
    fast = iter(FAST * 10)
    for y in range(H):
        for x in range(W):
            ch = g[y][x]
            cx = x + 0.5
            if ch == ":":
                c.line([(cx, y), (cx, y + 1)], LIFT_COLOR, 2)
            elif ch == "E":
                c.rect(x + 0.02, y, x + 0.98, y + 0.3, INK, radius=4)
                c.rect(x + 0.06, y + 0.04, x + 0.94, y + 0.26, LIFT_COLOR, radius=3)
            elif ch == "G":
                c.rect(x + 0.3, y, x + 0.7, y + 1, INK)
                c.rect(x + 0.36, y, x + 0.64, y + 1, LEVER_COLOR)
            elif ch == "L":
                c.rect(x + 0.2, y, x + 0.8, y + 0.22, (90, 96, 104), radius=4)
                c.line([(x + 0.5, y + 0.2), (x + 0.75, y + 0.85)], INK, 9)
                c.line([(x + 0.5, y + 0.2), (x + 0.75, y + 0.85)], (200, 204, 210), 5)
                c.draw.ellipse([c.px(x + 0.62, y + 0.98), c.px(x + 0.88, y + 0.72)],
                               fill=LEVER_COLOR, outline=INK, width=2)
            elif ch == "C":
                c.line([(x + 0.3, y), (x + 0.3, y + 1.9)], INK, 6)
                c.poly([(x + 0.33, y + 1.85), (x + 1.1, y + 1.6), (x + 0.33, y + 1.35)],
                       (80, 200, 100), INK, 2)
            elif ch == "h":
                c.sprite(sprites[next(healthy)], cx, y + 0.1, 0.75)
            elif ch == "f":
                c.sprite(sprites[next(fast)], cx, y + 0.08, 0.7)
            elif ch in "12":
                c.sprite(sprites["VitaHealthy"], cx, y, 1.25)
                tag = P1_COLOR if ch == "1" else P2_COLOR
                c.rect(x + 0.15, y + 1.32, x + 0.85, y + 1.62, tag, radius=8)
                c.text(cx, y + 1.47, "P" + ch, 11, (255, 255, 255))
            elif ch in "PQ" and (y == 0 or g[y - 1][x] != ch):
                tag = P1_COLOR if ch == "P" else P2_COLOR
                c.rect(x - 0.1, y, x + 1.1, y + 1.95, INK, radius=24)
                c.rect(x - 0.02, y + 0.04, x + 1.02, y + 1.88, tag, radius=20)
                c.rect(x + 0.14, y + 0.04, x + 0.86, y + 1.6, (22, 30, 46), radius=16)
                c.text(cx, y + 1.0, "P1" if ch == "P" else "P2", 13, tag)


def sign(c, cx, by, text):
    lines = text.split("\n")
    f = font(13)
    w = max(c.draw.textlength(line, font=f) for line in lines) / T + 0.45
    h = 0.36 * len(lines) + 0.3
    c.rect(cx - w / 2 - 0.05, by - 0.05, cx + w / 2 + 0.05, by + h + 0.05, INK, radius=10)
    c.rect(cx - w / 2, by, cx + w / 2, by + h, (82, 56, 40), radius=8)
    c.text(cx, by + h / 2, text, 13, (250, 244, 222))


def signs(c):
    for k in range(level.FLOORS):
        mirrored = k % 2 == 1
        l = level.long_row(k) + 1
        arrow = "←" if mirrored else "→"
        for hole in (level.HOLE_A, level.HOLE_B):
            local = hole - 2.4
            gx = W - local if mirrored else local
            # acima da cabeça dos jogadores; no último andar o teto é baixo
            base = l + 1.3 if k == level.FLOORS - 1 else l + 2.1
            sign(c, gx, base, f"↓ CURTO\n{arrow} LONGO")
    first_lever = next(k for k in range(level.FLOORS)
                       if any(item[0] == "lever" for item in level.LONG_CONTENT[k]))
    lever_x = next(item[1] for item in level.LONG_CONTENT[first_lever] if item[0] == "lever")
    gx = W - 1 - lever_x + 0.5 if first_lever % 2 else lever_x + 0.5
    sign(c, gx, level.long_row(first_lever) + 2.3, "PUXE A\nALAVANCA")


def hud(image):
    """HUD da câmera, desenhado sobre um recorte do tamanho da tela."""
    d = ImageDraw.Draw(image)
    f, small = font(26), font(20)
    for x0, color, label in ((20, P1_COLOR, "P1"), (1450, P2_COLOR, "P2")):
        d.rounded_rectangle([x0, 8, x0 + 450, 54], radius=12, fill=(20, 32, 26))
        d.text((x0 + 18, 31), label, font=f, fill=color, anchor="lm")
        d.rounded_rectangle([x0 + 70, 22, x0 + 300, 40], radius=8, fill=color)
        d.text((x0 + 430, 31), "0 pts", font=small, fill=(255, 220, 90), anchor="rm")
    d.rounded_rectangle([840, 4, 1080, 58], radius=14, fill=(20, 32, 26))
    d.text((960, 31), "00:00", font=f, fill=(240, 246, 236), anchor="mm")


def main():
    out = Path(sys.argv[1])
    g = level.build()
    sprites = atlas()
    c = Canvas()
    background(c)
    fast_corridors(c)
    structure(c, g)
    planks(c, g)
    decor(c, g)
    mechanisms(c, g, sprites)
    signs(c)
    full = c.image.convert("RGB")
    overview = full.resize((1920, int(1920 * H / W)), Image.LANCZOS)
    overview.save(out / "fase-01-conceito.png")
    # recorte: o que a câmera mostra no começo (andares 0 a 2, metade esquerda)
    detail = full.crop((0, (H - 18) * T, 32 * T, H * T)).convert("RGBA")
    hud(detail)
    detail.convert("RGB").save(out / "fase-01-detalhe.png")
    print("conceito", overview.size, "detalhe", detail.size)


if __name__ == "__main__":
    main()
