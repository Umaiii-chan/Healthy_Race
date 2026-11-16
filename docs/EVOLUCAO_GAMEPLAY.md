# Evolução — gameplay, mecânicas e progressão

Base: `EVOLUCAO_AUDITORIA.md` (o que existe), `EVOLUCAO_VISAO_PRODUTO.md` (pilares P1-P5) e
`EVOLUCAO_REFERENCIAS.md`. Os valores numéricos aqui são **pontos de partida experimentais**,
não parâmetros validados; a calibração está em `EVOLUCAO_BALANCEAMENTO.md`.

Cada proposta segue o formato: problema · evidência · solução · benefício · efeitos negativos ·
custo · dependências · critério de aceite. Custos em dias de uma pessoa (estimativa).

## 0. A proposta em uma página

O jogo hoje tem uma corrida, uma escolha morta e uma contagem de itens. A proposta muda **três
regras** e **mantém quase todo o resto**:

| Regra | Hoje | Proposta |
| --- | --- | --- |
| Quem vence | Mais pontos (coleta ≈ 63%) | **Quem chega primeiro**; pontos viram estrelas e placar de copa |
| O lanche rápido | Peso: meia velocidade por 5 s, −Bem-Estar, −100 | **Pico e queda**: impulso curto, depois sonolência; −Bem-Estar |
| A comida | Única, disputada, vale pontos | **Cada jogador tem a sua**; a fruta corta a sonolência; um item raro por andar é disputado |

Com isso, o atalho passa a ser **às vezes certo** (o impulso ganha tempo, a queda pode perder
mais), quem está atrás tem uma saída **declarada e arriscada** (apostar no atalho), e a comida
vira **recurso de corrida** em vez de contador. Os mapas, obstáculos, física e construtor de fases
continuam.

Decisões de aprovação: D1 a D6 no fim deste documento.

## 1. Movimentação e controles

Estado atual: velocidade 7 t/s, aceleração no chão 90 t/s², ar instantâneo, pulo de altura fixa
(ápice 2,0 tiles, 0,67 s no ar), coyote 0,10 s, buffer 0,12 s.

### G1 — Pulo variável e ápice suspenso · P1

- **Problema:** um único arco de pulo; não existe "pulo bem feito".
- **Evidência:** `PlayerMovement` não lê a soltura do botão; gravidade igual na subida e na descida.
- **Solução:**
  - soltar o pulo durante a subida multiplica a velocidade vertical por 0,5 (pulo curto ~1 tile);
  - gravidade ×0,5 enquanto |v.y| < 2 t/s **e** o botão está segurado (ápice suspenso, à moda Celeste);
  - gravidade ×1,4 na descida (pousa mais firme);
  - **ápice máximo continua 2,0 tiles** — o `JumpBudget` é recalculado para o arco novo e o
    orçamento de rotas (degrau e vão de 1 tile no pior Bem-Estar) tem de continuar valendo.
- **Benefício:** controle expressivo (pilar 4), pulos curtos sob tetos baixos, aterrissagem precisa.
- **Efeitos negativos:** muda o alcance horizontal; algumas rotas podem ficar mais fáceis ou
  quebrar. Exige a ação de pulo com evento de soltura no `PlayerInputReader`.
- **Custo:** 1,5 dia + recalibração do `JumpBudget`.
- **Dependências:** nenhuma. Fazer antes de mexer em fases.
- **Aceite:** testes de `JumpBudget` atualizados; `LevelRoutes` passa nas 10 fases com o arco novo;
  robô termina as 10 corridas; teste PlayMode prova pulo curto (soltar em 0,1 s → ápice < 1,2 tile).

### G2 — Correção de quina · P1

- **Problema:** bater a quina da cabeça numa plataforma corta o pulo (comum sob tetos de 1,5 tile).
- **Solução:** se a cabeça colide a até 0,25 tile da borda, deslocar o jogador para o lado livre.
- **Benefício:** menos "o jogo me roubou". **Negativo:** pode atravessar quina não intencional.
- **Custo:** 1 dia. **Aceite:** teste PlayMode com quina a 0,2 tile: o pulo continua.

### G3 — Controle no ar com aceleração · A validar

- **Problema:** mudar de direção no ar é instantâneo; o pulo não tem compromisso.
- **Alternativas:** (a) manter instantâneo; (b) aceleração no ar de 60 t/s².
- **Recomendação:** **manter (a)** até o playtest. O público é casual; ar instantâneo é
  perdoador. Só mudar se jogadores experientes acharem o movimento "flutuante".

### G4 — Feedback de movimento · P1

