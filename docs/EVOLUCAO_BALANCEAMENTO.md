# Evolução — balanceamento matemático

Base: código e dados da v1.0.0 e as corridas do robô (`TEST_REPORT.md`). Nenhum valor foi
alterado. Tudo que está marcado **experimental** é hipótese para testar, não parâmetro validado.

**Limite dos dados:** o robô é mais lento que uma pessoa no caminho longo (para no centro de
cada tile antes de pular), corre um caminho fixo e não disputa comida com o rival. Os tempos dele
servem para **comparar caminhos**, não para prever o tempo de uma pessoa.

## 1. Variáveis atuais

### Movimento e física

| Variável | Valor | Onde |
| --- | --- | --- |
| Velocidade | 7 t/s | `PlayerMovement._moveSpeed` |
| Aceleração no chão | 90 t/s² | `_groundAcceleration` |
| Velocidade de pulo | 12 t/s | `_jumpVelocity` |
| Gravidade da fase | −18 × escala 2 = 36 t/s² | `LevelBootstrap.PhaseGravityY`, `JumpBudget` |
| Ápice / tempo no ar / alcance | 2,0 t / 0,67 s / ~4,7 t | derivado |
| Coyote / buffer | 0,10 s / 0,12 s | `PlayerMovement` |
| Orçamento de rota (pior caso) | degrau 1 t, vão 1 t | `LevelRoutes`, `JumpBudget` |

### Bem-Estar, fadiga e comida

| Variável | Valor |
| --- | --- |
| Bem-Estar inicial / máximo | 100 / 100 |
| Faixas (velocidade × pulo) | 76-100: 1,00×1,00 · 51-75: 0,95×1,00 · 26-50: 0,90×0,95 · 1-25: 0,80×0,90 · 0: 0,70×0,80 |
| Fadiga | dispara ≤ 20; parada 2 s; +15; rearma > 25 |
| Saudáveis (10) | +8 a +16 Bem-Estar; 80 a 160 pontos (média ≈ 118) |
| Ultraprocessados (8) | −10 a −18 Bem-Estar; 0 pontos; −100 no fim; ×0,5 velocidade por 5 s (renova, não soma) |
| Lama | ×0,7 velocidade |
| Espinho | −5 Bem-Estar, volta ao checkpoint, recarga 1 s |
| Plataforma que cai | treme 0,45 s, volta em 2,5 s |
| Portão | abre 1,2 s depois da alavanca |
| Elevador | 5 t/s |

### Partida e pontos

| Variável | Valor |
| --- | --- |
| Janela do segundo jogador | 45 s |
| Tempo-limite | 300 s |
| Chegada | 1º 500 · 2º 250 |
| Bônus de tempo | 600 até o alvo; linear até 0 em 1,5 × alvo |
| Alavanca puxada | 150 |
| Bem-Estar final | × 5 |
| Penalidades | −100 por ultraprocessado · −200 por fadiga · −500 por não concluir |
| Tempos-alvo | F1 130 · F2 100 · F3 130 · F4 140 · F5 150 · F6 160 · F7 140 · F8 140 · F9 180 · F10 180 s |

### Conteúdo por fase (contagem nos mapas)

| Fase | Tamanho | Lanches | Saudáveis | Alavancas | Espinhos | Plataformas que caem | Lama |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 64×38 | 24 | 32 | 4 | 10 | 12 | 9 |
| 2 | 64×32 | 20 | 27 | 0 | 3 | 0 | 22 |
| 3 | 64×38 | 24 | 21 | 16 | 3 | 0 | 7 |
| 4 | 72×38 | 24 | 35 | 0 | 18 | 0 | 19 |
| 5 | 72×38 | 36 | 38 | 0 | 5 | 38 | 26 |
| 6 | 80×38 | 36 | 31 | 9 | 8 | 4 | 37 |
| 7 | 64×44 | 28 | 35 | 4 | 5 | 10 | 7 |
| 8 | 80×32 | 30 | 30 | 3 | 7 | 10 | 31 |
| 9 | 72×44 | 42 | 33 | 0 | 10 | 22 | 69 |
| 10 | 80×44 | 42 | 44 | 5 | 10 | 12 | 30 |

(Contagem de caracteres do mapa; alavanca, espinho e lama contam tiles, não unidades.)

