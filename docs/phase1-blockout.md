# Fase 1: Corrida do Equilíbrio — Arena Cooperativa de Tela Única

Este documento descreve o desenho de nível da **Fase 1** do **Healthy Race** após a
refatoração para arena vertical de tela única, no estilo *Fireboy & Watergirl*.
Ele cobre o enquadramento, os patamares, os portais cooperativos, o orçamento de
pulo, a distribuição pedagógica dos coletáveis e os critérios anti-deadlock.

---

## 1. Visão geral

- **Cena**: `Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity`
- **Orquestrador**: `HealthyRace.Level.Phase1Bootstrap` (constrói tudo em código)
- **Área jogável**: $X \in [0, 32]$, $Y \in [0, 18]$ (proporção 16:9)
- **Câmera**: fixa em $(16, 9, -10)$ com `orthographicSize = 9`, sem scroll nem zoom
- **Jogadores**: P1 e P2 locais, sempre visíveis
- **Objetivo**: os dois jogadores devem ocupar **simultaneamente** suas portas no
  patamar T4. Não há janela final nem DNF neste modo.
- **Tema pedagógico**: o dilema deixa de ser "rota de cima vs. rota de baixo" e passa
  a ser "pressa vs. bem-estar" em cada escolha de trajeto. Os ultraprocessados
  ficam enfileirados na rota rápida do piso; os saudáveis exigem subir, desviar ou
  cooperar.

---

## 2. Orçamento de pulo

Toda a geometria é calibrada contra `PlayerMovement` (`_jumpVelocity = 12`,
`_moveSpeed = 7`) com gravidade efetiva $g = 18 \times 2 = 36\ \text{m/s}^2$:

| Grandeza | Valor |
| :-- | :-- |
| Altura máxima de pulo | $2.0\ \text{m}$ |
| Alcance horizontal máximo | $\approx 4.67\ \text{m}$ |
| Degrau seguro por pulo simples | $\le 1.8\ \text{m}$ |
| Vão seguro em pulo com velocidade | $\le 4.0\ \text{m}$ |
| Altura da caixa empurrável | $1.22\ \text{m}$ |

O espaçamento entre patamares é de $3.4\ \text{m}$: **excede** o pulo de propósito.
Toda subida exige degrau intermediário, caixa ou plataforma acionada.

As constantes vivem em `Phase1Bootstrap` (`MaxJumpHeight`, `SafeStepHeight`,
`SafeGapWidth`, `TierTops`) e são usadas pelo teste de percorribilidade.

---

## 3. Patamares

| Patamar | Topo (Y) | Superfícies (X) |
| :-- | :-- | :-- |
| T0 — piso | 1.0 | Piso completo $[0, 32]$; nicho da alavanca $[0, 2]$ fechado por parede de 3 m em $X \in [2, 2.5]$ |
| T1 | 4.4 | Ilha esquerda $[4, 9.5]$, pedra $[13.5, 16.5]$, ilha direita $[19, 28]$; degrau S2 em $[10.5, 12.5]$ (topo 6.0) |
| T2 | 7.8 | Plataforma esquerda $[0, 10]$, pedra $[13.5, 15.5]$, trecho direito A $[17, 23]$, vão da AP3 $[23, 24.5]$, trecho direito B $[24.5, 32]$ |
| T3 | 11.2 | Plataforma direita $[24.5, 28]$, plataformas que caem $[18, 21]$ e $[13.5, 16.5]$, plataforma esquerda $[4, 12]$; degrau S4 em $[1, 3]$ (topo 13.0) |
| T4 — portas | 14.6 | Patamar $[5, 32]$ com Porta P1 em $X = 26.5$ e Porta P2 em $X = 30$ |

Largada: P1 em $(3.5, 1.7)$ e P2 em $(5.0, 1.7)$.

### Degraus intermediários

| Degrau | De | Para | Desnível | Vão |
| :-- | :-- | :-- | :-- | :-- |
| S2 (topo 6.0) | Ilha esquerda T1 (4.4) | Plataforma esquerda T2 (7.8) | 1.6 + 1.8 | 1.0 / 0.5 |
| S4 (topo 13.0) | Plataforma esquerda T3 (11.2) | Patamar T4 (14.6) | 1.8 + 1.6 | 1.0 / 2.0 |

---

## 4. Portais cooperativos

### G1 — T0 → T1 (botões + plataforma)

- **B1** (`PressureButton`) no piso em $X = 27$.
- **AP1** (`ActivatedPlatform`) em $X \in [28.5, 31.5]$, repouso com topo em 1.6 e
  curso de $+2.8$ até o topo 4.4, alinhada à ilha direita de T1.
