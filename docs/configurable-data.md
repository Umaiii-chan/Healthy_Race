# Guia de Dados Configuráveis (DATA-01)

## Descrição

A infraestrutura de dados configuráveis desacopla os valores de balanceamento de gameplay (alimentos, pontuações, modificadores de Bem-Estar e faixas fisiológicas) da lógica em código C#, permitindo que game designers ajustem e expandam os dados diretamente pelo Inspector da Unity através de ScriptableObjects.

## ScriptableObjects Implementados

### 1. FoodData (`HealthyRace.Collectibles.FoodData`)

Define os dados imutáveis de um item coletável alimentar:

- **Identificação**:
  - `Id`: chave de identificação do alimento (ex: `apple`, `burger`, `banana`, `soda`);
  - `DisplayName`: nome legível apresentado na interface (ex: `Apple`, `Banana`);
- **Categoria Alimentar** (`FoodCategory`):
  - `Healthy`: alimentos nutritivos que aumentam o Bem-Estar e conferem pontuação;
  - `UltraProcessed`: alimentos ultraprocessados que diminuem o Bem-Estar e não geram pontos positivos;
- **Gameplay**:
  - `BasePoints`: pontuação concedida ao coletar o alimento;
  - `WellbeingDelta`: valor de variação direta no Bem-Estar (positivo para saudáveis, negativo para ultraprocessados);
- **Apresentação Opcional**:
  - `Sprite`: ícone do alimento;
  - `CollectSound`: efeito sonoro disparado na coleta.

#### Catálogo dos 18 Alimentos do MVP

O catálogo é composto por 18 alimentos (10 saudáveis e 8 ultraprocessados), cada um com seu respectivo ScriptableObject em `Assets/_Game/ScriptableObjects/Food/` e sprite dedicado em `Assets/_Game/Art/Collectibles/`:

| Asset | Id | Nome Legível | Categoria | Base Points | Wellbeing Delta | Sprite |
| --- | --- | --- | --- | --- | --- | --- |
| `SO_Food_Apple.asset` | `apple` | Apple | `Healthy` | 100 | +10 | `spr_food_apple.png` |
| `SO_Food_Banana.asset` | `banana` | Banana | `Healthy` | 80 | +8 | `spr_food_banana.png` |
| `SO_Food_Orange.asset` | `orange` | Orange | `Healthy` | 100 | +10 | `spr_food_orange.png` |
| `SO_Food_Carrot.asset` | `carrot` | Carrot | `Healthy` | 120 | +12 | `spr_food_carrot.png` |
| `SO_Food_Broccoli.asset` | `broccoli` | Broccoli | `Healthy` | 150 | +15 | `spr_food_broccoli.png` |
| `SO_Food_Strawberry.asset` | `strawberry` | Strawberry | `Healthy` | 80 | +8 | `spr_food_strawberry.png` |
| `SO_Food_Watermelon.asset` | `watermelon` | Watermelon | `Healthy` | 100 | +10 | `spr_food_watermelon.png` |
| `SO_Food_Avocado.asset` | `avocado` | Avocado | `Healthy` | 140 | +14 | `spr_food_avocado.png` |
| `SO_Food_Fish.asset` | `fish` | Fish | `Healthy` | 160 | +16 | `spr_food_fish.png` |
| `SO_Food_Water.asset` | `water` | Water | `Healthy` | 150 | +15 | `spr_food_water.png` |
| `SO_Food_Burger.asset` | `burger` | Burger | `UltraProcessed` | 0 | -18 | `spr_food_burger.png` |
| `SO_Food_Soda.asset` | `soda` | Soda | `UltraProcessed` | 0 | -12 | `spr_food_soda.png` |
| `SO_Food_Fries.asset` | `fries` | Fries | `UltraProcessed` | 0 | -15 | `spr_food_fries.png` |
| `SO_Food_Donut.asset` | `donut` | Donut | `UltraProcessed` | 0 | -14 | `spr_food_donut.png` |
| `SO_Food_HotDog.asset` | `hotdog` | Hot Dog | `UltraProcessed` | 0 | -16 | `spr_food_hotdog.png` |
| `SO_Food_Pizza.asset` | `pizza` | Pizza | `UltraProcessed` | 0 | -18 | `spr_food_pizza.png` |
| `SO_Food_Candy.asset` | `candy` | Candy | `UltraProcessed` | 0 | -10 | `spr_food_candy.png` |
| `SO_Food_Chips.asset` | `chips` | Chips | `UltraProcessed` | 0 | -12 | `spr_food_chips.png` |

### 2. FoodCollectible (`HealthyRace.Collectibles.FoodCollectible`)

Componente MonoBehaviour associado aos prefabs de alimentos no cenário para mediar a interação de coleta:

- **Detecção Segura**: Requer `Collider2D` em modo trigger e detecta exclusivamente entidades com `WellbeingComponent` (jogadores);
- **Isolamento entre Jogadores**: Ao colidir com Player 1, altera estritamente o Bem-Estar de Player 1, mantendo Player 2 intocado;
- **Prevenção de Dupla Coleta**: Flag atômica `_isCollected` protege contra chamadas simultâneas ou subsequentes no mesmo quadro de física;
- **Desativação**: Desativa o GameObject imediatamente após a aplicação do efeito;
- **Apresentação Visual e Sonora**: Configura o sprite do `SpriteRenderer` a partir do `FoodData` atribuído e emite áudio caso `CollectSound` esteja configurado;
- **Eventos Desacoplados**: Dispara `event Action<FoodCollectible, WellbeingComponent> Collected` para escuta externa por sistemas de pontuação ou HUD.

