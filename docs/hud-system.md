# Guia do Sistema de HUD (HUD-01)

## Descrição

O sistema de HUD (Heads-Up Display) do **Healthy Race** provê feedback visual contínuo e em tempo real sobre o estado fisiológico dos jogadores, contagem de coletáveis, pontuação, cronometragem da corrida e alertas da janela final.

A arquitetura é estritamente desacoplada e orientada a eventos: nenhum componente de HUD realiza polling de variáveis em `Update()`. Toda atualização é acionada por eventos emitidos pelos componentes de domínio (`WellbeingComponent`, `FatigueComponent`, etc.) ou por chamadas pontuais de API (`MatchHUD.UpdateTimer()`, `PlayerHUD.AddScore()`).

## Componentes

### 1. PlayerHUD (`HealthyRace.UI.PlayerHUD`)

Painel individualizado por jogador (P1 e P2). Cada instância é vinculada a uma fonte específica de dados e mantém total independência de estado.

- **Identificação**: Rótulo do jogador (`P1` ou `P2`) com cor de destaque (`accentColor`: azul para P1, magenta para P2);
- **Bem-Estar**:
  - Exibição numérica (`Bem-estar: X / 100`);
  - Nome da faixa fisiológica ativa (`Faixa: VITA saudável`, `Faixa: VITA cansado`, `Faixa: SALO`);
  - Barra horizontal de preenchimento (`WellbeingFill`), colorida dinamicamente com a cor característica da faixa (`band.CharacterColor`);
- **Fadiga**:
  - Estado ativo (`Fadiga: Normal` em tom neutro ou `Fadiga: EM FADIGA!` em vermelho em negrito);
  - Contador cumulativo de episódios (`Episódios: N`);
- **Alimentos Coletados**:
  - Contador total e discriminação entre saudáveis e ultraprocessados (`Alimentos: N (S 🥗 | U 🍔)`);
- **Pontuação**:
  - Pontuação acumulada (`Pontos: N`), preparado para integração com `SCORE-01`;
- **Progresso de Corrida**:
  - Indicador percentual (`Progresso: N%`). Na Fase 1 é alimentado por `Phase1Bootstrap.GetProgress`, em quatro marcos (corredores B, C e D alcançados com apoio real, e saída cruzada): 0/25/50/75/100%.

#### Métodos e APIs Públicas do PlayerHUD

```csharp
public void Initialize(Transform parent, string playerLabel, Color accentColor, WellbeingComponent wellbeing, FatigueComponent fatigue, Vector2 anchor, Vector2 pivot, Vector2 position);
public void Bind(WellbeingComponent wellbeing, FatigueComponent fatigue);
public void Unbind();
public void UpdateScore(int score);
public void AddScore(int delta);
public void RegisterFoodCollected(FoodData food);
public void UpdateFoodCounts(int healthyCount, int ultraProcessedCount);
public void UpdateProgress(float progress01);
```

### 2. MatchHUD (`HealthyRace.UI.MatchHUD`)

Componente responsável por apresentar o estado global da partida no topo central da tela, desacoplado dos dados individuais de cada competidor:

- **Cronômetro Central**: Formatação de tempo decorrido ou restante (`Tempo: MM:SS`);
- **Janela Final de 20s**: Banner visual com contagem regressiva ativado quando o primeiro jogador cruza a linha de chegada (`JANELA FINAL: 20s`), alertando o segundo jogador para concluir o percurso antes da penalidade.

#### Métodos e APIs Públicas do MatchHUD

```csharp
public void Initialize(Transform parent, Vector2 position);
public void UpdateTimer(float seconds);
public void SetFinalWindow(bool active, float secondsRemaining = 20f);
```

## Especificação de Layout em 1280 × 720

Para a resolução de referência mínima de **1280 × 720** (e resoluções superiores escalonadas proporcionalmente via `CanvasScaler`):

| Painel | Dimensões (L × A) | Âncoras (`Min`, `Max`) | Posição Ancorada | Intervalo Horizontal |
| --- | --- | --- | --- | --- |
| **P1 HUD** | 280 × 175 px | `(0, 1)`, `(0, 1)` | `(20, -20)` | `[20, 300]` px |
| **Match HUD** | 280 × 64 px | `(0.5, 1)`, `(0.5, 1)` | `(0, -20)` | `[500, 780]` px |
| **P2 HUD** | 280 × 175 px | `(1, 1)`, `(1, 1)` | `(-20, -20)` | `[980, 1260]` px |

- **Espaço Livre entre P1 e Match HUD**: 200 px;
- **Espaço Livre entre Match HUD e P2**: 200 px;
- **Folga Total entre P1 e P2**: 680 px;
- **Garantia Visual**: Nenhuma sobreposição, textos legíveis sem quebra ou corte em 1280 × 720.

## Testes Automatizados

### EditMode (`PlayerHudEditModeTests.cs`)

1. `PlayerHUD_Initialize_ConfiguresLabelsAndBindings`: valida criação dos componentes de UI e vinculação inicial;
2. `PlayerHUD_WellbeingChanged_UpdatesValueAndFillAmount`: valida atualização numérica e preenchimento da barra via evento;
3. `PlayerHUD_WellbeingBandChanged_UpdatesBandTextAndColor`: valida atualização de texto e cor da faixa fisiológica;
4. `PlayerHUD_FatigueChanges_UpdateStateAndEpisodes`: valida exibição de estado ativo de fadiga e episódios cumulativos;
5. `PlayerHUD_ScoreFoodAndProgress_UpdateCorrectly`: valida métodos de pontuação, alimentos e progresso;
6. `PlayerHUD_Unbind_ClearsSubscriptionsCleanly`: valida cancelamento de eventos sem vazamentos nem exceções nulas;
7. `MatchHUD_TimerAndFinalWindow_UpdatesDisplayCorrectly`: valida cronometragem formatada e banner de janela final.

### PlayMode (`PlayerHudPlayModeTests.cs`)

1. `TwoPlayers_PlayerHUD_IsolatesP1AndP2_InRealTime`: valida isolamento absoluto entre P1 e P2 em tempo real sob física e eventos;
2. `HUD_Layout_At1280x720_HasNoOverlapBetweenP1AndP2`: valida matematicamente a ausência de sobreposição e folgas seguras na resolução 1280 × 720.