- **B2** na ilha direita de T1 em $X = 20$.
- **Fluxo**: um jogador segura B1, o outro embarca na AP1 e sobe. Quem subiu vai a
  B2; o parceiro embarca na AP1 (que desceu ao soltar B1) e sobe também.
- **Amarração**: dois `CooperativePuzzleController` (B1 → AP1 e B2 → AP1); a
  plataforma obedece ao OU dos dois botões.
- **Saída de emergência**: botões momentâneos; qualquer jogador pode voltar a T0
  se deixando cair. Sem estado irreversível.

### G2 — T1 → T2 (alavanca + portão)

- **L2** (`Lever`, não alternável) no nicho de T0 em $(1.3, 2.3)$. O nicho só é
  acessível por cima: a parede de 3 m impede a entrada pelo piso, e o jogador
  chega nele caindo da ilha esquerda de T1.
- **D2** (`PuzzleDoor`, movimento instantâneo com colisor desativado ao abrir) em
  $X = 17.6$ sobre o trecho direito A de T2. Fechado, bloqueia a passagem da pedra
  de T2 para o trecho direito (o pulo não vence os 2.2 m do portão).
- **AP2** no nicho (topo 1.6 → 4.4) acionada por **B2r** na ilha esquerda de T1
  ($X = 7.5$): é o resgate de quem acionou a alavanca.
- **Fluxo**: um jogador desce ao nicho e aciona L2 (o portão abre e fica aberto).
  O parceiro segura B2r e a AP2 devolve o acionador a T1. Os dois subem por S2 até
  T2 e atravessam o portão.
- **Saída de emergência**: espinhos no canto do nicho ($X \in [0, 0.5]$) devolvem o
  jogador ao último checkpoint por 5 de bem-estar. Como L2 não é alternável, o
  portão nunca volta a fechar.

### G3 — T2 → T3 (caixa + botão + plataforma)

- **Caixa** (`PushableBox`) nasce em $X = 20$ no trecho direito A de T2.
- **B3** em $X = 22$, ao lado do vão da **AP3** ($X \in [23, 24.5]$), que em repouso
  fica rente ao patamar e sobe $+3.0$ até o topo 10.8; de lá o degrau até a
  plataforma direita de T3 é de 0.4 m.
- **Fluxo cooperativo**: um jogador segura B3, o outro embarca na AP3 e sobe.
- **Rota anti-deadlock**: a caixa empurrada sobre B3 trava a plataforma erguida e
  vira degrau (topo 9.02): patamar → caixa (1.22 m) → AP3 (1.78 m) → T3 (0.4 m).
- **Reset**: alavanca **R3** em $X = 30.5$ devolve a caixa ao spawn. O bootstrap
  também a devolve automaticamente se ela cair abaixo de T2 (vão da AP3 aberta ou
  borda do patamar).

---

## 5. Perigos

| Perigo | Posição | Efeito |
| :-- | :-- | :-- |
| `MudHazard` | T0, $X \in [15, 19]$ e $[22, 26]$ | -30% de velocidade e fadiga para quem escolhe a rota fácil |
| `SpikeHazard` | T0, $X \in [10, 13]$ (sob o vão entre as ilhas de T1) | -5 de bem-estar e respawn no checkpoint |
| `SpikeHazard` | Nicho de G2, $X \in [0, 0.5]$ | Saída de emergência do nicho |
| `FallingPlatform` | T3, $X \in [18, 21]$ e $[13.5, 16.5]$ | 0.5 s de aviso, 3 s fora, pressão de tempo na travessia |

---

## 6. Checkpoints

| Checkpoint | Posição | Patamar |
| :-- | :-- | :-- |
| CP1 | $(26, 5.6)$ | Ilha direita de T1, na chegada da AP1 |
| CP2 | $(5, 9.0)$ | Plataforma esquerda de T2 |
| CP3 | $(26.25, 12.4)$ | Plataforma direita de T3, na chegada da AP3 |

A ativação continua isolada por jogador (`Checkpoint.ActivateForPlayer`).

---

## 7. Coletáveis

### Ultraprocessados — rota preguiçosa (T0, $Y = 1.7$)

`Burger` (7), `Soda` (9), `Fries` (14.5), `Donut` (17, na lama), `Pizza` (19.5),
`HotDog` (22), `Chips` (24, na lama) e `Candy` (26).

### Saudáveis — recompensa por explorar

