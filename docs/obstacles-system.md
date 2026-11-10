# Sistema de Obstáculos (HAZ-01)

## Descrição

O sistema de obstáculos e perigos (hazards) do Healthy Race introduz desafios de plataforma e consequências pedagógicas de jogabilidade através de três tipos principais de obstáculos: **Espinhos** (`SpikeHazard`), **Lama** (`MudHazard`) e **Plataformas que Caem** (`FallingPlatform`).

## Componentes Arquiteturais

### 1. SpikeHazard (`SpikeHazard.cs`)

- **Namespace**: `HealthyRace.Obstacles`;
- **Gatilho de Contato**: `BoxCollider2D` (`isTrigger = true`);
- **Detecção de Jogador**: Suporta `PlayerController` e `PlayerMovement`;
- **Penalidade de Bem-Estar**:
  - Aplica redução imediata no Bem-Estar (`_wellbeingPenalty = 5`);
  - Clampa respeitando o valor mínimo (`0 WB`);
- **Respawn no Checkpoint**:
  - Consulta `Checkpoint.GetLastCheckpoint(playerIndex)`;
  - Se um checkpoint já foi alcançado pelo jogador, ele é reposicionado em `checkpoint.RespawnPosition`;
  - Caso contrário, utiliza a posição padrão de spawn (`_defaultRespawnPosition`);
  - Zera o vetor de velocidade (`rb.linearVelocity = Vector2.zero`) para cessar inércia;
- **Janela de Invulnerabilidade (Cooldown)**:
  - Cooldown configurável (`_hitCooldown = 1.0s`) por índice de jogador;
  - Evita dano contínuo acidental ou penalidades em loop;
- **Eventos**: `event Action<int, Vector2> PlayerPenalized`, `event Action OnAnyPlayerHit`.

### 2. MudHazard (`MudHazard.cs`)

- **Namespace**: `HealthyRace.Obstacles`;
- **Gatilho de Área**: `BoxCollider2D` (`isTrigger = true`);
- **Penalidade de Velocidade**:
  - Multiplicador de velocidade horizontal (`_speedMultiplier = 0.7f`, correspondendo a -30%);
- **Rastreamento e Isolamento**:
  - Utiliza `HashSet<PlayerMovement>` para acompanhar os jogadores presentes na área;
  - Aplica o multiplicador no `OnTriggerEnter2D` e restaura `1.0f` no `OnTriggerExit2D`;
  - No `OnDisable` / `OnDestroy`, restaura automaticamente a velocidade normal de todos os jogadores para evitar debuffs permanentes;
- **Eventos**: `event Action<PlayerMovement> PlayerEntered`, `event Action<PlayerMovement> PlayerExited`.

### 3. FallingPlatform (`FallingPlatform.cs`)

- **Namespace**: `HealthyRace.Obstacles`;
- **Colisor Sólido**: `BoxCollider2D` na layer 6 (`Ground`);
- **Detecção de Pouso**:
  - `OnCollisionEnter2D` / `OnCollisionStay2D` validando colisão superior (pouso pelo topo);
- **Ciclo de Queda e Respawn**:
  1. **Antecipação / Tremor (`0.5s`)**:
     - Vibração visual e aviso sonoro/textual (integrado a `HazardPresentation` com texto `! VAI CAIR !` e shake angular/horizontal);
  2. **Queda / Desativação**:
     - Desativa o colisor físico e o renderer visual;
  3. **Respawn (`3.0s`)**:
     - Retorna à posição original, reativa o colisor e restaura o estado `Idle`;
  - Suporta restauração manual via `ResetPlatform()` para garantir anti-deadlock;
- **Eventos**: `event Action PlatformTriggered`, `event Action PlatformFell`, `event Action PlatformRespawned`.

## Integração na Fase 1 (Corrida do Equilíbrio)

- **Fase 1** (ver `docs/levels/fase-01.md`): todos no caminho longo, montados a partir do
  mapa em texto por `LevelBootstrap`.
  - `MudHazard` (`~`): 3 poças; o efeito vai para `PlayerMovement.SetTerrainMultiplier`,
    separado do multiplicador do Bem-Estar.
  - `SpikeHazard` (`^`): 10 trechos de 1 tile; o gatilho tem o tamanho das pontas
    desenhadas. Devolve ao último checkpoint com −5 de Bem-Estar.
  - `FallingPlatform` (`w`): 12, em pares sobre vãos de 5 tiles; cair delas leva ao
    corredor curto.

## Testes Automatizados

- **EditMode** (`ObstacleTests.cs`):
  - `SpikeHazard_PenalizesWellbeing_AndTeleportsPlayerToCheckpoint`
  - `SpikeHazard_RespectsHitCooldown`
  - `MudHazard_ConfiguresMultiplier_AndManagesAffectedPlayers`
  - `FallingPlatform_InitializesStable_AndStartsAnticipationOnTrigger`
- **PlayMode** (`ObstaclePlayModeTests.cs`):
  - `Player_StepsOnSpikeHazard_TakesPenaltyAndRespawns`
  - `Player_EntersMudHazard_ReducesSpeed_AndRestoresOnExit`
  - `Player_LandsOnFallingPlatform_TriggersCollapseAndRespawn`
