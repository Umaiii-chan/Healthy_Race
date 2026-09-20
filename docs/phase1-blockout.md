# Fase 1: Corrida do Equilíbrio — Documentação Técnica do Blockout

Este documento descreve a arquitetura, o design de nível (blockout), as zonas, a distribuição pedagógica de colecionáveis e os critérios de validação da **Fase 1: Corrida do Equilíbrio** do projeto **Healthy Race** (`[S3][LEVEL-01]`).

---

## 1. Visão Geral do Nível

- **Cena**: `Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity`
- **Orquestrador**: `HealthyRace.Level.Phase1Bootstrap`
- **Extensão Total**: ~230 unidades no eixo X ($X \in [-5, 228]$)
- **Jogadores**: 2 jogadores simultâneos locais (P1 e P2) com câmera compartilhada dinâmica (`SharedCameraController`)
- **Duração Estimada**: 3 a 5 minutos para percurso completo cooperativo/competitivo
- **Critério Fundamental de Aceitação**: A fase é integralmente percorrível do início ao fim com física 2D consistente e sem situações de deadlock permanente

---

## 2. Estrutura das 6 Zonas (Z1 a Z6)

### Zona 1 — Spawn ($X \in [-5, 32]$)

- **Propósito**: Ambientação, largada dos jogadores lado a lado e tutorial orgânico de controles e saltos básicos.
- **Geometria**:
  - Chão principal em $Y = -3.4$ ($37\text{m} \times 0.8\text{m}$).
  - Parede de contenção esquerda em $X = -5.5$ impedindo retorno para fora do cenário.
  - Linha de largada em $X = 0$.
  - Três plataformas escalonadas de teste de pulo com desníveis de $0.8\text{m}$ a $1.2\text{m}$ e vãos de $3.5\text{m}$.
- **Colecionáveis Pedagógicos**:
  - `Apple` (+10 WB, 100 pts)
  - `Banana` (+8 WB, 80 pts)
  - `Water` (+15 WB, 150 pts)

### Zona 2 — Caixa ($X \in [32, 65]$)

- **Propósito**: Desafio físico introdutório de cooperação e exploração.
- **Geometria**:
  - Desnível vertical com penhasco elevado em $Y = -0.6$, gerando uma parede vertical de $2.8\text{m}$ de altura.
  - Como o pulo máximo do jogador é de $2.0\text{m}$, o penhasco é intransponível por salto direto.
  - Caixa empurrável física (`PushableBox`, $1.22\text{m} \times 1.22\text{m}$, massa $1.2$) posicionada no chão.
  - Ao empurrar a caixa rente ao penhasco, a altura efetiva de salto cai para $1.58\text{m}$, permitindo escalar a borda com folga.
  - Poleiro elevado em $Y = +1.2$ acessível para jogadores que explorarem a mecânica de impulso.
- **Colecionáveis Pedagógicos**:
  - `Carrot` (+12 WB, 120 pts)
  - `Orange` (+10 WB, 100 pts)

### Zona 3 — Rotas ($X \in [65, 125]$)

- **Propósito**: Bifurcação estratégica entre duas rotas com dilema pedagógico.
- **Checkpoint 1**: Posicionado na entrada da zona em $X = 66, Y = 0.1$, com bandeira de feedback visual e persistência de respawn.
- **Rota Superior (Saudável / Ágil)**:
  - Sequência de plataformas aéreas suspensas entre $Y = 1.6$ e $Y = 3.0$.
  - Exige domínio de pulo, timing e espaçamento de saltos.
  - Recompensa os jogadores com alimentos saudáveis de pontuação e bem-estar máximos: `Broccoli` (+15 WB), `Strawberry` (+8 WB), `Watermelon` (+10 WB), `Avocado` (+14 WB), `Fish` (+16 WB) e `Banana` (+8 WB).
- **Rota Inferior (Tentação / Rápida)**:
  - Percurso no solo plano ($Y = -3.4$), oferecendo uma corrida veloz e direta.
  - Pista densamente povoada com 8 itens ultraprocessados: `Burger` (-18 WB), `Soda` (-12 WB), `Fries` (-15 WB), `Donut` (-14 WB), `Pizza` (-18 WB), `HotDog` (-16 WB), `Chips` (-12 WB) e `Candy` (-10 WB).
  - Jogadores que optarem pelo atalho rápido sofrem quedas severas de bem-estar e risco iminente de episódios de fadiga.

### Zona 4 — Reencontro ($X \in [125, 160]$)

- **Propósito**: Reunião dos dois caminhos em platô comum, trazendo P1 e P2 de volta para o mesmo enquadramento da câmera compartilhada.
- **Geometria**:
  - Degrau de transição em $Y = -2.6$ conectando a rota inferior ao platô principal.
  - Platô contínuo de $35\text{m}$ em $Y = -1.8$.
