# Fase 1: Corrida do Equilíbrio — Labirinto Competitivo de Tela Única

Este documento descreve o desenho de nível da **Fase 1** do **Healthy Race** após a
refatoração para labirinto competitivo de tela única, com a composição de rocha
escavada do *Fireboy & Watergirl*.
Ele cobre o enquadramento, o mapa de ladrilhos, os mecanismos de cada metade, o
orçamento de pulo, a distribuição pedagógica dos coletáveis e os critérios
anti-deadlock.

---

## 1. Visão geral

- **Cena**: `Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity`
- **Orquestrador**: `HealthyRace.Level.Phase1Bootstrap` (constrói tudo em código)
- **Área jogável**: $X \in [0, 32]$, $Y \in [0, 18]$ (proporção 16:9)
- **Câmera**: fixa em $(16, 9, -10)$, sem scroll nem zoom, com letterbox via
  `Camera.rect` para que nenhuma proporção de tela mostre além das paredes
- **Jogadores**: P1 e P2 locais, sempre visíveis, com largadas separadas nos cantos
  de baixo e checkpoints próprios
- **Objetivo**: corrida. Quem chega primeiro à saída única do topo vence; a segunda
  chegada tem 20 s antes do DNF
- **Tema pedagógico**: o dilema deixa de ser "rota de cima vs. rota de baixo" e passa
  a ser "pressa vs. bem-estar" em cada escolha de trajeto. Os ultraprocessados
  ficam no piso por onde se passa; os saudáveis exigem desvio, num nicho de salto
  ou num degrau sem saída.

---

## 2. Orçamento de pulo (derivado, não fixado)

O orçamento **não é declarado em constantes**. `JumpBudget`
(`Assets/_Game/Scripts/Player/JumpBudget.cs`) o calcula a partir da física viva:

| Termo | De onde vem |
| :-- | :-- |
| gravidade | módulo de `Physics2D.gravity.y` vezes o `gravityScale` do Rigidbody2D do jogador |
| velocidade de pulo | `PlayerMovement._jumpVelocity` vezes `JumpMultiplier` |
| velocidade horizontal | `PlayerMovement._moveSpeed` vezes `MovementMultiplier` |
| passo de física | `Time.fixedDeltaTime` |

### Integração discreta, não contínua

A fórmula de livro `h = v0^2 / (2g)` **superestima** o ápice, porque a Unity
integra em passos (Euler semi-implícito):

```text
v -= g * dt;
y += v * dt;
```

A perda é de aproximadamente `v0 * dt / 2`. Com `v0 = 12` e `dt = 0.02` isso são
0.12 m, ou 6% do salto. Era essa a divergência entre a documentação e o jogo: a
documentação prometia 2.00 m e a física entregava 1.88 m.

`JumpBudget` simula o integrador, então `Apex` é o valor que o jogador realmente
alcança. `IdealApex` fica disponível apenas para comparação.

### Intenção de projeto

A arena declara o ápice que quer e **deriva** a gravidade do jogador:

```text
PhaseGravityY  = -18
TargetJumpApex = 2.0 m
gravityScale   = JumpBudget.GravityScaleForApex(...)  ->  ~1.888
```

Se a gravidade global, o passo de física ou a velocidade de pulo mudarem, o ápice
continua sendo o declarado. Valores resultantes:

| Grandeza | Valor |
| :-- | :-- |
| Ápice medido | 2.00 m |
| Tempo no ar | ~0.70 s |
| Alcance horizontal | ~4.90 m |
| Degrau seguro (`SafeStep`) | ápice menos 0.2 m |
| Vão seguro (`SafeGap`) | alcance menos 0.67 m |
| Altura da caixa empurrável | 1.22 m |

O espaçamento entre corredores é de 4 m: **excede** o ápice de propósito. Toda
subida depende de escada, elevador ou caixa.

Geometria, HUD e testes consomem `Phase1Bootstrap.DesignBudget()` ou
`Phase1Bootstrap.BudgetFor(playerIndex)`. Nenhum número de orçamento é literal.

### Guarda-corpo

`JumpBudgetPlayModeTests` mede ápice e alcance em runtime e confronta com o
orçamento declarado, com tolerância de 5%. Foi esse teste que revelou a perda da
integração discreta. Um teste que compara constante com constante não detecta
divergência entre documentação e física.

---

## 3. Traçado: labirinto competitivo em ladrilhos

A Fase 1 é uma **corrida**: cada jogador larga num canto de baixo, atravessa a sua
metade do labirinto e quem chega primeiro à saída única, no centro do topo,
vence. A referência de composição é o Fireboy & Watergirl: massa de rocha com
corredores escavados, escadas, poços e nichos. O labirinto é o espaço negativo.

### Mapa

O nível é um mapa de 32 por 18 ladrilhos de 1 m, autorado em texto em
`Phase1Bootstrap.MazeRows`. `#` é rocha e `.` é túnel. A metade direita é o
espelho da esquerda, então as duas rotas têm exatamente a mesma dificuldade.

