# Fase 1 — Corrida do Equilíbrio

Status: **construída** (mapa versão 3 aprovado). Fases 2 a 10 usam o mesmo construtor.

- Fase de **2 × 2 telas**: 64 × 38 tiles, 1 tile = 1 unidade do jogo. São 2 linhas a mais
  que as 2 telas exatas: o último andar precisa do mesmo vão livre dos outros para pular.
- **Câmera compartilhada:** mostra no mínimo uma tela de altura (18 tiles) e se afasta
  quando os jogadores se separam, até caber a fase inteira. O HUD tem faixa própria em cima;
  a câmera desenha só abaixo dela.
- Mapa gerado por `tools/levels/fase01_map.py --level Assets/_Game/Levels/Fase01.txt`. O
  jogo lê o texto em `LevelGrid` e monta tudo em `LevelBootstrap`. A cena é gerada pelo menu
  **Healthy Race › Visuals › Build greenhouse levels**.
- Arte: `tools/art/build_greenhouse.py` (estufa e lanchonete, ladrilhada a 256 px por tile).
- Capturas reais do jogo em 1920 × 1080 (menu **Healthy Race › Evidence › Capture Level 01**):
  [fase inteira](../evidence/fase-01/fase-inteira.png),
  [baixo à esquerda](../evidence/fase-01/baixo-esquerda.png),
  [baixo à direita](../evidence/fase-01/baixo-direita.png),
  [cima à esquerda](../evidence/fase-01/cima-esquerda.png) e
  [cima à direita](../evidence/fase-01/cima-direita.png).
- Esboço de antes da construção: [conceito](fase-01-conceito.png) e
  [detalhe](fase-01-detalhe.png) (`tools/levels/render_concept.py docs/levels`).

## A proposta

É uma **corrida em 6 andares** em zigue-zague. Cada jogador escolhe o próprio caminho e
termina sozinho na própria porta; ninguém depende do outro.

Cada andar tem **duas escolhas**. Num buraco de 1 tile com placa, cair leva ao caminho
**curto**, e pular mantém no **longo**. Os dois caminhos se juntam no meio do andar (onde há um
checkpoint) e no fim (no elevador para o andar de cima).

| | Caminho curto | Caminho longo |
| --- | --- | --- |
| Onde fica | Corredor baixo de lanchonete (ladrilho vermelho, letreiros "FAST") | Por cima, na estufa (madeira, vasos, trepadeiras) |
| Percurso | Reto, sem pulos | Lombadas com rampas, lama, alavanca e portão, espinhos, plataformas que caem |
| Alimentos | 2 fast-foods por trecho, no meio do corredor: não dá para desviar | 2 a 3 saudáveis por trecho |
| Ganha | Chega antes no trecho | Pontos dos saudáveis e Bem-Estar alto |
| Paga | Perde Bem-Estar e pontos; com Bem-Estar baixo fica mais lento e pode ter fadiga | Tempo dos obstáculos |

No total: 32 saudáveis, 24 fast-foods, 11 checkpoints, 4 alavancas, 10 trechos de
espinhos, 12 plataformas que caem e 3 poças de lama.

## Os andares

A dificuldade do caminho longo sobe aos poucos:

| Andar | Sentido | Escolha A (longo) | Escolha B (longo) |
| --- | --- | --- | --- |
| 1 | → | Lombada alta | Lombada e lama |
| 2 | ← | Alavanca e portão (com placa explicando) | Dois espinhos e lombada |
| 3 | → | Plataformas que caem e lama | Alavanca e espinhos |
| 4 | ← | Lombada alta e dois espinhos | Plataformas que caem e lombada |
| 5 | → | Alavanca, espinhos e plataformas que caem | Plataformas que caem, lama e espinhos |
| 6 | ← | Espinhos, plataformas que caem e alavanca | Espinhos, plataformas que caem e espinhos |

- **Viradas:** elevador de autoatendimento no fim de cada andar. Ele sobe com quem estiver
  em cima e volta sozinho; um só por virada, largo o bastante para os dois.
