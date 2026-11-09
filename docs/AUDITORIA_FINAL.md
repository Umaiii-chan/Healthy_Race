# Auditoria Final — Healthy Race

> **Data:** 09/11/2026 · **Commit auditado:** `e683d76` (`develop`) · **Editor:** Unity 6000.3.23f1
> **Método:** leitura do código, cenas, prefabs, ScriptableObjects e ProjectSettings + execução
> local das suítes EditMode e PlayMode em batchmode. **Nada foi alterado no projeto.**

Legenda de estado: `NOT_STARTED` · `IN_PROGRESS` · `IMPLEMENTED` (existe, sem validação
suficiente) · `NEEDS_FIX` (defeito comprovado) · `BLOCKED` · `VERIFIED` (validado com evidência
executada nesta auditoria).

---

## 1. Estado geral

O projeto está **mais adiantado do que um MVP típico na camada de regras** e **menos pronto do que
o `docs/qa-report.md` afirma na camada de produto**.

- As regras de jogo (bem-estar, fadiga, alimentos, pontuação, puzzles, chegada) são classes
  pequenas, testáveis e cobertas por testes que **passam de verdade** (ver §6).
- A Fase 1 é jogável no Editor, mas o **executável não é um jogo distribuível**: não há menu,
  pausa, reinício funcional nem forma de sair; o áudio real não entra na build; a partida pode
  não terminar nunca.
- O `qa-report.md` descreve uma versão anterior da fase (6 zonas, X 0→215, janela de 20 s com DNF).
  O commit `e683d76` trocou a fase por uma **arena cooperativa de tela única** com portas por
  jogador e **sem janela final nem DNF**. A documentação não acompanhou.
- **A CI de testes nunca rodou o Unity.** O job "Unity Tests" termina em ~13 s porque o segredo
  `UNITY_LICENSE` não está configurado e o passo é pulado (`.github/workflows/unity-tests.yml`,
  passo *Detect Unity project and credentials*). O check verde no GitHub não é evidência de teste.

## 2. Arquitetura identificada

```text
Phase1_CorridaDoEquilibrio.unity  ── 1 GameObject com Phase1Bootstrap (2 233 linhas)
   └─ Phase1Bootstrap.Awake() constrói TUDO em código:
        arena (5 patamares), 3 portais cooperativos, perigos, 18 alimentos,
        2 jogadores (PF_Player), câmera fixa, HUD (Canvas), MatchCompletionController

PF_Player = PlayerController + PlayerInputReader + PlayerMovement + PlayerGroundCheck
          + WellbeingComponent (5 faixas SO) + FatigueComponent + Rigidbody2D
```

| Camada | Arquivos | Observação |
| --- | --- | --- |
| Input | `Scripts/Input/*`, `Input/HealthyRaceInputActions.inputactions` | Mapas `Player1`, `Player2`, `UI` (Pause). Cada jogador instancia a própria cópia do asset — isolamento correto. Só teclado. |
| Jogador | `Scripts/Player/*` | Movimento por velocidade direta; chão por BoxCast na layer 6; `JumpBudget` deriva o ápice da física real. |
| Bem-estar / Fadiga | `Scripts/WellBeing/*` | Valor 0–100 com clamp; 5 faixas em SO com multiplicadores; fadiga = episódio de 2 s ao cruzar 20↓, +15 ao fim, rearme > 25. Máquina de estados pura, separada do MonoBehaviour. |
| Alimentos | `Scripts/Collectibles/*`, `ScriptableObjects/Food/*` (18) | 10 saudáveis (+8 a +16, 80–160 pts) e 8 ultraprocessados (−10 a −18, 0 pts). |
| Puzzles / Obstáculos | `Scripts/Puzzles/*`, `Scripts/Obstacles/*` | Botão, alavanca, caixa, porta, plataforma acionada, espinhos, lama, plataforma que cai, checkpoint. |
| Partida / Pontuação | `Scripts/Level/MatchCompletionController.cs`, `Scripts/Scoring/*` | `ScoreService` é estático e puro; desempate pontos → bem-estar → tempo → ordem. |
| UI | `Scripts/UI/*` | HUD por jogador, HUD da partida, painel de resultados — tudo criado em código. |
| Áudio | `Scripts/Audio/*` | Singleton com fallback sintético. |
| Visual | `Scripts/Visuals/*`, `Editor/Visuals/VisualAssetBuilder.cs` | Apresentação desacoplada; menu/pausa/resultados "bonitos" existem **só** na cena `Test/VisualTest`. |

