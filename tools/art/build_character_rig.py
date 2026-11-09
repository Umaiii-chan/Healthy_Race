"""Gera a folha de partes do personagem articulado (VITA/SALO).

Uso:
    python tools/art/build_character_rig.py [--preview preview.png]

Saída: Assets/_Game/Art/Characters/SP_CharacterRig.png (2048 x 1280).

O personagem original era um desenho único por estado de Bem-Estar, então só
dava para esticar e balançar a imagem inteira. Aqui o mesmo desenho (cor chapada
e contorno preto grosso) é refeito em partes separadas, para que pernas, braços,
cabeça e folha se movam de forma independente.

Grade: 8 colunas x 5 linhas de células de 256 px, a 256 px por unidade do jogo.
    colunas: cabeça, rosto, rosto piscando, rosto de esforço, folha, tronco,
             braço, perna
    linhas:  saudável, estável, cansado, crítico, salo

Os pivôs e as posições de montagem ficam em PARTS e RIG, e são repetidos em
VisualAssetBuilder.CharacterRig*. Mudou aqui, mude lá.
"""
import argparse
import math
from pathlib import Path

from PIL import Image, ImageDraw

CELL = 256
SS = 4  # supersampling
OUTLINE = 9
INK = (22, 22, 22, 255)
WHITE = (255, 255, 255, 255)

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "Assets/_Game/Art/Characters/SP_CharacterRig.png"

COLUMNS = ["head", "face", "face_blink", "face_effort", "leaf", "torso", "arm", "leg"]

# pivô de cada parte em px da célula (origem no canto superior esquerdo)
PARTS = {
    "head": (128, 128),
    "face": (128, 128),
    "face_blink": (128, 128),
    "face_effort": (128, 128),
    "leaf": (128, 236),
    "torso": (128, 128),
    "arm": (128, 48),
    "leg": (128, 48),
}

# posição do pivô de cada parte no personagem montado, em unidades do jogo,
# com a origem entre os pés (y para cima)
RIG = {
    "leg_back": (-0.11, 0.25),
    "leg_front": (0.11, 0.25),
    "arm_back": (-0.18, 0.47),
    "torso": (0.0, 0.36),
    "leaf": (-0.02, 1.22),
    "head": (0.0, 0.85),
    "face": (0.0, 0.85),
    "arm_front": (0.18, 0.47),
}

SKINS = [
    # nome, pele, folha, nervura, humor, (base, controle, ponta) da lâmina da folha
    ("healthy", (156, 240, 84), (60, 204, 48), (24, 150, 36), "happy",
     ((138, 190), (146, 84), (224, 34), 47)),
    ("stable", (126, 204, 84), (60, 192, 48), (30, 140, 40), "calm",
     ((138, 190), (150, 90), (222, 46), 44)),
    ("tired", (174, 174, 36), (160, 160, 28), (112, 112, 20), "tired",
     ((140, 178), (168, 80), (228, 104), 38)),
    ("critical", (252, 96, 96), (168, 96, 48), (120, 66, 30), "sad",
     ((142, 162), (196, 70), (226, 170), 34)),
    ("salo", (132, 0, 0), (144, 84, 36), (100, 56, 22), "grim",
     ((142, 158), (202, 76), (220, 190), 31)),
]


def px(v):
    return v * SS


