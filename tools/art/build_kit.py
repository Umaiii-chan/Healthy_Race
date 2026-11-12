"""Gera a arte PROVISÓRIA do kit aprovado em 11/11/2026 (Alimentos 1, HUD 5, Cenário 3).

Uso:
    python tools/art/build_kit.py [--preview previa.png] [--only foods|hud|...]

Cada peça é um desenho vetorial (SVG) no traço da página de propostas
(docs/arte/propostas-2026-11-11.html): contorno de tinta verde-escura, cópia da forma
deslocada como sombra de tinta, cor chapada com um tom claro por dentro. O SVG é
rasterizado pelo Chrome sem interface (fundo transparente) e fatiado em PNG.

É arte de apoio, para o jogo funcionar com a direção aprovada: o acabamento final é do
ilustrador, nos mesmos arquivos e tamanhos.

Precisa do Google Chrome ou do Microsoft Edge instalado (variável CHROME para outro caminho).
"""
import argparse
import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
KIT = Path(__file__).resolve().parent / "kit"
ART = REPO / "Assets/_Game/Art"

INK = "#1E2A22"
CREAM = "#F8F3DC"

BROWSERS = [
    os.environ.get("CHROME", ""),
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    "/usr/bin/google-chrome",
    "/usr/bin/chromium",
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
]


# --------------------------------------------------------------------------- traço


def ink(d, base, light=None, inset=(-2.5, -1.5, 0.86), sw=1.6, shadow=True):
    """Uma forma no traço do kit: sombra de tinta, cor base, tom claro por dentro, contorno."""
    parts = []
    if shadow:
        parts.append(
            f'<g transform="translate(1.3,1.9)"><path d="{d}" fill="{INK}" stroke="{INK}" '
            f'stroke-width="3.4" stroke-linejoin="round"/></g>'
        )
    parts.append(f'<path d="{d}" fill="{base}"/>')
    if light:
        dx, dy, scale = inset
        parts.append(
            f'<g transform="translate({dx},{dy}) scale({scale})"><path d="{d}" fill="{light}"/></g>'
        )
    parts.append(
        f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="{sw}" stroke-linejoin="round"/>'
    )
    return "".join(parts)


def line(d, color=INK, width=3.0, opacity=None):
    extra = f' opacity="{opacity}"' if opacity else ""
    return (
        f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{width}" '
        f'stroke-linecap="round" stroke-linejoin="round"{extra}/>'
    )


def dot(x, y, r, fill, stroke=None):
    s = f' stroke="{INK}" stroke-width="{stroke}"' if stroke else ""
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"{s}/>'


def circle_path(cx, cy, r):
    return f"M{cx - r},{cy} a{r},{r} 0 1,0 {2 * r},0 a{r},{r} 0 1,0 {-2 * r},0 Z"


LEAF_TOP = "M4,-22 C9,-33 22,-33 25,-27 C18,-19 9,-19 4,-22 Z"

# --------------------------------------------------------------------------- alimentos

# suporte: o natural pousa numa folha (base curva); o lanche vem na bandeja de papel
# listrada (base reta), a mesma do piso do corredor da cantina
LEAF_SUPPORT = "M-42,28 C-36,10 -10,6 24,12 C40,15 46,24 40,32 C10,40 -24,40 -42,28 Z"
TRAY_SUPPORT = "M-42,22 L42,22 L36,38 L-36,38 Z"


def support(natural):
    if natural:
        return ink(LEAF_SUPPORT, "#5DB84A", sw=1.44) + line(
            "M-34,30 C-8,24 16,24 36,28", "#2E7D2A", 2
        )
    stripes = "".join(
        f'<path d="M{x},23.5 L{x + 9},23.5 L{x + 7.5},36.5 L{x - 1.5},36.5 Z" fill="#E8423A"/>'
        for x in (-36, -18, 0, 18)
    )
    return (
        ink(TRAY_SUPPORT, "#fff", sw=1.44)
        + stripes
        + f'<path d="{TRAY_SUPPORT}" fill="none" stroke="{INK}" stroke-width="1.6"/>'
    )


def banana():
    body = "M-25,-10 C-22,12 2,24 26,10 C29,7 27,2 22,3 C8,10 -8,4 -15,-13 C-18,-18 -26,-16 -25,-10 Z"
    return (
        ink(body, "#D9A520", "#FFD84A", inset=(-1, -3, 0.9))
        + line("M-22,-13 l-3,-6", INK, 4)
        + line("M24,5 l4,-3", "#6B4B2B", 4)
        + line("M-14,-2 C-6,9 6,12 18,8", "#B8860B", 1.6, 0.7)
    )


