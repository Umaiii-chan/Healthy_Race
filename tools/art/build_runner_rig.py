"""Gera a folha de peças PROVISÓRIAS de Zuri e Tom (rig de membros em curva).

Uso:
    python tools/art/build_runner_rig.py [--preview preview.png]

Saída: Assets/_Game/Art/Characters/SP_RunnerRig.png (13 colunas x 2 linhas de 256 px,
a 256 px por unidade do jogo).

As peças seguem as proporções da folha de modelos aprovada em 11/11/2026
(docs/arte/folha-de-modelos-zuri-tom-2026-11-11.html): Zuri com 3,3 cabeças e Tom com
3,5; corpo de perfil, cabeça em três-quartos, olhando para a direita. São desenho por
código, para o rig funcionar e ser testado: o desenho final é do ilustrador, com as
mesmas células e os mesmos pivôs.

Braços e pernas não estão aqui: são curvas desenhadas em malha pelo componente HoseLimb.

Os pivôs (PARTS) e as posições de montagem (RIGS) são repetidos em
VisualAssetBuilder.Runners.cs. Mudou aqui, mude lá.
"""
import argparse
import math
from pathlib import Path

from PIL import Image, ImageDraw

CELL = 256
SS = 4  # supersampling
INK = (0x1E, 0x2A, 0x22, 255)
CREAM = (0xF8, 0xF3, 0xDC, 255)
WHITE = (255, 255, 255, 255)
# contorno: fino no lado da luz (cima, esquerda), grosso no lado do peso
INK_THIN = 3
INK_HEAVY = 7

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "Assets/_Game/Art/Characters/SP_RunnerRig.png"

COLUMNS = [
    "head", "face", "face_blink", "face_effort", "face_sleepy", "face_rush",
    "hair_back", "hair_front", "band", "tail", "torso", "hand", "shoe",
]

# pivô de cada peça em px da célula (origem no canto superior esquerdo)
PARTS = {
    "head": (128, 128),
    "face": (128, 128),
    "face_blink": (128, 128),
    "face_effort": (128, 128),
    "face_sleepy": (128, 128),
    "face_rush": (128, 128),
    "hair_back": (128, 128),
    "hair_front": (128, 128),
    "band": (128, 128),
    "tail": (200, 128),   # nó da faixa: a ponta sai para a esquerda
    "torso": (128, 176),  # quadril
    "hand": (128, 128),   # punho
    "shoe": (104, 118),   # tornozelo
}

CHARACTERS = {
    "zuri": dict(
        skin=(0x97, 0x60, 0x3C), skin_shade=(0x87, 0x59, 0x39), hair=(0x2B, 0x1F, 0x1C),
        player=(0x2F, 0x80, 0xED), player_shade=(0x2C, 0x6F, 0xC4), shorts=(0x2C, 0x3A, 0x4A),
        number="1",
    ),
    "tom": dict(
        skin=(0xF0, 0xCB, 0xA2), skin_shade=(0xD5, 0xB6, 0x91), hair=(0x7A, 0x44, 0x2A),
        player=(0xF2, 0x99, 0x4A), player_shade=(0xCB, 0x83, 0x42), shorts=(0xD0, 0xD8, 0xD6),
        number="2",
    ),
}

# posições de montagem em unidades do jogo, origem entre os pés, y para cima
RIGS = {
    # o tornozelo fica a 0,18 do chão: é a altura do tênis (46 px da sola ao tornozelo)
    "zuri": dict(hip=0.60, neck=0.34, head=0.21, shoulder=0.31, arm=0.38, leg=0.45,
                 feet=((-0.10, 0.18), (0.08, 0.18)), hands=((-0.13, 0.58), (0.11, 0.56))),
    "tom": dict(hip=0.64, neck=0.36, head=0.21, shoulder=0.34, arm=0.40, leg=0.49,
                feet=((-0.09, 0.18), (0.09, 0.18)), hands=((-0.12, 0.60), (0.10, 0.60))),
}


def px(v):
    return v * SS


def rgba(color):
    return (*color, 255) if len(color) == 3 else color


