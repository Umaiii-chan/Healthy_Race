"""Mapa da Fase 1 em grade de tiles (proposta para aprovação, versão 3).

Uso:
    python tools/levels/fase01_map.py [--txt saida.txt]

1 tile = 1 unidade do jogo. Grade 64 x 38 (2 x 2 telas, com folga no alto); y cresce para cima, mas o
texto é impresso de cima para baixo, como se vê na tela.

Corrida em 6 andares em zigue-zague. Cada andar tem duas escolhas: cair no buraco
leva ao caminho CURTO (corredor baixo, reto, com fast-food); pular o buraco mantém
no LONGO (por cima, com obstáculos e saudáveis). Os dois caminhos se juntam no meio
do andar (checkpoint) e no fim (elevador). Ninguém depende do outro.

Cada andar é desenhado da esquerda para a direita e espelhado quando corre para a
esquerda. Andar k: piso do curto em y=6k+1, piso do longo em y=6k+3.
"""
import argparse

# 38 de altura: o último andar precisa do mesmo vão livre dos outros (3,5) para
# pular buraco e espinhos; com 36 o teto da moldura ficava a 2,0 do piso.
W, H = 64, 38
FLOORS = 6

LEGEND = {
    "#": "bloco sólido (moldura, chão, lombadas)",
    "=": "tábua fina (o topo da célula é o piso)",
    "/": "rampa 45° subindo para a direita",
    "\\": "rampa 45° descendo para a direita",
    "~": "lama (poça no piso)",
    "^": "espinhos no piso",
    "w": "plataforma que cai (treme, cai e volta)",
    "E": "elevador de autoatendimento (sobe com quem está em cima)",
    ":": "curso do elevador",
    "L": "alavanca",
    "G": "portão (fica aberto depois da alavanca)",
    "C": "checkpoint",
    "h": "alimento saudável",
    "f": "fast-food",
    "1": "largada P1",
    "2": "largada P2",
    "P": "porta P1",
    "Q": "porta P2",
    ".": "vazio",
}

# Posições no sistema local do andar (sempre da esquerda para a direita).
HOLE_A = 5  # escolha A
LONG_A = (6, 28)
MERGE = (29, 33)  # trecho em que os dois caminhos se juntam
CHECKPOINT = 31
RAMP = 34  # rampa de 2 tiles de volta ao nível do longo (34 e 35)
HOLE_B = 38  # escolha B
LONG_B = (39, 58)
TURN = (61, 62)  # elevador (ou portas, no último andar)

# Conteúdo do caminho longo, por andar: cada item é (tipo, x local[, extra]).
# A dificuldade sobe: lombadas e lama primeiro, depois alavanca e espinhos, por fim
# plataformas que caem.
LONG_CONTENT = {
    0: [("hump2", 9), ("food", 12, 2), ("food", 18), ("food", 24),
        ("hump1", 42), ("food", 43, 1), ("mud", 49, 3), ("food", 54), ("food", 57)],
    1: [("lever", 10), ("food", 16), ("hump1", 19), ("food", 20, 1), ("food", 26),
        ("spikes", 43), ("food", 46), ("spikes", 49), ("food", 52), ("hump1", 54)],
    2: [("falling", 12), ("food", 13, 1), ("food", 20), ("mud", 23, 3), ("food", 27),
        ("lever", 41), ("food", 47), ("spikes", 51), ("food", 55)],
    3: [("hump2", 8), ("food", 11, 2), ("spikes", 18), ("food", 21), ("spikes", 24),
        ("food", 27), ("falling", 42), ("food", 43, 1), ("food", 50), ("hump1", 52),
        ("food", 53, 1)],
    4: [("lever", 9), ("spikes", 16), ("food", 19), ("falling", 22), ("food", 23, 1),
        ("falling", 42), ("food", 45, 1), ("mud", 50, 3), ("food", 55), ("spikes", 57)],
    5: [("spikes", 10), ("food", 13), ("falling", 16), ("food", 17, 1), ("lever", 22),
        ("food", 27), ("spikes", 42), ("food", 45), ("falling", 47), ("food", 48, 1),
        ("spikes", 54), ("food", 57)],
}
SHORT_FOOD = (12, 22, 44, 54)


