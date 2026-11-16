# Roadmap do produto final

Consolida `EVOLUCAO_AUDITORIA.md`, `EVOLUCAO_VISAO_PRODUTO.md`, `EVOLUCAO_REFERENCIAS.md`,
`EVOLUCAO_GAMEPLAY.md`, `EVOLUCAO_ART_DIRECTION.md`, `EVOLUCAO_UI_UX.md`,
`EVOLUCAO_BALANCEAMENTO.md` e `EVOLUCAO_QUALIDADE.md`. Os códigos (F1, U4, Q2…) remetem a esses
documentos.

**Decisões D0 a D8 aprovadas pelo usuário em 11/11/2026, todas na opção recomendada.** A
implementação segue a ordem das fases A a E.

## 1. Decisões (aprovadas em 11/11/2026)

| # | Decisão | Recomendação | Se recusada |
| --- | --- | --- | --- |
| D0 | Fazer um **playtest de linha de base com a v1.0.0** antes de mudar regras | **Sim** — confirma bola de neve, câmera e *ghosting*, hoje só inferidos | Seguir com as inferências do código; risco de corrigir o problema errado |
| D1 | Vitória: **chegada** (+ estrelas e copa) ou placar rebalanceado | **Chegada** | Placar Alternativa B (`EVOLUCAO_BALANCEAMENTO.md` §4) |
| D2 | Lanche: **pico e queda** no lugar de "pesado" (sai a palavra "pesado") | **Sim** | Manter o peso; o atalho continua castigo e o discurso de "escolha" sai do jogo |
| D3 | Comida **por jogador** + uma cesta disputada por andar | **Sim** | Manter compartilhada e medir a bola de neve no D0 |
| D4 | Fase 1 vira **tutorial curto**; cada fase ganha um elemento dono | **Sim** | Manter as 10 fases e só reordenar |
| D5 | Assumir "mecanismos" e remover o código cooperativo morto | **Sim** | Manter o nome "puzzle" sem puzzles |
| D6 | Botão Interagir | **Remover** | Dar um uso (custo extra, nenhum ganho identificado) |
| D7 | Direção de arte | **A — Estufa ilustrada** (estilo do menu) | B (pixel) ou C (papel), ambas de custo alto |
| D8 | Público principal de 10 a 16 anos, em pares | **Sim** | Público geral, sem ajuste de leitura nem assistência |

## 2. Escalas de priorização

Pontuação **heurística** para ordenar o trabalho, não medida científica.

- **Impacto (I), 1 a 5:** 1 = quase imperceptível; 3 = melhora percebida numa área; 5 = muda a
  experiência central ou cumpre um pilar.
- **Confiança (C):** 0,5 = hipótese de design; 0,8 = evidência indireta (código, números,
  referências); 1,0 = medido ou observado.
- **Esforço (E):** dias de uma pessoa, estimados.
- **Prioridade = (I × C) / E.** Itens marcados *habilitador* não têm pontuação própria: valem
  pelo que destravam.

Classes: **P0** obrigatório para o produto final · **P1** alto impacto · **P2** evolução · **P3** opcional.

## 3. Backlog priorizado

### P0 — obrigatório

