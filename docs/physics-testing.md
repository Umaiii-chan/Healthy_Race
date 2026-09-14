# Teste de Física Básica (PHYS-01)

## Descrição

A infraestrutura física básica define a interação previsível e consistente entre personagens, cenário e elementos interativos do puzzle. Inclui configuração de camadas físicas (layers), caixa empurrável (`PushableBox`) e botão de pressão (`PressureButton`).

## Camadas Físicas (Layers)

- **Layer 6 - Ground**: superfícies estáticas e plataformas transitáveis.
- **Layer 7 - Player**: personagens P1 e P2.
- **Layer 8 - Obstacles**: caixas empurráveis e obstáculos dinâmicos.
- **Layer 9 - Interactive**: botões de pressão e gatilhos de puzzle.

## Elementos Implementados

### 1. PushableBox (`PF_PushableBox.prefab`)

- **Componente**: `PushableBox` (namespace `HealthyRace.Obstacles`);
- **Rigidbody2D**:
  - `BodyType`: `Dynamic`;
  - `Mass`: `1.5` (equilibrada para empurrão intencional sem esforço excessivo);
  - `Linear Damping`: `1.5` (evita deslizamento perpétuo após cessar o empurrão);
  - `Gravity Scale`: `2.4`;
  - `Constraints`: `FreezeRotationZ` (mantém a caixa sempre nivelada);
  - `Collision Detection`: `Continuous` (impede tunneling contra paredes e chão).

### 2. PressureButton (`PressureButton.cs`)

- **Componente**: `PressureButton` (namespace `HealthyRace.Puzzles`);
- **Collider2D**: `isTrigger = true` (detecta sobreposição de volume);
- **Ativadores Válidos**: caixas empurráveis (`PushableBox`) e jogadores (`PlayerMovement` / `PlayerController`);
- **Feedback**: emissão de eventos C# (`OnPressed`, `OnReleased`, `OnPressedStateChanged`) e deslocamento/coloração visual.

## Roteiro de Validação Manual

1. Inicie a cena no Unity Editor com uma caixa empurrável e um botão de pressão no chão.
2. Aproxime P1 ou P2 da caixa e mantenha a direção de caminhada contra ela:
   - A caixa deve se mover de forma suave e contínua enquanto o jogador empurra.
   - Ao soltar o controle, a caixa deve parar rapidamente sem deslizar indefinidamente.
3. Empurre a caixa em direção a uma parede ou borda:
   - A caixa deve colidir solidamente sem atravessar a geometria.
4. Empurre a caixa sobre a plataforma do botão de pressão:
   - O botão deve acionar (emitindo evento e alterando o estado visual para verde).
5. Empurre a caixa para fora do botão:
   - O botão deve desarmar (voltando ao estado inicial).
6. Caminhe com o próprio jogador sobre o botão:
   - O botão deve ser acionado pelo peso do personagem.

## Testes Automatizados

- **EditMode** (`PhysicsPuzzleTests.cs`):
  - `PushableBox_InitializesPhysicsPropertiesCorrectly`: valida massa, amortecimento, rotação congelada e detecção contínua.
  - `PushableBox_SetMassAndDamping_UpdatesRigidbody`: valida ajuste de parâmetros dinâmicos.
  - `PressureButton_StartsUnpressed`: valida estado inicial desarmado.
  - `PressureButton_IdentifiesBoxAsValidActivator`: valida reconhecimento de caixas.
  - `PressureButton_IdentifiesPlayerAsValidActivator`: valida reconhecimento de jogadores.
  - `PressureButton_RejectsUnrelatedCollider`: valida rejeição de colisores não pertinentes.
  - `PressureButton_EmitsEventsOnStateChange`: valida disparo de eventos de ativação e liberação.
- **PlayMode** (`PhysicsInteractionPlayModeTests.cs`):
  - `Box_FallsAndLandsOnGround_WithoutTunneling`: valida queda e repouso estável sobre o chão.
  - `Player_CanPushBoxHorizontally`: valida empurrão horizontal responsivo pelo jogador.
  - `Box_TriggersPressureButton_WhenPlacedOnIt`: valida acionamento pelo peso da caixa.
  - `Player_TriggersPressureButton_WhenSteppingOnIt`: valida acionamento pelo peso do jogador.