def short_row(k):
    """Linha da tábua (ou do chão) que serve de piso ao caminho curto do andar k."""
    return 0 if k == 0 else 6 * k


def long_row(k):
    return 6 * k + 2


def build():
    g = [["." for _ in range(W)] for _ in range(H)]

    for y in range(H):
        g[y][0] = g[y][W - 1] = "#"
    for x in range(W):
        g[0][x] = g[H - 1][x] = "#"

    for k in range(FLOORS):
        build_floor(g, k)
    return g


def build_floor(g, k):
    mirrored = k % 2 == 1
    last = k == FLOORS - 1
    s_row, l_row = short_row(k), long_row(k)
    s, l = s_row + 1, l_row + 1  # pisos (altura onde o jogador fica de pé)
    gate_height = 3

    def X(x):
        return W - 1 - x if mirrored else x

    def put(x, y, c):
        g[y][X(x)] = c

    def ramp_up(x, y):
        """Rampa que sobe no sentido do andar."""
        put(x, y, "\\" if mirrored else "/")

    def ramp_down(x, y):
        put(x, y, "/" if mirrored else "\\")

    # piso do curto (o andar 0 usa o chão da moldura)
    if k > 0:
        # no último andar a tábua vai até a parede: as portas ficam sobre ela
        for x in range(4, W - 1 if last else TURN[0]):
            put(x, s_row, "=")
        # batente: o corredor curto não termina no poço do elevador de baixo
        put(3, s_row, "#")
        put(3, s_row + 1, "#")
    # piso do longo: começo, A, rampa de volta e B
    for x in range(1 if k == 0 else 3, LONG_A[1] + 1):
        put(x, l_row, "=")
    for x in range(RAMP + 2, LONG_B[1] + 1):
        put(x, l_row, "=")
    put(HOLE_A, l_row, ".")
    put(HOLE_B, l_row, ".")
    # rampa do trecho juntado de volta ao nível do longo
    ramp_up(RAMP, s)
    put(RAMP + 1, s, "#")
    ramp_up(RAMP + 1, s + 1)
    put(CHECKPOINT, s, "C")

    for item in LONG_CONTENT[k]:
        kind, x = item[0], item[1]
        if kind == "hump1":
            ramp_up(x, l)
            put(x + 1, l, "#")
            put(x + 2, l, "#")
            ramp_down(x + 3, l)
        elif kind == "hump2":
            ramp_up(x, l)
            put(x + 1, l, "#")
            ramp_up(x + 1, l + 1)
            for dx in (2, 3):
                put(x + dx, l, "#")
                put(x + dx, l + 1, "#")
            put(x + 4, l, "#")
            ramp_down(x + 4, l + 1)
            ramp_down(x + 5, l)
        elif kind == "food":
            put(x, l + (item[2] if len(item) > 2 else 0), "h")
        elif kind == "mud":
            for dx in range(item[2]):
                put(x + dx, l_row, "~")
        elif kind == "spikes":
            put(x, l_row, "^")
        elif kind == "falling":
            # vão de 5 com duas plataformas que caem: pulos de 1 tile
            for dx in range(5):
                put(x + dx, l_row, ".")
            put(x + 1, l_row, "w")
            put(x + 3, l_row, "w")
        elif kind == "lever":
            put(x, l, "L")
            for dy in range(gate_height):
                put(x + 3, l + dy, "G")

    for x in SHORT_FOOD:
        put(x, s, "f")

    if k == 0:
        put(2, l, "1")
        put(3, l, "2")
    else:
        # checkpoint na chegada do elevador: espinhos do andar não devolvem ao andar de baixo
        put(4, l, "C")
    if last:
        for dy in (0, 1):
            put(59, s + dy, "P")
            put(61, s + dy, "Q")
    else:
        # elevador do fim do andar até o longo do andar de cima. Embaixo dele há
        # piso fixo: com o elevador lá em cima, quem chega espera em vez de cair.
        for x in TURN:
            put(x, s, "E")
            if k > 0:
                put(x, s_row, "=")
            for y in range(s + 1, long_row(k + 1) + 1):
                put(x, y, ":")


SOLID_FULL = set("#/\\")
PLANK = set("=^~w")