| ID | Item | I | C | E | Pontos | Depende de |
| --- | --- | --- | --- | --- | --- | --- |
| — | Largadas equidistantes (P2 larga 1 tile à frente) | 3 | 1,0 | 0,5 | 6,0 | — |
| F5 | Feedback de coleta (ícone + valor, nomes em português) | 4 | 1,0 | 1 | 4,0 | — |
| U1 | Contagem 3-2-1 com prévia da porta | 3 | 1,0 | 1 | 3,0 | — |
| A-P | P1 ≠ P2: silhueta da folha + número + contorno | 5 | 1,0 | 2 | 2,5 | — |
| D0 | Playtest de linha de base (5 pares) + registro de partida (Q7) | 5 | 1,0 | 2+1 | 1,7 | Q7 |
| F3 | Fruta corta a queda + fruta no desvio após cada reencontro | 4 | 0,8 | 1,5 | 2,1 | F1 |
| F4 | Coleta por jogador + cesta disputada | 4 | 0,8 | 1,5 | 2,1 | Q2 |
| F1 | Lanche "pico e queda" | 5 | 0,8 | 2 | 2,0 | Q1 |
| C1 | Vitória pela chegada + estrelas | 5 | 0,8 | 2 | 2,0 | D1 |
| O4 | Bifurcação que se explica (ícones, prévia de câmera) | 4 | 0,8 | 2 | 1,6 | F1 |
| U2 | Seleção de fase | 4 | 1,0 | 1,5 | 2,7 | Q6 |
| U4 | HUD mínimo + estados perto do personagem | 4 | 1,0 | 3 | 1,3 | Q4, Q9 |
| U5 | Resultado que explica ("por quê" + estrelas + revanche) | 4 | 0,8 | 3 | 1,1 | C1 |
| — | Registro de origem e licença de toda arte e som + créditos (U8) | 3 | 1,0 | 1 | 3,0 | — |
| Q1 | Efeitos do jogador (`PlayerEffects`) | habilitador | | 1,5 | — | — |
| Q2 | Coleta por jogador | habilitador | | 1,5 | — | — |
| Q3 | Balanceamento em asset (`SO_Balance`) | habilitador | | 1 | — | — |
| Q4 | Extrair câmera e HUD do `LevelBootstrap` | habilitador | | 1,5 | — | — |
| Q6 | Salvamento (fases, estrelas, recordes, opções) | habilitador | | 1 | — | — |
| Q7 | Registro de partida | habilitador | | 1 | — | — |

### P1 — alto impacto

| ID | Item | I | C | E | Pontos |
| --- | --- | --- | --- | --- | --- |
| B2 | Fadiga anunciada e curta | 3 | 0,8 | 0,5 | 4,8 |
| O3 | Remover código cooperativo morto e `VerticalSlice_Core` da build | 2 | 1,0 | 0,5 | 4,0 |
| G2 | Correção de quina | 3 | 0,8 | 1 | 2,4 |
| U6 | Como jogar em 3 cartões + "Treinar agora" | 3 | 0,8 | 1 | 2,4 |
| A-F | Alimentos no estilo do atlas (13 existem, 5 a desenhar) | 4 | 1,0 | 2 | 2,0 |
| G4 | *Squash & stretch*, poeira, tremor leve | 3 | 0,8 | 1,5 | 1,6 |
| C2 | Copa de 3 fases | 4 | 0,8 | 2 | 1,6 |
| C5 | Câmera com limite + seta na borda | 4 | 0,8 | 2 | 1,6 |
| U3 | Tela "Prontos?" | 3 | 0,8 | 1,5 | 1,6 |
| — | Sons por estado (pico, queda, cesta) | 3 | 0,8 | 1,5 | 1,6 |
| G1 | Pulo variável + ápice suspenso | 4 | 0,8 | 2,5 | 1,3 |
| — | Estrelas e recordes persistidos na seleção de fase | 3 | 0,8 | 2 | 1,2 |
| — | Assistência por jogador | 3 | 0,8 | 2 | 1,2 |
| B1 | Faixas de Bem-Estar com piso ×0,80 e bônus na faixa saudável | 3 | 0,5 | 1,5 | 1,0 |
| U7 | Opções: controles, assistência, texto maior (com Q8, Q9) | 3 | 1,0 | 3 | 1,0 |
| O2 | Marco visual por fase | 3 | 0,8 | 3 | 0,8 |
| — | Música por fase ou por intensidade | 3 | 0,8 | 3 | 0,8 |
| O1 | Uma fase, um elemento (gerador com trechos autorais, 2 obstáculos novos, regeração) | 5 | 0,8 | 7 | 0,6 |
| A-E | Estilo único na fase: plataformas, estufa, placas, fundo com valor separado | 5 | 1,0 | 8 | 0,6 |

### P2 — evolução

| ID | Item | I | C | E | Pontos |
| --- | --- | --- | --- | --- | --- |
| — | Contra o relógio solo | 2 | 0,8 | 1 | 1,6 |
| C4 | Colisão entre jogadores (decidir após playtest) | 2 | 0,5 | 0,5 | 2,0 |
| F2 | Famílias de alimento com efeito próprio | 3 | 0,5 | 3 | 0,5 |
| Q10 | CI com testes do Unity; corridas do robô à noite | 2 | 1,0 | 1 | 2,0 |
| — | Elevador por jogador (se o playtest mostrar espera) | 3 | 0,5 | 2 | 0,8 |