*Squash & stretch* no pulo e na aterrissagem, poeira em queda alta, pequeno tremor de câmera em
queda de 3+ tiles (desligável pela opção que já existe). Custo 1,5 dia. Aceite: visível nas
capturas do `CharacterEvidence`; desligado com "reduzir flashes e tremores".

### Habilidades avaliadas e não recomendadas

| Habilidade | Avaliação | Decisão |
| --- | --- | --- |
| Pulo duplo | Quebra o orçamento de rota (degrau de 1 tile) e torna o corredor curto (teto baixo) irrelevante | Não |
| Dash permanente | Aumenta precisão exigida e vantagem do jogador experiente; conflita com público | Não como base |
| Dash como efeito de comida | Ver F2 (banana); temporário e visível | Talvez, P2 |
| Escalar trepadeira | Mecânica nova cara (estado, animação, rotas) | Não para o 1.x |
| Botão Interagir | Mapeado e sem uso hoje | **Remover** do mapa e das telas, ou dar uso (alavanca por botão é pior que por toque) |

## 2. Alimentação como mecânica central

Princípio: **efeitos de jogo, não afirmações nutricionais.** Nenhum alimento é chamado de "ruim".
As categorias falam de *energia rápida* e *energia estável*, que é o recorte que o tema permite
sem simular nutrição.

### F1 — Lanche rápido: pico e queda · P0

- **Problema:** o atalho é dominado; o "peso" só pune. E "pesado" liga comida a peso corporal —
  enquadramento arriscado para público escolar (ver `EVOLUCAO_REFERENCIAS.md` §7.5).
- **Evidência:** curto mais lento e 7 500 a 11 900 pontos atrás em todas as fases (`TEST_REPORT.md`).
- **Solução:**
  - ao comer, **pico**: velocidade ×1,3 por 2 s (efeito visual de faísca, som próprio);
  - depois, **queda** ("sonolento"): velocidade ×0,6 por 4 s (bocejo, olhos pesados — sem mudar
    o corpo);
  - comer outro lanche no pico **renova o pico**, mas a queda seguinte dura +2 s (máx. 8 s):
    encadear lanches é possível e cada vez mais caro;
  - Bem-Estar −10 a −18, como hoje;
  - **fruta ou água durante a queda encerra a queda** (ver F3).
- **Benefício:** o atalho vira aposta (pilar 2). O tema aparece na pele do controle: energia
  rápida que acaba rápido. A palavra "pesado" sai do jogo.
- **Efeitos negativos:** se o pico for forte demais, o atalho vira dominante. Por isso o
  parâmetro principal é **onde a queda acontece** (ver balanceamento §3).
- **Custo:** 2 dias (substitui `ApplySluggishness` por uma curva de dois estágios; arte e som).
- **Dependências:** F3 para o contra-jogo; balanceamento §3 para os números.
- **Aceite:** o robô, comendo os 2 lanches de uma bifurcação e seguindo, chega ao reencontro
  **antes** do longo; o robô que entra em lama ou espinho durante a queda chega **depois**; teste
  PlayMode da curva (pico, queda, renovação, corte pela fruta).

### F2 — Famílias de alimento com um efeito cada · P2

Hoje 10 saudáveis só diferem no número. Proposta em quatro famílias, cada uma com **um**
efeito curto e visível:

| Família | Alimentos atuais | Efeito proposto (experimental) |
| --- | --- | --- |
| Fruta | maçã, banana, laranja, morango, melancia | +Bem-Estar; encerra a queda do lanche |
| Água | água | +Bem-Estar; encerra a queda e a fadiga em curso |
| Refeição | peixe, brócolis, cenoura, abacate | +Bem-Estar; "firme" por 6 s: lama e espinho não reduzem velocidade ao sair |
| Lanche rápido | hambúrguer, pizza, batata, cachorro-quente, rosquinha, bala, salgadinho, refrigerante | Pico e queda (F1) |

- **Por que P2 e não P0:** F1 + F3 já criam a decisão. Famílias acrescentam profundidade, mas
  também texto a aprender. Entram só se o playtest mostrar que a escolha de fruta × refeição é
  percebida.
- **Custo:** 3 dias. **Aceite:** depois de 2 partidas, jogadores descrevem o efeito de pelo
  menos 2 famílias sem ler (pergunta do formulário).

### F3 — Fruta corta a queda: a "combinação" · P0 (junto com F1)

- **Solução:** fruta ou água coletada durante a queda a encerra na hora. Os mapas passam a ter
  **uma fruta logo depois de cada reencontro** de bifurcação (o gerador já posiciona itens por
  trecho).
