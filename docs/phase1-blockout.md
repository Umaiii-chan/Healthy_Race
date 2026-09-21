# Fase 1: Corrida do Equilíbrio — Labirinto Cooperativo de Tela Única

Este documento descreve o desenho de nível da **Fase 1** do **Healthy Race** após a
refatoração para labirinto de corredores escavados numa tela única, no estilo *Fireboy & Watergirl*.
Ele cobre o enquadramento, os corredores, os portais cooperativos, o orçamento
de pulo, a distribuição pedagógica dos coletáveis e os critérios anti-deadlock.

---

## 1. Visão geral

- **Cena**: `Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity`
- **Orquestrador**: `HealthyRace.Level.Phase1Bootstrap` (constrói tudo em código)
- **Área jogável**: $X \in [0, 32]$, $Y \in [0, 18]$ (proporção 16:9)
- **Câmera**: fixa em $(16, 9, -10)$, sem scroll nem zoom, com letterbox via
  `Camera.rect` para que nenhuma proporção de tela mostre além das paredes
- **Jogadores**: P1 e P2 locais, sempre visíveis
- **Objetivo**: os dois jogadores devem ocupar **simultaneamente** suas portas no
  corredor C4. Não há janela final nem DNF neste modo.
- **Tema pedagógico**: o dilema deixa de ser "rota de cima vs. rota de baixo" e passa
  a ser "pressa vs. bem-estar" em cada escolha de trajeto. Os ultraprocessados
  ficam na linha reta dos corredores baixos; os saudáveis exigem subir num pilar
  ou atravessar até uma das pontas.

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

O espaçamento entre corredores é de 3.2 m: **excede** o ápice de propósito. Toda
subida depende de um poço vertical e do mecanismo que o serve.

Geometria, HUD e testes consomem `Phase1Bootstrap.DesignBudget()` ou
`Phase1Bootstrap.BudgetFor(playerIndex)`. Nenhum número de orçamento é literal.

### Guarda-corpo

`JumpBudgetPlayModeTests` mede ápice e alcance em runtime e confronta com o
orçamento declarado, com tolerância de 5%. Foi esse teste que revelou a perda da
integração discreta. Um teste que compara constante com constante não detecta
divergência entre documentação e física.

---

## 3. Traçado: labirinto de corredores escavados

A referência de composição é um labirinto: massa de rocha sólida com corredores
escavados, ligados por poços verticais. Não há plataformas soltas no vazio.

### Corredores

Cada corredor tem 2.6 m de altura livre, contra 1.25 m do jogador.

| Corredor | Piso | Teto | Trechos de piso (X) |
| :-- | --: | --: | :-- |
| C0 | 1.0 | 3.6 | 0 a 32, inteiriço |
| C1 | 4.2 | 6.8 | 0 a 25.5 e 28.5 a 32 |
| C2 | 7.4 | 10.0 | 0 a 3 e 6 a 32 |
| C3 | 10.6 | 13.2 | 0 a 19.5 e 22.5 a 32 |
| C4 | 13.8 | 16.4 | 0 a 8 e 11 a 32 |

O espaçamento entre pisos é de 3.2 m e o ápice de pulo é de 2.0 m. Nenhum
corredor alcança o de cima por salto: subir sempre depende de um poço.

### Poços verticais

Os buracos nas lajes ficam escalonados de propósito. Como alternam de lado, é
preciso percorrer cada corredor de ponta a ponta antes de subir, e o percurso
vira um ziguezague. É o que dá a leitura de labirinto.

| Poço | Laje | X | Liga | Mecanismo |
| :-- | :-- | :-- | :-- | :-- |
| A | 1 | 25.5 a 28.5 | C0 para C1 | G1, botão e plataforma |
| B | 2 | 3 a 6 | C1 para C2 | G2, alavanca e plataforma |
| C | 3 | 19.5 a 22.5 | C2 para C3 | G3, caixa, botão e plataforma |
| D | 4 | 8 a 11 | C3 para C4 | degrau de rocha, livre |

Largada no corredor C0: P1 em (3.5, 1.7) e P2 em (5.0, 1.7).

### Textura do labirinto

Dentro dos corredores, dois elementos quebram a linha reta sem nunca fechar a
passagem:

- **Pilares** de 1.6 m, abaixo do degrau seguro, vencidos com um pulo. Sete no
  total, e quatro deles carregam alimento saudável no topo.
- **Arcos** presos ao teto, deixando 2.0 m de passagem livre embaixo. Quatro no
  total.

---

## 4. Portais cooperativos

### G1 — C0 para C1, poço A

- Botão **B1** no meio do corredor C0, em X 19, longe do poço.
- Plataforma **AP1** no poço, repouso com topo em 1.6 e curso até 4.2.
- Botão **B2** já em C1, em X 23, para devolver o favor.
- Enquanto um jogador mantém o botão, a plataforma sobe com o outro. A distância
  entre botão e poço é o que impede a travessia solo.
