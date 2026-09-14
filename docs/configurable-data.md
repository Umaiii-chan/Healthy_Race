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

#### Exemplos de Configuração Criados

| Asset | Id | Categoria | Base Points | Wellbeing Delta |
| --- | --- | --- | --- | --- |
| `SO_Food_Apple.asset` | `apple` | `Healthy` | 100 | +10 |
| `SO_Food_Burger.asset` | `burger` | `UltraProcessed` | 0 | -18 |
| `SO_Food_Banana.asset` | `banana` | `Healthy` | 80 | +8 |
| `SO_Food_Soda.asset` | `soda` | `UltraProcessed` | 0 | -12 |

### 2. WellbeingBandData (`HealthyRace.WellBeing.WellbeingBandData`)

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

Os testes EditMode em `ConfigurableDataTests.cs` validam:

1. `FoodData_FactoryCreatesValidInstanceWithExpectedProperties`: criação e integridade de propriedades em tempo de execução;
2. `FoodData_ExistingAssets_AppleAndBurger_AreValid`: integridade dos assets existentes `SO_Food_Apple` e `SO_Food_Burger`;
3. `FoodData_NewAssets_BananaAndSoda_AreValid`: integridade dos novos assets de exemplo `SO_Food_Banana` e `SO_Food_Soda`;
4. `WellbeingBandData_Contains_ReturnsTrueForValuesInRange`: verificação precisa de limites inclusivos;
5. `WellbeingBandData_Contains_ReturnsFalseForValuesOutOfRange`: rejeição de valores externos ao intervalo;
6. `WellbeingBandData_ExistingBands_CoverZeroToOneHundredContinuouslyWithoutGaps`: garantia matemática de que todas as notas de 0 a 100 pertencem a exatamente uma faixa;
7. `WellbeingBandData_ExistingBands_MultipliersAreValidAndPositive`: garantia de que multiplicadores são positivos e coerentes com a curva de progressão;
8. `WellbeingBandData_FactoryCreatesValidBandWithProperties`: factory programática para testes e customização.