def orange():
    return (
        ink(circle_path(0, 3, 23), "#D96F16", "#F59B3A")
        + line("M0,-20 Q1,-25 4,-28", INK, 3.2)
        + ink("M3,-23 C8,-33 20,-33 23,-27 C16,-20 8,-20 3,-23 Z", "#4CB83E")
        + "".join(dot(x, y, 1.2, "#B85A10") for x, y in ((-6, 6), (5, 12), (10, -2), (-2, -6)))
        + line("M-15,-5 Q-14,-11 -8,-13", "#fff", 3, 0.6)
    )


def strawberry():
    body = "M0,27 C-16,16 -27,2 -24,-9 C-21,-19 -8,-19 0,-12 C8,-19 21,-19 24,-9 C27,2 16,16 0,27 Z"
    seeds = "".join(
        line(f"M{x},{y} l0,2.4", "#FFF3D1", 1.8)
        for x, y in ((-12, -4), (0, -2), (12, -4), (-7, 6), (7, 6), (0, 14))
    )
    calyx = "M-15,-12 L-7,-20 L0,-13 L7,-20 L15,-12 L6,-8 L0,-3 L-6,-8 Z"
    return ink(body, "#B8302B", "#E8423A") + seeds + ink(calyx, "#4CB83E")


def carrot():
    body = "M-13,-9 C-13,-17 13,-17 13,-9 C12,7 6,22 0,28 C-6,22 -12,7 -13,-9 Z"
    leaves = (
        ink("M0,-13 C-4,-23 -12,-27 -15,-30 C-6,-30 1,-24 0,-13 Z", "#4CB83E")
        + ink("M0,-13 C-3,-25 2,-32 4,-35 C7,-28 5,-20 0,-13 Z", "#5BC24A")
        + ink("M0,-13 C6,-21 12,-25 17,-26 C13,-19 7,-15 0,-13 Z", "#4CB83E")
    )
    return (
        leaves
        + ink(body, "#D0611A", "#F28C28")
        + line("M-7,-2 l8,0", "#A94E12", 1.8)
        + line("M-1,8 l7,0", "#A94E12", 1.8)
        + line("M-5,16 l6,0", "#A94E12", 1.8)
    )


def avocado():
    skin = "M0,-27 C10,-27 14,-15 16,-5 C20,8 16,26 0,26 C-16,26 -20,8 -16,-5 C-14,-15 -10,-27 0,-27 Z"
    flesh = "M0,-20 C7,-20 10,-11 12,-4 C15,7 12,20 0,20 C-12,20 -15,7 -12,-4 C-10,-11 -7,-20 0,-20 Z"
    return (
        ink(skin, "#3F7D2E")
        + f'<path d="{flesh}" fill="#D6E694"/>'
        + f'<path d="{flesh}" fill="none" stroke="#9DBB55" stroke-width="1.4"/>'
        + ink(circle_path(0, 8, 8.5), "#7A4A22", "#9C6230", shadow=False)
        + line("M-4,4 Q-3,1 0,1", "#fff", 2, 0.55)
    )


def fish():
    body = "M-27,2 C-19,-14 8,-16 18,0 L28,-11 L28,15 L18,4 C8,18 -19,16 -27,2 Z"
    return (
        ink(body, "#3F8CC6", "#7CC0EE", inset=(-1, -3, 0.9))
        + line("M-6,-9 C-2,-3 -2,5 -6,11", "#2C6A9C", 1.8)
        + dot(-16, -1, 3.6, "#fff", 1.2)
        + dot(-16.6, -1, 1.5, INK)
        + line("M4,-2 q4,3 8,0", "#2C6A9C", 1.6)
        + line("M2,5 q4,3 8,0", "#2C6A9C", 1.6)
    )


def watermelon():
    rind = "M-28,-7 L28,-7 C28,13 14,26 0,26 C-14,26 -28,13 -28,-7 Z"
    red = "M-23,-7 L23,-7 C23,9 12,20 0,20 C-12,20 -23,9 -23,-7 Z"
    seeds = "".join(
        f'<ellipse cx="{x}" cy="{y}" rx="1.6" ry="2.6" fill="{INK}"/>'
        for x, y in ((-12, 1), (0, 3), (12, 1), (-6, 10), (6, 10))
    )
    return (
        ink(rind, "#3E9A3A")
        + f'<path d="M-25,-7 L25,-7 C25,11 13,23 0,23 C-13,23 -25,11 -25,-7 Z" fill="#DDF0C0"/>'
        + f'<path d="{red}" fill="#E8423A"/>'
        + seeds
        + f'<path d="{rind}" fill="none" stroke="{INK}" stroke-width="1.6" stroke-linejoin="round"/>'
    )


def candy():
    left = "M-13,0 L-28,-11 L-25,0 L-28,11 Z"
    right = "M13,0 L28,-11 L25,0 L28,11 Z"
    body = "M-15,0 C-15,-16 15,-16 15,0 C15,16 -15,16 -15,0 Z"
    return (
        ink(left, "#F6A8D2")
        + ink(right, "#F6A8D2")
        + ink(body, "#D9479A", "#F06CB4")
        + line("M-6,-11 C-1,-4 -1,4 -6,11", "#fff", 2.6, 0.8)
        + line("M4,-12 C9,-4 9,4 4,12", "#fff", 2.6, 0.8)
    )