```text
################################
###########.########.###########
####........................####
####........................####
####........................####
####..#..####.####.####..#..####
####...........##...........####
####....#......##......#....####
####....#......##......#....####
#############..##..#############
#########......##......#########
########.......##.......########
#######....#...##...#....#######
##.#.#...##############...#.#.##
#.......################.......#
#......##################......#
##....####################....##
################################
```

Cada sequência horizontal de rocha vira um bloco com colisor exato e visual
ladrilhado. Os túneis são a cor de fundo da câmera.

### Corredores

Quatro corredores por metade, com 3 ladrilhos de altura livre contra 1.25 m do
jogador. O espaçamento de 4 m entre pisos excede o ápice de pulo de 2 m: subir
depende sempre de escada, elevador ou caixa.

| Corredor | Piso | Metade esquerda (colunas) | Como se sobe para o próximo |
| :-- | --: | :-- | :-- |
| A | 1 | 1 a 5, largada | Escada de rocha nas colunas 6 a 9 |
| B | 5 | 9 a 14 | Elevador 1 nas colunas 13 e 14 |
| C | 9 | 6 a 14 | Elevador 2 nas colunas 4 e 5 |
| D | 13 | 4 a 14, com poço nas colunas 7 e 8 | Saída na câmara central, colunas 14 a 17 |

Largadas: P1 em (2.5, 1.75) e P2 em (29.5, 1.75), espelhadas.

### Regras de movimento que o mapa respeita

Validadas pelo simulador de travessia dos testes:

- Andar exige 2 ladrilhos livres, um para os pés e um para a cabeça.
- Subir 1 ladrilho exige 3 livres acima do piso de partida.
- Saltar um vão de até 3 ladrilhos sob teto de 3, ou de 4 sob teto de 4.
- Subir 2 ladrilhos de uma vez é impossível: é parede.
- Cair é sempre permitido.

O nicho que dá folga de cabeça no topo do muro da caixa (linha 12, colunas 7 e 8)
é também o piso do corredor D. Isso abre um poço de dois ladrilhos em D, logo
depois do elevador 2: quem não salta cai de volta no corredor C, ao lado do
elevador, e sobe de novo. É a armadilha da reta final.

---

## 4. Mecanismos, um conjunto por metade

Numa corrida nenhum mecanismo pode depender do outro jogador. Cada metade tem:

### Escada de rocha (A para B)

Quatro degraus de 1 m nas colunas 6 a 9, com o teto recortado para dar folga de
salto. Não precisa de mecanismo.

### Elevadores de uso individual (B para C e C para D)

`ActivatedPlatform` com um `PressureButton` montado na própria plataforma. O
passageiro pisa no botão, a plataforma sobe com ele, e desce sozinha quando ele
sai no topo. Quem cair de volta encontra a plataforma embaixo de novo. Um
`CooperativePuzzleController` amarra botão e plataforma.

### Portão e alavanca (corredor C)

`PuzzleDoor` na coluna 12 de C, aberto pela `Lever` do degrau da coluna 11 de B,
não alternável. Quem passa reto pela alavanca encontra o portão fechado e precisa
voltar: é o custo de navegação do labirinto. Aberto, o portão fica translúcido
e sem colisor, no lugar.

### Caixa e muro (corredor C)

`PushableBox` na coluna 11, empurrada até o muro de 2 m da coluna 8. Com 1.22 m
de altura, a caixa vira degrau: faltam 0.78 m até o topo do muro. Do topo se desce
para a parte esquerda de C. Se a caixa cair do corredor pelo poço do elevador,
o bootstrap a devolve ao spawn.

---

## 5. Perigos

Espelhados nas duas metades, sempre na rota direta.

| Perigo | Metade esquerda | Efeito |
| :-- | :-- | :-- |
| Lama | A, colunas 3 e 4; D, colunas 9 e 10 | Velocidade a 70% e fadiga acelerada |
| Espinhos | B, coluna 12; D, coluna 12 | Menos 5 de bem-estar e respawn no último checkpoint do jogador |

Os espinhos ocupam 1 ladrilho e se vencem com um pulo. Em B, eles ficam logo
abaixo do degrau da alavanca: descer sem olhar custa.

---

## 6. Checkpoints e respawn separado

Quatro checkpoints por metade. O primeiro fica na largada e é ativado pelo
bootstrap para o dono da metade assim que os jogadores nascem, então morrer nos
espinhos antes de qualquer outro checkpoint devolve cada um à sua própria
largada, nunca à do adversário. O isolamento por jogador já existente em
`Checkpoint` garante o resto.

| Checkpoint | P1 | P2 |
| :-- | :-- | :-- |
| Largada | (2.5, 2.3) | (29.5, 2.3) |
| Topo da escada, B | (9.5, 6.2) | (22.5, 6.2) |
| Corredor C | (10.5, 10.2) | (21.5, 10.2) |
| Corredor D | (6.5, 14.2) | (25.5, 14.2) |

---

## 7. Coletáveis

