"""Resume os registros de partida do Healthy Race (playtest).

Lê os arquivos `.jsonl` gravados pelo jogo em `%USERPROFILE%/AppData/LocalLow/<empresa>/<produto>/partidas`
(ou a pasta passada como argumento) e imprime as métricas de `docs/EVOLUCAO_QUALIDADE.md` §6:

- vitórias de P1, P2 e empates; vitórias de quem chegou primeiro;
- diferença de chegada e tempo de partida;
- trocas de liderança;
- escolha do atalho por bifurcação (andar × metade da fase);
- fadigas, espinhos, alavancas e coletas por jogador.

Uso:
    python tools/playtest/analisar_partidas.py PASTA [--csv saida.csv]
"""

from __future__ import annotations

import argparse
import csv
import json
import statistics
import sys
from collections import Counter, defaultdict
from pathlib import Path


def ler(arquivo: Path) -> list[dict]:
    eventos = []
    for numero, linha in enumerate(arquivo.read_text(encoding="utf-8").splitlines(), 1):
        linha = linha.strip()
        if not linha:
            continue
        try:
            eventos.append(json.loads(linha))
        except json.JSONDecodeError:
            print(f"  aviso: {arquivo.name} linha {numero} ilegível, ignorada")
    return eventos


def resumir(arquivo: Path) -> dict | None:
    eventos = ler(arquivo)
    largada = next((e for e in eventos if e["evento"] == "largada"), None)
    if largada is None:
        return None
    fim = next((e for e in eventos if e["evento"] == "fim"), None)
    resultados = {e["jogador"]: e for e in eventos if e["evento"] == "resultado"}
    chegadas = {e["jogador"]: e["tempo"] for e in eventos if e["evento"] == "chegada"}
    largura = largada.get("largura", 0)

    # bifurcação = andar (linha do corredor) × metade da fase onde o corredor começa
    atalhos = []
    for e in eventos:
        if e["evento"] == "corredor_entrada":
            metade = "esq" if largura and e["x"] < largura / 2 else "dir"
            atalhos.append((e["jogador"], f"y{e['y']}-{metade}"))

    contagem = Counter(e["evento"] for e in eventos)
    por_jogador = defaultdict(Counter)
    for e in eventos:
        if "jogador" in e and e["evento"] in ("fadiga", "espinho", "alavanca", "coleta"):
            chave = e["evento"]
            if chave == "coleta":
                chave = "saudavel" if e.get("saudavel") else "lanche"
            por_jogador[e["jogador"]][chave] += 1

    primeiro = min(chegadas, key=chegadas.get) if chegadas else 0
    return {
        "arquivo": arquivo.name,
        "fase": largada["fase"],
        "nome": largada.get("nome", ""),
        "concluida": fim is not None,
        "abandonada": contagem["abandono"] > 0,
        "vencedor": fim["vencedor"] if fim else None,
        "primeiro_a_chegar": primeiro,
        "diferenca_chegada": (
            abs(chegadas[1] - chegadas[2]) if 1 in chegadas and 2 in chegadas else None
        ),
        "duracao": max((e["t"] for e in eventos), default=0),
        "trocas_de_lideranca": fim["trocas_de_lideranca"] if fim else None,
        "atalhos": atalhos,
        "por_jogador": por_jogador,
        "resultados": resultados,
    }


def mediana(valores):
    valores = [v for v in valores if v is not None]
    return statistics.median(valores) if valores else None


def fmt(valor, sufixo=""):
    if valor is None:
        return "—"
    if isinstance(valor, float):
        return f"{valor:.1f}{sufixo}"
    return f"{valor}{sufixo}"


def main() -> None:
    # o console do Windows não é UTF-8 por padrão
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("pasta", type=Path)
    parser.add_argument("--csv", type=Path, help="grava uma linha por partida")
    args = parser.parse_args()

    arquivos = sorted(args.pasta.glob("*.jsonl"))
    partidas = [p for p in (resumir(a) for a in arquivos) if p]
    if not partidas:
        print("Nenhuma partida encontrada.")
        return

    concluidas = [p for p in partidas if p["concluida"]]
    print(f"Partidas: {len(partidas)} (concluídas {len(concluidas)}, "
          f"abandonadas {sum(p['abandonada'] for p in partidas)})")

    vencedores = Counter(p["vencedor"] for p in concluidas)
    print(f"Vitórias: P1 {vencedores[1]} · P2 {vencedores[2]} · empates {vencedores[0]}")
    com_dois = [p for p in concluidas if p["primeiro_a_chegar"] and p["vencedor"]]
    if com_dois:
        acertos = sum(p["vencedor"] == p["primeiro_a_chegar"] for p in com_dois)
        print(f"Quem chegou primeiro venceu: {acertos}/{len(com_dois)} "
              f"({100 * acertos / len(com_dois):.0f}%)")
    print(f"Diferença de chegada (mediana): {fmt(mediana(p['diferenca_chegada'] for p in concluidas), ' s')}")
    print(f"Duração (mediana): {fmt(mediana(p['duracao'] for p in concluidas), ' s')}")
    trocas = [p["trocas_de_lideranca"] for p in concluidas if p["trocas_de_lideranca"] is not None]
    if trocas:
        print(f"Trocas de liderança: média {statistics.mean(trocas):.1f}; "
              f"partidas com ≥ 1: {100 * sum(t >= 1 for t in trocas) / len(trocas):.0f}%")

    print("\nAtalho por bifurcação (entradas no corredor ÷ jogadores × partidas da fase):")
    por_fase = defaultdict(list)
    for p in partidas:
        por_fase[p["fase"]].append(p)
    for fase in sorted(por_fase):
        lista = por_fase[fase]
        entradas = Counter(b for p in lista for _, b in p["atalhos"])
        total = 2 * len(lista)
        linha = ", ".join(f"{b} {100 * n / total:.0f}%" for b, n in sorted(entradas.items()))
        print(f"  Fase {fase} ({len(lista)} partidas): {linha or 'nenhum atalho'}")

    print("\nPor jogador (soma de todas as partidas):")
    soma = defaultdict(Counter)
    for p in partidas:
        for jogador, contagem in p["por_jogador"].items():
            soma[jogador].update(contagem)
    for jogador in (1, 2):
        c = soma[jogador]
        print(f"  P{jogador}: saudáveis {c['saudavel']} · lanches {c['lanche']} · "
              f"fadigas {c['fadiga']} · espinhos {c['espinho']} · alavancas {c['alavanca']}")

    if args.csv:
        with args.csv.open("w", newline="", encoding="utf-8") as saida:
            escritor = csv.writer(saida)
            escritor.writerow(["arquivo", "fase", "concluida", "vencedor", "primeiro_a_chegar",
                               "diferenca_chegada", "duracao", "trocas_de_lideranca",
                               "atalhos_p1", "atalhos_p2"])
            for p in partidas:
                escritor.writerow([
                    p["arquivo"], p["fase"], p["concluida"], p["vencedor"],
                    p["primeiro_a_chegar"], p["diferenca_chegada"], p["duracao"],
                    p["trocas_de_lideranca"],
                    sum(1 for j, _ in p["atalhos"] if j == 1),
                    sum(1 for j, _ in p["atalhos"] if j == 2),
                ])
        print(f"\nCSV: {args.csv}")


if __name__ == "__main__":
    main()