### 3. WellbeingBandData (`HealthyRace.WellBeing.WellbeingBandData`)

Define uma faixa de Bem-Estar e seus respectivos multiplicadores de gameplay e apresentação visual:

- **Identidade e Limites**:
  - `Id`: identificador textual da faixa;
  - `DisplayName`: nome da faixa exibido no HUD;
  - `MinValue` e `MaxValue`: intervalo inclusivo de Bem-Estar `[MinValue, MaxValue]`;
- **Multiplicadores de Gameplay**:
  - `MovementMultiplier`: multiplicador aplicado à velocidade horizontal do jogador;
  - `JumpMultiplier`: multiplicador aplicado à força de pulo do jogador;
- **Apresentação Visual**:
  - `VisualForm`: forma visual do personagem (`Vita` ou `Salo`);
  - `CharacterColor`: tonalidade principal do personagem;
  - `LeafColor`: tonalidade da folha do personagem;
  - `GlowColor` e `GlowScale`: configurações do efeito de brilho;
  - `ShowTiredEyes`: indicador de fadiga visual (olhos sonolentos/pesados).

#### Faixas Padrão Configuradas

| Faixa Asset | Intervalo | Forma | Mult. Movimento | Mult. Pulo | Feedback Visual |
| --- | --- | --- | --- | --- | --- |
| `SO_Wellbeing_Healthy` | [76, 100] | `Vita` | 1.0x | 1.0x | Verde vibrante, brilho forte |
| `SO_Wellbeing_Stable` | [51, 75] | `Vita` | 0.95x | 1.0x | Verde moderado |
| `SO_Wellbeing_Tired` | [26, 50] | `Vita` | 0.90x | 0.95x | Amarelado, olhos cansados |
| `SO_Wellbeing_Critical` | [1, 25] | `Vita` | 0.80x | 0.90x | Avermelhado, olhos cansados |
| `SO_Wellbeing_Salo` | [0, 0] | `Salo` | 0.70x | 0.80x | Alaranjado escuro, forma Salo |

As 5 faixas cobrem continuamente todo o intervalo de 0 a 100 sem lacunas nem sobreposições.

## Como Criar Novos Dados no Unity Editor

1. Na janela **Project**, navegue até a pasta de destino (ex: `Assets/_Game/ScriptableObjects/Food/`).
2. Clique com o botão direito e selecione:
   - Para alimentos: **Create > Healthy Race > Food Data**;
   - Para faixas de Bem-Estar: **Create > Healthy Race > Wellbeing Band**.
3. No **Inspector**, configure os valores de balanceamento desejados.
4. O ScriptableObject pode ser referenciado diretamente por componentes de gameplay (`FoodCollectible`, `WellbeingComponent`, etc.).

## Testes Automatizados

### EditMode (`ConfigurableDataTests.cs`)

1. `FoodData_FactoryCreatesValidInstanceWithExpectedProperties`: criação e integridade de propriedades em tempo de execução;
2. `FoodData_ExistingAssets_AppleAndBurger_AreValid`: integridade dos assets existentes `SO_Food_Apple` e `SO_Food_Burger`;
3. `FoodData_NewAssets_BananaAndSoda_AreValid`: integridade dos novos assets de exemplo `SO_Food_Banana` e `SO_Food_Soda`;
4. `FoodCatalog_ContainsAll18MvpFoods_WithValidCategoriesAndCounts`: validação dos 18 assets no catálogo, contendo 10 saudáveis e 8 ultraprocessados;
5. `FoodCatalog_All18Foods_HaveUniqueIdsAndValidFields`: verificação de unicidade de IDs, nomes válidos, pontuação correta e sprites atribuídos;
6. `FoodCollectible_Configure_UpdatesVisualWithFoodSprite`: atualização visual de sprite ao configurar alimento;
7. `FoodCollectible_ResetState_ReactivatesAndClearsCollectedFlag`: reativação e limpeza de estado para reutilização;
8. `WellbeingBandData_Contains_ReturnsTrueForValuesInRange`: verificação precisa de limites inclusivos;
9. `WellbeingBandData_Contains_ReturnsFalseForValuesOutOfRange`: rejeição de valores externos ao intervalo;
10. `WellbeingBandData_ExistingBands_CoverZeroToOneHundredContinuouslyWithoutGaps`: garantia matemática de cobertura contínua sem lacunas;
11. `WellbeingBandData_ExistingBands_MultipliersAreValidAndPositive`: multiplicadores positivos e coerentes;
12. `WellbeingBandData_FactoryCreatesValidBandWithProperties`: factory programática de faixas.

### PlayMode (`FoodCollectiblePlayModeTests.cs`)

1. `HealthyFood_WhenCollected_IncreasesCollectorWellbeingAndDeactivates`: coleta de alimento saudável eleva o Bem-Estar e desativa o objeto;
2. `UltraProcessedFood_WhenCollected_DecreasesCollectorWellbeingAndDeactivates`: coleta de alimento ultraprocessado reduz o Bem-Estar e desativa o objeto;
3. `FoodCollection_IsolatesPlayerOneFromPlayerTwo`: garantia de isolamento total entre P1 e P2 durante a coleta;
4. `FoodCollectible_PreventsDoubleCollection`: prevenção contra disparos múltiplos no mesmo ciclo de física;
5. `NonPlayerCollider_DoesNotTriggerFoodCollection`: colisão com caixas/obstáculos não consome o alimento;
6. `FoodCollectible_FiresCollectedEventOnPickup`: disparo do evento desacoplado `Collected`.
