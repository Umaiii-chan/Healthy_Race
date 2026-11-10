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

## Integração na Fase 1 (corrida)

A Fase 1 é uma corrida: nenhum mecanismo exige os dois jogadores. `LevelBootstrap` monta,
a partir do mapa em texto (ver `docs/levels/fase-01.md`):

1. **Alavanca e portão** (`L` e `G` no mapa): `Lever` não alternável; ao ser puxada, a
   `PuzzleDoor` (movimento instantâneo) treme por 1,2 s e abre de vez. A espera faz parte
   do custo do caminho longo.
2. **Elevador de autoatendimento** (`E` e `:`): `ActivatedPlatform` com um
   `PressureButton` filho que viaja junto. Sobe com quem estiver em cima, volta sozinho
   quando fica vazio e, em repouso, fica rente ao piso.

`CooperativePuzzleController` e `PushableBox` continuam no código e nos testes, mas a
Fase 1 não os usa.

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