- **Checkpoint 2**: Posicionado em $X = 130, Y = -0.8$, salvando o progresso da segunda metade do percurso.
- **Colecionáveis de Recuperação**:
  - `Water` (+15 WB)
  - `Apple` (+10 WB)
  - `Orange` (+10 WB)

### Zona 5 — Puzzle Final ($X \in [160, 195]$)

- **Propósito**: Desafio cooperativo antes da reta de chegada, com proteção estrita contra deadlock.
- **Geometria**:
  - Barreira vertical em $X = 175$ de altura $2.2\text{m}$.
  - Mecanismo cooperativo com `PressureButton` e `PushableBox`.
  - **Rota Alternativa Anti-Deadlock**: Três plataformas suspensas em $Y = 0.2$, $Y = 1.2$ e $Y = 0.2$ que permitem transpor a barreira por meio de saltos ágeis caso os jogadores não cooperem no manuseio da caixa.
- **Colecionáveis Pedagógicos**:
  - `Carrot` (+12 WB)
  - `Avocado` (+14 WB)

### Zona 6 — Chegada ($X \in [195, 230]$)

- **Propósito**: Sprint final, pórtico de chegada e celebração.
- **Geometria**:
  - Reta plana em $Y = -1.8$.
  - Linha de chegada (`FinishLine`) em $X = 215$ com trigger vertical de $5\text{m}$ e arco de celebração.
  - Pódio dos vencedores em $X = 221$.
  - Parede de contenção direita em $X = 228$.

---

## 3. Métricas de Plataforma e Física

As dimensões de vãos e alturas de plataformas foram calibradas diretamente contra os parâmetros físicos do `PlayerMovement`:

$$\text{Velocidade de pulo } v_0 = 12\,\text{m/s}, \quad g = 36\,\text{m/s}^2$$

$$\text{Altura máxima de pulo } h = \frac{v_0^2}{2g} = \frac{144}{72} = 2.0\,\text{m}$$

$$\text{Duração no ar } t_{\text{ar}} = \frac{2 v_0}{g} = \frac{24}{36} \approx 0.67\,\text{s}$$

$$\text{Alcance horizontal máximo } d_{\text{máx}} = v_{\text{mov}} \times t_{\text{ar}} = 7 \times 0.67 \approx 4.67\,\text{m}$$

| Elemento | Dimensão | Capacidade do Jogador | Status |
| :--- | :--- | :--- | :--- |
| **Degraus da Zona 1** | Altura $0.8\text{m}$, vão $3.5\text{m}$ | Máx $2.0\text{m}$ altura, $4.67\text{m}$ vão | Percorrível por pulo simples |
| **Penhasco da Zona 2** | Altura $2.8\text{m}$ | Excede pulo simples ($2.8 > 2.0$) | Exige auxílio da caixa ($1.22\text{m}$) |
| **Plataformas da Zona 3** | Vãos de $3.5\text{m}$ a $4.0\text{m}$ | Vão máx $4.67\text{m}$ | Percorrível com salto em velocidade |
| **Barreira da Zona 5** | Altura $2.2\text{m}$ | Degraus alternativos de $1.0\text{m}$ | Anti-deadlock garantido |

---

## 4. Integração com Sistemas Existentes

- **`PlayerHUD`**: Atualiza percentual de progresso em tempo real ($0\%$ a $100\%$) a partir da coordenada $X$ do jogador em relação à largada ($X = 0$) e chegada ($X = 215$).
- **`MatchHUD`**: Exibe o cronômetro da prova (`MM:SS`) e banner de janela final.
- **`SharedCameraController`**: Enquadra P1 e P2 simultaneamente com padding, zoom suave e confinamento na área útil do nível ($X \in [-10, 240]$).
- **`Checkpoint`**: Registra ativação independente para P1 e P2 via eventos, mantendo isolamento total.
- **`FinishLine`**: Registra chegada, armazena tempos e dispara eventos desacoplados preparando a sprint de encerramento (`GOAL-01`).

---

## 5. Testes Automatizados

- **EditMode** (`Phase1BlockoutTests.cs`):
  - Validação de integridade da cena `Phase1_CorridaDoEquilibrio.unity` sem scripts ausentes e com todas as referências serializadas válidas.
  - Validação dos 18 ScriptableObjects no catálogo de alimentos.
  - Validação das 6 zonas com colliders na layer `Ground`.
  - Validação do isolamento de ativação e registro estático de checkpoints.
  - Validação do registro de chegada na `FinishLine`.
  - Validação estática das métricas de pulo e percorribilidade.
- **PlayMode** (`Phase1PlayModeTests.cs`):
  - Carregamento assíncrono da cena em PlayMode.
  - Spawning e verificação de grounding de P1 e P2.
  - Ativação de Checkpoint 1 com isolamento de jogador em tempo real.
  - Acionamento da `FinishLine` ao cruzar a marca de chegada.