def chips():
    bag = "M-18,-25 L18,-25 L20,-19 L18,21 L20,27 L-20,27 L-18,21 L-20,-19 Z"
    return (
        ink(bag, "#D99A1C", "#F6C23A", inset=(-1.5, -1, 0.88))
        + line("M-18,-19 L18,-19", "#B37A12", 1.6)
        + line("M-18,21 L18,21", "#B37A12", 1.6)
        + ink(circle_path(0, 1, 10), "#E8423A", shadow=False)
        + f'<path d="M-6,3 C-4,-4 4,-4 6,3 C2,6 -2,6 -6,3 Z" fill="#FFE08A" stroke="{INK}" stroke-width="1"/>'
    )


def donut():
    ring = circle_path(0, 2, 24) + " " + "M-7,2 a7,7 0 1,1 14,0 a7,7 0 1,1 -14,0 Z"
    icing = (
        "M-21,-2 C-21,-22 21,-22 21,-2 C22,6 16,6 15,11 C13,16 8,10 4,14 C0,18 -5,12 -9,15 "
        "C-14,17 -15,9 -19,9 C-22,8 -21,3 -21,-2 Z M-7,2 a7,7 0 1,1 14,0 a7,7 0 1,1 -14,0 Z"
    )
    sprinkles = "".join(
        line(f"M{x},{y} l{dx},{dy}", color, 2.2)
        for x, y, dx, dy, color in (
            (-13, -8, 3, -2, "#FFE08A"),
            (-3, -15, 3, 1, "#7CC0EE"),
            (10, -10, 2, 3, "#FFF"),
            (14, 1, -3, 2, "#FFE08A"),
            (-15, 4, 2, 3, "#7CC0EE"),
        )
    )
    return (
        f'<g transform="translate(1.3,1.9)"><path d="{circle_path(0, 2, 24)}" fill="{INK}" '
        f'stroke="{INK}" stroke-width="3.4"/></g>'
        + f'<path d="{ring}" fill="#D9A05B" fill-rule="evenodd" stroke="{INK}" stroke-width="1.6"/>'
        + f'<path d="{icing}" fill="#F06CB4" fill-rule="evenodd" stroke="{INK}" stroke-width="1.3" '
        f'stroke-linejoin="round"/>'
        + sprinkles
    )


def hotdog():
    bun = "M-28,6 C-28,-6 28,-6 28,6 C28,18 -28,18 -28,6 Z"
    sausage = "M-30,-2 C-30,-10 30,-10 30,-2 C30,6 -30,6 -30,-2 Z"
    return (
        ink(bun, "#C98632", "#E9A24A", inset=(-1, -2, 0.92))
        + ink(sausage, "#A93A26", "#D4553A", inset=(-1, -1.5, 0.94))
        + line("M-22,-1 l6,-4 l6,4 l6,-4 l6,4 l6,-4 l6,4 l6,-4", "#FFD23F", 3)
    )


def pizza():
    cheese = "M-24,-17 C-8,-25 8,-25 24,-17 L0,28 Z"
    crust = "M-27,-15 C-9,-27 9,-27 27,-15 L22,-8 C8,-17 -8,-17 -22,-8 Z"
    pepperoni = "".join(
        dot(x, y, 4.2, "#C7332E", 1.1) for x, y in ((-8, -6), (8, -4), (0, 8))
    )
    return (
        ink(cheese, "#E0A92E", "#F6C445", inset=(-1, -1.5, 0.9))
        + pepperoni
        + ink(crust, "#C98632", "#E3A35E", inset=(0, -1.2, 0.94))
    )


NEW_FOODS = {
    "banana": banana,
    "orange": orange,
    "strawberry": strawberry,
    "carrot": carrot,
    "avocado": avocado,
    "fish": fish,
    "watermelon": watermelon,
    "candy": candy,
    "chips": chips,
    "donut": donut,
    "hotdog": hotdog,
    "pizza": pizza,
}

NATURAL = ["apple", "avocado", "banana", "broccoli", "carrot", "fish", "orange", "strawberry",
           "water", "watermelon"]
SNACK = ["burger", "candy", "chips", "donut", "fries", "hotdog", "pizza", "soda"]

FOOD_CELL = 256
FOOD_VIEW = 104  # unidades do desenho na célula; o suporte tem 88 de largura


def food_svg(name):
    """Alimento sobre o suporte, centrado na célula."""
    proposal = json.loads((KIT / "proposal_foods.json").read_text(encoding="utf-8"))
    if name in proposal:
        item = proposal[name]
    else:
        item = f'<g transform="translate(0,-8) scale(.9)">{NEW_FOODS[name]()}</g>'
    return f'<g transform="translate(0,2)">{support(name in NATURAL)}{item}</g>'


