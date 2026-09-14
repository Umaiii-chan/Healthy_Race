# Sistema de Fadiga (FAT-01)

## Descrição

O Sistema de Fadiga atua como um mecanismo compensatório e punitivo quando o Bem-Estar de um jogador atinge níveis críticos. Ele bloqueia temporariamente a movimentação do personagem, aplica uma recuperação parcial de saúde e controla episódios de forma estritamente rearmável.

## Regras Funcionais

### 1. Gatilho de Disparo

- Ocorre quando o Bem-Estar cruza o limiar de cima para baixo: `> 20 -> <= 20`.
- Casos válidos: `21 -> 20`, `30 -> 10`, etc.
- Valores que permanecem abaixo do limiar (ex: `20 -> 19`) sem rearme **não** disparam novo episódio.
- Se o jogador estiver no ar no momento do cruzamento, o estado entra em `FatiguePending` e o episódio só inicia efetivamente (`Fatigued`) após o contato com o chão (`IsGrounded == true`).

### 2. Efeitos do Episódio

- **Bloqueio de Movimento**: o jogador fica imóvel por exatamente 2 segundos (`SetMovementEnabled(false)`).
- **Recuperação Automática**: ao fim dos 2 segundos, o jogador recebe `+15` de Bem-Estar automaticamente via `ApplyDelta(15)`.
- **Contador de Episódios**: o contador (`FatigueEpisodeCount`) é incrementado em exatamente 1 por episódio.
- **Desarme**: o sistema é imediatamente desarmado (`Readiness = Disarmed`), impedindo disparos repetitivos contínuos.

### 3. Regra de Rearme

- O sistema só pode ser rearmado quando o Bem-Estar subir estritamente acima de 25: `Bem-Estar > 25`.
- Subir para 25 não rearma (requer `> 25`, por exemplo, 26).
- Após o rearme (`Readiness = Armed`), um novo cruzamento `> 20 -> <= 20` pode provocar um novo episódio.

### 4. Independência entre Jogadores

- O estado de fadiga de P1 é completamente independente de P2.
- Se P1 entra em fadiga, P2 continua se movendo normalmente e seu contador/rearme não são afetados.

## Estados da Máquina de Fadiga

| Estado | Significado | Locomoção Permitida |
| --- | --- | --- |
| `Normal` | Operação padrão, monitorando cruzamento | Sim |
| `FatiguePending` | Cruzou o limiar no ar; aguarda tocar o chão | Sim (queda física) |
| `Fatigued` | Bloqueado por 2 segundos | Não |
| `Recovering` | Aplicando recuperação de +15 | Não (transição) |

## Testes Automatizados

### EditMode (`FatigueStateMachineTests.cs`)

- `CrossingTwenty_TriggersOneFatigueEpisode`: transição 21 -> 20;
- `CrossingThirtyToTen_TriggersOneFatigueEpisode`: transição 30 -> 10;
- `RemainingBelowThreshold_DoesNotTriggerAnotherEpisode`: permanência sem rearme;
- `CompletingFatigue_AddsRecoveryWellbeing`: aplicação dos +15 de recuperação;
- `RecoveryAtTwenty_RemainsDisarmedUntilWellbeingExceedsTwentyFive`: desarme mantido em <= 25;
- `WellbeingAtTwentyFive_DoesNotRearm`: teste de fronteira em 25 exatos;
- `WellbeingAtTwentySix_RearmsAndAllowsANewFatigueEpisode`: rearme em 26 e disparo subsequente;
- `MultipleDropsBelowThreshold_WithoutRearm_DoNotTriggerEpisodes`: múltiplos decréscimos sem rearme;
- `CrossingThresholdInAir_WaitsForGroundBeforeStartingFatigue`: transição pendente no ar até o chão;
- `SeparateFatigueInstances_DoNotSharePlayerState`: P1 em fadiga não afeta P2;
- `PlayerTwo_EnteringFatigue_DoesNotAffectPlayerOne`: P2 em fadiga não afeta P1;
- `FatigueComponent_Integration_LocksMovementAndAppliesRecovery`: integração do componente com locomoção.

### PlayMode (`FatiguePlayModeTests.cs`)

- `Fatigue_LocksMovementForDuration_AndAppliesAutoRecovery_InPlayMode`: bloqueio real e recuperação cronometrada no motor de física;
- `Fatigue_CrossingThresholdInAir_DelaysUntilGrounded_InPlayMode`: atraso no ar e ativação ao pousar;
- `TwoPlayers_FatigueEpisodeOnPlayerOne_LeavesPlayerTwoCompletelyUnaffected_InPlayMode`: isolamento comprovado em cena PlayMode.