class Layer:
    """Formas desenhadas duas vezes: a tinta (expandida e deslocada para o lado do
    peso) e o preenchimento por cima. O deslocamento dá o contorno fino em cima e à
    esquerda e grosso embaixo e à direita."""

    def __init__(self):
        size = (CELL * SS, CELL * SS)
        self.ink = Image.new("RGBA", size, (0, 0, 0, 0))
        self.fill = Image.new("RGBA", size, (0, 0, 0, 0))
        self._ink = ImageDraw.Draw(self.ink)
        self._fill = ImageDraw.Draw(self.fill)

    @staticmethod
    def _grow():
        return (INK_THIN + INK_HEAVY) / 2, (INK_HEAVY - INK_THIN) / 2

    def ellipse(self, cx, cy, rx, ry, color, outline=True):
        if outline:
            grow, shift = self._grow()
            self._ink.ellipse(
                [px(cx - rx - grow + shift), px(cy - ry - grow + shift),
                 px(cx + rx + grow + shift), px(cy + ry + grow + shift)], fill=INK)
        self._fill.ellipse([px(cx - rx), px(cy - ry), px(cx + rx), px(cy + ry)], fill=rgba(color))

    def polygon(self, points, color, outline=True):
        scaled = [(px(x), px(y)) for x, y in points]
        if outline:
            grow, shift = self._grow()
            moved = [(x + px(shift), y + px(shift)) for x, y in scaled]
            self._ink.polygon(moved, fill=INK)
            self._ink.line(moved + [moved[0]], fill=INK, width=int(px(2 * grow)), joint="curve")
            r = px(grow)
            for x, y in moved:
                self._ink.ellipse([x - r, y - r, x + r, y + r], fill=INK)
        self._fill.polygon(scaled, fill=rgba(color))

    def stroke(self, points, width, color=INK):
        """Linha solta (boca, pálpebra, listra), sem contorno."""
        scaled = [(px(x), px(y)) for x, y in points]
        self._fill.line(scaled, fill=rgba(color), width=int(px(width)), joint="curve")
        r = px(width) / 2
        for x, y in (scaled[0], scaled[-1]):
            self._fill.ellipse([x - r, y - r, x + r, y + r], fill=rgba(color))

    def chord(self, cx, cy, rx, ry, start, end, color):
        self._fill.chord([px(cx - rx), px(cy - ry), px(cx + rx), px(cy + ry)], start, end,
                         fill=rgba(color))

    def image(self):
        return Image.alpha_composite(self.ink, self.fill).resize((CELL, CELL), Image.LANCZOS)


def arc(cx, cy, rx, ry, start, end, steps=28):
    return [
        (cx + rx * math.cos(math.radians(start + (end - start) * i / steps)),
         cy + ry * math.sin(math.radians(start + (end - start) * i / steps)))
        for i in range(steps + 1)
    ]


# --------------------------------------------------------------------------
# Cabeça e rostos (cabeça em três-quartos, olhando para a direita)
# --------------------------------------------------------------------------
HEAD_R = 54  # 0,21 unidade


def head(c):
    layer = Layer()
    layer.ellipse(84, 136, 11, 14, c["skin_shade"])            # orelha, atrás
    layer.ellipse(128, 128, HEAD_R, HEAD_R - 1, c["skin"])     # crânio
    layer.ellipse(170, 150, 14, 12, c["skin"], outline=False)  # bochecha da frente
    return layer.image()


EYES = ((134, 130), (166, 130))  # olho de trás e olho da frente


