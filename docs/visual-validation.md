# Validação da trilha visual

## Resultado

Unity **6000.3.23f1**, worktree `Healthy_Race_Visual`, base `656fbab`.
Execução final em **20/09/2026, 15:57–15:58 UTC** (12:57–12:58 em São Paulo).

| Verificação | Resultado |
| --- | --- |
| Geração dos assets pelo Editor | Exit code 0 |
| Suite EditMode completa | 80 passaram, 0 falharam, 0 ignorados |
| Suite PlayMode completa | 21 passaram, 0 falharam, 0 ignorados |
| Capturas da câmera/Canvas | 7 telas em 1920x1080 e 1280x720 |
| CSharpier | 56 arquivos verificados |
| `.meta` e GUIDs | Sem metas ausentes ou GUIDs duplicados em Assets |

Foram executados todos os testes existentes, além dos novos testes visuais.
Os XMLs e logs completos permanecem localmente em `Logs/final-EditMode.xml`,
`Logs/final-PlayMode.xml` e `Logs/final-build.log`; não são assets do jogo.

## Cobertura adicionada

- Mapeamento de valores e fronteiras de Bem-Estar para aparência.
- Formatação de relógio, valores indisponíveis e janela final.
- Resultados usam o score fornecido, incluindo valores negativos e não conclusão.
- Catálogo com 18 sprites distintos de alimentos e cinco formas do personagem.
- Texturas dos sprites de UI/VFX sobrevivem à serialização do Editor.
- Animator com 12 clips e ausência de Animation Events de gameplay.
- Galeria sem Missing Script, Rigidbody2D ou Collider2D.
- Troca de telas e emissão de intenção sem acionar regras funcionais.
- Inscrição, desinscrição, reativação e troca da origem dos eventos.
- Isolamento dos estados visuais entre os jogadores.
- PlayMode: cena carregada, Animator executando Move, coleta/reset visual,
  navegação de menu, redução de flashes e encerramento dos VFX.

## Revisão visual

As imagens foram renderizadas pela Unity; não são mockups nem capturas de um
protótipo web. A captura usa câmera com RenderTexture e Canvas em ScreenSpaceCamera
para exportação. O prefab do jogo permanece ScreenSpaceOverlay.

Foram corrigidos durante a revisão: escala excessiva de TextMesh, ordenação da
captura de UI, contraste dos estados de puzzle, referência de textura dos badges
e tamanho dos avisos de perigo. A verificação de textura virou uma asserção.

### Galeria / HUD

![Galeria e HUD em 1920x1080](evidence/visual/Gameplay-1920.png)

![Galeria e HUD em 1280x720](evidence/visual/Gameplay-1280.png)

### Telas em 1280x720

![Menu](evidence/visual/MainMenu-1280.png)

![Resultados](evidence/visual/Results-1280.png)

![Pausa](evidence/visual/Pause-1280.png)

![Controles](evidence/visual/Controls-1280.png)

![Áudio e acessibilidade](evidence/visual/Audio-1280.png)

![Boot](evidence/visual/Boot-1280.png)

Os equivalentes em 1920x1080 também estão em `docs/evidence/visual`.
Os botões de inspeção da galeria são criados em PlayMode, por isso não aparecem
nas capturas estáticas feitas no Editor. A navegação de menu e o comportamento
visual foram exercitados em PlayMode.

## Limites da validação

Esta validação cobre os componentes independentes e a galeria. Ainda é necessário
validar o fluxo de partida após a composição com as features do agente de gameplay.
Os pontos de integração estão em [visual-mvp.md](visual-mvp.md).

Duas alterações automáticas do Editor em GraphicsSettings e QualitySettings
foram inspecionadas e descartadas. Nenhum ProjectSettings foi incluído na entrega.
O único ajuste compartilhado necessário é habilitar o módulo nativo Animation
no manifest e no lockfile de Packages.