### P3 — opcional

Chapéus e cores cosméticos · personagens desbloqueáveis · fantasma do recorde · alavanca de rota
(abre o seu portão, fecha o do rival) · mais fases além de 10.

### Matriz impacto × esforço

| | Esforço baixo (≤ 1,5 dia) | Esforço alto (≥ 2 dias) |
| --- | --- | --- |
| **Impacto alto (4-5)** | **Ganhos rápidos:** F5, U2, F3, F4, largadas | **Apostas centrais:** F1, C1, P1≠P2, U4, U5, O4, O1, estilo único, C2, C5 |
| **Impacto médio (≤ 3)** | **Polimento barato:** U1, B2, G2, O3, U6, contra o relógio | **Adiar ou cortar:** famílias de alimento, música por fase, cosméticos, personagens |

## 4. Roadmap por fases

Estimativas em dias de uma pessoa. Com duas pessoas em paralelo, o calendário é ~metade.

### Fase A — Fundação e identidade (≈ 9 dias)

- **Objetivo:** decidir com dados e preparar a base para mudar regras com segurança.
- **Entregáveis:** decisões D0-D8 registradas (ADR curto por decisão de regra); relatório do
  playtest de linha de base; `SO_Balance`; registro de partida; salvamento; extração de câmera e
  HUD; código morto removido; largadas equidistantes; registro de licenças.
- **Tarefas:** Q7 → D0 (5 pares, v1.0.0) · Q3 · Q6 · Q4 · O3/Q5 · largadas + teste de equidade ·
  inventário de origem das artes e sons · guia de estilo da direção A (paleta, fontes, regras de
  contorno) em uma página.
- **Dependências:** aprovação do D0; licença da fonte escolhida.
- **Critérios de aceite:** relatório de linha de base com as métricas de `EVOLUCAO_QUALIDADE.md`
  §6; todos os testes atuais passam; balanceamento ajustável sem recompilar.
- **Riscos:** o playtest pode contradizer a auditoria (ex.: ninguém sente bola de neve) — isso é
  o objetivo, não um problema; recrutar 5 pares leva tempo.
- **Condição para avançar:** decisões D1-D3 aprovadas à luz do playtest.

### Fase B — Gameplay (≈ 19 dias)

- **Objetivo:** tornar a escolha real e a corrida decisiva.
- **Entregáveis:** pico e queda (F1), fruta corta a queda (F3), coleta por jogador + cesta (F4),
  vitória pela chegada + estrelas (C1), bifurcação legível (O4), feedback de coleta (F5),
  contagem (U1), P1 ≠ P2 (A-P), fadiga anunciada (B2), correção de quina (G2), pulo variável
  (G1), câmera com limite (C5).
- **Tarefas:** Q1 → F1 → F3; Q2 → F4; C1 + resultados mínimos; robô com modos E1-E4; recalibrar
  `JumpBudget` para G1 e rodar `LevelRoutes`; regenerar fases com fruta no desvio.
- **Dependências:** Fase A; D1-D3.
- **Critérios de aceite:** robô confirma o modelo de `EVOLUCAO_BALANCEAMENTO.md` §3 (atalho bem
  jogado ganha, mal jogado perde) nas 10 fases; 10 corridas completas; testes novos para cada
  item; playtest rápido (2 pares) mostra escolha do atalho entre 25% e 75%.
- **Riscos:** o atalho virar dominante (mitigação: S e duração da queda em `SO_Balance`); G1
  quebrar rotas (mitigação: `LevelRoutes` antes do merge).
- **Condição para avançar:** nenhuma bifurcação fora de 15-85% no playtest rápido.

### Fase C — Conteúdo e progressão (≈ 18 dias)

- **Objetivo:** fases distintas, ensino gradual e motivo para voltar.
- **Entregáveis:** gerador com trechos autorais; nova Fase 1 tutorial; ordem por elemento dono;
  esteira e aspersor; marcos por fase; seleção de fase; estrelas e recordes; copa de 3 fases;
  tela "Prontos?"; Como jogar em cartões.