- **Checkpoints:** na chegada de cada andar e no meio de cada um. Cair nos espinhos devolve
  ao último checkpoint com −5 de Bem-Estar; cair de uma plataforma leva ao caminho curto.
- **Chegada:** portas P1 e P2 no fim do sexto andar, na esquerda. Quando o primeiro chega, o
  outro tem 45 segundos; depois disso sai como "não concluiu". A partida tem limite de 5 min.

## Mapa

```text
    0         1         2         3         4         5         6
    0123456789012345678901234567890123456789012345678901234567890123
37  ################################################################
36  #..............................................................#
35  #.....................................G........................#
34  #..............h......................G.......h................#
33  #.....h...........h.................h.G..L........h........C...#
32  #.Q.P====^==.w.w.====^===.==\......========.w.w.=====^====.==::#
31  #.Q.P....f.........f........#\..C........f.........f........#::#
30  #===========================================================#::#
29  #...........G................................................::#
28  #...........G..........h.....................h...............::#
27  #...C....L..G......h...................................h.....::#
26  #::==.==========^=====.w.w.==....../==.===.w.w.===~~~====^=..::#
25  #::#........f.........f........C../#........f.........f......EE#
24  #::#=========================================================..#
23  #::.................................................h..........#
22  #::.......h.........h............................../##\........#
21  #::...../##\.h......................h.....h......./####\...C...#
20  #::..============.w.w.===.==\......====^=====^============.==::#
19  #EE......f.........f........#\..C........f.........f........#::#
18  #..=========================================================#::#
17  #...........................................G................::#
16  #............h..............................G................::#
15  #...C...............h......h.............L..G..h.......h.....::#
14  #::==.======.w.w.======~~~===....../==.============^=======..::#
13  #::#........f.........f........C../#........f.........f......EE#
12  #::#=========================================================..#
11  #::...............................................G............#
10  #::........................................h......G............#
 9  #::.../##\.h.....h...................h.../##\..h..G..L.....C...#
 8  #::..=========^=====^====.==\......=======================.==::#
 7  #EE......f.........f........#\..C........f.........f........#::#
 6  #..=========================================================#::#
 5  #...........h................................................::#
 4  #........./##\.............................h.................::#
 3  #.12...../####\...h.....h................./##\........h..h...::#
 2  #====.=======================....../==.==========~~~=======..::#
 1  #...........f.........f........C../#........f.........f......EE#
 0  ################################################################
```