def face(c, variant):
    layer = Layer()
    lid = {"open": 0.34, "blink": 1.0, "effort": 0.45, "sleepy": 0.72, "rush": 0.0}[variant]
    wide = variant == "rush"
    for index, (cx, cy) in enumerate(EYES):
        rx = (9 if index == 0 else 11) + (2 if wide else 0)
        ry = 12 + (3 if wide else 0)
        if lid >= 1.0:
            layer.stroke(arc(cx, cy + 3, rx, 5, 200, 340), 4)
            continue
        layer.ellipse(cx, cy, rx, ry, WHITE[:3], outline=False)
        layer.stroke(arc(cx, cy, rx, ry, 0, 360), 3)
        layer.ellipse(cx + 3, cy + 2 + lid * 4, 5, 6, INK[:3], outline=False)
        if lid > 0:
            # pálpebra: cobre a parte de cima do olho na cor da pele
            layer.chord(cx, cy, rx + 1, ry + 1, 180 + (1 - lid) * 70, 360 - (1 - lid) * 70,
                        c["skin"])
            y = cy - ry + 2 * ry * lid * 0.62
            layer.stroke([(cx - rx, y), (cx + rx, y)], 4)
    # sobrancelhas: baixas e retas (atitude), franzidas no esforço, altas no pico
    brow = {"open": 0, "blink": 0, "effort": 5, "sleepy": -2, "rush": -9}[variant]
    tilt = 5 if variant == "effort" else (-3 if variant == "sleepy" else 2)
    for (cx, cy), side in zip(EYES, (-1, 1)):
        layer.stroke([(cx - 12, cy - 21 + brow + tilt * side * -1),
                      (cx + 12, cy - 21 + brow + tilt * side)], 5, c["hair"])
    layer.stroke(arc(180, 146, 7, 6, 250, 420), 3)  # nariz
    if variant == "sleepy":
        layer.ellipse(160, 166, 9, 11, INK[:3], outline=False)       # bocejo
    elif variant == "effort":
        layer.stroke([(146, 166), (158, 163), (172, 166)], 4)
    elif variant == "rush":
        layer.chord(158, 160, 17, 13, 0, 180, INK[:3])               # sorriso aberto
        layer.stroke([(143, 161), (173, 161)], 3, CREAM)
    else:
        layer.stroke(arc(160, 158, 14, 9, 20, 120), 4)                # meio-sorriso
    return layer.image()


def hair_back(name, c):
    layer = Layer()
    if name == "zuri":
        layer.ellipse(98, 66, 31, 30, c["hair"])    # coque de trás
        layer.ellipse(156, 54, 28, 27, c["hair"])   # coque da frente
        layer.ellipse(122, 120, 58, 57, c["hair"])  # nuca
    else:
        layer.ellipse(120, 118, 58, 57, c["hair"])
        for x, y, dx, dy in ((84, 92, -22, -26), (104, 72, -8, -34), (132, 66, 10, -34),
                             (160, 74, 26, -24), (74, 122, -30, -8)):
            layer.polygon([(x - 13, y + 8), (x + dx, y + dy), (x + 14, y + 6)], c["hair"])
    return layer.image()


def hair_front(name, c):
    layer = Layer()
    if name == "zuri":
        # linha do cabelo: uma calota sobre a testa
        layer.polygon(arc(128, 128, HEAD_R + 1, HEAD_R, 196, 338) + [(176, 104), (132, 96), (84, 110)],
                      c["hair"])
    else:
        layer.polygon(arc(128, 128, HEAD_R + 1, HEAD_R, 190, 336) + [(178, 108), (160, 96),
                      (150, 112), (134, 94), (120, 112), (104, 96), (82, 116)], c["hair"])
    return layer.image()


def band(c):
    """Faixa de cabeça na cor do jogador: a base do indicador de Bem-Estar."""
    layer = Layer()
    outer = arc(128, 128, HEAD_R + 3, HEAD_R + 2, 186, 352)
    inner = arc(128, 136, HEAD_R + 3, HEAD_R - 6, 352, 186)
    layer.polygon(outer + inner, c["player"])
    layer.ellipse(76, 124, 9, 10, c["player_shade"])  # nó, atrás da cabeça
    return layer.image()


def tail(c):
    """Uma ponta da faixa: sai do nó (pivô à direita) e afina para a esquerda."""
    layer = Layer()
    layer.polygon([(200, 119), (152, 108), (118, 112), (126, 124), (112, 138), (154, 142),
                   (200, 137)], c["player"])
    layer.stroke([(190, 128), (150, 126)], 3, c["player_shade"])
    return layer.image()