def food_icon_svg(name):
    """Só o alimento, sem suporte: ícone para placa, HUD e resultado."""
    proposal = json.loads((KIT / "proposal_foods.json").read_text(encoding="utf-8"))
    item = proposal[name] if name in proposal else f"<g>{NEW_FOODS[name]()}</g>"
    return f'<g transform="translate(0,10) scale(1.45)">{item}</g>'


# --------------------------------------------------------------------------- HUD 5

WOOD = "#E3A35E"
WOOD_LIGHT = "#F5C68A"
WOOD_DARK = "#B87A3E"
LEAF_PATH = "M0,12 C-11,5 -12,-8 1,-15 C12,-7 10,6 0,12 Z"
PIECE_SCALE = 2  # px por unidade do desenho: a interface é desenhada a 1920x1080


def leaf(x, y, angle, scale, fill, stroke=1.5):
    return (
        f'<g transform="translate({x},{y}) rotate({angle}) scale({scale})">'
        f'<path d="{LEAF_PATH}" fill="{fill}" stroke="{INK}" stroke-width="{stroke}" '
        f'stroke-linejoin="round"/>'
        f'<path d="M0,10 Q1,0 1,-11" fill="none" stroke="{INK}" stroke-width="{stroke * 0.55}" '
        f'stroke-linecap="round" opacity=".55"/></g>'
    )


def hud_sign():
    """Placa de madeira pendurada: tábua com filete de luz em cima e duas juntas."""
    return (
        f'<rect x="7" y="8" width="470" height="110" rx="18" fill="{INK}"/>'
        f'<rect x="4" y="4" width="470" height="110" rx="18" fill="{WOOD}" stroke="{INK}" '
        f'stroke-width="6"/>'
        f'<rect x="14" y="13" width="450" height="12" rx="6" fill="{WOOD_LIGHT}"/>'
        f'<path d="M10,46 L468,46 M10,82 L468,82" stroke="{WOOD_DARK}" stroke-width="3"/>'
        f'<path d="M170,46 L170,82 M330,13 L330,46 M300,82 L300,111" stroke="{WOOD_DARK}" '
        f'stroke-width="2.5"/>'
    )


def hud_clock():
    """Relógio de madeira: aro de tábua e mostrador creme, sem ponteiros (o tempo é texto)."""
    ticks = "".join(
        f'<path d="M64,26 L64,32" stroke="{INK}" stroke-width="3" stroke-linecap="round" '
        f'transform="rotate({a} 64 64)" opacity=".5"/>'
        for a in (0, 90, 180, 270)
    )
    return (
        f'<circle cx="66" cy="67" r="57" fill="{INK}"/>'
        f'<circle cx="64" cy="64" r="57" fill="{WOOD}" stroke="{INK}" stroke-width="6"/>'
        f'<path d="M24,44 A46,46 0 0,1 104,44" fill="none" stroke="{WOOD_LIGHT}" stroke-width="6" '
        f'stroke-linecap="round"/>'
        f'<circle cx="64" cy="64" r="42" fill="{CREAM}" stroke="{INK}" stroke-width="4"/>'
        + ticks
    )


def hud_vine(width=840):
    """Trepadeira da corrida: o caule ondula e as folhas alternam de lado."""
    waves = (width - 40) // 130
    d = "M20,34"
    x = 20
    for index in range(waves * 2):
        up = -1 if index % 2 == 0 else 1
        d += f" C{x + 22},{34 + 13 * up} {x + 43},{34 + 13 * up} {x + 65},34"
        x += 65
    d += f" L{width - 20},34"
    leaves = "".join(
        leaf(
            52 + 65 * i,
            34 + (-15 if i % 2 == 0 else 15),
            -40 if i % 2 == 0 else 140,
            0.95,
            "#4CC23E",
            2.2,
        )
        for i in range(waves * 2)
    )
    return (
        f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="13" stroke-linecap="round"/>'
        f'<path d="{d}" fill="none" stroke="#5BC24A" stroke-width="6.5" stroke-linecap="round"/>'
        + leaves
    )


def hud_goal():
    """A porta de chegada, no fim da trepadeira."""
    return (
        '<g transform="translate(28,38) scale(1.25)">'
        f'<rect x="-16" y="-26" width="32" height="44" rx="14" fill="#7BD34A" stroke="{INK}" '
        'stroke-width="4"/>'
        '<rect x="-9" y="-17" width="18" height="30" rx="8" fill="#2A3B4A" opacity=".55"/>'
        '<circle cx="6" cy="0" r="2.5" fill="#FFD23F"/></g>'
    )


def hud_tag():
    """Etiqueta creme para texto; a borda de 16 px é fatiada na importação."""
    return (
        f'<rect x="5" y="6" width="86" height="38" rx="10" fill="{INK}"/>'
        f'<rect x="3" y="3" width="86" height="38" rx="10" fill="{CREAM}" stroke="{INK}" '
        'stroke-width="4"/>'
    )