def ceilings(g, x):
    spans = []
    for y in range(H):
        c = g[y][x]
        if c in SOLID_FULL:
            spans.append((y, y + 1))
        elif c in PLANK:
            spans.append((y + 0.5, y + 1))
        elif c == "E":
            spans.append((y, y + 0.3))
    return spans


def headroom_report(g, minimum=1.5):
    """Pisos onde se fica de pé com menos de `minimum` de espaço livre acima."""
    problems = []
    for x in range(1, W - 1):
        spans = ceilings(g, x)
        for _, top in spans:
            above = [b for b, _ in spans if b >= top]
            if above and 0 < min(above) - top < minimum:
                problems.append((x, top, min(above), round(min(above) - top, 2)))
    return problems


def render_text(g):
    lines = ["    " + "".join(str(x // 10) if x % 10 == 0 else " " for x in range(W)).rstrip()]
    lines.append("    " + "".join(str(x % 10) for x in range(W)))
    for y in range(H - 1, -1, -1):
        lines.append(f"{y:>2}  " + "".join(g[y]))
    return "\n".join(lines)


def counts(g):
    flat = "".join("".join(r) for r in g)
    return {c: flat.count(c) for c in "hfCLGw^~"}


def signs():
    """Placas: (centro x, base y, texto com '|' separando linhas)."""
    result = []
    for k in range(FLOORS):
        mirrored = k % 2 == 1
        l = long_row(k) + 1
        arrow = "←" if mirrored else "→"
        base = l + 2.0
        for hole in (HOLE_A, HOLE_B):
            # bem em cima do buraco: é ali que se decide
            x = W - 1 - hole + 0.5 if mirrored else hole + 0.5
            result.append((round(x, 2), round(base, 2), f"↓ CURTO|{arrow} LONGO"))
    first = next(k for k in range(FLOORS) if any(i[0] == "lever" for i in LONG_CONTENT[k]))
    lever_x = next(i[1] for i in LONG_CONTENT[first] if i[0] == "lever")
    x = W - 1 - lever_x + 0.5 if first % 2 else lever_x + 0.5
    result.append((x, long_row(first) + 2.3, "PUXE A|ALAVANCA"))
    return result


def decor(g):
    """Enfeites sem colisão: (x, y, tipo). Vasos no caminho longo, trepadeiras penduradas."""
    import random

    rng = random.Random(11)
    result = []
    for k in range(FLOORS):
        l = long_row(k) + 1
        mirrored = k % 2 == 1
        spots = [x for x in range(7, 58)
                 if all(g[l][x + d] == "." and g[l - 1][x + d] == "=" for d in (-1, 0, 1))
                 and g[l + 1][x] == "."]
        for x in rng.sample(spots, min(3, len(spots))):
            result.append((x, l, "vaso"))
        if k + 1 < FLOORS:
            ceiling = short_row(k + 1)
            for x in rng.sample(range(8, 56), 4):
                gx = W - 1 - x if mirrored else x
                result.append((gx, ceiling, "trepadeira"))
    return result


def level_file(g):
    """Arquivo lido pelo jogo: mapa (linha de cima primeiro), placas e enfeites."""
    lines = ["; Fase 1 - Corrida do Equilíbrio. Gerado por tools/levels/fase01_map.py.",
             "[tema]", "numero 1", "nome Corrida do Equilíbrio", "tempo-alvo 130", "[mapa]"]
    lines += ["".join(g[y]) for y in range(H - 1, -1, -1)]
    lines.append("[placas]")
    lines += [f"{x} {y} {text}" for x, y, text in signs()]
    lines.append("[enfeites]")
    lines += [f"{x} {y} {kind}" for x, y, kind in decor(g)]
    return "\n".join(lines) + "\n"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--txt")
    parser.add_argument("--level", help="arquivo de fase lido pelo jogo")
    args = parser.parse_args()
    g = build()
    text = render_text(g)
    print(text)
    print()
    print("contagem:", counts(g))
    for x, top, ceiling, room in headroom_report(g):
        print(f"espaço livre baixo: x={x} piso={top} teto={ceiling} livre={room}")
    if args.level:
        with open(args.level, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(level_file(g))
    if args.txt:
        with open(args.txt, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text + "\n")


if __name__ == "__main__":
    main()