- **Benefício:** cria a combinação "lanche + fruta" — decisão em dois passos e mensagem positiva
  (equilíbrio, não proibição). É a única combinação proposta; outras ficam fora do escopo.
- **Negativo:** se a fruta estiver sempre no caminho, a queda deixa de existir. Por isso a fruta
  fica **num desvio de 1-2 s** (um degrau acima), não na linha reta.
- **Custo:** 0,5 dia de código + gerador de fases.
- **Aceite:** robô "lanche + desvio da fruta" vs "lanche sem fruta" medido por bifurcação.

### F4 — Coleta por jogador + item disputado · P0 (decisão D3)

- **Problema:** comida única. Quem está na frente pega tudo; quem vem atrás encontra vazio
  (inferência pelo código; o robô não disputa — **a validar**).
- **Alternativas:**
  - (a) **Cada jogador tem a sua cópia** de cada item comum (o item coletado fica translúcido
    para quem já pegou). Remove a bola de neve; perde a disputa pela comida.
  - (b) Manter compartilhado; item reaparece 4 s depois.
  - (c) Manter como está e medir.
- **Recomendação:** **(a)** + **um item disputado por andar** ("cesta da estufa", destaque
  dourado, efeito: pico sem queda por 3 s). A disputa fica concentrada num ponto visível, onde é
  divertida, e some dos 30 itens espalhados, onde é bola de neve.
- **Custo:** 1,5 dia (estado por jogador no `FoodCollectible`, apresentação translúcida, item novo).
- **Aceite:** teste PlayMode: P1 coleta, item continua para P2; item disputado some para os dois.

### F5 — Feedback na coleta · P0

- **Problema:** a coleta mostra uma partícula; ninguém sabe o que ganhou (B-12, aberto).
- **Solução:** ícone do efeito subindo do personagem (raio para pico, lua para queda, coração
  para Bem-Estar) + número de Bem-Estar ("+12"), 0,8 s. Nomes dos alimentos em português nos
  dados (`_displayName`), usados na tela de "Como jogar".
- **Custo:** 1 dia. **Aceite:** visível em captura; texto ≥ 28 px em 1080p.

## 3. Bem-Estar, fadiga e energia

### B1 — Bem-Estar com efeito perceptível nos dois sentidos · P1

- **Problema:** quem não come lanche fica em 100 e nunca sente o sistema; acima de 25 o efeito
  é de 5-10%.
- **Solução:**
  - faixa saudável (76-100) passa a dar **bônus pequeno e visível**: pulo do ápice suspenso mais
    longo (+0,1 s) e folha brilhando — recompensa de manter o equilíbrio, sem velocidade extra
    (velocidade é do lanche);
  - faixas baixas mantêm a perda, com piso de velocidade ×0,8 (hoje vai a ×0,7) para atenuar o
    laço de reforço apontado pelo MDA;
  - faixa mostrada **no personagem** (já existe na cor e na folha) e com ícone acima da cabeça
    quando muda.
- **Alternativa descartada:** trocar Bem-Estar por uma barra de "energia" consumida ao correr.
  Seria um terceiro recurso para dois jogadores lerem; o Bem-Estar já faz esse papel.
- **Custo:** 1,5 dia. **Aceite:** testes de faixa; rotas continuam válidas no pior caso.

### B2 — Fadiga curta e anunciada · P1

- **Problema:** parada de 2 s com limiar fixo, sem aviso, atinge quem já está perdendo.
- **Solução:** aviso de 1 s antes (personagem ofega, ícone pisca), parada de 1,2 s, +15 de
  Bem-Estar. Mantém o limiar ≤ 20.
- **Custo:** 0,5 dia. **Aceite:** teste PlayMode do aviso; tempo total de parada medido.

### Perguntas do briefing respondidas

| Pergunta | Hoje | Com F1-F5 e B1-B2 |
| --- | --- | --- |
| O jogador entende por que perde desempenho? | Não (só HUD) | Sim: ícone e animação no personagem |
| Os efeitos são perceptíveis? | Só o peso (×0,5) | Pico ×1,3 e queda ×0,6 são fortes; faixa saudável visível |
| O sistema cria decisões? | Não | Atalho agora/depois; desvio da fruta; cesta disputada |
| Existe estratégia dominante? | Sim: nunca pegar o atalho | A medir; risco declarado em balanceamento §5 |
| Quem erra se recupera? | Mal (fadiga pune quem já perde) | Fruta corta a queda; piso de velocidade |
| Gera competição ou frustração? | Frustração para quem cai no corredor | Aposta para quem está atrás |

## 4. Competição entre jogadores