# nome: (desenho, largura, altura), em unidades do desenho
HUD_PIECES = {
    "hud_sign": (hud_sign, 482, 124),
    "hud_leaf_on": (lambda: leaf(32, 34, 20, 2.0, "#4CC23E"), 64, 64),
    "hud_leaf_off": (lambda: leaf(32, 34, 20, 2.0, "#C9B48F"), 64, 64),
    "hud_clock": (hud_clock, 128, 128),
    "hud_vine": (hud_vine, 840, 68),
    "hud_goal": (hud_goal, 56, 72),
    "hud_disk": (lambda: '<circle cx="32" cy="32" r="30" fill="#F6E7C8"/>', 64, 64),
    "hud_ring_ink": (
        lambda: f'<circle cx="64" cy="64" r="54" fill="none" stroke="{INK}" stroke-width="12"/>',
        128,
        128,
    ),
    "hud_ring_fill": (
        lambda: '<circle cx="64" cy="64" r="54" fill="none" stroke="#fff" stroke-width="7.6"/>',
        128,
        128,
    ),
    "hud_tag": (hud_tag, 96, 48),
}


def render_piece(content, width, height, scale=PIECE_SCALE):
    """Rasteriza um desenho com origem no canto de cima, à esquerda."""
    px = (int(width * scale), int(height * scale))
    html = (
        "<!doctype html><meta charset='utf-8'><style>html,body{margin:0;background:transparent;"
        "overflow:hidden}svg{display:block}</style><body>"
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" '
        f'width="{px[0]}" height="{px[1]}">{content}</svg></body>'
    )
    with tempfile.TemporaryDirectory() as folder:
        page = Path(folder) / "piece.html"
        shot = Path(folder) / "piece.png"
        page.write_text(html, encoding="utf-8")
        subprocess.run(
            [
                browser(),
                "--headless=new",
                "--disable-gpu",
                "--hide-scrollbars",
                "--force-device-scale-factor=1",
                "--default-background-color=00000000",
                f"--window-size={px[0]},{px[1]}",
                f"--screenshot={shot}",
                page.as_uri(),
            ],
            check=True,
            capture_output=True,
        )
        return Image.open(shot).convert("RGBA")


def build_pieces(pieces, scale=PIECE_SCALE):
    (ART / "Kit").mkdir(parents=True, exist_ok=True)
    paths = []
    for name, (draw, width, height) in pieces.items():
        path = ART / "Kit" / f"{name}.png"
        render_piece(draw(), width, height, scale).save(path)
        paths.append(path)
    return paths


# --------------------------------------------------------------------------- Cenário 3

MECHANISM = "#FFC83D"  # tudo o que se aciona é amarelo
DANGER = "#E8423A"  # tudo o que machuca é vermelho
ENV_SCALE = 2.56  # 100 unidades do desenho = 1 unidade do jogo = 256 px

ARROW = "M-24,-8 L4,-8 L4,-20 L26,0 L4,20 L4,8 L-24,8 Z"


def arrow(x, y, angle, fill):
    return (
        f'<g transform="translate({x},{y}) rotate({angle})">'
        f'<path d="{ARROW}" fill="{INK}" transform="translate(1.6,2.2)"/>'
        f'<path d="{ARROW}" fill="{fill}" stroke="{INK}" stroke-width="4" '
        f'stroke-linejoin="round"/></g>'
    )


def bolt_badge(x, y, r=13):
    return (
        f'<g transform="translate({x},{y})"><circle r="{r}" fill="{INK}"/>'
        f'<path d="M2,-9 L-6,2 L-1,2 L-3,9 L6,-3 L1,-3 Z" fill="#FFD23F"/></g>'
    )


def food_item(name, x, y, scale):
    proposal = json.loads((KIT / "proposal_foods.json").read_text(encoding="utf-8"))
    item = proposal[name] if name in proposal else f"<g>{NEW_FOODS[name]()}</g>"
    return f'<g transform="translate({x},{y}) scale({scale})">{item}</g>'


def board(x, y, width, height):
    return (
        f'<rect x="{x + 3}" y="{y + 4}" width="{width}" height="{height}" rx="14" fill="{INK}"/>'
        f'<rect x="{x}" y="{y}" width="{width}" height="{height}" rx="14" fill="{WOOD}" '
        f'stroke="{INK}" stroke-width="6"/>'
        f'<rect x="{x + 9}" y="{y + 8}" width="{width - 18}" height="9" rx="4.5" '
        f'fill="{WOOD_LIGHT}"/>'
    )


def ropes(xs, bottom):
    return "".join(
        f'<path d="M{x},0 L{x},{bottom}" stroke="#6B4B2B" stroke-width="6"/>' for x in xs
    )