**Cenas:** `Phase1_CorridaDoEquilibrio` (jogo), `VerticalSlice_Core` (slice antigo), `InputTest`,
`MovementTest`, `Test/VisualTest`. A *Build Settings* inclui `Phase1` e `VerticalSlice_Core`, mas
nenhum código carrega cena (`SceneManager` não aparece em `Scripts/`), então o slice entra na
build sem ser alcançável.

**Física 2D:** gravidade global sobrescrita para −18 pela fase; `gravityScale` do jogador derivado
do ápice-alvo de 2,0 m. **Player settings:** `bundleVersion: 0.1.0`, janela 1920×1080, modo
janela, backend de script não definido (Mono por padrão).

## 3. Matriz de funcionalidades

| Funcionalidade | Estado | Evidência | Ação |
| --- | --- | --- | --- |
| Compilação | `VERIFIED` | Batchmode compilou sem `error CS`; 135 testes EditMode executaram. | — |
| Movimentação dos jogadores | `IMPLEMENTED` | `PlayerMovement.cs`; testes PlayMode de movimento (ver §6). | Teste manual com 2 pessoas. |
| Multiplayer local (2 no teclado) | `IMPLEMENTED` | Mapas separados + cópia do asset por jogador (`PlayerInputReader.cs`); teste `PlayerInputReaderIsolationTests`. | Validar *ghosting* de teclado com teclas simultâneas; avaliar suporte a gamepad. |
| Física e colisões | `IMPLEMENTED` | `JumpBudget` + testes de geometria em `Phase1BlockoutTests`. | Ver risco R1 (bem-estar baixo). |
| Sistema de alimentação | `VERIFIED` (regras) | 18 SO com valores coerentes; testes `ConfigurableDataTests`, `FoodCollectiblePlayModeTests`. | Feedback educativo (nome/efeito do alimento) — ver B-12. |
| Sistema de bem-estar | `VERIFIED` (regras) | Clamp 0–100, faixas contíguas 0/1–25/26–50/51–75/76–100. | — |
| Sistema de fadiga | `NEEDS_FIX` | Regras corretas e testadas, mas a recuperação **reativa o movimento depois do fim da partida** (`FatigueComponent.cs:131`, `:167`). | B-06 |
| Puzzles cooperativos | `IMPLEMENTED` | G1/G2/G3 com saídas anti-deadlock declaradas; `PuzzlePlayModeTests`. | Teste manual completo da arena. |
| Obstáculos | `IMPLEMENTED` | Espinhos/lama/plataforma que cai + testes. | — |
| Pontuação | `IMPLEMENTED` | `ScoreService` testado. Crédito de puzzle é igual para os dois jogadores (`MatchCompletionController.cs:432/447`); `ScoreConfig.CreateDefault()` cria um SO novo a cada cálculo (`ScoreService.cs:19`). | B-10, B-11 |
| HUD | `IMPLEMENTED` | Barras, contadores, timer, progresso por marcos. | Revisão visual na build. |
| Menus (principal, pausa, configurações, créditos, sair) | `NOT_STARTED` no jogo | Existem apenas como maquete na cena `VisualTest` (`VisualTestDriver`). A ação `UI/Pause` (Esc) só alimenta `PauseInputDebug`. | B-01, B-03 |
| Reiniciar / voltar ao menu | `NEEDS_FIX` | Botão "Jogar de novo" dispara `ReplayRequested` (`MatchResultsPanel.cs:169`), que **ninguém assina**; a cena também **não tem EventSystem**, então o botão nem recebe clique. | B-02 |
| Progressão das fases | `NOT_STARTED` | Uma fase só. Escopo de 1.0 precisa ser decidido (ver §8). | Decisão |
| Vitória / derrota / empate | `IMPLEMENTED` | Resultado compilado e painel exibido; empate tratado. | — |
| Fim de partida garantido | `NEEDS_FIX` | No modo "portas duplas" a partida só termina com **os dois** nas portas ao mesmo tempo, sem limite de tempo (`MatchCompletionController.cs:268`). Jogador que não consegue subir trava a partida para sempre. | B-04 |
| Áudio | `NEEDS_FIX` | Os `.wav` reais só carregam no Editor (`AudioService.cs:271`, `#if UNITY_EDITOR` + `AssetDatabase`). Na build todo som vira o bipe sintético. `Music_Menu`, `Sfx_Defeat`, `Sfx_Step` nunca são tocados. Alimento com `CollectSound` tocaria em dobro (`FoodCollectible.cs:70` + `HandleFoodCollected`). | B-05 |
| Configurações (volume) | `IMPLEMENTED` sem UI | `AudioService` persiste volume em `PlayerPrefs`, mas não há tela para alterá-lo. | B-14 |
| Build Windows | `IMPLEMENTED` | `BuildScript.BuildStandaloneWindows64` em batchmode: **Succeeded**, 102 s, 106 MB, Mono x64. O relatório conta 6 erros — são do import de ShaderGraph do URP ("Host type is not matching any asset type"), não do jogo. O `.exe` iniciou em batchmode por 25 s sem exceção no log. **Não foi jogado com tela.** A pasta `*_BurstDebugInformation_DoNotShip` sai junto e não deve ir no pacote. | B-07 |
| CI | `NEEDS_FIX` | Testes Unity pulados por falta de licença; build só em tag. | B-17 |

