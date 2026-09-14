# Sistema de Bem-Estar (WELL-01)

## Descrição

O Sistema de Bem-Estar gerencia a condição física e nutricional individual de cada jogador em Healthy Race. O valor varia estritamente no intervalo fechado de 0 a 100 e determina dinamicamente multiplicadores de velocidade horizontal e força de pulo através da integração com ScriptableObjects de faixas de Bem-Estar (`WellbeingBandData`).

## Regras e Arquitetura

### 1. Limites e Clamping

- O valor de Bem-Estar é individual para cada jogador (`0 <= BemEstar <= 100`).
- Qualquer alteração (positiva ou negativa) aplica `Mathf.Clamp(value, 0, 100)`.
- Valores nunca excedem 100 nem ficam abaixo de 0.

### 2. Desacoplamento e Reatividade

- O componente `WellbeingComponent` não possui acoplamento rígido com `PlayerMovement`.
- `WellbeingComponent` dispara eventos reativos em tempo de execução:
  - `ValueChanged(int newValue)`: notifica o novo valor numérico;
  - `WellbeingChanged(int previousValue, int currentValue)`: notifica o valor anterior e o novo;
  - `WellbeingBandChanged(WellbeingBandData newBand)`: notifica a transição de faixa;
  - `MultipliersChanged(float moveMultiplier, float jumpMultiplier)`: notifica a alteração imediata dos multiplicadores.
- `PlayerController` orquestra a sincronização imediata com `PlayerMovement.SetMovementMultipliers(moveMultiplier, jumpMultiplier)`, garantindo resposta instantânea sem atraso de frame.

### 3. Isolamento entre Jogadores

- P1 e P2 possuem instâncias independentes de `WellbeingComponent`.
- A alteração do Bem-Estar de P1 não afeta o valor, a faixa nem os multiplicadores de P2, e vice-versa.

## Faixas e Efeitos na Locomoção

| Faixa | Intervalo | Modificador Movimento | Modificador Pulo | Forma Visual |
| --- | --- | --- | --- | --- |
| `VITA saudável` | [76, 100] | 1.00x | 1.00x | Vita normal com brilho verde |
| `VITA estável` | [51, 75] | 0.95x | 1.00x | Vita verde moderado |
| `VITA cansado` | [26, 50] | 0.90x | 0.95x | Vita amarelado com olhos cansados |
| `Estado crítico` | [1, 25] | 0.80x | 0.90x | Vita avermelhado com olhos cansados |
| `SALO` | [0, 0] | 0.70x | 0.80x | Forma Salo (laranja/castanho) |

## API Pública

- `SetValue(int value)`: define diretamente o valor respeitando o intervalo [0, 100].
- `ApplyDelta(int delta)`: aplica variação relativa positiva ou negativa.
- `ConfigureBands(WellbeingBandData[] bands)`: configura a coleção de faixas para resolução.
- `Initialize(int initialValue, WellbeingBandData[] bands)`: inicializa o componente em runtime ou teste.
- `CurrentValue`: valor numérico atual (0 a 100).
- `CurrentBand`: ScriptableObject da faixa atualmente ativa.
- `MovementMultiplier`: multiplicador atual de velocidade de locomoção.
- `JumpMultiplier`: multiplicador atual de força de pulo.
- `NormalizedValue`: razão percentual normalizada de 0.0f a 1.0f (ideal para barras de HUD).

## Testes Automatizados

### EditMode (`WellbeingComponentTests.cs`)

- `ApplyDelta_AtMaximumValue_StaysAtOneHundred`: clamping no teto de 100;
- `ApplyDelta_AfterBurgerPenalty_ReducesToEightyTwo`: redução correta por penalidade;
- `ApplyDelta_BelowMinimum_ClampsToZero`: clamping no piso de 0;
- `SetValue_UsesTheExpectedWellbeingBand`: mapeamento das 5 faixas nos limites;
- `SetValue_ClampsAboveMaximum_ToMaximumValue`: saturação superior de valores arbitrários;
- `SetValue_ClampsBelowMinimum_ToMinimumValue`: saturação inferior de valores arbitrários;
- `SetValue_WhenValueChanges_EmitsExpectedEvents`: emissão completa de todos os eventos C#;
- `SetValue_WhenValueDoesNotChange_DoesNotEmitEvents`: supressão de eventos redundantes;
- `TwoPlayers_WellbeingChanges_RemainStrictlyIsolated`: independência total entre P1 e P2;
- `NormalizedValue_ReturnsExpectedRatio`: cálculo correto de 0.0f a 1.0f;
- `PlayerController_ImmediatelyUpdatesMovementMultipliers_OnWellbeingChange`: sincronização imediata entre Wellbeing e locomoção do personagem.

### PlayMode (`WellbeingPlayModeTests.cs`)

- `TwoPlayers_WellbeingLocomotionMultipliers_AffectSpeedIndependently_InPlayMode`: validação de velocidade e distância percorrida no motor de física com jogadores independentes;
- `Player_WellbeingApplyDelta_UpdatesSpeedImmediately_InPlayMode`: resposta imediata de velocidade ao aplicar deltas em tempo de execução.