def env_fork(long_angle):
    """Placa da bifurcação, sem texto: seta e alimento de cada caminho.

    Em cima o caminho longo (seta verde para o lado, alimento natural); embaixo o curto
    (seta amarela para baixo, lanche com o raio do impulso).
    """

    def draw():
        return (
            ropes((40, 140), 44)
            + board(5, 40, 170, 142)
            + f'<path d="M9,111 L171,111" stroke="{WOOD_DARK}" stroke-width="3"/>'
            + arrow(50, 80, long_angle, "#5BC24A")
            + food_item("apple", 124, 88, 0.92)
            + arrow(50, 147, 90, MECHANISM)
            + food_item("burger", 120, 157, 0.82)
            + bolt_badge(152, 128, 12)
        )

    return draw


def lever_art(x, y, scale=1.0):
    return (
        f'<g transform="translate({x},{y}) scale({scale})">'
        f'<path d="M-4,-2 L20,-40" stroke="{INK}" stroke-width="9" stroke-linecap="round"/>'
        f'<circle cx="21" cy="-42" r="11" fill="{DANGER}" stroke="{INK}" stroke-width="4"/>'
        f'<path d="M-30,10 C-30,-18 30,-18 30,10 Z" fill="{MECHANISM}" stroke="{INK}" '
        f'stroke-width="5" stroke-linejoin="round"/></g>'
    )


def env_lever_sign():
    """Placa que pede a alavanca, sem texto: a alavanca e a seta do puxão."""
    return (
        ropes((34, 126), 44)
        + board(5, 40, 150, 112)
        + lever_art(66, 124, 1.15)
        + '<g transform="translate(118,92) rotate(155)">'
        + f'<path d="M-22,0 C-10,-18 10,-18 20,-4" fill="none" stroke="{INK}" stroke-width="7" '
        'stroke-linecap="round"/>'
        + f'<path d="M12,-14 L24,-2 L8,4 Z" fill="{INK}"/></g>'
    )


def env_fast_badge():
    """Selo do corredor do lanche: no lugar do letreiro "FAST"."""
    return (
        f'<circle cx="52" cy="53" r="42" fill="{INK}"/>'
        f'<circle cx="50" cy="50" r="42" fill="{MECHANISM}" stroke="{INK}" stroke-width="5"/>'
        f'<circle cx="50" cy="50" r="33" fill="none" stroke="#FFE08A" stroke-width="3"/>'
        + food_item("burger", 48, 58, 0.95)
        + bolt_badge(78, 22, 14)
    )


def env_spikes():
    """Espinhos claros: quatro pontas com um lado na sombra."""
    out = []
    for index in range(4):
        x = 3 + index * 24
        out.append(
            f'<path d="M{x},50 L{x + 11},5 L{x + 22},50 Z" fill="#F4F1E4" stroke="{INK}" '
            f'stroke-width="3.2" stroke-linejoin="round"/>'
            f'<path d="M{x + 11},9 L{x + 19},48 L{x + 11},48 Z" fill="#C5CCC6"/>'
        )
    return "".join(out)


def stripes(width, height, color_a, color_b, step=16, lean=10):
    clip = f'<clipPath id="c"><rect x="2" y="2" width="{width - 4}" height="{height - 4}" rx="3"/></clipPath>'
    bars = "".join(
        f'<path d="M{x},{height} L{x + lean},0 L{x + lean + step / 2},0 L{x + step / 2},{height} Z" '
        f'fill="{color_a}"/>'
        for x in range(-step, int(width) + step, step)
    )
    return (
        f"<defs>{clip}</defs>"
        f'<rect x="2" y="2" width="{width - 4}" height="{height - 4}" rx="3" fill="{color_b}"/>'
        f'<g clip-path="url(#c)">{bars}</g>'
        f'<rect x="2" y="2" width="{width - 4}" height="{height - 4}" rx="3" fill="none" '
        f'stroke="{INK}" stroke-width="3.2"/>'
    )


def env_gate():
    """Grade do portão: duas barras escuras; ladrilha na vertical."""
    return (
        f'<rect x="8" y="0" width="13" height="100" fill="#33414E"/>'
        f'<rect x="8" y="0" width="4" height="100" fill="#55677A"/>'
        f'<rect x="29" y="0" width="13" height="100" fill="#33414E"/>'
        f'<rect x="29" y="0" width="4" height="100" fill="#55677A"/>'
        f'<path d="M8,0 L8,100 M21,0 L21,100 M29,0 L29,100 M42,0 L42,100" stroke="{INK}" '
        'stroke-width="2.4"/>'
    )


def env_lift():
    """Elevador: tábua em cima, corpo amarelo de mecanismo. Borda fatiada na importação."""
    return (
        f'<rect x="2" y="2" width="96" height="21" rx="5" fill="{MECHANISM}" stroke="{INK}" '
        'stroke-width="4"/>'
        f'<rect x="4" y="4" width="92" height="8" rx="3" fill="{WOOD}"/>'
        f'<path d="M4,12 L96,12" stroke="{INK}" stroke-width="2"/>'
    )


