# Sistema de Chegada e Encerramento (GOAL-01)

## Descrição

O sistema de chegada e encerramento do Healthy Race gerencia a conclusão da corrida, congelamento do vencedor, contagem regressiva da janela final, penalização de DNF (Did Not Finish), compilação das estatísticas e apresentação da tela final de resultados.

## Regras de Jogo e Fluxo de Encerramento

1. **Chegada Individual (`FinishLine.cs`)**:
   - Cada jogador cruza a linha de chegada de forma independente;
   - Registra o tempo exato de cruzamento via `Time.timeSinceLevelLoad`;
   - Congela imediatamente a locomoção do jogador concluinte (`PlayerController.SetMovementEnabled(false)` e `rb.linearVelocity = Vector2.zero`) para impedir que ele continue correndo após a faixa.
2. **Janela Final de 20 Segundos (`MatchCompletionController.cs`)**:
   - A chegada do 1º colocado inicia uma contagem regressiva de 20 segundos (`FinalWindowDuration = 20.0f`);
   - O HUD central (`MatchHUD`) exibe o alerta com contagem regressiva: `JANELA FINAL: Xs`.
3. **Chegada do Segundo Jogador**:
   - Se o 2º colocado cruzar a linha dentro da janela de 20 segundos, ele é classificado normalmente em 2º lugar;
   - Seu movimento é congelado e a partida é finalizada com ambos os jogadores qualificados.
4. **Expiração da Janela (DNF - Did Not Finish)**:
   - Se os 20 segundos se esgotarem antes do 2º jogador cruzar a linha:
     - O jogador é marcado como `DNF (Did Not Finish)`;
     - Recebe penalidade de Bem-Estar de **-20 WB** (`_dnfPenalty = 20`);
     - Sua locomoção é congelada imediatamente;
     - A partida é encerrada.
5. **Critérios de Desempate e Vencedor**:
   - O jogador com melhor colocação e sem DNF é declarado vencedor;
   - Em caso de tempos idênticos, o desempate é feito pelo maior valor de **Bem-Estar final**;
   - Se ainda persistir igualdade, o resultado é considerado empate (`WinnerPlayerIndex = 0`).

## Modo Portas Duplas (Fase 1 — arena cooperativa)

Na arena da Fase 1 a chegada funciona como em *Fireboy & Watergirl*:

1. Há duas `FinishLine`, cada uma com `AcceptedPlayerIndex` (Porta P1 só aceita P1,
   Porta P2 só aceita P2). A porta registra ocupação (`IsOccupiedBy`) e a libera ao
   sair do gatilho.
2. `MatchCompletionController.Configure(..., secondFinishLine, requireBothAtDoors: true)`
   ativa o modo. A primeira chegada define a ordem (1º/2º) e leva o estado a
   `WaitingForPartner`, **sem congelar** o jogador e **sem janela final nem DNF**.
3. A partida encerra (`Concluded`) quando `AreBothPlayersAtDoors()` é verdadeiro, isto
   é, P1 e P2 estão simultaneamente em suas portas. Se o primeiro sair da porta, a
   partida aguarda até que ele volte.
4. O `MatchHUD` mostra `P1 NA PORTA - AGUARDANDO P2` (ou vice-versa) via
   `ShowStatusMessage`.

O fluxo de linha única com janela de 20 s e DNF descrito acima continua disponível
quando `requireBothAtDoors` é falso.

## Componentes Arquiteturais

### 1. FinishLine (`FinishLine.cs`)

- **Namespace**: `HealthyRace.Level`;
- **Colisor de Linha**: `BoxCollider2D` (`isTrigger = true`);
- **Isolamento**: Registra independentemente os tempos de P1 e P2;
- **Eventos**: `event Action<int, float> PlayerCrossedFinish`, `static event Action<int, float> AnyPlayerFinished`.

### 2. MatchCompletionController (`MatchCompletionController.cs`)

- **Namespace**: `HealthyRace.Level`;
- **Estados**:
  - `Running`: corrida em andamento;
  - `FinalCountdown`: 1º jogador finalizou, janela de 20s ativa;
  - `Concluded`: partida encerrada, resultados compilados;
- **Estruturas de Dados**:
  - `PlayerMatchResult`: armazena `PlayerIndex`, `FinishTime`, `IsDnf`, `FinalWellbeing` e `Placement`;
  - `MatchResultData`: consolida os dados de P1 e P2, `WinnerPlayerIndex`, `TotalDuration` e flag `HasDnf`;
- **Eventos**: `event Action<int, float> PlayerFinished`, `event Action<float> FinalWindowStarted`, `event Action<float> FinalWindowTicked`, `event Action<MatchResultData> MatchConcluded`.

### 3. MatchResultsPanel (`MatchResultsPanel.cs`)

- **Namespace**: `HealthyRace.UI`;
- **Tela de Resultados (Canvas Overlay)**:
  - Cabeçalho: Título e anúncio de vencedor (`VENCEDOR: JOGADOR 1!`, `VENCEDOR: JOGADOR 2!` ou `EMPATE!`);
  - Coluna do Jogador 1: Posição (1º/2º/DNF), tempo formatado (`mm:ss`) e Bem-Estar final;
  - Coluna do Jogador 2: Posição (1º/2º/DNF), tempo formatado (`mm:ss`) e Bem-Estar final;
  - Mensagem de encerramento da corrida.

## Testes Automatizados

- **EditMode** (`MatchCompletionTests.cs`):
  - `MatchCompletion_FirstFinisher_FreezesPlayerAndStartsFinalCountdown`: valida transição para `FinalCountdown`, janela de 20s e congelamento de P1;
  - `MatchCompletion_SecondFinisherInTime_CompletesMatchWithoutDnf`: valida conclusão normal sem DNF quando P2 conclui a tempo;
  - `MatchCompletion_Timeout_AppliesDnfPenaltyAndConcludes`: valida aplicação de -20 WB e marcação de DNF em caso de timeout;
  - `MatchCompletion_CompileResults_HandlesP2Winner`: valida atribuição correta de vitória para P2.
- **PlayMode** (`MatchCompletionPlayModeTests.cs`):
  - `Player_CrossingFinishLine_StopsLocomotion_InPlayMode`: valida cessação física de movimento após a faixa;
  - `FirstFinisher_TriggersFinalCountdownInMatchHUD_InPlayMode`: valida ativação do HUD da janela final;
  - `MatchCompletion_CountdownExpiration_MarksDnfAndAppliesPenalty_InPlayMode`: valida expiração e penalidade em tempo real.