# --------------------------------------------------------------------------
# Tronco, mão e tênis
# --------------------------------------------------------------------------
def torso(name, c, rig):
    layer = Layer()
    hip = PARTS["torso"][1]
    neck = hip - rig["neck"] * CELL
    # bermuda: do quadril para baixo, com listra lateral
    layer.polygon([(98, hip - 14), (160, hip - 14), (164, hip + 38), (132, hip + 40),
                   (128, hip + 24), (122, hip + 40), (94, hip + 36)], c["shorts"])
    layer.stroke([(152, hip - 6), (155, hip + 30)], 5, CREAM)
    # regata: ombros largos, cintura mais estreita
    layer.polygon([(100, neck + 8), (116, neck), (140, neck), (158, neck + 10),
                   (160, hip - 26), (156, hip - 6), (102, hip - 6), (98, hip - 26)], c["player"])
    layer.stroke([(150, neck + 16), (152, hip - 12)], 5, CREAM)          # faixa lateral
    layer.stroke(arc(128, neck + 2, 12, 10, 20, 160), 4, c["player_shade"])  # gola
    draw = layer._fill
    if c["number"] == "1":
        draw.line([px(124), px(neck + 34), px(130), px(neck + 28), px(130), px(neck + 58)],
                  fill=WHITE, width=px(6))
    else:
        points = arc(128, neck + 36, 9, 8, 200, 380) + [(119, neck + 58), (139, neck + 58)]
        draw.line([(px(x), px(y)) for x, y in points], fill=WHITE, width=px(6), joint="curve")
    # pescoço, por cima da gola
    layer.polygon([(120, neck - 14), (136, neck - 14), (137, neck + 6), (119, neck + 6)], c["skin"])
    return layer.image()


def hand(c):
    layer = Layer()
    layer.polygon([(114, 110), (142, 110), (143, 126), (113, 126)], c["player"])  # munhequeira
    layer.ellipse(128, 142, 16, 16, c["skin"])                                     # punho fechado
    layer.stroke(arc(130, 144, 9, 8, 300, 420), 3)
    return layer.image()


def shoe(c):
    layer = Layer()
    ax, ay = PARTS["shoe"]
    layer.polygon([(ax - 13, ay - 20), (ax + 13, ay - 20), (ax + 14, ay + 2), (ax - 14, ay + 2)],
                  CREAM[:3])                                                      # meia
    layer.polygon([(ax - 22, ay + 2), (ax + 18, ay), (ax + 50, ay + 14), (ax + 62, ay + 30),
                   (ax + 58, ay + 40), (ax - 24, ay + 40)], c["player"])           # cabedal
    layer.polygon([(ax - 26, ay + 34), (ax + 62, ay + 34), (ax + 60, ay + 46), (ax - 24, ay + 46)],
                  CREAM[:3])                                                      # sola
    layer.stroke([(ax + 6, ay + 12), (ax + 30, ay + 22)], 4, CREAM)                # cadarço
    return layer.image()


def build_cells():
    cells = {}
    for name, c in CHARACTERS.items():
        cells[name] = {
            "head": head(c),
            "face": face(c, "open"),
            "face_blink": face(c, "blink"),
            "face_effort": face(c, "effort"),
            "face_sleepy": face(c, "sleepy"),
            "face_rush": face(c, "rush"),
            "hair_back": hair_back(name, c),
            "hair_front": hair_front(name, c),
            "band": band(c),
            "tail": tail(c),
            "torso": torso(name, c, RIGS[name]),
            "hand": hand(c),
            "shoe": shoe(c),
        }
    return cells


# --------------------------------------------------------------------------
# Conferência: monta o personagem parado, com os membros como arcos
# --------------------------------------------------------------------------
def limb(draw, origin, scale, a, b, length, bend, width, color):
    """Arco de círculo entre a e b, a mesma conta de HoseCurve.Solve."""
    ax, ay = a
    bx, by = b
    d = math.hypot(bx - ax, by - ay)
    points = [a, b]
    if 1e-5 < d < length:
        max_half = math.radians(100)
        wanted = length / d
        half = max_half
        if wanted < max_half / math.sin(max_half):
            low, high = 0.0, max_half
            for _ in range(24):
                mid = (low + high) / 2
                if mid / math.sin(mid) < wanted:
                    low = mid
                else:
                    high = mid
            half = (low + high) / 2
        side = -1 if bend < 0 else 1
        ux, uy = (bx - ax) / d, (by - ay) / d
        nx, ny = -uy * side, ux * side
        radius = d / (2 * math.sin(half))
        cx = (ax + bx) / 2 - nx * radius * math.cos(half)
        cy = (ay + by) / 2 - ny * radius * math.cos(half)
        start = math.atan2(ay - cy, ax - cx)
        sweep = -side * 2 * half
        points = [(cx + radius * math.cos(start + sweep * i / 16),
                   cy + radius * math.sin(start + sweep * i / 16)) for i in range(17)]
    to_px = [(origin[0] + x * CELL * scale, origin[1] - y * CELL * scale) for x, y in points]
    for extra, shift, fill in ((INK_THIN + INK_HEAVY, (INK_HEAVY - INK_THIN) / 2, INK),
                               (0, 0, rgba(color))):
        w = (width * CELL + extra) * scale
        moved = [(x + shift * scale, y + shift * scale) for x, y in to_px]
        draw.line(moved, fill=fill, width=int(w), joint="curve")
        for x, y in (moved[0], moved[-1]):
            draw.ellipse([x - w / 2, y - w / 2, x + w / 2, y + w / 2], fill=fill)