def env_hanging_pot():
    """Vaso pendurado, no valor do fundo: dá a escala vertical da estufa."""
    leaf_color = "#9CC5A8"
    blades = "".join(
        f'<path d="M60,92 C{60 + dx * 0.4},{70 - lift} {60 + dx},{66 - lift} {60 + dx * 1.2},{84 - lift} '
        f'C{60 + dx * 0.8},{80 - lift} {60 + dx * 0.3},{86} 60,92 Z" fill="{leaf_color}"/>'
        for dx, lift in ((-34, 0), (-18, 14), (0, 22), (18, 14), (34, 0))
    )
    tendrils = (
        f'<path d="M46,128 C40,150 52,168 44,196" fill="none" stroke="{leaf_color}" '
        'stroke-width="6" stroke-linecap="round"/>'
        f'<path d="M74,128 C82,146 70,160 78,180" fill="none" stroke="{leaf_color}" '
        'stroke-width="6" stroke-linecap="round"/>'
    )
    return (
        '<path d="M60,0 L60,74" stroke="#A9C4B8" stroke-width="3"/>'
        + tendrils
        + blades
        + '<path d="M34,90 L86,90 L78,130 L42,130 Z" fill="#D3B3A2"/>'
        + '<path d="M34,90 L86,90 L84,100 L36,100 Z" fill="#C4A292"/>'
    )


def env_light():
    """Faixa de luz pintada: borda dura, aponta para a saída."""
    return '<path d="M118,0 L200,0 L82,400 L0,400 Z" fill="#FFF8D8"/>'


def landmark_watering_can():
    return (
        '<path d="M110,110 L270,110 L286,270 L94,270 Z"/>'
        '<path d="M270,140 L366,70 L384,92 L286,196 Z"/>'
        '<path d="M356,48 L400,84 L388,100 L344,64 Z"/>'
        '<path d="M120,120 C20,110 20,250 104,236 L102,210 C56,220 56,140 118,146 Z"/>'
        '<path d="M130,110 C130,60 250,60 250,110 L226,110 C226,84 154,84 154,110 Z"/>'
    )


def landmark_wheelbarrow():
    return (
        '<path d="M70,120 L330,120 L290,220 L120,220 Z"/>'
        '<path d="M300,150 L396,96 L400,112 L310,170 Z"/>'
        '<circle cx="120" cy="250" r="44"/>'
        '<path d="M250,216 L300,290 L282,296 L232,224 Z"/>'
    )


def landmark_sunflower():
    petals = "".join(
        f'<ellipse cx="200" cy="52" rx="20" ry="46" transform="rotate({a} 200 110)"/>'
        for a in range(0, 360, 30)
    )
    return (
        petals
        + '<circle cx="200" cy="110" r="52"/>'
        + '<path d="M190,150 L210,150 L214,300 L186,300 Z"/>'
        + '<path d="M210,220 C260,180 310,200 300,240 C270,250 240,246 210,232 Z"/>'
        + '<path d="M190,250 C140,214 96,234 104,272 C134,282 164,276 190,262 Z"/>'
    )


def landmark_windmill():
    blades = "".join(
        f'<path d="M200,110 L214,20 L250,28 L226,112 Z" transform="rotate({a} 200 110)"/>'
        for a in (10, 100, 190, 280)
    )
    return (
        blades
        + '<circle cx="200" cy="110" r="20"/>'
        + '<path d="M176,120 L224,120 L250,300 L150,300 Z"/>'
    )


def landmark_water_tank():
    return (
        '<path d="M110,50 C110,20 290,20 290,50 L290,150 C290,176 110,176 110,150 Z"/>'
        '<path d="M130,160 L150,160 L140,300 L112,300 Z"/>'
        '<path d="M250,160 L270,160 L288,300 L260,300 Z"/>'
        '<path d="M134,220 L266,220 L268,236 L132,236 Z"/>'
        '<path d="M196,170 L212,170 L212,300 L196,300 Z"/>'
    )


def landmark(draw):
    return lambda: f'<g fill="#fff">{draw()}</g>'