### C1 — Vitória pela chegada · P0 (decisão D1)

- **Problema:** chegar primeiro vale 250 pontos (2 frutas); a corrida não decide.
- **Alternativas:**
  - (a) **Vence quem chega**; pontos viram 3 estrelas por jogador e placar de copa.
  - (b) Placar mantido, rebalanceado: chegada + diferença de tempo ≥ 50% dos pontos (números em
    balanceamento §4).
  - (c) Manter.
- **Recomendação:** **(a)**. É a regra que um visitante entende sem explicação, e é a que o
  título promete. (b) é aceitável se a equipe quiser manter o placar detalhado do 1.0.
- **Estrelas por jogador por fase (proposta):** ★ chegou; ★ Bem-Estar final ≥ 60; ★ tempo ≤ alvo
  da fase. Independentes do rival: quem perde a corrida ainda pode sair com 3 estrelas.
- **Custo:** 2 dias (resultado, estrelas, `ScoreService` simplificado; testes).
- **Aceite:** testes de vitória por chegada, empate (mesmo passo de física → desempate por
  Bem-Estar), DNF; tela de resultado nova (ver UI/UX).

### C2 — Copa de 3 fases · P1

Partida = 3 fases seguidas (escolhidas ou em sequência). Vitória na fase = 3 pontos, derrota = 1,
cada estrela = +1. Placar visível entre as fases. Dá motivo para continuar depois de perder uma.
Custo 2 dias. Aceite: teste de fluxo de copa e placar persistido entre cenas.

### C3 — Recuperação: medir antes, e por escolha · P1

- **Avaliação:** não aplicar *rubber-banding*. Com F4 (comida por jogador) a principal bola de
  neve some. A recuperação natural passa a ser **a aposta no atalho** — quem está atrás tem
  motivo para arriscar, quem está na frente tem motivo para não arriscar. É o mesmo princípio do
  bônus *Solo* do UCH: o risco é a saída.
- **Medir no playtest:** trocas de liderança por partida e diferença de chegada.
- **Só se a medição mostrar bola de neve:** subsídio declarado (ex.: perdedor da fase anterior
  larga com a cesta do primeiro andar já disponível só para ele), nunca punição ao líder.

### C4 — Colisão entre jogadores · A validar

- **Hoje:** jogadores colidem entre si (matriz de colisão cheia). Num corredor de 1,5 tile de
  altura e no elevador, um pode travar o outro.
- **Alternativas:** (a) atravessar o outro; (b) manter colisão.
- **Recomendação:** testar (b) no playtest; se houver bloqueio proposital ou travamento no
  elevador, mudar para (a). Custo 0,5 dia.

### C5 — Câmera com limite · P1

- **Problema:** afasta até a fase inteira (personagem ~30 px).
- **Solução:** zoom máximo limitado (ex.: 28 tiles de altura); acima disso, o jogador fora da tela
  aparece como seta com retrato na borda. Depois que o primeiro chega, a câmera passa a seguir
  só o outro.
- **Alternativa:** tela dividida — já rejeitada pelo usuário em 10/11/2026; não reabrir.
- **Custo:** 2 dias. **Aceite:** testes de `SharedCameraController` para limite e seta; personagem
  ≥ 50 px em 1080p em qualquer situação.

### Interação direta avaliada

| Ideia | Avaliação | Decisão |
| --- | --- | --- |
| Empurrão / ataque | Contra o tema e o público; aumenta frustração | Não |
| Alavanca de rota (abre seu portão e fecha o do rival) | Interação indireta forte, mas exige fases com dois portões | P3, se a copa pedir variedade |
| Cesta disputada (F4) | Disputa concentrada, legível | **Sim** |

## 5. Obstáculos, mecanismos e fases

### O1 — Uma fase, um elemento · P1

- **Problema:** as 10 fases têm a mesma forma e sorteiam os mesmos 5 obstáculos.
- **Solução:** cada fase "é dona" de um elemento, apresentado em quatro tempos (apresentar,
  desenvolver, reviravolta, domínio — ver referências §7.1). O gerador (`build_levels.py`) ganha
  "trechos autorais": blocos desenhados à mão inseridos em posições fixas, além do sorteio.
- **Reordenação proposta:**

  | Fase | Elemento dono | Observação |
  | --- | --- | --- |
  | 1 | Bifurcação e lanche (pico e queda) | Curta, 2 andares, sem espinho: a fase-tutorial |
  | 2 | Lombadas e lama | Hoje Horta Suspensa |
  | 3 | Alavanca e portão | Hoje Pomar das Alavancas |
  | 4 | Espinhos | |
  | 5 | Plataforma que cai | |
  | 6 | **Esteira** (novo) | Feira Livre; anda a favor ou contra |
  | 7 | Elevadores em sequência | Torre |
  | 8 | **Aspersor** (novo) | Jato que empurra; ciclo visível |
  | 9 | Lama + espinho + plataforma | Combinação |
  | 10 | Tudo | Grande Final |

