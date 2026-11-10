"""Gera as fases 2 a 10 com a mesma estrutura de corrida da Fase 1.

Uso:
    python tools/levels/build_levels.py            # escreve Assets/_Game/Levels/FaseNN.txt
    python tools/levels/build_levels.py --print 4  # mostra o mapa da fase 4

Cada andar é um zigue-zague com duas bifurcações (buraco: cair = curto, pular =
longo), trecho juntado com checkpoint e rampa, e elevador para o andar de cima. O
que muda de uma fase para outra: largura, número de andares, a mecânica em destaque
no caminho longo, a dificuldade e o tema de cores. A Fase 1 continua em
fase01_map.py, que é o mapa aprovado à mão.

Regras que o jogo confere em teste (LevelTests): todo degrau e vão obrigatório tem
no máximo 1 tile; 1,5 tile livre acima de qualquer piso; fast-food só no corredor
curto e saudáveis só fora dele; cada jogador chega sozinho à própria porta.
"""
import argparse
import random
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "Assets/_Game/Levels"

# Larguras dos itens do caminho longo (em tiles, no sentido do andar).
WIDTH = {"hump1": 4, "hump2": 6, "mud": 4, "spikes": 1, "falling": 5, "lever": 4}

# Mecânicas liberadas por andar: os primeiros andares de cada fase são mais calmos.
EARLY = {"hump1", "hump2", "mud"}

LEVELS = [
    dict(n=2, nome="Horta Suspensa", W=64, F=5, seed=21, itens=3, alvo=100,
         peso={"hump1": 3, "hump2": 2, "mud": 4, "spikes": 1},
         tema=dict(parede="#EEF6D8", tijolo="#6E8A3A", folhagem="#A8C878")),
    dict(n=3, nome="Pomar das Alavancas", W=64, F=6, seed=31, itens=3, alvo=130,
         peso={"lever": 5, "hump1": 2, "mud": 1, "spikes": 1},
         tema=dict(parede="#FBEFD8", tijolo="#8A5A3A", folhagem="#E0B070")),
    dict(n=4, nome="Estufa dos Espinhos", W=72, F=6, seed=41, itens=4, alvo=140,
         peso={"spikes": 6, "hump1": 2, "hump2": 1, "mud": 1},
         tema=dict(parede="#F4E2E2", tijolo="#7A3E4A", folhagem="#D89098")),
    dict(n=5, nome="Ponte de Folhas", W=72, F=6, seed=51, itens=4, alvo=150, fast_gap=8,
         peso={"falling": 6, "hump1": 1, "spikes": 1, "mud": 1},
         tema=dict(parede="#DFF0F4", tijolo="#3E6A7A", folhagem="#88B8C8")),
    dict(n=6, nome="Feira Livre", W=80, F=6, seed=61, itens=4, alvo=160,
         peso={"mud": 3, "lever": 3, "hump2": 2, "spikes": 2, "falling": 1},
         tema=dict(parede="#FFF4DA", tijolo="#9A6A2A", folhagem="#E8C068")),
    dict(n=7, nome="Torre das Trepadeiras", W=64, F=7, seed=71, itens=3, alvo=140,
         peso={"hump2": 3, "falling": 2, "spikes": 2, "lever": 2, "mud": 1},
         tema=dict(parede="#E2F2E6", tijolo="#2E6A4A", folhagem="#78B890")),
    dict(n=8, nome="Mercado Comprido", W=80, F=5, seed=81, itens=5, alvo=140,
         peso={"mud": 2, "spikes": 3, "falling": 2, "lever": 2, "hump1": 2},
         tema=dict(parede="#F2E6F4", tijolo="#6A3E7A", folhagem="#B898C8")),
    dict(n=9, nome="Labirinto da Lama", W=72, F=7, seed=91, itens=4, alvo=180, fast_gap=8,
         peso={"mud": 5, "spikes": 3, "falling": 2, "hump2": 1},
         tema=dict(parede="#EDE4D6", tijolo="#5A4A32", folhagem="#A89070")),
    dict(n=10, nome="Grande Final", W=80, F=7, seed=101, itens=5, alvo=180,
         peso={"spikes": 3, "falling": 3, "lever": 2, "mud": 2, "hump2": 2, "hump1": 1},
         tema=dict(parede="#E8EEF8", tijolo="#34446A", folhagem="#8C9CC8")),
]


