# Trilha visual do MVP

## Referência e isolamento

Fonte: [Healthy Race - Pacote de Pré-Produção v1.1](https://docs.google.com/document/d/1hkBoG3NNELzWKCnqntCyk7kbHcxEVnVo_on70rFZbnc/edit), seções 3.1, 4, 5 e 8, figura 4 / A4.
Consulta em 14/09/2026 (horário de São Paulo). A prancha foi conferida também na cópia PDF local, página 14.

Branch `feature/visual-complete-mvp`, criada de `origin/develop` em `656fbab`.
Worktree separado: `C:/dev/GitHub_Pessoal/Healthy_Race_Visual`.
O checkout de alimentos não foi trocado, mesclado nem modificado.

## Auditoria antes da implementação

| Classificação | Arquivos / evidência | Decisão |
| --- | --- | --- |
| Pode alterar | Novas pastas `Visual`, `Visuals` e `VisualAtlas` | Assets e código exclusivos desta trilha |
| Evitar alterar | `PlayerMovement`, `PlayerController`, `WellbeingComponent`, `FatigueComponent`, `FoodCollectible`, `PressureButton`, `PlayerHUD` | Nenhuma alteração funcional |
| Evitar alterar | `PF_Player`, `PF_FoodCollectible`, `PF_PushableBox`, cenas existentes, ProjectSettings | Preservados; referências foram reparadas no commit `656fbab` |
| Precisa integração | Bem-Estar e fadiga | Eventos públicos existentes; Bind explícito e simétrico |
| Precisa integração | Locomoção | `PlayerController.CurrentState` não tem evento; leitura em LateUpdate, após o controlador |
| Precisa integração | Coleta | Base `656fbab` desativa o objeto sem evento; API visual `Pickup()` pronta |
| Precisa integração | Botão | `OnPressedStateChanged` e `IsPressed` já disponíveis |
| Criar novo | HUD, telas, atlas, material, animações, prefabs, VFX, VisualTest | Construídos usando a serialização do Editor |

As pastas Art e Animations não tinham arte/clips de produção. O controlador visual
existente cria quadrados/círculos em runtime. O HUD existente também é de protótipo.
Não havia Animator Controller, fonte própria, shaders próprios ou partículas de produção.
Foi mantida a fonte embutida `LegacyRuntime.ttf`. Usa-se material URP 2D unlit.
Sorting order da trilha: fundo -30; ambiente 0; objetos 10; alimentos 12;
personagens 20; identificadores 24/25; rótulos 30; VFX 35.
Nenhuma sorting layer ou layer foi adicionada.

Commits recentes auditados: correção de serialização (`656fbab`), fadiga (`c028294`),
Bem-Estar (`9b5df1f`), dados (`92a1123`) e física (`916fcdc`).

## Conteúdo

- Atlas RGBA com 36 sprites: cinco formas do personagem, 18 alimentos e 13 peças.
- Fundo em camadas visuais suaves, árvore, arbusto, chão, plataforma, parede,
  caixa, botão, alavanca, porta, checkpoint, chegada, espinhos e lama.
- Prefabs derivados para plataforma acionada e plataforma instável.
- Identificação persistente P1 azul/círculo e P2 violeta/losango.
- 12 Animation Clips e Animator Controller com parâmetro inteiro `Action`.
- Estados: Idle, Move, Jump, Fall, Land, Push, Interact, Eat, Fatigue, Recover,
  Victory, Defeat. Animação por deformação/pose do sprite; nenhuma Animation Event.
- HUD individual: Bem-Estar, forma, score, dois contadores de alimentos, fadigas,
  progresso. HUD compartilhado: cronômetro, progresso da fase e janela final.
- Boot, MainMenu, Gameplay, Pause, Results, Controls e Audio.
- 11 presets de VFX; estrelas, corações, gotas e anéis. Máximo 10 partículas
  por emissão, duração de 0,55 a 0,85 s, destruídos ao terminar.
- Redução de flashes diminui contagem, opacidade e alcance; desativa tremor.

Os números mostrados na galeria são amostras explícitas. Não há cálculo de score,
cronômetro de partida, regras de coleta ou física nos prefabs visuais.
Áudio nesta entrega é a apresentação dos controles e emissão de intenções de volume;
o serviço/mixer e os arquivos de música/SFX pertencem à integração de áudio.

## Como validar

1. Abrir este worktree em Unity 6000.3.23f1.
2. Abrir `Assets/_Game/Scenes/Test/VisualTest.unity` e entrar em Play Mode.
3. Conferir P1 e P2 nas cinco faixas, os 18 alimentos e as seis zonas.
4. Usar a barra de ferramentas da galeria: menu, galeria, pausa, resultados,
   ciclo das 12 animações, coleta/reset, quatro estados de puzzle e 11 VFX.
5. Em Áudio, alternar `Reduzir flashes e tremores`; repetir VFX/perigo.
6. Conferir Game View em 1920x1080 e 1280x720.
7. Executar EditMode e PlayMode pelo Test Runner.

`Healthy Race > Visuals > Build owned visual assets` recria somente assets desta
trilha e a galeria. Ele preserva GUIDs de slices existentes. Não executar para
preservar edições manuais posteriores nos prefabs de saída: esses prefabs são
reconstruídos. O menu pede para salvar cenas sujas antes de abrir a galeria.
Não é necessário executar o gerador para usar os assets já versionados.

## Contratos de integração

```csharp
// Instanciar PF_VIS_Player_P1/P2 como filho do jogador funcional.
presentation.Bind(wellbeing, fatigue, playerController);
hud.Render(snapshot);
hud.Bind(wellbeing, fatigue);

// Chamadas da camada de integração quando o gameplay publicar o evento:
foodPresentation.Pickup();
presentation.Play(VisualAction.Eat, 0.2f);
puzzlePresentation.Bind(pressureButton);
puzzlePresentation.Present(PuzzleAppearance.Active);
hazardPresentation.PresentWarning(normalizedAnticipation);
screens.RenderRace(elapsedSeconds, progress, finalWindowRemaining);
screens.RenderResults(winnerLabel, p1Result, p2Result);
screens.Show(VisualScreen.Results);
```

`CommandRequested`, `AudioRequested` e `ReduceFlashesRequested` expressam intenção.
Um adaptador do fluxo funcional deve inscrever-se e aplicar comandos válidos.
O componente não carrega cenas, fecha o jogo, modifica Time.timeScale ou decide o vencedor.
`VisualTestDriver` apenas navega pela apresentação e fornece snapshots de exemplo.

Não manter o `PlayerVisualController` de placeholders ativo junto com a nova
apresentação. Na futura composição do prefab funcional, substituir sua apresentação
e ocultar somente o renderer antigo; preservar colliders e transform do jogador.
`ResetPresentation()` prepara vitória/derrota para reutilização.

### Pendências de integração com gameplay

| Arquivo / ponto | Alteração necessária | Motivo / impacto | Alternativa |
| --- | --- | --- | --- |
| `FoodCollectible` na base auditada | Publicar coleta confirmada antes de desativar o objeto; conectar `Pickup()` | O burst nasce fora da hierarquia do alimento e sobrevive à desativação | Consumir evento equivalente da futura FOOD-01 em um adaptador novo |
| Prefab funcional do jogador | Adicionar filho visual, fornecer Bind e retirar apresentação antiga | Evita renderização duplicada; nenhum collider muda | Instanciar filho via bootstrap da integração |
| `PlayerController.CurrentState` | Nenhuma agora; opcional publicar StateChanged futuramente | Atualmente a leitura é feita em LateUpdate, sem escrever estado | Manter leitura do estado público |
| Push, Interact, Eat, chegada e resultados | Encaminhar eventos confirmados para `Play()` e `RenderResults()` | Preserva prioridade funcional de ações | Adaptador específico da feature pronta |
| Hazards | Chamar `PresentWarning` durante os 0,5 s de antecipação definidos no gameplay | Apresentação não agenda queda ou respawn | Conectar evento de progresso da futura HAZ-01 |
| Áudio / acessibilidade | Conectar intenções de volume e reduzir efeitos nos emissores instanciados | O menu não altera serviços globais | Aplicar preferências no bootstrap |

### Alteração compartilhada indispensável

`Packages/manifest.json`: adição de `com.unity.modules.animation: 1.0.0`.
O Editor gerou a entrada correspondente em `Packages/packages-lock.json`.
Motivo: o projeto não referenciava o módulo e Animator/AnimationClip não compilavam.
Impacto: habilita módulo nativo da mesma versão de Unity, sem pacote externo.
Alternativa: animação manual por Update, que não entrega o Animator solicitado.
Nenhum arquivo funcional existente foi alterado.

## Riscos e limites

Não houve integração em Level01 ou nos prefabs funcionais compartilhados.
Os sprites são uma adaptação nova da Art Bible, não recortes da prancha.
O atlas foi importado por API de Sprite Editor, sem editar manualmente fileIDs.
Referência técnica: [Unity Sprite Editor Data Provider API](https://docs.unity.cn/Manual/Sprite-data-provider-api.html).
O risco de merge concentra-se nas duas entradas de Packages; comparar as adições,
preservando as dependências da outra frente. Não escolher ours/theirs em YAML Unity.
Não realizar merge automático desta branch.

## Evidências e resultados de testes

As capturas são renderizações reais da câmera e Canvas na Unity, obtidas por
`HealthyRace.Editor.Visuals.VisualEvidence.Capture`. Os resultados finais e o
resumo de testes ficam registrados na descrição do PR e em `visual-validation.md`.