class Layer:
    """Desenha formas duas vezes: expandidas em preto (contorno) e no tamanho real."""

    def __init__(self):
        self.ink = Image.new("RGBA", (CELL * SS, CELL * SS), (0, 0, 0, 0))
        self.fill = Image.new("RGBA", (CELL * SS, CELL * SS), (0, 0, 0, 0))
        self._ink = ImageDraw.Draw(self.ink)
        self._fill = ImageDraw.Draw(self.fill)

    def ellipse(self, cx, cy, rx, ry, color, outline=OUTLINE):
        if outline:
            self._ellipse(self._ink, cx, cy, rx + outline, ry + outline, INK)
        self._ellipse(self._fill, cx, cy, rx, ry, color)

    def capsule(self, x0, y0, x1, y1, width, color, outline=OUTLINE):
        if outline:
            self._capsule(self._ink, x0, y0, x1, y1, width + 2 * outline, INK)
        self._capsule(self._fill, x0, y0, x1, y1, width, color)

    def polygon(self, points, color, outline=OUTLINE):
        scaled = [(px(x), px(y)) for x, y in points]
        if outline:
            self._ink.polygon(scaled, fill=INK)
            self._ink.line(scaled + [scaled[0]], fill=INK, width=px(2 * outline), joint="curve")
            for x, y in scaled:
                r = px(outline)
                self._ink.ellipse([x - r, y - r, x + r, y + r], fill=INK)
        self._fill.polygon(scaled, fill=color)

    def rounded(self, cx, cy, w, h, radius, color, outline=OUTLINE):
        if outline:
            self._rounded(self._ink, cx, cy, w + 2 * outline, h + 2 * outline,
                          radius + outline, INK)
        self._rounded(self._fill, cx, cy, w, h, radius, color)

    def stroke(self, points, width, color):
        """Linha solta (boca, pálpebra, nervura), sem contorno."""
        scaled = [(px(x), px(y)) for x, y in points]
        self._fill.line(scaled, fill=color, width=px(width), joint="curve")
        r = px(width) / 2
        for x, y in (scaled[0], scaled[-1]):
            self._fill.ellipse([x - r, y - r, x + r, y + r], fill=color)

    def pie(self, cx, cy, rx, ry, start, end, color):
        self._fill.pieslice(
            [px(cx - rx), px(cy - ry), px(cx + rx), px(cy + ry)], start, end, fill=color
        )

    def image(self):
        merged = Image.alpha_composite(self.ink, self.fill)
        return merged.resize((CELL, CELL), Image.LANCZOS)

    @staticmethod
    def _ellipse(draw, cx, cy, rx, ry, color):
        draw.ellipse([px(cx - rx), px(cy - ry), px(cx + rx), px(cy + ry)], fill=color)

    @staticmethod
    def _capsule(draw, x0, y0, x1, y1, width, color):
        draw.line([px(x0), px(y0), px(x1), px(y1)], fill=color, width=px(width))
        r = width / 2
        for x, y in ((x0, y0), (x1, y1)):
            draw.ellipse([px(x - r), px(y - r), px(x + r), px(y + r)], fill=color)

    @staticmethod
    def _rounded(draw, cx, cy, w, h, radius, color):
        draw.rounded_rectangle(
            [px(cx - w / 2), px(cy - h / 2), px(cx + w / 2), px(cy + h / 2)],
            radius=px(radius),
            fill=color,
        )


def rgba(color):
    return (*color, 255)


def shade(color, factor):
    return tuple(max(0, min(255, round(c * factor))) for c in color)


def bezier(p0, p1, p2, t):
    u = 1 - t
    return (
        u * u * p0[0] + 2 * u * t * p1[0] + t * t * p2[0],
        u * u * p0[1] + 2 * u * t * p1[1] + t * t * p2[1],
    )


def arc(cx, cy, rx, ry, start, end, steps=24):
    return [
        (
            cx + rx * math.cos(math.radians(start + (end - start) * i / steps)),
            cy + ry * math.sin(math.radians(start + (end - start) * i / steps)),
        )
        for i in range(steps + 1)
    ]


# --------------------------------------------------------------------------
# Partes
# --------------------------------------------------------------------------
def head(skin):
    layer = Layer()
    layer.ellipse(128, 128, 98, 98, rgba(skin))
    # brilho discreto no alto à esquerda, como reflexo de casca de fruta
    layer.stroke(arc(128, 128, 76, 76, 205, 250), 10, rgba(shade(skin, 1.12)))
    return layer.image()


EYES = ((89, 116), (167, 116))