# nome: (desenho, largura, altura), em centésimos de unidade do jogo
ENV_PIECES = {
    "env_fork_right": (env_fork(0), 182, 188),
    "env_fork_left": (env_fork(180), 182, 188),
    "env_lever_sign": (env_lever_sign, 162, 158),
    "env_fast_badge": (env_fast_badge, 100, 100),
    "env_spikes": (env_spikes, 100, 52),
    "env_hazard": (lambda: stripes(100, 30, DANGER, "#fff"), 100, 30),
    "env_gate": (env_gate, 50, 100),
    "env_gate_cap": (lambda: stripes(80, 30, INK, MECHANISM, step=18, lean=12), 80, 30),
    "env_lift": (env_lift, 100, 25),
    "env_lever_base": (
        lambda: f'<path d="M6,46 C6,14 74,14 74,46 Z" fill="{MECHANISM}" stroke="{INK}" '
        'stroke-width="5" stroke-linejoin="round"/>',
        80,
        50,
    ),
    "env_knob": (
        lambda: f'<circle cx="20" cy="20" r="15" fill="{DANGER}" stroke="{INK}" stroke-width="4"/>'
        '<path d="M12,16 Q13,11 18,10" fill="none" stroke="#fff" stroke-width="3" '
        'stroke-linecap="round" opacity=".7"/>',
        40,
        40,
    ),
    "env_hanging_pot": (env_hanging_pot, 120, 200),
    "env_light": (env_light, 200, 400),
    "env_landmark_0": (landmark(landmark_watering_can), 400, 300),
    "env_landmark_1": (landmark(landmark_wheelbarrow), 400, 300),
    "env_landmark_2": (landmark(landmark_sunflower), 400, 300),
    "env_landmark_3": (landmark(landmark_windmill), 400, 300),
    "env_landmark_4": (landmark(landmark_water_tank), 400, 300),
}


# --------------------------------------------------------------------------- render


def browser():
    for path in BROWSERS:
        if path and Path(path).exists():
            return path
    sys.exit("Chrome ou Edge não encontrado; defina a variável CHROME.")


def render(cells, cell, view, columns):
    """Rasteriza uma grade de desenhos (cada um com origem no centro da célula)."""
    rows = (len(cells) + columns - 1) // columns
    width, height = columns * cell, rows * cell
    half = view / 2
    body = []
    for index, content in enumerate(cells):
        x, y = (index % columns) * cell, (index // columns) * cell
        body.append(
            f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{-half} {-half} {view} {view}" '
            f'width="{cell}" height="{cell}" style="position:absolute;left:{x}px;top:{y}px">'
            f"{content}</svg>"
        )
    html = (
        "<!doctype html><meta charset='utf-8'><style>html,body{margin:0;background:transparent;"
        "overflow:hidden}</style><body>" + "".join(body) + "</body>"
    )
    with tempfile.TemporaryDirectory() as folder:
        page = Path(folder) / "kit.html"
        shot = Path(folder) / "kit.png"
        page.write_text(html, encoding="utf-8")
        subprocess.run(
            [
                browser(),
                "--headless=new",
                "--disable-gpu",
                "--hide-scrollbars",
                "--force-device-scale-factor=1",
                "--default-background-color=00000000",
                f"--window-size={width},{height}",
                f"--screenshot={shot}",
                page.as_uri(),
            ],
            check=True,
            capture_output=True,
        )
        sheet = Image.open(shot).convert("RGBA")
    return [
        sheet.crop(((i % columns) * cell, (i // columns) * cell,
                    (i % columns) * cell + cell, (i // columns) * cell + cell))
        for i in range(len(cells))
    ]


def build_foods():
    names = NATURAL + SNACK
    for name, image in zip(names, render([food_svg(n) for n in names], FOOD_CELL, FOOD_VIEW, 6)):
        image.save(ART / "Collectibles" / f"spr_food_{name}.png")
    icons = render([food_icon_svg(n) for n in names], FOOD_CELL, FOOD_VIEW, 6)
    sheet = Image.new("RGBA", (6 * FOOD_CELL, 3 * FOOD_CELL), (0, 0, 0, 0))
    for index, image in enumerate(icons):
        sheet.paste(image, ((index % 6) * FOOD_CELL, (index // 6) * FOOD_CELL))
    (ART / "Kit").mkdir(parents=True, exist_ok=True)
    sheet.save(ART / "Kit" / "SP_FoodIcons.png")
    return names


def preview(paths, out, background=(206, 230, 214, 255)):
    images = [Image.open(p).convert("RGBA") for p in paths]
    width = max(i.width for i in images)
    if all(i.size == images[0].size for i in images):
        columns = 6
        rows = (len(images) + columns - 1) // columns
        sheet = Image.new("RGBA", (columns * width, rows * width), background)
        for index, image in enumerate(images):
            sheet.alpha_composite(image, ((index % columns) * width, (index // columns) * width))
    else:
        # peças de tamanhos diferentes: uma embaixo da outra
        sheet = Image.new("RGBA", (width, sum(i.height + 8 for i in images)), background)
        y = 0
        for image in images:
            sheet.alpha_composite(image, (0, y))
            y += image.height + 8
    sheet.convert("RGB").save(out)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview")
    parser.add_argument("--only", default="")
    args = parser.parse_args()
    built = []
    if args.only in ("", "foods"):
        names = build_foods()
        built += [ART / "Collectibles" / f"spr_food_{n}.png" for n in names]
    if args.only in ("", "hud"):
        built += build_pieces(HUD_PIECES)
    if args.only in ("", "env"):
        built += build_pieces(ENV_PIECES, ENV_SCALE)
    if args.preview:
        preview(built, args.preview)
    print("KIT_COMPLETE:", len(built), "arquivos")


if __name__ == "__main__":
    main()
