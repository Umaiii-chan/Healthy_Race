# Sistema de puzzles e interações (PUZZ-01)

## Componentes reutilizados

A Fase 1 usa os sistemas existentes `PushableBox`, `PressureButton`, `Lever`, `PuzzleDoor`, `ActivatedPlatform` e `CooperativePuzzleController`. A caixa mantém reset automático ao cair; cada área com caixa também possui uma alavanca de recuperação.

## Z2 — ensino espacial

1. A caixa nasce no pátio inferior.
2. A placa fica depois da caixa e aciona um elevador junto à parede do jardim.
3. Alimentos visíveis no jardim superior comunicam o objetivo sem uma caixa de tutorial.
4. A alavanca **Recuperar caixa** restaura a tentativa.

A sequência usa `requireAll: false` porque existe uma única entrada, a placa.

## Z5 — clímax cooperativo

1. Um jogador empurra `Z5_PushableBox` até `Z5_PressureButton`.
2. A placa aciona `Z5_CooperationLift`.
3. O elevador dá acesso a `Z5_UpperLever`.
4. O controlador final usa `requireAll: true`: o portão abre somente enquanto placa e alavanca estão ativas.
5. A caixa sustenta a placa e permite que os dois atravessem.

As antigas plataformas `Z5_Step1`, `Z5_Step2` e `Z5_Step3`, que anulavam o puzzle, foram removidas. Dois degraus de recuperação existem apenas no lado de entrada e não cruzam o portão. A moldura arquitetônica deixa passagem superior a 1,5 m em cada lado do portão móvel.

## Anti-deadlock

- caixa abaixo do limite: `PushableBox.ResetToSpawn()`;
- caixa presa: `Z5_ResetSwitch` chama `ResetPuzzle()`;
- alavanca em estado incorreto: o mesmo reset redefine a alavanca;
- jogador que cai do poleiro: degraus e elevador permitem nova subida;
- placa solta: o elevador retorna e o portão fecha de acordo com o estado atual.

## Validação

Os testes verificam os estados **só placa**, **só alavanca** e **ambos**, a ausência do bypass, a presença do reset e a largura física da passagem. Os testes do sistema continuam cobrindo eventos, movimento da porta/plataforma e reset da caixa.
