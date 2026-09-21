# Teste de Câmera Compartilhada (CAM-01)

## Descrição

O `SharedCameraController` é responsável pelo enquadramento simultâneo e suave de P1 e P2 em uma câmera ortográfica única 2D. O sistema é completamente desacoplado da mecânica de locomoção dos personagens (`PlayerMovement`), consumindo apenas suas posições no espaço do mundo.

## Parâmetros Configuráveis

No Inspector do componente `SharedCameraController`:

- **Targets**: `_firstTarget` (P1) e `_secondTarget` (P2);
- **Smoothing**:
  - `_positionSmoothTime` (default `0.18s`): amortecimento suave do movimento da câmera;
  - `_zoomSmoothTime` (default `0.15s`): amortecimento suave da transição de zoom;
- **Zoom & Padding**:
  - `_minimumOrthographicSize` (default `5.0`): zoom máximo permitido (jogadores muito próximos);
  - `_maximumOrthographicSize` (default `9.0`): zoom mínimo permitido (jogadores afastados);
  - `_padding` (default `(2.0, 1.5)`): margem de segurança nas bordas horizontais e verticais;
- **Playable Area Bounds**:
  - `_usePlayableBounds`: ativa restrição de movimento da câmera à área jogável;
  - `_playableBounds`: limites de enquadramento que a visão da câmera não pode ultrapassar.

## Modo de Enquadramento Fixo (arena de tela única)

`SetFixedFraming(position, orthographicSize, minimumHalfWidth)` trava a câmera em uma
posição e tamanho fixos e faz *bypass* de `CalculateDesiredPosition()` e
`CalculateDesiredOrthographicSize()`. É o modo usado pela Fase 1: câmera em
$(16, 9, -10)$ com `orthographicSize = 9`, e `minimumHalfWidth = 16` garante que a
arena de 32 unidades de largura continue visível em proporções mais estreitas que
16:9. `ClearFixedFraming()` volta ao modo dinâmico.

## Roteiro de Validação Manual

1. Inicie a cena no Unity Editor e mova ambos os jogadores juntos:
   - A câmera deve acompanhar o ponto médio entre eles de forma suave e contínua.
2. Afaste os jogadores horizontalmente (um para cada lado):
   - A câmera deve aumentar gradualmente o tamanho ortográfico (zoom out) mantendo ambos na tela.
   - Ao atingir a distância limite, o zoom deve parar em `_maximumOrthographicSize`.
3. Pule ou suba em plataformas com um dos jogadores:
   - A câmera deve acomodar a distância vertical sem solavancos.
4. Conduza os personagens até as extremidades do cenário:
   - A câmera deve parar suavemente ao encostar nos limites da área jogável configurada.

## Testes Automatizados

Executados via Unity Test Runner:

- **EditMode**:
  - `DesiredPosition_TracksMidpointOfTargets`: valida cálculo preciso do ponto médio;
  - `DesiredSize_IncreasesWhenTargetsSeparate`: valida expansão do zoom ao afastar alvos;
  - `DesiredSize_ClampedToMinAndMaxLimits`: valida respeito estrito a min/max zoom;
  - `DesiredPosition_ClampedToPlayableBounds`: valida confinamento aos limites de cenário;
  - `VerticalSeparation_ExpandsOrthographicSize`: valida compensação de distância vertical;
  - `SharedCamera_FixedFraming_IgnoresTargetsAndKeepsSize` (em `Phase1BlockoutTests`): valida o modo fixo.
- **PlayMode**:
  - `Camera_KeepsBothPlayersWithinViewport_WhenSeparating`: confirma que ambos os jogadores permanecem com coordenadas de viewport entre 0 e 1;
  - `Camera_FollowsBothPlayersMovingTogether`: confirma acompanhamento dinâmico do movimento;
  - `Camera_StopsAtPlayableBounds`: confirma interrupção da câmera nas bordas da área jogável;
  - `Phase1_CameraStaysFixed_WhilePlayersMoveAcrossTheArena` (em `Phase1PlayModeTests`): confirma posição e `orthographicSize` imóveis na arena.