- **Custo:** 6-8 dias (gerador, 2 obstáculos novos, regeração, robô).
- **Aceite:** o robô termina as 10 fases nos dois caminhos; cada fase tem um trecho em que o
  elemento aparece sozinho e sem risco antes do primeiro uso com risco.

### O2 — Marco visual por fase · P1

Um objeto grande e único por fase (regador gigante, caixa d'água, girassol da torre) visível de
qualquer andar, e a porta de chegada anunciada na largada (prévia de 2 s da câmera até a porta).
Custo 3 dias com arte. Aceite: em captura da fase inteira, o marco é identificável.

### O3 — "Puzzles" viram "mecanismos" · P1

- **Avaliação:** puzzle de verdade (parar para pensar) briga com o ritmo de corrida. O jogo não
  tem puzzles hoje; a alavanca é encostar e esperar.
- **Recomendação:** **não criar puzzles**. Assumir "mecanismos" (alavanca, elevador, esteira,
  aspersor) e tirar a palavra *puzzle* do jogo e da documentação. Excluir o código cooperativo
  morto (`CooperativePuzzleController`, `PressureButton`, `PushableBox`).
- **Custo:** 0,5 dia. **Aceite:** build sem referência aos componentes removidos; testes passam.

### O4 — A bifurcação se explica sozinha · P0

- **Problema:** a escolha é anunciada por placa de texto e o jogador não vê o que há em cada lado.
- **Solução:** na entrada de cada bifurcação, o corredor curto mostra o letreiro da lanchonete
  com o ícone de raio (pico) e a estufa mostra a fruta e o obstáculo seguinte; a câmera mostra
  1 a 2 s dos dois lados antes do buraco (zoom suave). Sem palavras.
- **Custo:** 2 dias (arte + gerador). **Aceite:** no playtest, ≥ 80% dos jogadores novos
  descrevem os dois caminhos depois da primeira bifurcação (pergunta do formulário).

## 6. Rejogabilidade e progressão

| Recurso | Valor real | Custo | Prioridade |
| --- | --- | --- | --- |
| Seleção de fase (com fases já liberadas) | Alto: hoje a Fase 7 exige passar por 6 | 1,5 dia | **P0** |
| Estrelas por jogador e recorde por fase (persistidos) | Alto: meta além de vencer o rival | 2 dias | P1 |
| Copa de 3 fases | Alto: partida com arco | 2 dias | P1 |
| Fase-tutorial curta (nova Fase 1) | Alto para primeira impressão (feira) | Incluído em O1 | P1 |
| Assistência por jogador (velocidade do jogo 80%, sem fadiga) | Médio-alto: famílias e iniciantes | 2 dias | P1 |
| Contra o relógio solo (recorde por fase) | Médio: um jogador sozinho | 1 dia (reusa recorde) | P2 |
| Cores e chapéus cosméticos dos brotos | Baixo-médio | 3 dias com arte | P3 |
| Personagens desbloqueáveis | Baixo: mais arte, nenhuma decisão nova | 5+ dias | P3 |
| Fantasma do recorde | Médio, caro | 4 dias | P3 |
| Dificuldades globais (fácil/difícil) | Coberto pela assistência por jogador | — | Não |

## Decisões (aprovadas em 11/11/2026, opção recomendada)

| # | Decisão | Recomendação | Impacto se aprovada |
| --- | --- | --- | --- |
| D1 | Vitória pela chegada (C1-a) ou placar rebalanceado (C1-b) | **Chegada** + estrelas | Muda `ScoreService`, resultados e documentação do 1.0 |
| D2 | Lanche "pico e queda" no lugar de "pesado" (F1) | **Sim** | Muda o efeito central e o texto do jogo |
| D3 | Comida por jogador + cesta disputada (F4-a) | **Sim** | Muda `FoodCollectible`; elimina disputa nos itens comuns |
| D4 | Fase 1 vira tutorial curto e as fases ganham dono (O1) | **Sim** | Regera as 10 fases; tempos e notas do `TEST_REPORT` mudam |
| D5 | Remover "puzzle" e o código cooperativo morto (O3) | **Sim** | Reduz código; muda nomes na pontuação e telas |
| D6 | Botão Interagir: remover ou dar uso | **Remover** | Simplifica controles e telas |
