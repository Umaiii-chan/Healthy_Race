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
| Comida | Lanches no meio do corredor, impossível desviar | Frutas, água e refeições |
| Efeito | **Pico e queda** (abaixo) e menos Bem-Estar | O tempo dos obstáculos |

Os dois caminhos se juntam no meio do andar (com checkpoint) e no fim, no **elevador** para
o andar de cima.

## O lanche: pico e queda

- **Pico:** ao comer um lanche, o personagem dispara — 30% mais rápido por 2 s.
- **Queda:** quando o pico acaba, ele fica **sonolento** — 40% mais lento por 4 s.
- Comer outro lanche ainda no pico **renova o pico**, mas a queda seguinte fica 2 s mais longa.
  Encadear lanches rende, e cobra cada vez mais.
- **O Bem-Estar manda no efeito.** Os números acima valem com Bem-Estar cheio. Quanto mais
  baixo ele estiver, mais curto o pico e mais longa a queda: com Bem-Estar zero, o pico dura
  0,5 s e a queda, 12 s (o teto é 16 s). O primeiro lanche rende; o décimo, não.
- **Fruta, água ou refeição durante a queda acorda o personagem na hora.** No pico, não mudam nada.
- O atalho é uma aposta: o pico ganha tempo; a queda pode perder mais, sobretudo se ela pegar
  você na lama ou perto dos espinhos.

Na coleta, o nome do alimento, a mudança de Bem-Estar e o efeito sobem do personagem: por
exemplo "Hambúrguer −18" com "PICO!" embaixo, ou "Maçã +10" com "ACORDOU!".

## Cada um tem a sua comida

Todo alimento pode ser pego **uma vez por jogador**. Depois que um pega, o item fica mais
apagado, na cor de quem ainda falta, e só some quando os dois pegaram. Quem vem atrás não
encontra o caminho vazio.

## Bem-Estar e fadiga

- Começa em 100. Frutas, água e refeições sobem; lanches descem.
- Com Bem-Estar baixo o personagem anda e pula menos.
- Com Bem-Estar em 20 ou menos vem a **fadiga**: um aviso de 1 s (o personagem ainda anda),
  depois ele para por 1,2 s e recupera 15 de Bem-Estar.

## Obstáculos

| Obstáculo | O que faz |
| --- | --- |
| Lombada com rampa | Sobe andando; a lombada alta tem rampa dupla |
| Lama | Deixa mais lento enquanto você estiver nela |
| Espinhos | −5 de Bem-Estar e volta ao último checkpoint |
| Plataforma que cai | Treme e cai logo depois que você pisa; quem cai vai para o corredor curto |
| Alavanca e portão | Encoste na alavanca; o portão demora um pouco e fica aberto |
| Elevador | Sobe com quem está em cima e volta sozinho |

## Largada, chegada e estrelas

- **Largada:** contagem "3, 2, 1, JÁ!". Ninguém anda e o relógio não corre antes do "JÁ!".
- **Vence quem chega primeiro à própria porta.** Se os dois chegam no mesmo instante, vence
  quem terminou com mais Bem-Estar; depois, quem teve menos fadigas; persistindo, é empate.
- Quando o primeiro chega, o outro tem **45 s**. Quem não chega a tempo sai como "não
  concluiu". A fase também acaba aos 5 minutos.
- A tela de resultado diz em uma frase por que a corrida terminou assim ("P2 chegou 3,2 s antes.").
- Cada jogador ganha até **3 estrelas** na fase, independentes do rival:

| Estrela | Como ganhar |
| --- | --- |
| Chegada | Chegar à própria porta |
| Bem-Estar | Terminar com Bem-Estar 60 ou mais |
| Tempo | Chegar até o tempo-alvo da fase |

Os pontos (alimentos, alavancas, tempo, Bem-Estar, penalidades) continuam na tela de resultado
como detalhe, mas não decidem a fase.

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
