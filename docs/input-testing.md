# Teste de input local para dois jogadores

Abra `Assets/_Game/Scenes/InputTest.unity` e entre em Play Mode. A cena mostra
dois quadrados, contadores de ações e o valor horizontal de cada jogador.

## P1

- [ ] `A` desloca somente o quadrado azul para a esquerda.
- [ ] `D` desloca somente o quadrado azul para a direita.
- [ ] `W` e `Space` incrementam somente o contador de Jump do P1.
- [ ] `E` incrementa somente o contador de Interact do P1.

## P2

- [ ] `Left Arrow` desloca somente o quadrado rosa para a esquerda.
- [ ] `Right Arrow` desloca somente o quadrado rosa para a direita.
- [ ] `Up Arrow` incrementa somente o contador de Jump do P2.
- [ ] `Right Ctrl` incrementa somente o contador de Interact do P2.

## Isolamento e pause

- [ ] Nenhuma tecla de P1 muda o painel ou o quadrado do P2.
- [ ] Nenhuma tecla de P2 muda o painel ou o quadrado do P1.
- [ ] `Esc` incrementa o contador de Pause no painel central.

## Troubleshooting

Se nenhum painel responder, confirme que `Edit > Project Settings > Player >
Active Input Handling` está em **Input System Package (New)** e que o package
`com.unity.inputsystem` está instalado. Feche o Editor e remova somente
`Library/` para forçar a reimportação dos packages se o asset de actions não
aparecer no Inspector.