## 4. Problemas encontrados

### Bugs reproduzíveis (pela leitura do código; confirmação em jogo pendente)

1. **Botão de replay morto** — `MatchResultsPanel.ReplayRequested` sem assinante e cena sem
   `EventSystem`. Após a primeira partida o executável fica parado no painel de resultados; só
   Alt+F4 sai.
2. **Áudio da build é sintético** — `AudioService.LoadClip` só lê `AssetDatabase` dentro de
   `#if UNITY_EDITOR`; fora do Editor devolve `null` e cai em `CreateSyntheticFallback`.
3. **Jogador "descongela" depois do resultado** — `ConcludeMatch` congela os dois; se um deles
   estava em fadiga, a coroutine termina 2 s depois, chama `SetMovementEnabled(true)` e ainda
   aplica +15 de bem-estar, divergindo do valor mostrado no painel.
4. **Partida sem fim garantido** — ver B-04 e risco R1.

### Inconsistências entre implementação e documentação

- `docs/qa-report.md`: 6 zonas X 0→215, janela final de 20 s, DNF com 20 % de penalidade,
  "acúmulo de fadiga por corrida e recuperação ao parar", "178 testes, 100 % aprovados",
  executável em `Build/Windows/`. Nada disso descreve o `develop` atual (arena 32×18, portas
  duplas sem DNF, fadiga por limiar de bem-estar, 135 + 54 testes, `Build/` ignorado
  pelo git).
- `README.md` cita "Windows Build Support (IL2CPP)"; o projeto não define IL2CPP e a máquina de
  auditoria só tem o módulo Mono instalado.
- `docs/` cita GDD/TDD/LDD que não estão no repositório.

### Dívida técnica relevante (não bloqueia, mas encarece correções)

- `Phase1Bootstrap.cs` tem 2 233 linhas e constrói a fase, HUD, câmera, áudio e partida. Toda
  mudança de UI/fluxo passa por ele.