## 2. Avaliações obrigatórias

### 2.1 Existe estratégia claramente superior?

**Sim: nunca pegar o atalho.** Em todas as fases o curto é mais lento (8 a 35 s) e termina com
7 500 a 11 900 pontos a menos. Comendo só uma bifurcação, a conta também perde (estimativa pelo
código, a medir no experimento E2):

| Parcela de uma bifurcação curta | Efeito |
| --- | --- |
| 2 lanches | −200 pontos, −20 a −36 de Bem-Estar |
| Peso de 5 s renovado | até ~4 s a meia velocidade |
| Saudáveis e alavanca do trecho longo que ficam para trás | −230 a −390 pontos |
| Distância menor do corredor | ganho de tempo **não medido** (E1) |

### 2.2 Uma escolha alimentar torna outra inútil?

Sim. Entre os saudáveis só muda o número (água e brócolis são idênticos: 150 pontos, +15);
entre lanche e saudável, o lanche é sempre pior. Não existe escolha *entre* alimentos.

### 2.3 O jogador mais rápido sempre vence?

**Não, e pelo motivo errado.** Simulação com a fórmula atual na Fase 1 (32 saudáveis, 4 alavancas,
alvo 130 s), dois jogadores no caminho longo disputando a comida:

| Cenário | P1 | P2 | Vencedor |
| --- | --- | --- | --- |
| A — P1 na frente pega 60% da comida | 120 s, 19 itens (2 240), 3 alavancas, 1º → **4 290** | 123 s, 13 itens (1 530), 1 alavanca, 2º → **3 030** | P1 por 1 260 |
| B — P1 corre, P2 coleta | 120 s, 12 itens (1 416), 2 alavancas, 1º → **3 316** | 126 s, 20 itens (2 360), 2 alavancas, 2º → **4 010** | **P2, chegando 6 s depois** |
| C — os dois iguais, P1 chega 0,2 s antes | 16 itens, 2 alavancas, 1º → 3 788 | 16 itens, 2 alavancas, 2º → 3 538 | P1 por 250 (= 2 frutas) |

(Itens × 118 de média; Bem-Estar 100 → 500; tempo abaixo do alvo → 600 para os dois.)

Leitura: a chegada vale tanto quanto **2 frutas**, e o tempo não diferencia quem está abaixo do
alvo. O jogo premia coletar mais, e a coleta favorece quem está na frente (cenário A).

### 2.4 Existe recuperação?

Pouca. Três laços de reforço favorecem quem está na frente ou pune quem já está mal:

1. **Comida compartilhada:** o líder coleta, o perseguidor encontra vazio.
2. **Elevador compartilhado:** quem chega quando o outro está subindo espera a volta (até ~2-3 s
   por andar, conforme a altura) — **a validar** no playtest.
3. **Bem-Estar baixo → mais lento → fadiga**, que só alcança quem já comeu muito (MDA, "Monopoly").

O único alívio para quem está atrás é involuntário: o portão aberto pelo líder.

### 2.5 A dificuldade aumenta de maneira consistente?

**Não.** A Fase 1 tem 10 tiles de espinho e 12 de plataforma que cai; a Fase 2 tem 3 e 0. A Fase 4
tem 18 de espinho; a 5, 5. O gerador sorteia por pesos por fase, sem curva. Tempos-alvo vão de
100 a 180 s sem relação com o número de mecânicas.

### 2.6 Os jogadores têm oportunidades equivalentes?

Quase. Pontos a conferir:

- **P2 larga 1 tile à frente do P1** em todas as fases (`.12` no mapa), ~0,14 s. Num empate de
  corrida, decide. Corrigir: largadas equidistantes da primeira bifurcação.
- Distância de cada porta ao último elevador: **a validar** (portas lado a lado, `EE`).
- Teclado: setas e WASD têm ergonomia diferente; *ghosting* pode afetar um lado — **a validar**.

### 2.7 A habilidade é recompensada sem eliminar a estratégia?

Hoje a habilidade conta pouco (um único arco de pulo) e a estratégia não existe (atalho dominado).
As propostas de gameplay G1 (pulo variável) e F1 (pico e queda) atacam os dois lados.

## 3. Modelo do atalho com "pico e queda" (proposta F1)

Por bifurcação, para quem pega o atalho e come os 2 lanches:

```text
ganho_bruto = S                       (tempo que o corredor economiza sem efeito nenhum — medir em E1)
ganho_pico  = (m_pico − 1) × t_pico   (em segundos equivalentes)
perda_queda = (1 − m_queda) × t_queda × k_terreno
saldo       = S + ganho_pico − perda_queda − custo_futuro_do_BemEstar
```

Com os valores experimentais de F1 (pico ×1,3 por 2 s, renovado pelo 2º lanche ≈ 3,1 s de pico;
queda ×0,6 por 4 + 2 s):

| Situação | Ganho do pico | Perda da queda | Saldo sem S |
| --- | --- | --- | --- |
| Queda em piso livre | +0,9 s | −2,4 s | **−1,5 s** |
| Queda em lama (×0,7 sobre ×0,6) | +0,9 s | ~−3,5 s | **−2,6 s** |
| Fruta no desvio (1,5 s) aos 1,5 s de queda | +0,9 s | −0,6 s − 1,5 s | **−1,2 s** |
| Queda num espinho (volta ao checkpoint) | +0,9 s | −2,4 s − retorno | muito negativo |

**Alvo de projeto (experimental):** S ≈ 3 s por bifurcação. Assim o atalho bem jogado ganha ~1,5 s
e o mal jogado (lama, espinho) perde 1 a 3 s ou mais. O número que manda é **S**, controlado pelo
comprimento do corredor no gerador (`fast_gap`).

**Custo de Bem-Estar:** 2 lanches tiram ~28. De 100 vai a ~72 (faixa ×0,95) até a próxima fruta.
Em ~10 s de trecho, isso custa ~0,5 s. Três atalhos seguidos sem fruta levam a ~16 → fadiga.

**Risco de estratégia dominante nova:** pegar o atalho **só no último andar**, quando o custo
futuro é zero. Se os dois fizerem isso, o final fica previsível. Mitigação a testar: a cesta
disputada (F4) no caminho longo do último andar, ou S menor no último andar.

## 4. Pontuação: as duas alternativas

### Alternativa A (recomendada, decisão D1) — vence quem chega

| Elemento | Regra |
| --- | --- |
| Vitória na fase | Primeiro a cruzar a própria porta |
| Empate (mesmo passo de física) | Bem-Estar final, depois menos fadigas |
| Estrelas por jogador | ★ chegou · ★ Bem-Estar final ≥ 60 · ★ tempo ≤ alvo |
| Copa (3 fases) | vitória 3, derrota 1, +1 por estrela |
| Não concluiu | 0 pontos de copa, 0 estrelas |

Risco: para quem nunca pega atalho, os saudáveis viram enfeite (já está em 100 e ganha a estrela
de Bem-Estar sem esforço). Se o playtest confirmar, trocar a 2ª estrela por "colheita" (≥ 60% dos
saudáveis da sua cópia) — o que faz o desvio pela fruta custar tempo de verdade.

### Alternativa B — placar rebalanceado

Se a equipe preferir manter o placar detalhado:

| Parcela | Hoje | Proposta experimental | Motivo |
| --- | --- | --- | --- |
| Chegada | 500 / 250 | 1 000 / 0 | Chegar primeiro tem de pesar |
| Tempo | 600 até o alvo, linear até 1,5× | **100 por segundo de vantagem** sobre o rival, até 1 000 | Diferencia quem está abaixo do alvo |
| Saudáveis | 80-160 cada | ÷ 2 (40-80) | Coleta deixa de dominar |
| Bem-Estar | × 5 | × 5 | Mantém |
| Lanche | −100 cada | −50 cada | O atalho bem jogado pode compensar |
| Alavanca | 150 | 100 | Proporcional |

Simulação do cenário B da §2.3 com a Alternativa B: P1 (chega 6 s antes, 12 itens) =
1 000 + 600 + 708 + 200 + 500 = **3 008**; P2 (20 itens) = 0 + 0 + 1 180 + 200 + 500 = **1 880**.
Quem chega primeiro com 6 s de vantagem vence; com só 1 s de vantagem e os mesmos 8 itens a
menos, P1 ainda vence por ~630. A chegada decide; a coleta pesa no placar.

### Tempos-alvo