def eye(layer, cx, cy, skin, lid=0.0, look=(2, 3)):
    layer.ellipse(cx, cy, 24, 26, WHITE, outline=6)
    layer.ellipse(cx + look[0], cy + look[1], 10, 11, INK, outline=0)
    layer.ellipse(cx + look[0] - 4, cy + look[1] - 4, 4, 4, WHITE, outline=0)
    if lid > 0:
        # pálpebra: segmento de cima do olho (contorno incluído) na cor da pele
        edge = cy - 26 + 52 * lid
        rx, ry = 30, 32
        theta = math.degrees(math.asin(max(-1.0, min(1.0, (edge - cy) / ry))))
        layer._fill.chord(
            [px(cx - rx), px(cy - ry), px(cx + rx), px(cy + ry)],
            180 - theta,
            360 + theta,
            fill=rgba(skin),
        )
        layer.stroke([(cx - 26, edge), (cx + 26, edge)], 7, INK)


def closed_eye(layer, cx, cy, happy):
    if happy:
        layer.stroke(arc(cx, cy + 10, 22, 17, 200, 340), 7, INK)
    else:
        layer.stroke(arc(cx, cy - 6, 22, 15, 20, 160), 7, INK)


def mouth(layer, mood, effort=False):
    if effort:
        layer.ellipse(128, 172, 11, 13, INK, outline=0)
        return
    if mood == "happy":
        layer.pie(128, 158, 30, 30, 0, 180, INK)
        layer.pie(128, 173, 17, 13, 0, 180, (226, 60, 70, 255))
    elif mood == "calm":
        layer.stroke(arc(128, 160, 22, 13, 25, 155), 7, INK)
    elif mood == "tired":
        layer.stroke([(110, 174), (146, 174)], 7, INK)
    elif mood == "sad":
        layer.stroke(arc(128, 186, 20, 14, 205, 335), 7, INK)
    else:
        layer.stroke(arc(128, 190, 24, 17, 205, 335), 8, INK)


LIDS = {"happy": 0.0, "calm": 0.0, "tired": 0.42, "sad": 0.5, "grim": 0.56}


def clip_to_head(image):
    """Nada do rosto pode sair do círculo da cabeça (pálpebras vazam)."""
    mask = Image.new("L", (CELL * SS, CELL * SS), 0)
    ImageDraw.Draw(mask).ellipse([px(128 - 96), px(128 - 96), px(128 + 96), px(128 + 96)],
                                 fill=255)
    mask = mask.resize((CELL, CELL), Image.LANCZOS)
    alpha = Image.composite(image.getchannel("A"), Image.new("L", image.size, 0), mask)
    image.putalpha(alpha)
    return image


def face(skin, mood, variant):
    layer = Layer()
    if variant == "blink":
        for cx, cy in EYES:
            closed_eye(layer, cx, cy, happy=False)
        mouth(layer, mood)
    elif variant == "effort":
        for i, (cx, cy) in enumerate(EYES):
            eye(layer, cx, cy, skin, lid=max(0.3, LIDS[mood]), look=(3, 4))
            # sobrancelha inclinada para dentro
            inner = 1 if i == 0 else -1
            layer.stroke([(cx - 24 * inner, cy - 42), (cx + 22 * inner, cy - 30)], 8, INK)
        mouth(layer, mood, effort=True)
    else:
        for cx, cy in EYES:
            eye(layer, cx, cy, skin, lid=LIDS[mood])
        if mood in ("sad", "grim"):
            for i, (cx, cy) in enumerate(EYES):
                inner = 1 if i == 0 else -1
                layer.stroke([(cx - 22 * inner, cy - 34), (cx + 20 * inner, cy - 44)], 7, INK)
        mouth(layer, mood)
    return clip_to_head(layer.image())


def leaf(color, rib, blade):
    base, control, tip, half_width = blade
    layer = Layer()
    # talo: do pivô (128, 236) até a base da lâmina
    layer.capsule(128, 236, base[0], base[1], 9, rgba(shade(color, 0.8)))
    left, right = [], []
    steps = 26
    for i in range(steps + 1):
        t = i / steps
        x, y = bezier(base, control, tip, t)
        nx, ny = bezier(base, control, tip, min(1.0, t + 0.01))
        dx, dy = nx - x, ny - y
        length = math.hypot(dx, dy) or 1.0
        width = half_width * math.sin(math.pi * t) ** 0.75
        left.append((x - dy / length * width, y + dx / length * width))
        right.append((x + dy / length * width, y - dx / length * width))
    layer.polygon(left + right[::-1], rgba(color))
    layer.stroke([bezier(base, control, tip, 0.08 + 0.74 * i / 12) for i in range(13)], 5,
                 rgba(rib))
    return layer.image()