- Estado de jogo na UI: a pontuação de alimentos vive em `PlayerHUD.Score` e é lida de lá pelo
  `MatchCompletionController`.
- Eventos estáticos (`Checkpoint.AnyCheckpointReached`, `FinishLine.AnyPlayerFinished`) e
  registro estático de checkpoints — hoje limpo em `OnDestroy`, mas é ponto de vazamento quando
  houver recarga de cena.
- `Phase1Bootstrap.Awake` carrega catálogo visual por `AssetDatabase` se a referência estiver
  vazia; na build isso não existe. Hoje a cena tem as referências serializadas, então funciona,
  mas falha em silêncio se alguém limpar o campo.

## 5. Riscos técnicos

| ID | Risco | Probabilidade | Impacto |
| --- | --- | --- | --- |
| R1 | **Soft-lock por bem-estar baixo.** A geometria é validada só com multiplicadores 1,0 (`Phase1Bootstrap.DesignBudget`). O ápice cai com o quadrado do multiplicador: faixa Cansado (×0,95) → ~1,80 m; Crítico (×0,9) → ~1,62 m; Salo (×0,8) → ~1,28 m. A rota G3 declara desnível de 1,78 m. O piso T0 tem os 8 ultraprocessados em sequência (−115 no total): quem atravessa o piso pegando tudo chega a 0. Somado a B-04, a partida pode nunca terminar. | Alta | Bloqueia |
| R2 | Ghosting de teclado: A/D/W/Espaço + setas simultâneas podem não registrar em teclados baratos. | Média | Alto |
| R3 | CI sem Unity: regressões passam com check verde. | Alta | Médio |
| R4 | `Phase1Bootstrap` monolítico: correções de UI e fluxo colidem no mesmo arquivo. | Alta | Médio |
| R5 | IL2CPP exigido pelo plano mas não instalado; build Mono é suficiente para 1.0 Windows. | — | Decisão |
| R6 | Abrir o projeto no Unity 6000.3.23f1 reescreve 4 arquivos versionados (`DefaultVolumeProfile.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `HealthyRace_2D_RenderPipeline.asset`, `ProjectSettings.asset` — upgrade de assets do URP). Cada contribuidor gera diff espúrio; o `qa-report.md` resolve revertendo à mão. | Certa | Baixo |

## 6. Testes executados nesta auditoria

| Suíte | Total | Passou | Falhou | Duração | Arquivo |
| --- | --- | --- | --- | --- | --- |
| EditMode | 135 | 135 | 0 | 1,7 s | `editmode.xml` |
| PlayMode | 54 | 54 | 0 | 30,3 s | `playmode.xml` |
| Build Windows x64 | — | Succeeded | — | 102 s | `build.log` |
| Executável (headless, 25 s) | — | Sem exceção | — | — | `player.log` |

Primeira execução local real conhecida (a CI pula estes testes). **Não houve teste manual nem
teste do executável** — itens marcados `IMPLEMENTED` dependem disso para virar `VERIFIED`.

## 7. Backlog priorizado

### P0 — Bloqueadores do 1.0

| ID | Item | Critério de aceite |
| --- | --- | --- |
| B-01 | **Fluxo de cenas mínimo:** cena `MainMenu` (Jogar, Como jogar, Configurações, Créditos, Sair) como cena 0 da build; reaproveitar `RaceScreenView`/`VisualUiData` em vez de criar UI nova. | Executável abre no menu; "Jogar" carrega a Fase 1; "Sair" fecha. |
| B-02 | **Reiniciar e voltar ao menu** no painel de resultados: `EventSystem` + `InputSystemUIInputModule` na cena, assinatura de `ReplayRequested` recarregando a cena, botão "Menu". | Duas partidas seguidas sem fechar o jogo; estado inicial restaurado (bem-estar 100, alimentos de volta, checkpoints limpos). |
| B-03 | **Pausa** com Esc (ação `UI/Pause` já existe): continuar, reiniciar, menu, sair. | Esc congela a partida (`timeScale`) e retoma sem perder estado. |
| B-04 | **Fim de partida garantido** no modo portas duplas: janela final após a primeira chegada (o controlador já tem `FinalCountdown` e DNF para o modo de porta única) ou tempo-limite. Decisão de design — ver §8. | Nenhuma partida dura indefinidamente. |
| B-05 | **Áudio real na build:** referências serializadas (catálogo de áudio em SO) em vez de `AssetDatabase`; manter o sintético só como fallback com aviso no log. | Build toca os `.wav`; teste EditMode garante que todo `AudioClipId` tem clip atribuído. |
| B-06 | Fadiga não reativa movimento após `ConcludeMatch` (cancelar coroutine ou consultar estado da partida). | Teste PlayMode: fadiga em curso + conclusão → jogador continua parado e bem-estar igual ao do painel. |
| B-07 | **Build Windows x64** pelo `BuildScript`, `bundleVersion 1.0.0`, só cenas alcançáveis, testada fora do Editor. | `.exe` abre em máquina sem Unity, joga do menu ao resultado e reinicia. |
| B-08 | **Validar R1 (soft-lock por bem-estar baixo)**: teste de geometria com o pior multiplicador alcançável em cada trecho + partida manual "pegando tudo de ruim". Corrigir geometria, faixas ou fluxo conforme o resultado. | Fase percorrível do spawn às portas em qualquer faixa de bem-estar, ou partida encerra por tempo. |

### P1 — Mecânica principal

| ID | Item |
| --- | --- |
| B-09 | Teste manual estruturado da arena com 2 pessoas (cenários da §6.3 do plano), registrado em `docs/TEST_REPORT.md`. |
| B-10 | Crédito de puzzle por jogador (quem acionou) em vez de igual para os dois — ou documentar que é cooperativo por design. |
| B-11 | `ScoreConfig` como asset (`SO_ScoreConfig`) ligado ao controlador; remover a criação de SO por cálculo. |
| B-12 | Feedback educativo na coleta: nome do alimento e efeito (+/− bem-estar) visível — é o núcleo ODS 3 e hoje é só som + partícula. |

### P2 — Experiência

| ID | Item |
| --- | --- |
| B-13 | Tela "Como jogar" com controles e regras (alimentos, fadiga, portas). |
| B-14 | Tela de configurações ligada ao `AudioService` (volumes já persistem). |
| B-15 | Revisar HUD e resultados em 1280×720 e 1920×1080 na build. |
| B-16 | Suporte a gamepad (opcional; mitiga R2). |

### P3 — Entrega

| ID | Item |
| --- | --- |
| B-17 | CI: licença Unity nos segredos, ou documentar execução local obrigatória antes de merge. |
| B-18 | Remover `VerticalSlice_Core` da build (manter no repositório se ainda servir de referência). |
| B-19 | Documentação: corrigir `qa-report.md`; criar `GAMEPLAY.md`, `CONTROLS.md`, `TEST_REPORT.md`, `RELEASE_NOTES.md`; ajustar README (Mono vs IL2CPP). |
| B-20 | Release `v1.0.0`: tag, zip do executável, notas de versão. |
| B-21 | Versionar uma vez os assets do URP reescritos pelo Editor (R6), para acabar com o diff espúrio. |

## 8. Decisões que precisam do dono do projeto

1. **Escopo do 1.0:** uma fase (a arena atual) ou mais fases? O plano fala em "progressão das
   fases", o repositório tem uma.
2. **Fim de partida (B-04):** janela final de N segundos após a primeira chegada, tempo-limite
   global, ou outra regra?
3. **Backend:** Mono (instalado, suficiente para Windows) ou IL2CPP (exige instalar o módulo)?
4. **Fluxo de trabalho:** branches a partir de `develop` com PR por item, como manda o
   `CONTRIBUTING.md`?
