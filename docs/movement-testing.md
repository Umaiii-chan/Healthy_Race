# Teste de movimentação local

## Cena de teste

Abra `Assets/_Game/Scenes/MovementTest.unity` no Unity 6000.3.23f1 e entre em
Play Mode. A cena contém um chão, três plataformas e duas instâncias
independentes de `PF_Player`:

- `Player1`, no ponto inicial esquerdo;
- `Player2`, no ponto inicial direito.

Os painéis de debug no topo da Game View mostram a identidade, o estado da
FSM, o valor de grounded e a velocidade de cada jogador. Eles não escrevem no
Console e não alteram a física.

## Controles

| Jogador | Mover | Pular |
| --- | --- | --- |
| P1 | `A` / `D` | `W` ou `Space` |
| P2 | `Left Arrow` / `Right Arrow` | `Up Arrow` |

## Parâmetros configuráveis

No prefab `Assets/_Game/Prefabs/Characters/PF_Player.prefab`:

- `PlayerMovement`: `_moveSpeed` (7) e `_jumpVelocity` (12);
- `Rigidbody2D`: corpo Dynamic, gravidade 2 e rotação Z congelada;
- `PlayerGroundCheck`: layer mask `Ground`, distância de checagem 0,08,
  largura relativa 0,82 e altura 0,04.

Os valores acima são defaults do prefab e podem ser ajustados no Inspector por
personagem. O estado de jogo não é compartilhado entre as instâncias.

## Roteiro de validação manual

1. Mantenha `A` ou `D` e confirme que apenas P1 se desloca. Repita com as setas
   horizontais para confirmar que apenas P2 se desloca.
2. Acione os controles dos dois jogadores ao mesmo tempo, em direções opostas.
   Ambos devem continuar respondendo sem travar ou mover o outro personagem.
3. Com um jogador sobre o chão ou uma plataforma, pule e observe a sequência
   `Jump`, `Fall`, `Land` e então `Idle` ou `Move` no painel de debug.
4. Enquanto o jogador estiver no ar, pressione seu botão de pulo novamente.
   Não deve haver um segundo impulso vertical.
5. Caminhe pelas plataformas e deixe o jogador cair. `Grounded` deve ser falso
   durante a queda e verdadeiro apenas ao aterrissar sobre objetos na layer
   `Ground`.

Resultado esperado: P1 e P2 percorrem a sala e usam as plataformas de forma
independente; a rotação do sprite/collider não ocorre; o Console não apresenta
erros nem exceções relacionados à cena.

## Testes automatizados

No Unity Test Runner, execute EditMode e PlayMode. A cobertura inclui:

- transições puras da `PlayerStateMachine`;
- configuração da `MovementTest` e ausência de Missing Scripts;
- queda e reconhecimento de chão;
- pulo somente em grounded, sem salto adicional no ar;
- movimento independente de duas instâncias;
- isolamento das bindings P1/P2 pelo `PlayerInputReader`.

## Troubleshooting

- Se `Grounded` nunca ficar verdadeiro, confira se o collider da plataforma
  está na layer `Ground` e se a `PlayerGroundCheck` mantém essa layer na máscara.
- Se os dois jogadores responderem ao mesmo controle, valide os mapas
  `Player1` e `Player2` em
  `Assets/_Game/Input/HealthyRaceInputActions.inputactions` e a identidade no
  `PlayerInputReader` de cada instância.
- Se o personagem não se mover, confirme que o `Rigidbody2D` está simulado e
  que `PlayerMovement`, `PlayerGroundCheck` e `PlayerController` estão ativos.
- Se o projeto abrir em Safe Mode, use a versão Unity indicada acima e confira
  o primeiro erro real do Console antes de modificar assets ou arquivos `.meta`.