- **Tarefas:** O1, O2, U2, U3, U6, C2, recalibrar tempos-alvo (0,85 × robô).
- **Dependências:** Fase B (regras estáveis antes de desenhar fases).
- **Critérios de aceite:** cada fase tem um trecho que apresenta o elemento sem risco; robô
  completa as 10 fases nos dois caminhos; seleção de fase em ≤ 3 cliques; copa com placar
  persistido entre cenas.
- **Riscos:** escopo de conteúdo cresce (mitigação: se faltar tempo, 6 fases com dono + 4 de
  combinação, em vez de 10 com dono).
- **Condição para avançar:** taxa de conclusão ≥ 90% nas fases 1-5 em playtest.

### Fase D — Polimento (≈ 25 dias)

- **Objetivo:** tirar a cara de protótipo.
- **Entregáveis:** estilo único na fase (direção A); alimentos do atlas; HUD mínimo; resultado
  que explica; *game feel* (G4); sons por estado; música por fase ou intensidade; opções com
  controles, assistência e texto maior; TextMeshPro.
- **Tarefas:** A-F, A-E, U4, U5, U7, Q8, Q9, G4, áudio.
- **Dependências:** Fase C (arte sobre fases definitivas); D7.
- **Critérios de aceite:** critérios de `EVOLUCAO_ART_DIRECTION.md` §7 e `EVOLUCAO_UI_UX.md` §6;
  capturas 720p e 1080p regeradas; personagem ≥ 50 px no zoom máximo.
- **Riscos:** arte gerada inconsistente entre lotes (mitigação: guia de estilo da Fase A e uma
  prancha de referência por lote).
- **Condição para avançar:** nenhum P0 aberto.

### Fase E — Validação e release (≈ 8 dias)

- **Objetivo:** provar com pessoas e entregar.
- **Entregáveis:** 2 rodadas de playtest (5 pares cada) com relatório; ajustes de balanceamento
  em `SO_Balance`; regressão completa (EditMode, PlayMode, 10 corridas); medição de desempenho
  na máquina alvo; build Windows; `TEST_REPORT.md`, `GAMEPLAY.md`, `CONTROLS.md`,
  `RELEASE_NOTES.md` atualizados; créditos; release no GitHub.
- **Dependências:** Fase D.
- **Critérios de aceite:** metas de `EVOLUCAO_QUALIDADE.md` §6 revisadas e atingidas ou
  justificadas; 60 fps na máquina alvo (ou meta revista com medição); zero falhas nos testes.
- **Riscos:** playtest final pedir mudança de regra (mitigação: só ajustar números; mudança de
  regra vai para a versão seguinte).
- **Condição para lançar:** relatório de playtest aprovado pela equipe.

**Total estimado:** ≈ 79 dias de uma pessoa (≈ 8 semanas com duas pessoas em paralelo). Estimativa de
ordem de grandeza; revisar ao fim da Fase A.

## 5. Escopo mínimo da versão final

### Indispensável

- Decisões D1-D3 implementadas: chegada decide, pico e queda, fruta corta a queda, comida por
  jogador + cesta.
- Bifurcação legível, feedback de coleta, contagem de largada, largadas equidistantes.
- P1 ≠ P2 por forma; HUD mínimo; resultado que explica; seleção de fase.
- Fase-tutorial + pelo menos 6 fases com elemento dono (as outras podem ser de combinação).
- Alimentos e fase num estilo só (direção A).
- Salvamento de fases liberadas e opções.
- Registro de origem e licença + créditos.
- Dois playtests documentados; regressão completa; build e release.

### Desejável (entra se a Fase C terminar no prazo)

Copa de 3 fases · estrelas e recordes · "Prontos?" · pulo variável · câmera com seta na borda ·
assistência por jogador · controle e remapeamento · música por fase.

### Atualizações futuras

Famílias de alimento · contra o relógio · obstáculos além de esteira e aspersor · cosméticos ·
personagens · fantasma · alavanca de rota · mais fases.

## 6. Ordem recomendada em uma linha

**Medir a v1.0.0 com pessoas → decidir as regras → consertar a escolha e a vitória → dar a cada
fase uma identidade → unificar a arte e a interface → medir de novo e lançar.**