| Símbolo | Significado |
| --- | --- |
| `#` | bloco sólido (moldura, lombadas) |
| `=` | tábua: piso do longo (madeira) ou do curto (ladrilho) |
| `/` `\` | rampa de 45° subindo / descendo para a direita |
| `~` | lama |
| `^` | espinhos |
| `w` | plataforma que cai (treme, cai e volta) |
| `E` / `:` | elevador / curso do elevador |
| `L` / `G` | alavanca / portão (fica aberto) |
| `C` | checkpoint |
| `h` / `f` | alimento saudável / fast-food |
| `1` `2` | largada P1 / P2 |
| `P` `Q` | porta P1 / porta P2 |

## Métricas e conferências

| Situação | Degrau seguro | Vão seguro |
| --- | --- | --- |
| Bem-Estar cheio | 1,8 tile | 4,0 tiles |
| Bem-Estar zerado (pulo ×0,8, velocidade ×0,7) | 1,0 tile | 1,9 tile |

Conferido por teste (`LevelTests`, EditMode):

- **Pior caso de pulo:** nenhum trecho obrigatório pede mais de 1 tile de degrau ou de vão.
  O orçamento de pulo com Bem-Estar zero cobre isso.
- **Rotas de um jogador só:** a busca em `LevelRoutes` encontra, para cada jogador, um
  caminho da largada até a própria porta; um pelo longo sem tocar o corredor curto; e de
  cada fast-food do corredor até a porta. Pulos com vão exigem o arco inteiro livre.
- **Espaço livre:** no mínimo 1,5 tile acima de cada ponto em que se fica de pé (o jogador
  tem 1,25). Os corredores curtos têm exatamente 1,5: dá para andar, não para pular.
- **Fast-food só no corredor curto, saudáveis só fora dele.**
- **Portões:** 3 tiles de altura; não dá para passar por cima.

## Tempos medidos

Medidos pelo teste `LevelPlayModeTests` com física real: um robô segue a rota de cada
jogador (P1 pelo longo, P2 pelo curto comendo todo o fast-food), em tempo de jogo.

| Partida | Longo (P1) | Curto (P2) |
| --- | --- | --- |
| Começando com Bem-Estar cheio | 127,9 s, termina com Bem-Estar 100 | 103,0 s, termina com 0 |
| Começando com Bem-Estar zero | 126,4 s, termina com 100 | 91,9 s, termina com 0 |

- As duas rotas terminam sozinhas, inclusive partindo de Bem-Estar zero.
- O robô para no centro de cada tile antes de pular, então um jogador de verdade faz o
  longo mais rápido. Mesmo assim, **pelo tempo o curto vence**: a lentidão do Bem-Estar baixo
  não compensa os obstáculos do longo. Quem vai pelo longo termina com Bem-Estar 100 e os
  pontos dos saudáveis; quem vai pelo curto, com 0.
- A estimativa antes da construção (68 s longo, 76 s curto) não se confirmou.

## Checklist da Etapa 3

| Item | Situação |
| --- | --- |
| HUD só na faixa de cima, nos cantos; sem cobrir a fase | Feito: faixa de 64 px, câmera desenha abaixo dela |
| Rótulos de depuração só com F1 | Feito: escondidos ao abrir (teste) |
| Placas com letra de pelo menos 24 px a 1080p | Feito: cerca de 27 px na menor distância de câmera (teste) |
| Parallax | Feito: folhagem ladrilhada que acompanha a câmera a 35% |
| Arte ladrilhada, sem esticar | Feito: tijolo, topo, ladrilho e folhagem ladrilhados; só a parede de fundo cobre a fase inteira |
| Enfeites apoiados em superfície | Feito: vasos sobre o piso, trepadeiras penduradas na tábua |
| Porta com cerca de 1,5× a altura do personagem | Feito: 2 tiles para 1,25 |
| Plataformas com acabamento nas pontas | Feito: tábua em 9-slice |
| Capturas da fase inteira e de cada parte | Feito: 5 capturas em 1920 × 1080 |
| Percurso completo com os dois jogadores | Feito: robô nas duas rotas, com tempos |

## Pontos em aberto

- **O curto deve ser sempre mais rápido?** Hoje ele é, por 25 a 35 s. Se a ideia é "o atalho
  cobra depois" também no tempo, dá para deixar o fast-food mais pesado na velocidade ou o
  corredor mais longo; se a cobrança é só em pontos e Bem-Estar, fica como está.
- **Equilíbrio de pontos:** o bônus por chegar primeiro (100 pontos) é pequeno perto dos
  saudáveis; falta medir com a pontuação final e propor ajuste.
- **Câmera:** se um jogador ficar muito à frente, a câmera afasta até a fase inteira; o HUD
  mostra o progresso dos dois.

## Defeitos de jogo achados pelo robô (já corrigidos)

- Parado numa rampa de 45° depois de um pulo, o jogador não contava como "no chão" e não
  pulava: o sensor de chão é mais estreito que o corpo. Agora aceita uma folga só sobre rampa.
- O elevador em repouso ficava 0,3 acima do piso e virava degrau: agora fica rente.
- O gatilho dos espinhos era maior que o desenho: quem pulava da beira resvalava nele.
- No último andar o teto ficava a 2,0 do piso e não dava para pular buraco nem espinho.
- A porta do P2 ficava sobre um vão sem piso.
- A animação de empurrar nunca disparava contra as paredes da fase (sonda inteira dentro da
  parede, e o terreno tem colisor só de contorno).