O dilema pedagógico é desvio contra pressa. Os ultraprocessados ficam no piso por
onde se passa, junto da lama. Os saudáveis exigem desvio: um nicho de salto no
teto ou um degrau sem saída.

As duas metades somam **590 pontos** de alimento saudável cada, e as penalidades
dos ultraprocessados diferem em 1 ponto de bem-estar (57 contra 58), para a
corrida ser justa.

| | Metade esquerda (P1) | Metade direita (P2) |
| :-- | :-- | :-- |
| Ultraprocessados no piso | `Burger` e `Fries` em A, `Donut` em B, `Candy` em C | `Pizza` e `HotDog` em A, `Soda` em B, `Chips` em C |
| Saudável em degrau sem saída | `Water`, coluna 1 de A | `Broccoli`, coluna 30 de A |
| Saudáveis em nicho de salto | `Banana` e `Carrot` no teto de A, `Strawberry` no teto de C, `Fish` no teto de D | `Apple` e `Avocado` no teto de A, `Orange` no teto de C, `Watermelon` no teto de D |

---

## 8. Chegada

Uma `FinishLine` única em X 16, aceitando os dois jogadores. O
`MatchCompletionController` roda no modo de corrida do GOAL-01: a primeira
chegada congela o jogador e abre a janela final de 20 s; a segunda encerra a
partida, ou o atrasado recebe DNF. O modo de portas duplas continua disponível
no controlador, mas a Fase 1 não o usa.

---

## 9. Progresso no HUD

Quatro marcos, com apoio real conferido pelo `PlayerGroundCheck`: corredor B,
corredor C, corredor D e saída cruzada, em 25%, 50%, 75% e 100%. Pular no
lugar não avança nada e cair de volta não reduz o progresso.

---

## 10. Anti-deadlock

1. Todo poço fica aberto embaixo do elevador, então dá para cair de qualquer
   corredor e refazer o percurso.
2. Elevadores voltam sozinhos ao soltar o botão. Alavancas não são alternáveis:
   o portão nunca volta a fechar.
3. A caixa volta ao spawn automaticamente se cair do corredor C.
4. Morrer nos espinhos devolve ao próprio checkpoint sem alterar mecanismos.
5. O simulador dos testes garante que nenhuma célula em pé fica inalcançável em
   nenhuma metade.

---

## 11. Limitação conhecida

A assimetria entre P1 e P2 descrita em `docs/player-profiles.md` foi pensada para
o modo cooperativo. Numa corrida com metades espelhadas ela deixaria as rotas
desiguais, então não foi implementada. Se voltar à pauta, o caminho é adaptar
cada metade ao perfil do seu jogador.

---

## 12. Testes automatizados

- **EditMode** (`Phase1BlockoutTests.cs`):
  - integridade da cena e das referências serializadas;
  - `Maze_MapIsWellFormed_AndMirrored`: 18 linhas de 32 ladrilhos, borda fechada,
    metades espelhadas, largadas em pé e saída aberta;
  - `Phase1Bootstrap_BuildsMirroredMaze_WithSeparateSpawnsAndSharedExit`: largadas
    separadas e espelhadas, checkpoint de largada ativo para o dono da metade,
    saída única em modo corrida, mecanismos espelhados, câmera fixa;
  - `Phase1Bootstrap_MazeIsTraversable_ForEachPlayer_OnlyWithMechanisms`:
    **guarda-corpo do design**. Um simulador percorre o mapa célula a célula com
    as regras de movimento (andar, subir 1, cair, saltar até 3, ou 4 sob teto de
    4) e os mecanismos registrados. Cada jogador precisa chegar à saída pela
    própria metade, nenhum pode chegar sem elevadores, caixa e portão, nenhuma
    célula em pé pode ficar órfã e os dois caminhos precisam ter o mesmo
    comprimento;
  - `Phase1Bootstrap_FoodsFollowTheDetourRule_AndHalvesAreBalanced`:
    ultraprocessado no piso de passagem, saudável em nicho ou beco, mesmos pontos
    nas duas metades;
  - `Phase1Bootstrap_SpawnsEveryFood_ResolvedFromTheCatalog`: 18 alimentos, 10
    saudáveis e 8 ultraprocessados;
  - `JumpBudget_IsDerivedFromArenaPhysics_NotFromLiterals`: as regras do simulador
    cabem no orçamento real de pulo;
  - `SharedCamera_FixedFraming_IgnoresTargetsAndKeepsSize` e
    `SharedCamera_Letterbox_MatchesTheArenaAspect`.
- **PlayMode** (`Phase1PlayModeTests.cs`):
  - largadas separadas, espelhadas e no chão, cada jogador com o próprio
    checkpoint de retorno;
  - checkpoint com isolamento por jogador;
  - espinhos devolvem cada jogador à própria largada;
  - progresso por corredor, e pular no lugar não conta;
  - corrida: a primeira chegada abre a janela final e congela o jogador, a segunda
    encerra sem DNF com as colocações certas;
  - câmera imóvel enquanto os jogadores percorrem o labirinto, com ambos sempre
    dentro do viewport.