Os alvos atuais ficam logo acima do tempo do robô, que é mais lento que uma pessoa no caminho
longo. Proposta experimental: **alvo inicial = 0,85 × tempo do robô no longo**, recalibrado para a
**mediana dos jogadores** depois do primeiro playtest (estrela de tempo deve ser alcançada por
~30-50% das partidas — hipótese a ajustar).

## 5. Tabela de balanceamento

| Mecânica | Valor atual | Problema | Proposta | Justificativa | Como validar |
| --- | --- | --- | --- | --- | --- |
| Efeito do lanche | ×0,5 por 5 s, renova | Atalho dominado; "pesado" | Pico ×1,3 por 2 s → queda ×0,6 por 4 s (+2 s por lanche encadeado, máx. 8 s) | Risco e recompensa; tira a conotação de peso | E1-E3; playtest: escolha do atalho entre 25% e 75% |
| Corredor curto (S) | não medido | — | ≈ 3 s de economia bruta por bifurcação | Torna o saldo positivo só se bem jogado | E1 por fase |
| Fruta durante a queda | — | — | Encerra a queda | Contra-jogo e combinação | E3 |
| Comida compartilhada | Única | Bola de neve | Cópia por jogador + 1 cesta disputada por andar | Remove o laço 1 | Playtest: trocas de liderança |
| Vitória | Pontos | Coleta decide | Chegada (Alt. A) ou placar Alt. B | Pilar 1 | Playtest: % de vitórias de quem chega primeiro |
| Bônus de tempo | Satura no alvo | Não diferencia | Estrela (A) ou vantagem sobre o rival (B) | Diferenciar quem é rápido | Simulação §4 |
| Faixas de Bem-Estar | piso ×0,70 | Laço 3 | Piso ×0,80; faixa saudável com ápice +0,1 s | Recuperação e recompensa | E4; rotas no pior caso |
| Fadiga | 2 s sem aviso | Pune quem perde | Aviso 1 s + parada 1,2 s | Legível e menos punitiva | Teste PlayMode; playtest de frustração |
| Largada | P2 1 tile à frente | Desigual | Equidistante | Justiça | Teste EditMode nos 10 mapas |
| Elevador | Compartilhado | Espera favorece o líder | Um elevador por jogador ou elevador que leva os dois | Remove o laço 2 | Medir espera no playtest |
| Tempo-alvo | ≈ robô | Fácil demais para pessoas | 0,85 × robô, depois mediana humana | Estrela com significado | Playtest |
| Curva de dificuldade | Sorteio por peso | F1 entre as mais difíceis | Uma mecânica por fase, introdução em 4 tempos | Progressão | Contagem por fase + taxa de conclusão |
| Velocidade/pulo base | 7 t/s, ápice 2,0 | — | Manter | Rotas provadas; mexer é caro | — |
| Espinho | −5 e checkpoint | — | Manter | Já é rápido e justo | Playtest: frequência de quedas |

## 6. Experimentos (robô e playtest)

| # | Experimento | Ferramenta | Pergunta |
| --- | --- | --- | --- |
| E1 | Robô no curto **sem efeito de comida** | `LevelPlayModeTests`, modo novo | Quanto o corredor economiza (S) em cada fase? |
| E2 | Robô "um atalho" (só a bifurcação k) | Idem | Qual o saldo de pegar uma bifurcação, hoje e com F1? |
| E3 | Robô com F1: queda em piso livre / lama / fruta no desvio | Idem | O saldo da §3 se confirma? |
| E4 | Rotas com piso de Bem-Estar ×0,80 e arco novo (G1) | `LevelRoutes` | As 10 fases continuam completáveis no pior caso? |
| E5 | Playtest com telemetria de rota | Log local por partida (ver qualidade Q7) | Cada bifurcação fica entre 25% e 75% de escolha do atalho? |
| E6 | Playtest de disputa | Log + observação | Trocas de liderança por partida; % de vitória de quem chega primeiro |
| E7 | Iniciantes × experientes | Playtest em pares misturados | Assistência por jogador equilibra? |
| E8 | Tempos humanos por fase | Log | Mediana para recalibrar alvos |

Os limiares (25-75%, 30-50% de estrela) são pontos de partida tirados da regra de que nenhuma
opção deve ser obrigatória nem inútil; ajustar depois da primeira rodada de dados.