| Item | Posição | Exige |
| :-- | :-- | :-- |
| `Apple` | $(5.5, 5.1)$ | G1 + travessia das ilhas de T1 |
| `Carrot` | $(8.8, 5.1)$ | G1 + travessia das ilhas de T1 |
| `Orange` | $(15, 5.1)$ | G1 + pedra de T1 |
| `Broccoli` | $(2.5, 8.5)$ | S2, plataforma esquerda de T2 |
| `Avocado` | $(8, 8.5)$ | S2, plataforma esquerda de T2 |
| `Water` | $(14.5, 8.5)$ | Pedra de T2 |
| `Fish` | $(8, 11.9)$ | G2 + G3 + travessia de T3 |
| `Strawberry` | $(15, 11.9)$ | Plataforma que cai em T3 |
| `Watermelon` | $(10, 15.3)$ | Patamar das portas |
| `Banana` | $(18, 15.3)$ | Patamar das portas |

---

## 8. Chegada

- Duas portas (`FinishLine` com `AcceptedPlayerIndex`): a Porta P1 só aceita P1 e a
  Porta P2 só aceita P2.
- `MatchCompletionController` em modo `requireBothAtDoors`: a primeira chegada
  define a ordem (1º/2º lugar) e o estado passa a `WaitingForPartner`, mas o jogador
  **não é congelado** e pode sair para ajudar. A partida encerra quando os dois estão
  em suas portas ao mesmo tempo.
- O HUD central mostra `P1 NA PORTA - AGUARDANDO P2` (ou vice-versa) até o
  encerramento.

---

## 9. Progresso no HUD

`PlayerHUD.UpdateProgress` continua recebendo um valor $[0, 1]$, mas quem o
alimenta é `Phase1Bootstrap.GetProgress`, calculado em cinco marcos:

| Marco | Condição | Progresso |
| :-- | :-- | :-- |
| 0 | Piso | 0% |
| 1 | Pés em T1 | 20% |
| 2 | Pés em T2 | 40% |
| 3 | Pés em T3 | 60% |
| 4 | Pés em T4 | 80% |
| 5 | Chegada registrada na própria porta | 100% |

O marco é monotônico por jogador: cair de volta não reduz o progresso.

---

## 10. Critérios anti-deadlock

1. **Nenhum estado irreversível**: botões são momentâneos, a alavanca de G2 só abre,
   a caixa de G3 volta ao spawn pelo reset ou automaticamente ao cair.
2. **Sempre é possível descer**: nenhum patamar tem teto ou parede que impeça a
   queda até T0, de onde G1 pode ser refeito.
3. **Nicho de G2**: saída cooperativa (AP2 + B2r) e saída de emergência (espinhos →
   checkpoint). Dois jogadores presos no nicho saem pelos espinhos.
4. **Morte em espinhos**: respawn no último checkpoint do jogador; os puzzles não
   mudam de estado com a morte.
5. **Portas**: o primeiro a chegar não é congelado, então pode voltar e ajudar.

Cada portal documenta sua saída de emergência em comentário XML no método
`CreateGateOne/Two/Three` de `Phase1Bootstrap`.

---

## 11. Testes automatizados

- **EditMode** (`Phase1BlockoutTests.cs`):
  - integridade da cena e das referências serializadas;
  - `FinishLine_DoorForPlayer_RejectsOtherPlayerAndTracksOccupancy`;
  - `MatchCompletion_DualDoors_*`: conclusão só com os dois nas portas;
  - `SharedCamera_FixedFraming_IgnoresTargetsAndKeepsSize`;
  - `Phase1Bootstrap_BuildsArenaTiersGatesAndDoors`: patamares, portais, portas,
    checkpoints, perigos e câmera fixa;
  - `Phase1Bootstrap_AllGeometryAndActorsStayInsideArenaRectangle`: geometria,
    jogadores e distribuição saudável/ultraprocessado;
  - `Phase1Bootstrap_ArenaIsTraversable_WithinJumpBudget_UsingCooperativeLinks`:
    **guarda-corpo do design** — busca em largura sobre as superfícies registradas
    (`Surfaces`) e ligações cooperativas (`CooperativeLinks`); falha se alguma
    superfície exigir desnível > 1.8 m ou vão > 4.0 m sem mecanismo, e falha se T1 ou
    as portas forem alcançáveis sem cooperação.
- **PlayMode** (`Phase1PlayModeTests.cs`):
  - spawn dos dois jogadores dentro do retângulo e no chão;
  - checkpoint com isolamento por jogador;
  - progresso por patamar (correr no piso não conta);
  - conclusão da partida exigindo os dois jogadores em suas próprias portas;
  - câmera imóvel (posição e `orthographicSize`) enquanto os jogadores percorrem a
    arena, com ambos sempre dentro do viewport.