def assemble(name, cells, face_name="face", tails=(-28, -8), scale=1.0):
    c, rig = CHARACTERS[name], RIGS[name]
    size = int(520 * scale)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    origin = (size / 2, size - 24 * scale)

    def paste(part, at, angle=0.0):
        nonlocal canvas, draw
        image = cells[part]
        pivot = PARTS[part]
        pad = Image.new("RGBA", (CELL * 2, CELL * 2), (0, 0, 0, 0))
        pad.paste(image, (CELL - pivot[0], CELL - pivot[1]))
        pad = pad.rotate(angle, resample=Image.BICUBIC)
        if scale != 1.0:
            pad = pad.resize((int(pad.width * scale), int(pad.height * scale)), Image.LANCZOS)
        layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        layer.paste(pad, (int(origin[0] + at[0] * CELL * scale - pad.width / 2),
                          int(origin[1] - at[1] * CELL * scale - pad.height / 2)))
        canvas = Image.alpha_composite(canvas, layer)
        draw = ImageDraw.Draw(canvas)

    hip = (0.0, rig["hip"])
    neck = (0.0, rig["hip"] + rig["neck"])
    head_at = (0.0, neck[1] + rig["head"])
    shoulders = ((-0.07, rig["hip"] + rig["shoulder"]), (0.07, rig["hip"] + rig["shoulder"]))
    hips = ((-0.03, rig["hip"]), (0.03, rig["hip"]))
    arm_w, leg_w = 0.09, 0.11

    limb(draw, origin, scale, shoulders[0], rig["hands"][0], rig["arm"], -1, arm_w, c["skin_shade"])
    paste("hand", rig["hands"][0])
    limb(draw, origin, scale, hips[0], rig["feet"][0], rig["leg"], 1, leg_w, c["skin_shade"])
    paste("shoe", rig["feet"][0])
    limb(draw, origin, scale, hips[1], rig["feet"][1], rig["leg"], 1, leg_w, c["skin"])
    paste("shoe", rig["feet"][1])
    paste("torso", hip)
    knot = (head_at[0] - 0.20, head_at[1] - 0.02)
    paste("tail", knot, tails[0])
    paste("tail", knot, tails[1])
    paste("hair_back", head_at)
    paste("head", head_at)
    paste(face_name, head_at)
    paste("hair_front", head_at)
    paste("band", head_at)
    limb(draw, origin, scale, shoulders[1], rig["hands"][1], rig["arm"], -1, arm_w, c["skin"])
    paste("hand", rig["hands"][1])
    return canvas


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview")
    args = parser.parse_args()

    cells = build_cells()
    sheet = Image.new("RGBA", (CELL * len(COLUMNS), CELL * len(CHARACTERS)), (0, 0, 0, 0))
    for row, name in enumerate(CHARACTERS):
        for col, column in enumerate(COLUMNS):
            sheet.paste(cells[name][column], (col * CELL, row * CELL))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT, optimize=True)
    print("sheet", OUT, sheet.size)

    if args.preview:
        # ângulo das pontas da faixa: negativo levanta, positivo deixa cair
        variants = [("face", (-28, -8)), ("face_blink", (-28, -8)), ("face_effort", (10, 30)),
                    ("face_sleepy", (40, 60)), ("face_rush", (-50, -30))]
        board = Image.new("RGBA", (520 * len(variants), 520 * len(CHARACTERS)),
                          (0xE8, 0xF1, 0xE4, 255))
        for row, name in enumerate(CHARACTERS):
            for col, (face_name, tails) in enumerate(variants):
                board.alpha_composite(assemble(name, cells[name], face_name, tails),
                                      (col * 520, row * 520))
        board.save(args.preview)
        print("preview", args.preview)


if __name__ == "__main__":
    main()