- **Saída de emergência**: os dois botões são momentâneos e o poço fica aberto,
  então qualquer jogador se deixa cair de volta a C0. Nenhum estado trava.

### G2 — C1 para C2, poço B

- Alavanca **L2** na ponta oposta do corredor C1, em X 29, não alternável.
- Plataforma **AP2** no poço, repouso com topo em 4.8 e curso até 7.4.
- Quem puxa atravessa o corredor inteiro, então os dois avançam em vez de um
  ficar parado esperando.
- **Saída de emergência**: a alavanca não volta, então a plataforma fica erguida
  para sempre e o caminho nunca fecha.

### G3 — C2 para C3, poço C

- Botão **B3** em X 15.5 e plataforma **AP3** no poço, de 8.0 até 10.6.
- Rota cooperativa: um segura B3 e o outro sobe.
- Rota anti-deadlock: a caixa empurrada sobre B3 trava a plataforma erguida, e os
  dois sobem sem depender de sincronia.
- **Saída de emergência**: a alavanca **R3** em X 30 devolve a caixa ao spawn, e o
  bootstrap também a devolve se ela cair do corredor.

### Subida final — C3 para C4, poço D

Degrau de rocha livre, com topo em 12.2. Sem mecanismo: os dois convergem e
chegam juntos ao corredor das portas.

---

## 5. Perigos

| Perigo | Posição | Efeito |
| :-- | :-- | :-- |
| Lama | C0 em X 13 a 17 e 20 a 24, C1 em X 12 a 16 | Velocidade a 70% e fadiga acelerada |
| Espinhos | C0 em X 9 a 12, entre os dois pilares | Menos 5 de bem-estar e respawn no checkpoint |
| Plataformas que caem | C3, vão de 5.0 m entre X 12 e 17 | 0.5 s de aviso e 3 s fora |

O vão de C3 excede o alcance de pulo, então as duas plataformas são obrigatórias.
Quem cai volta para C2 e sobe de novo pela AP3, que continua travada pela caixa.

---

## 6. Checkpoints

Um por corredor, sempre na saída do poço, mantendo o isolamento por jogador.

| Checkpoint | Posição | Corredor |
| :-- | :-- | :-- |
| CP1 | (30.0, 5.2) | C1 |
| CP2 | (7.5, 8.4) | C2 |
| CP3 | (24.0, 11.6) | C3 |
| CP4 | (12.5, 14.8) | C4 |

---

## 7. Coletáveis

O dilema deixou de ser altura e passou a ser desvio. Os ultraprocessados ficam na
linha reta dos corredores baixos, junto da lama e dos espinhos. Os saudáveis
exigem subir num pilar ou atravessar até uma das pontas.

**Ultraprocessados**, na rota direta: `Burger` (6, C0), `Soda` (12.9, C0),
`Fries` (15, C0), `Donut` (22, C0), `Pizza` (25, C0), `HotDog` (14, C1),
`Chips` (18.4, C1) e `Candy` (27, C1).

**Saudáveis sobre pilares**: `Apple` (8.4, C0), `Carrot` (11, C1),
`Orange` (25.8, C2) e `Broccoli` (5.6, C3).

**Saudáveis nas pontas**: `Water` (1.5, C1), `Fish` (1.5, C2),
`Avocado` (30.5, C2), `Watermelon` (2, C3), `Strawberry` (30.5, C3) e
`Banana` (3, C4).

---

## 8. Chegada

Duas portas no corredor C4, `FinishLine` com `AcceptedPlayerIndex`: a Porta P1 em
X 26.5 só aceita P1, e a Porta P2 em X 30 só aceita P2. A partida encerra quando
os dois estão em suas portas ao mesmo tempo. O primeiro a chegar não congela e
pode voltar para ajudar.

---

## 9. Progresso no HUD

Cinco marcos: corredor alcançado, com apoio real conferido pelo `PlayerGroundCheck`,
e porta ocupada. Pular no lugar não avança nada. O marco é monotônico: cair de
volta não reduz o progresso.

---

## 10. Anti-deadlock

1. Todo poço fica aberto, então dá para descer de qualquer corredor até C0 e
   refazer o percurso.
2. Os botões são momentâneos e as alavancas não são alternáveis: nenhum mecanismo
   volta a um estado pior.
3. A caixa de G3 tem alavanca de reset e retorno automático se cair.
4. Morrer nos espinhos devolve ao checkpoint sem alterar o estado dos portais.
5. Pilares e arcos nunca fecham um corredor.

---

## 11. Limitação conhecida

O G2 usa alavanca permanente, então é cooperação fraca: um jogador sozinho
consegue puxar e depois subir. Quem impede de fato a travessia solo é o G1, que
exige alguém segurando o botão enquanto o outro sobe. A cooperação forte nos três
portais depende da assimetria entre P1 e P2, ainda não implementada.

---

## 12. Testes automatizados

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
