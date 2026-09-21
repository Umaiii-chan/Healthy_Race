# Sistema de Puzzles e Interações (PUZZ-01)

## Descrição

O sistema de puzzles e interações cooperativas do Healthy Race provê mecanismos físicos e lógicos integrados que exigem coordenação entre os jogadores ou resolução de quebra-cabeças no cenário. Inclui botões de pressão, caixas empurráveis com respawn, alavancas físicas, portas de passagem, plataformas móveis e salvaguardas anti-deadlock.

## Componentes Arquiteturais

### 1. Lever (`Lever.cs`)

- **Namespace**: `HealthyRace.Puzzles`;
- **Gatilho de Contato**: `BoxCollider2D` (`isTrigger = true`), detecta `PlayerController` ou `PlayerMovement`;
- **Debouncing**: Cooldown configurável (`ToggleCooldown = 0.5s`) para evitar alternâncias rápidas acidentais;
- **Feedback Visual**:
  - Rotação angular da alavanca (-35° desativada, +35° ativada);
  - Mudança de cor do indicador visual (vermelho/desativado para verde/ativado);
- **Eventos**: `OnToggled(bool)`, `OnActivated`, `OnDeactivated`.

### 2. PuzzleDoor (`PuzzleDoor.cs`)

- **Namespace**: `HealthyRace.Puzzles`;
- **Colisor de Bloqueio**: `BoxCollider2D` em layer 6 (`Ground`), impedindo a passagem de jogadores e objetos enquanto fechada;
- **Modos de Operação**:
  - Movimento suave interpolado via `Vector3.MoveTowards` com velocidade parametrizada;
  - Deslocamento instantâneo com desativação do colisor quando aberta;
- **Eventos**: `OnStateChanged(bool)`, `OnOpened`, `OnClosed`.

### 3. ActivatedPlatform (`ActivatedPlatform.cs`)

- **Namespace**: `HealthyRace.Puzzles`;
- **Física Cinemática**: `Rigidbody2D` (`bodyType = Kinematic`, `Interpolate`), move-se suavemente via `MovePosition` sem deslizar passageiros;
- **Retorno Automático**: Configuração opcional `_returnWhenDeactivated` para retorno à posição de repouso quando desativada;
- **Eventos**: `OnStateChanged(bool)`.

### 4. PushableBox Aprimorada (`PushableBox.cs`)

- **Namespace**: `HealthyRace.Obstacles`;
- **Spawn Caching**: Grava a posição inicial de spawn na inicialização;
- **Anti-Deadlock / Auto-Reset**:
  - Monitora queda abaixo de limiar (`_fallResetYThreshold = -10f`);
  - Reposiciona automaticamente na posição inicial (`ResetToSpawn()`);
  - Notifica ouvintes pelo evento `OnResetToSpawn`.

### 5. CooperativePuzzleController (`CooperativePuzzleController.cs`)

- **Namespace**: `HealthyRace.Puzzles`;
- **Coordenação Multi-Elemento**: Vincula `PressureButton`, `Lever`, `PushableBox`, `PuzzleDoor`, `ActivatedPlatform` e uma alavanca de emergência (`ResetSwitch`);
- **Lógica Flexível**: Modos `requireAll = true` (ambos acionados) ou `false` (qualquer um acionado);
- **Recuperação de Estado Impossível (Anti-Deadlock)**:
  - Método `ResetPuzzle()` restaura caixa ao spawn original, redefine alavancas e portas;
  - Acionável por botão/alavanca de reset dedicada no cenário ou por gatilho de código.

## Integração na Fase 1 (arena cooperativa)

A Fase 1 é uma arena de tela única com três portais cooperativos, todos amarrados
por `CooperativePuzzleController.Bind(...)` em `Phase1Bootstrap`
(métodos `CreateGateOne`, `CreateGateTwo` e `CreateGateThree`):

1. **G1 (T0 → T1)**: `PressureButton` B1 no piso e B2 na ilha direita de T1 erguem a
   `ActivatedPlatform` AP1 (topo 1.6 → 4.4). Dois controladores, um por botão; a
   plataforma obedece ao OU dos dois.
2. **G2 (T1 → T2)**: `Lever` L2 (não alternável) no nicho de T0 abre a `PuzzleDoor` D2
   (movimento instantâneo, colisor desligado quando aberta) que bloqueia o trecho
   direito de T2. Um segundo controlador liga o botão de resgate B2r à plataforma
   AP2 do nicho, que devolve o acionador a T1.
3. **G3 (T2 → T3)**: `PressureButton` B3 ergue a AP3 (7.8 → 10.8). A `PushableBox`
   pode ser empurrada sobre B3 para travar a plataforma e servir de degrau. A
   alavanca de reset R3 (`_resetSwitch`) devolve a caixa ao spawn, e o bootstrap a
   devolve automaticamente se ela cair abaixo de T2.

Detalhes de coordenadas e as saídas de emergência de cada portal estão em
`docs/phase1-blockout.md`.

## Testes Automatizados

- **EditMode** (`PhysicsPuzzleTests.cs`):
  - `Lever_StartsDeactivated_AndTogglesState`
  - `PuzzleDoor_OpensAndCloses_UpdatingStateAndEvents`
  - `ActivatedPlatform_ConfiguresTargetAndTogglesActivation`
  - `PushableBox_CachesSpawnPosition_AndResetsToSpawn`
  - `CooperativePuzzleController_SolvesAndRecovers_FromImpossibleState`
- **PlayMode** (`PuzzlePlayModeTests.cs`):
  - `Player_ContactWithLever_TogglesState`
  - `PressureButton_PressedByBox_OpensPuzzleDoor`
  - `ActivatedPlatform_MovesTowardsTarget_WhenActivated`
  - `CooperativePuzzle_ResetRecoversCratePosition`