def torso(skin):
    layer = Layer()
    layer.rounded(128, 128, 88, 86, 32, rgba(skin))
    return layer.image()


def arm(skin):
    layer = Layer()
    layer.capsule(128, 48, 128, 102, 30, rgba(skin))
    return layer.image()


def leg(skin):
    layer = Layer()
    layer.capsule(128, 48, 128, 88, 36, rgba(skin))
    layer.ellipse(134, 94, 24, 14, rgba(skin))
    # o pé e a perna são uma forma só: apaga o contorno entre eles
    layer.capsule(128, 60, 128, 88, 36, rgba(skin), outline=0)
    return layer.image()


def build_cells():
    cells = {}
    for name, skin, leaf_color, rib, mood, blade in SKINS:
        cells[name] = {
            "head": head(skin),
            "face": face(skin, mood, "open"),
            "face_blink": face(skin, mood, "blink"),
            "face_effort": face(skin, mood, "effort"),
            "leaf": leaf(leaf_color, rib, blade),
            "torso": torso(skin),
            "arm": arm(skin),
            "leg": leg(skin),
        }
    return cells


def assemble(parts, pose=None, scale=1.0):
    """Monta o personagem parado para conferência visual (mesma conta do jogo)."""
    pose = pose or {}
    size = int(640 * scale)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    origin = (size / 2, size - 40 * scale)
    order = ["arm_back", "leg_back", "leg_front", "torso", "leaf", "head", "face", "arm_front"]
    for slot in order:
        part = slot.split("_")[0] if slot.startswith(("arm", "leg")) else slot
        image = parts[pose.get("face", "face")] if slot == "face" else parts[part]
        pivot = PARTS[part]
        rest = {"arm_back": -18, "arm_front": 18}.get(slot, 0)
        angle = 0 if slot == "face" else pose.get(slot, rest)
        x, y = RIG[slot]
        if scale != 1.0:
            image = image.resize((int(CELL * scale), int(CELL * scale)), Image.LANCZOS)
        pad = Image.new("RGBA", (image.width * 2, image.height * 2), (0, 0, 0, 0))
        pad.paste(image, (int(image.width - pivot[0] * scale), int(image.height - pivot[1] * scale)))
        pad = pad.rotate(angle, resample=Image.BICUBIC)
        layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        layer.paste(
            pad,
            (int(origin[0] + x * CELL * scale - pad.width / 2),
             int(origin[1] - y * CELL * scale - pad.height / 2)),
        )
        canvas = Image.alpha_composite(canvas, layer)
    return canvas


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview")
    args = parser.parse_args()

    cells = build_cells()
    sheet = Image.new("RGBA", (CELL * len(COLUMNS), CELL * len(SKINS)), (0, 0, 0, 0))
    for row, (name, *_rest) in enumerate(SKINS):
        for col, column in enumerate(COLUMNS):
            sheet.paste(cells[name][column], (col * CELL, row * CELL))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT, optimize=True)
    print("sheet", OUT, sheet.size)

    if args.preview:
        poses = [
            {},
            {"face": "face_blink"},
            {"face": "face_effort", "arm_front": 80, "arm_back": 70},
            {"leg_front": 32, "leg_back": -32, "arm_front": -30, "arm_back": 30},
            {"arm_front": 150, "arm_back": -150, "leg_front": 14, "leg_back": -22},
        ]
        board = Image.new("RGBA", (640 * len(poses) // 2, 640 * len(SKINS) // 2),
                          (120, 190, 235, 255))
        for row, (name, *_rest) in enumerate(SKINS):
            for col, pose in enumerate(poses):
                board.alpha_composite(assemble(cells[name], pose, 0.5), (col * 320, row * 320))
        board.save(args.preview)
        print("preview", args.preview)


if __name__ == "__main__":
    main()
