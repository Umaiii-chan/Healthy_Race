# Como se joga

Healthy Race é uma **corrida para dois jogadores** em 10 fases. Cada fase é uma estufa em
zigue-zague, com vários andares. Os dois saem juntos e cada um termina sozinho na
**própria porta** (P1 azul, P2 roxa). Nenhum trecho exige ajuda do outro.

Controles em [CONTROLS.md](CONTROLS.md).

## Curto ou longo

Em cada andar há duas **bifurcações**, marcadas por uma placa `↓ CURTO / → LONGO`:

| | Caminho curto | Caminho longo |
| --- | --- | --- |
| Como entra | Cai no buraco embaixo da placa | Pula o buraco |
| Onde fica | Corredor de lanchonete (piso vermelho e branco) | Por cima, na estufa |
| Percurso | Reto, sem pulos (o teto é baixo) | Lombadas, lama, espinhos, plataformas que caem, alavancas |
| Comida | Fast-food no meio do corredor, impossível desviar | Alimentos saudáveis |
| Custo | Cada fast-food deixa você **pesado** (metade da velocidade por 5 s), tira Bem-Estar e pontos | O tempo dos obstáculos |

Os dois caminhos se juntam no meio do andar (com checkpoint) e no fim, no **elevador** para
o andar de cima. Comendo todo o fast-food, o curto acaba **mais lento** que o longo: o
atalho cobra no tempo, nos pontos e no Bem-Estar. Usá-lo de vez em quando pode valer a pena.

## Bem-Estar e fadiga

- Começa em 100. Saudáveis sobem, ultraprocessados descem.
- Com Bem-Estar baixo o personagem anda e pula menos, e pode ter **fadiga**: fica parado
  por um instante.
- O Bem-Estar no fim da fase vale pontos (×5).

## Obstáculos

| Obstáculo | O que faz |
| --- | --- |
| Lombada com rampa | Sobe andando; a lombada alta tem rampa dupla |
| Lama | Deixa mais lento enquanto você estiver nela |
| Espinhos | −5 de Bem-Estar e volta ao último checkpoint |
| Plataforma que cai | Treme e cai logo depois que você pisa; quem cai vai para o corredor curto |
| Alavanca e portão | Encoste na alavanca; o portão demora um pouco e fica aberto |
| Elevador | Sobe com quem está em cima e volta sozinho |

## Fim da fase e pontos

- Quando o primeiro chega à porta, o outro tem **45 s**. Quem não chega a tempo sai como
  "não concluiu" (−500 pontos). A fase também acaba aos 5 minutos.
- Vence quem tiver **mais pontos**:

| Item | Pontos |
| --- | --- |
| Alimentos saudáveis | O valor de cada um (80 a 160) |
| Alavanca puxada | +150 para quem puxou |
| Chegada | 1º lugar +500, 2º lugar +250 |
| Tempo | +600 até o tempo-alvo da fase; cai até zero em 1,5× o tempo-alvo |
| Bem-Estar final | ×5 |
| Ultraprocessado comido | −100 cada |
| Episódio de fadiga | −200 cada |
| Não concluiu | −500 |

Empate em pontos é desempatado pelo Bem-Estar final e depois pelo tempo.

## As 10 fases

| Fase | Nome | Destaque |
| --- | --- | --- |
| 1 | Corrida do Equilíbrio | Introdução: um pouco de tudo |
| 2 | Horta Suspensa | Lombadas e lama, sem alavancas |
| 3 | Pomar das Alavancas | Muitas alavancas e portões |
| 4 | Estufa dos Espinhos | Espinhos |
| 5 | Ponte de Folhas | Plataformas que caem |
| 6 | Feira Livre | Fase larga, lama e alavancas |
| 7 | Torre das Trepadeiras | Sete andares |
| 8 | Mercado Comprido | Larga e baixa: cinco andares, de tudo um pouco |
| 9 | Labirinto da Lama | Lama, espinhos e plataformas, sete andares |
| 10 | Grande Final | Tudo junto, a maior fase (80 × 44) |

O mapa de cada fase é um arquivo de texto em `Assets/_Game/Levels/`; ver
[levels/fase-01.md](levels/fase-01.md) para o formato e as conferências.