class Layout:
    """Posições de um andar no sistema local (sempre da esquerda para a direita)."""

    def __init__(self, w, fast_gap=10):
        mid = w // 2
        self.W = w
        self.hole_a = 5
        self.long_a = (6, mid - 4)
        self.merge = (mid - 3, mid + 1)
        self.checkpoint = mid - 1
        self.ramp = mid + 2
        self.hole_b = mid + 6
        self.long_b = (self.hole_b + 1, w - 6)
        self.turn = (w - 3, w - 2)
        # fast-food a cada `fast_gap` tiles no corredor curto (menor = atalho mais caro)
        self.short_food = list(range(12, mid - 4, fast_gap)) + list(
            range(self.hole_b + 6, w - 6, fast_gap)
        )


def short_row(k):
    return 0 if k == 0 else 6 * k


def long_row(k):
    return 6 * k + 2


def plan_segment(rng, spec, k, start, end):
    """Itens do caminho longo num trecho [start, end) do andar k: (tipo, x[, largura])."""
    kinds = [kind for kind in spec["peso"] if k >= 1 or kind in EARLY]
    weights = [spec["peso"][kind] for kind in kinds]
    count = min(spec["itens"], 2 + k // 2)
    items, cursor = [], start + 2
    for _ in range(count):
        kind = rng.choices(kinds, weights)[0]
        width = WIDTH[kind] if kind != "mud" else rng.choice((3, 4, 5))
        if cursor + width > end - 1:
            break
        items.append((kind, cursor, width))
        cursor += width + rng.choice((2, 3, 3, 4))
    return items


def free_spots(items, start, end):
    """Tiles do caminho longo onde dá para pôr comida ou vaso (sem item por perto)."""
    busy = set()
    for kind, x, width in items:
        busy.update(range(x - 1, x + width + 1))
    return [x for x in range(start + 1, end) if x not in busy]


def build(spec):
    W, F = spec["W"], spec["F"]
    H = 6 * F + 2
    lay = Layout(W, spec.get("fast_gap", 10))
    rng = random.Random(spec["seed"])
    g = [["." for _ in range(W)] for _ in range(H)]
    for y in range(H):
        g[y][0] = g[y][W - 1] = "#"
    for x in range(W):
        g[0][x] = g[H - 1][x] = "#"

    plan = []
    for k in range(F):
        mirrored = k % 2 == 1
        last = k == F - 1
        s_row, l_row = short_row(k), long_row(k)
        s, l = s_row + 1, l_row + 1

        def X(x):
            return W - 1 - x if mirrored else x

        def put(x, y, c):
            g[y][X(x)] = c

        def ramp_up(x, y):
            put(x, y, "\\" if mirrored else "/")

        def ramp_down(x, y):
            put(x, y, "/" if mirrored else "\\")

        if k > 0:
            for x in range(4, W - 1 if last else lay.turn[0]):
                put(x, s_row, "=")
            put(3, s_row, "#")
            put(3, s_row + 1, "#")
        for x in range(1 if k == 0 else 3, lay.long_a[1] + 1):
            put(x, l_row, "=")
        for x in range(lay.ramp + 2, lay.long_b[1] + 1):
            put(x, l_row, "=")
        put(lay.hole_a, l_row, ".")
        put(lay.hole_b, l_row, ".")
        ramp_up(lay.ramp, s)
        put(lay.ramp + 1, s, "#")
        ramp_up(lay.ramp + 1, s + 1)
        put(lay.checkpoint, s, "C")

        segments = [(lay.hole_a + 1, lay.long_a[1]), (lay.hole_b + 1, lay.long_b[1])]
        floor_items = []
        for start, end in segments:
            items = plan_segment(rng, spec, k, start, end)
            floor_items.append(items)
            for kind, x, width in items:
                if kind == "hump1":
                    ramp_up(x, l)
                    put(x + 1, l, "#")
                    put(x + 2, l, "#")
                    ramp_down(x + 3, l)
                    put(x + 1, l + 1, "h")
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
                    put(x + 3, l + 2, "h")
                elif kind == "mud":
                    for dx in range(width):
                        put(x + dx, l_row, "~")
                elif kind == "spikes":
                    put(x, l_row, "^")
                elif kind == "falling":
                    for dx in range(5):
                        put(x + dx, l_row, ".")
                    put(x + 1, l_row, "w")
                    put(x + 3, l_row, "w")
                    put(x + 2, l + 1, "h")
                elif kind == "lever":
                    put(x, l, "L")
                    for dy in range(3):
                        put(x + 3, l + dy, "G")
            # saudáveis no chão do caminho longo, entre os itens
            spots = free_spots(items, start, end)
            for x in spots[1::5][:2]:
                put(x, l, "h")

        for x in lay.short_food:
            put(x, s, "f")

        if k == 0:
            put(2, l, "1")
            put(3, l, "2")
        else:
            put(4, l, "C")
        if last:
            for dy in (0, 1):
                put(W - 5, s + dy, "P")
                put(W - 3, s + dy, "Q")
        else:
            for x in lay.turn:
                put(x, s, "E")
                if k > 0:
                    put(x, s_row, "=")
                for y in range(s + 1, long_row(k + 1) + 1):
                    put(x, y, ":")
        plan.append(floor_items)
    return g, plan, lay


def signs(spec, g, lay):
    W, F = spec["W"], spec["F"]
    result = []
    for k in range(F):
        mirrored = k % 2 == 1
        l = long_row(k) + 1
        arrow = "←" if mirrored else "→"
        for hole in (lay.hole_a, lay.hole_b):
            x = W - 1 - hole + 0.5 if mirrored else hole + 0.5
            result.append((round(x, 2), l + 2.0, f"↓ CURTO|{arrow} LONGO"))
    # placa só na primeira alavanca da fase (g é indexado de baixo para cima)
    for y in range(len(g)):
        row = g[y]
        if "L" in row:
            x = row.index("L")
            result.append((x + 0.5, y + 1.3, "PUXE A|ALAVANCA"))
            break
    return result


def decor(spec, g, lay):
    W, F = spec["W"], spec["F"]
    rng = random.Random(spec["seed"] + 7)
    result = []
    for k in range(F):
        l = long_row(k) + 1
        spots = [x for x in range(7, W - 6)
                 if all(g[l][x + d] == "." and g[l - 1][x + d] == "=" for d in (-1, 0, 1))
                 and g[l + 1][x] == "."]
        for x in rng.sample(spots, min(3, len(spots))):
            result.append((x, l, "vaso"))
        if k + 1 < F:
            ceiling = short_row(k + 1)
            cols = [x for x in range(8, W - 8) if g[ceiling][x] == "=" and g[ceiling - 1][x] == "."]
            for x in rng.sample(cols, min(4, len(cols))):
                result.append((x, ceiling, "trepadeira"))
    return result


def level_file(spec):
    g, _, lay = build(spec)
    H = len(g)
    lines = [f"; Fase {spec['n']} - {spec['nome']}. Gerado por tools/levels/build_levels.py.",
             "[tema]", f"numero {spec['n']}", f"nome {spec['nome']}",
             f"tempo-alvo {spec['alvo']}"]
    lines += [f"{key} {value}" for key, value in spec["tema"].items()]
    lines.append("[mapa]")
    lines += ["".join(g[y]) for y in range(H - 1, -1, -1)]
    lines.append("[placas]")
    lines += [f"{x} {y} {text}" for x, y, text in signs(spec, g, lay)]
    lines.append("[enfeites]")
    lines += [f"{x} {y} {kind}" for x, y, kind in decor(spec, g, lay)]
    return "\n".join(lines) + "\n", g


def render(g):
    W = len(g[0])
    lines = ["    " + "".join(str(x % 10) for x in range(W))]
    for y in range(len(g) - 1, -1, -1):
        lines.append(f"{y:>2}  " + "".join(g[y]))
    return "\n".join(lines)


def counts(g):
    flat = "".join("".join(r) for r in g)
    return {c: flat.count(c) for c in "hfCLw^~"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--print", type=int, help="mostra o mapa de uma fase")
    args = parser.parse_args()
    for spec in LEVELS:
        text, g = level_file(spec)
        if args.print:
            if spec["n"] == args.print:
                print(render(g))
                print(counts(g))
            continue
        path = OUT / f"Fase{spec['n']:02d}.txt"
        path.write_text(text, encoding="utf-8", newline="\n")
        print(path.name, f"{spec['W']}x{len(g)}", spec["nome"], counts(g))


if __name__ == "__main__":
    main()
