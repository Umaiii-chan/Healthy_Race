# Evolução — auditoria do estado atual (v1.0.0)

Data: 10/11/2026. Base: `develop` @ `bd76863` (tag `v1.0.0`).

Este documento descreve o jogo como ele está, para que as propostas dos outros documentos
`EVOLUCAO_*` partam de fatos. Nada foi alterado no projeto para produzi-lo.

## Como a auditoria foi feita, e o que ela não cobre

| Fonte | O que deu |
| --- | --- |
| Leitura do código (`Assets/_Game/Scripts`, `Editor`, `Tests`) | Regras, parâmetros, fórmulas e dependências |
| Dados (`ScriptableObjects/Food`, `ScriptableObjects/WellBeing`, `Levels/Fase01..10.txt`) | Valores atuais de alimentos, faixas e conteúdo de cada fase |
| Robô de corrida (`LevelPlayModeTests`, física real) | Tempos e pontos medidos por fase, em `docs/TEST_REPORT.md` |
| Capturas geradas pelo Editor (`docs/evidence/`) | Aparência do menu, das fases, do HUD e dos resultados |

**Limitações que valem para todo o documento:**

- **Ninguém jogou com teclado e janela para esta auditoria.** As afirmações sobre "sensação"
  (game feel, legibilidade em movimento, diversão) são inferências a partir de código, números
  e capturas, marcadas como **A validar** quando dependem de uma pessoa jogando.
- O robô joga um caminho fixo e não disputa com o outro jogador. Ele não mede contenção pela
  comida, bloqueio de corpo nem ritmo humano.
- A captura `docs/evidence/visual/Results-*.png` é anterior à pontuação de corrida (mostra bônus
  de chegada de 100/200). A tela é a mesma; os números não.

## Legenda de classificação

| Classificação | Significado |
| --- | --- |
| Completo | Atende ao padrão de produto final |
| Funcional, precisa de polimento | Funciona, mas não está no padrão final |
| Incompleto | Existe, faltam partes importantes |
| Problemático | Tem falha ou prejudica a experiência |
| Ausente | Deveria existir e não existe |
| A validar | Não foi possível verificar |

## Matriz resumida

| Sistema | Classificação | Problema principal | Esforço · risco da evolução |
| --- | --- | --- | --- |
| Arquitetura e pastas | Funcional, precisa de polimento | `LevelBootstrap` com 1 624 linhas; código legado do protótipo cooperativo ainda compilado | Médio · baixo |
| Cenas e fluxo | Funcional, precisa de polimento | Sem seleção de fase; "Jogar" sempre abre a Fase 1 | Baixo · baixo |
| Personagens e controles | Problemático | P1 e P2 são o mesmo boneco; botão Interagir mapeado e sem uso; só teclado | Médio · baixo |
| Física e movimentação | Funcional, precisa de polimento | Pulo de altura fixa, controle aéreo instantâneo, gravidade simétrica | Baixo · médio (mexe nas rotas) |
| Corrida e escolha de rota | **Problemático** | O caminho curto é pior em tudo: não é uma escolha | Médio · médio |
| Alimentação | Incompleto | Efeito binário (bom/ruim), sem explicação na coleta, nomes em inglês nos dados | Médio · baixo |
| Bem-Estar e fadiga | Funcional, precisa de polimento | Efeitos pequenos e pouco visíveis acima de 25; fadiga só para quem já foi ao fundo | Médio · médio |
| Obstáculos e puzzles | Funcional, precisa de polimento | Cinco obstáculos repetidos em todas as fases; alavanca não é puzzle | Médio · baixo |
| Pontuação e vitória | **Problemático** | Coleta vale ~63% dos pontos; chegar primeiro vale 2 frutas; comida compartilhada gera bola de neve | Baixo · médio |
| Fases (level design) | Problemático | As 10 fases têm a mesma estrutura gerada; sem marcos visuais nem introdução gradual | Alto · médio |
| Câmera compartilhada | Funcional, precisa de polimento | Afasta até a fase inteira: personagens com ~30 px de altura | Médio · médio |
| HUD | Problemático | Lê como painel de depuração: texto pequeno, fonte padrão, dados demais | Médio · baixo |
| Menus e configurações | Funcional, precisa de polimento | Menu bonito; sem seleção de fase, sem remapeamento, sem créditos | Baixo · baixo |
| Tela de resultados | Problemático | Lista de números sem hierarquia; não explica por que alguém venceu | Baixo · baixo |
| Animações e efeitos | Funcional, precisa de polimento | Rig com 12 clipes; poucos efeitos de impacto; sem tremor de câmera | Médio · baixo |
| Direção de arte | **Problemático** | Três estilos misturados (vetor no menu, pixel art nos itens, placas de texto) | Alto · médio |
| Áudio | Incompleto | Uma música para as 10 fases; efeitos gerados (qualidade **A validar**) | Médio · baixo |
| Estados e eventos | Funcional, precisa de polimento | Pausa por `timeScale` global; controladores grandes com muita responsabilidade | Baixo · baixo |
| Dados configuráveis | Incompleto | Pontuação criada em código (`ScoreConfig.CreateForRace`); constantes de comida em `const` | Baixo · baixo |
| Persistência | Ausente | Só volumes e "reduzir flashes"; nenhum recorde, progresso ou estatística | Baixo · baixo |
| Acessibilidade | Incompleto | Sem remapeamento nem controle; distinção dos jogadores por cor; "reduzir flashes" existe | Médio · baixo |
| Testes e CI | Funcional, precisa de polimento | 324 testes locais; a CI pula os testes do Unity (sem licença) | Baixo · baixo |
| Desempenho | A validar | Nunca medido fora da máquina de desenvolvimento | — |

## Sistemas em detalhe

### 1. Arquitetura e pastas

- **Existe:** código em `Assets/_Game/Scripts` por domínio (`Player`, `WellBeing`, `Collectibles`,
  `Level`, `Obstacles`, `Puzzles`, `Scoring`, `UI`, `Visuals`, `Audio`, `Core`, `Input`), cerca de
  12 700 linhas. Fases são texto (`Levels/FaseNN.txt`) montado por `LevelBootstrap`; as cenas
  `FaseNN.unity` são geradas pelo menu do Editor `Build greenhouse levels`. A arte é gerada por
  scripts Python (`tools/art`) e importada por `VisualAssetBuilder`.
- **Funciona:** sim. É a melhor decisão técnica do projeto: uma fase nova é um arquivo de texto e
  um teste parametrizado a mais.
- **Provisório:**
  - `LevelBootstrap.cs` (1 624 linhas) monta terreno, itens, obstáculos, câmera, HUD, partida e
    feedback visual. Toda mudança de fase passa por ele.
  - Restos do protótipo cooperativo continuam compilados e alcançáveis por referência:
    `CooperativePuzzleController`, `PressureButton`, `PushableBox`, `VerticalSliceBootstrap`
    (498 linhas) e a cena `VerticalSlice_Core`, que ainda está na build.
  - Duas telas de resultado: `MatchResultsPanel` (texto legado) e `RaceScreenView`.
- **Esforço · risco:** médio · baixo. Os testes cobrem montagem e corridas; refatorar com eles é
  seguro. Detalhe em `EVOLUCAO_QUALIDADE.md`.

### 2. Cenas e fluxo de navegação

- **Existe:** `MainMenu` → `Phase1_CorridaDoEquilibrio` → `Fase02` … `Fase10`. `GameFlow` carrega
  por número; resultados oferecem "Próxima fase", "Jogar novamente" e "Menu". Pausa com Esc
  (continuar, reiniciar, controles, áudio, menu).
- **Funciona:** sim (testes `GameFlowPlayModeTests`).
- **Para produto final:** não. `VisualCommand.Play` chama `GameFlow.LoadLevel(1)` sempre: para
  jogar a Fase 7 é preciso vencer ou pular 6 fases. Não há conceito de partida com várias fases
  (placar acumulado), nem escolha de quem é P1.
- **Oportunidade:** seleção de fase e "copa" de 3 fases com placar acumulado (ver `EVOLUCAO_GAMEPLAY.md`).
- **Esforço · risco:** baixo · baixo.

### 3. Personagens e controles

- **Existe:** um personagem articulado (folha na cabeça, rig com 12 clipes) usado pelos dois
  jogadores; P1 e P2 diferem por uma etiqueta colorida acima da cabeça (azul e roxa). Teclado:
  P1 `A/D` + `W`/`Espaço`, P2 `←/→` + `↑`. Interagir: `E` e `Ctrl` direito.
- **Problemas observados:**
  - **Os dois jogadores são idênticos.** Numa corrida com câmera compartilhada, a primeira
    pergunta do jogador é "qual sou eu?". A etiqueta mede ~20 px na captura em 1080p.
  - **Interagir não faz nada.** O único consumidor de `InteractPerformed` é o depurador de
    entrada; alavancas disparam por contato. O botão aparece em `CONTROLS.md` e no menu.
  - Só teclado, sem remapeamento. Dois jogadores no mesmo teclado sofrem com *ghosting*
    (teclas simultâneas não registradas) em teclados comuns — **A validar** no hardware alvo.
- **Classificação:** Problemático (identidade e legibilidade), não por defeito técnico.
- **Esforço · risco:** médio · baixo (variante de cor/forma do rig; suporte a controle pelo Input System).

### 4. Física e movimentação

Valores atuais (`PlayerMovement`, `LevelBootstrap`):

| Parâmetro | Valor | Consequência |
| --- | --- | --- |
| Velocidade horizontal | 7 tiles/s | — |
| Aceleração no chão | 90 tiles/s² (0 → 7 em ~0,08 s) | Resposta quase instantânea |
| Controle no ar | instantâneo (sem aceleração) | Muda de direção no ar sem custo |
| Velocidade de pulo | 12 tiles/s | — |
| Gravidade efetiva | 36 tiles/s² (−18 × escala 2) | Calculada para ápice de 2,0 tiles |
| Tempo no ar (plano) | ~0,67 s | Alcance horizontal ~4,7 tiles |
| Pulo variável (soltar = pulo curto) | **ausente** | Um único arco possível |
| Gravidade maior na descida | **ausente** | Subida e descida simétricas |
| Coyote time / buffer de pulo | 0,10 s / 0,12 s | Presentes e testados |
| Encaixe em rampa | 0,35 tile de "grude" | Corrigido pelo robô; funciona |

- **Funciona:** sim, e é consistente — as rotas são provadas por `LevelRoutes` com o orçamento
  de pulo do pior Bem-Estar.
- **Para produto final:** falta expressividade. Com pulo de altura fixa e ar instantâneo, não há
  como "jogar bem" um salto: todos os pulos bons são iguais. Celeste e a maioria dos plataformas
  de precisão usam pulo variável e queda mais rápida — ver `EVOLUCAO_REFERENCIAS.md`.
- **Risco:** médio. Qualquer mudança no arco muda `JumpBudget` e pode invalidar rotas; os
  testes de rota pegam isso.
- **Sensação real:** **A validar** com pessoas (ver plano em `EVOLUCAO_QUALIDADE.md`).

### 5. Corrida e escolha de rota

- **Existe:** em cada andar há duas bifurcações. Cair no buraco embaixo da placa leva ao corredor
  curto (sem pulos, teto baixo, fast-food inevitável); pular mantém no longo (obstáculos e
  saudáveis). Os caminhos se reencontram no checkpoint do meio e no elevador.
- **Objetivo:** tornar a escolha alimentar uma decisão tática durante a corrida.
- **Funciona tecnicamente:** sim. **Funciona como decisão: não.** Medições do robô
  (`TEST_REPORT.md`):

| Fase | Longo | Curto (comendo tudo) | Pontos longo | Pontos curto |
| --- | --- | --- | --- | --- |
| 1 | 125,0 s | 140,6 s | 5 980 | −3 050 |
| 5 | 142,1 s | 166,9 s | 4 860 | −5 448 |
| 10 | 173,6 s | 209,0 s | 7 540 | −4 346 |

O curto é **mais lento e vale de 7 500 a 11 900 pontos a menos** em todas as 10 fases. Não existe
situação em que o jogador racional o escolha. Ele só funciona como castigo para quem cai de uma
plataforma — e aí o castigo se soma: cair → fast-food inevitável → peso → mais lento.

- **Problema de experiência:** a mensagem do menu ("Cada escolha muda a corrida") não se
  cumpre. A partir da segunda fase, o jogador que entendeu a regra nunca mais escolhe.
- **Uma decisão parcial não medida:** pegar só uma bifurcação curta. Estimativa pelo código: 2
  ultraprocessados (−200 pontos, −20 a −36 de Bem-Estar, até 5 s a meia velocidade) contra os
  saudáveis e a alavanca do trecho longo que se perde (~+230 a +390). Também perde. **A validar**
  com o robô em modo "um atalho".
- **Classificação:** Problemático. É o principal problema de design do jogo.

### 6. Sistema de alimentação

- **Existe:** 18 alimentos em `ScriptableObjects/Food`: 10 saudáveis (80 a 160 pontos, +8 a +16
  de Bem-Estar) e 8 ultraprocessados (0 pontos, −10 a −18, peso de 5 s a meia velocidade, −100
  pontos no fim).
- **Funciona:** sim. Coleta é única: o item some para os dois jogadores.
- **Provisório ou incompleto:**
  - Só há duas categorias e um efeito por categoria. Água, peixe e brócolis só diferem no número.
  - Na coleta aparece uma partícula (`FoodPresentation.Pickup`); o jogador não vê o nome nem o
    efeito (item B-12 da auditoria anterior, ainda aberto).
  - `_displayName` está em inglês ("Apple", "Hot Dog") — não aparece hoje, mas aparecerá assim que
    houver feedback de coleta.
  - Classificação moral binária ("saudável" vs "ultraprocessado") é a forma mais fácil de o tema
    virar sermão. Água vale +15 e refrigerante −12, sem contexto.
- **Problema de experiência:** a comida é um contador de pontos. Não muda a forma de jogar, exceto
  o peso, que só existe no caminho que ninguém deveria pegar.
- **Classificação:** Incompleto.

### 7. Bem-Estar e fadiga

| Faixa | Bem-Estar | Velocidade | Pulo (velocidade) |
| --- | --- | --- | --- |
| VITA saudável | 76–100 | ×1,00 | ×1,00 |
| VITA estável | 51–75 | ×0,95 | ×1,00 |
| VITA cansado | 26–50 | ×0,90 | ×0,95 |
| Estado crítico | 1–25 | ×0,80 | ×0,90 |
| SALO | 0 | ×0,70 | ×0,80 (ápice ~1,28 tile) |

Fadiga (`FatigueComponent`): dispara com Bem-Estar ≤ 20, para o jogador por 2 s, devolve +15 e
rearma acima de 25.

- **Funciona:** sim, com testes. A forma do personagem muda de cor e folha por faixa.
- **Problemas:**
  - Quem fica no caminho longo nunca sai dos 100. Para essa pessoa, o sistema não existe.
  - Acima de 25, a diferença é de 5 a 10% de velocidade — difícil de perceber em movimento
    (**A validar**), e o HUD só mostra um número.
  - A fadiga é previsível (limiar fixo) e só alcança quem já comeu 6 ou 7 ultraprocessados.
    Ela pune o jogador que já está perdendo, sem criar decisão.
- **Classificação:** Funcional, precisa de polimento — e de um papel no jogo.

### 8. Obstáculos e puzzles

| Elemento | Parâmetros | Observação |
| --- | --- | --- |
| Lombada com rampa (simples e dupla) | rampa 45° | Não exige pulo; é "estrada mais longa" |
| Lama | velocidade ×0,7 | Multiplicador de terreno separado do Bem-Estar |
| Espinhos | −5 Bem-Estar, volta ao checkpoint, recarga 1 s | Único obstáculo que exige precisão |
| Plataforma que cai | treme 0,45 s, volta em 2,5 s | Quem cai vai ao corredor curto |
| Alavanca + portão | portão abre 1,2 s depois, fica aberto | +150 para quem puxou; abre para os dois |
| Elevador | 5 tiles/s, piso fixo embaixo | Liga os andares nas pontas |

- **Funciona:** sim; o robô encontrou e corrigiu 9 defeitos de física (`TEST_REPORT.md`).
- **Problemas:**
  - "Alavanca e portão" não é puzzle: é encostar e esperar 1,2 s. Não há nenhum puzzle no jogo
    hoje, apesar de a proposta citá-los.
  - Os mesmos 5 obstáculos aparecem em todas as fases, sorteados por peso (`build_levels.py`).
    Não há obstáculo novo depois da Fase 1, nem combinação ensinada.
  - O portão aberto pelo líder fica aberto para quem vem atrás — bom para alcançar (ver §9),
    mas não intencional.
  - Nenhum obstáculo usa o tema alimentar.
- **Classificação:** Funcional, precisa de polimento.

### 9. Pontuação e condição de vitória

Fórmula atual (`ScoreService`, `ScoreConfig.CreateForRace`):

```text
pontos = soma(pontos dos saudáveis) + 150 × alavancas
       + chegada (1º 500, 2º 250)
       + bônus de tempo (600 até o tempo-alvo; cai linear até 0 em 1,5 × alvo)
       + 5 × Bem-Estar final
       − 100 × ultraprocessados − 200 × fadigas − 500 se não concluiu
```

Decomposição dos 5 980 pontos do caminho longo na Fase 1 (alvo 130 s, robô em 125 s, 4 alavancas):

| Parcela | Pontos | Fatia |
| --- | --- | --- |
| Saudáveis (32 itens) | ~3 780 | 63% |
| Alavancas | 600 | 10% |
| Bônus de tempo | 600 | 10% |
| Chegada (1º) | 500 | 8% |
| Bem-Estar 100 × 5 | 500 | 8% |

- **Problemas:**
  - **Chegar primeiro vale 250 a mais que chegar segundo — o mesmo que duas frutas.** O jogo
    se apresenta como corrida, mas a vitória é decidida pela coleta.
  - **O bônus de tempo satura.** Abaixo do alvo, todos ganham 600. Dois jogadores rápidos não se
    diferenciam pelo tempo.
  - **Comida compartilhada + coleta dominante = bola de neve.** Quem está na frente pega os
    itens; quem vem atrás encontra o caminho vazio e não tem como recuperar. Inferência pelo
    código — **A validar** em partidas reais, o robô não disputa.
  - Desempate (Bem-Estar, tempo, saudáveis, fadigas) está correto e testado.
- **Classificação:** Problemático. Detalhe e propostas em `EVOLUCAO_BALANCEAMENTO.md`.

### 10. Fases

- **Existe:** 10 fases geradas por `tools/levels/build_levels.py` a partir de uma especificação
  (largura, andares, semente, pesos de obstáculo, tempo-alvo, tema de cor). Tamanhos de 64 × 32 a
  80 × 44; 20 a 42 ultraprocessados e 21 a 44 saudáveis por fase.
- **Funciona:** sim, todas validadas pelo robô nos dois caminhos.
- **Problemas** (capturas `docs/evidence/fases/fase-01.png` e `fase-10.png` lado a lado):
  - **As 10 fases têm a mesma forma**: zigue-zague de andares, duas bifurcações por andar, a
    mesma placa, o mesmo elevador nas pontas. Muda o tamanho, a cor e a mistura de obstáculos.
  - **Sem marcos visuais.** Um andar é igual ao outro; não há "a parte da cachoeira" para
    lembrar ou combinar ("te pego na ponte").
  - **Sem introdução gradual.** A Fase 1 apresenta todos os obstáculos de uma vez; não existe
    uma fase de aprendizado curta e segura.
  - Placas de texto ("↓ CURTO / → LONGO", "PUXE A ALAVANCA", "FAST") fazem o papel que o desenho
    da fase deveria fazer.
- **Classificação:** Problemático para produto final; correto como base técnica.

### 11. Câmera compartilhada

- **Existe:** `SharedCameraController` enquadra os dois jogadores com zoom dinâmico de 18 tiles de
  altura até a fase inteira (38 a 44 tiles), suavizado.
- **Problema:** quando os jogadores se separam (andares diferentes), a câmera afasta até mostrar
  a fase inteira. Na Fase 10 em 1080p, o personagem cai de ~78 px para ~30 px de altura — perto
  do limite de legibilidade para um jogo de precisão. **A validar** com pessoas.
- **Classificação:** Funcional, precisa de polimento.

### 12. HUD

- **Existe:** faixa de 64 px no alto; para cada jogador: Bem-Estar (número e barra), faixa,
  fadiga, episódios, progresso %, pontos, contagem de saudáveis e ultraprocessados; no centro,
  tempo e nome da fase.
- **Problemas** (captura `docs/evidence/fase-01/baixo-esquerda.png`):
  - Oito informações por jogador, várias em fonte de ~10 px (fonte padrão `LegacyRuntime`).
  - Texto de estado interno ("Fadiga: Normal", "Ep. 0", "Sem faixa") que é linguagem de depuração.
    "Sem faixa" aparecendo com 100/100 na captura é **A validar** (pode ser só o instante da captura).
  - Nada aparece perto do personagem: o jogador olha para o alto para saber se está pesado.
  - Sem indicação de quem está na frente.
- **Classificação:** Problemático.

### 13. Menus e configurações

- **Existe:** menu principal ilustrado (captura `MainMenu-1280.png`): Jogar, Como jogar, Áudio,
  Sair. Tela de controles, tela de áudio com volumes e "reduzir flashes e tremores", pausa.
- **Bom:** é a tela mais próxima de produto final; tem identidade e clareza.
- **Falta:** seleção de fase, créditos (pedido no B-01), remapeamento de teclas, opções de jogo
  (ex.: assistências), explicação dos alimentos.
- **Classificação:** Funcional, precisa de polimento.

### 14. Tela de resultados

- **Existe:** título com o vencedor, frase, duas colunas com 13 linhas de números cada,
  três botões.
- **Problemas:** todas as linhas com o mesmo peso visual; não diz o que decidiu a corrida
  ("venceu pela coleta, mesmo chegando depois"); sem momento de comemoração; texto em fonte
  padrão sobre fundo escurecido.
- **Classificação:** Problemático (a informação existe, a comunicação não).

### 15. Animações e efeitos

- **Existe:** rig com 12 clipes (parado, correr, pular, cair, aterrissar, empurrar, fadiga,
  vitória, derrota e outros), piscar, mola da folha, rosto de esforço quando pesado; partículas de
  coleta, pulo e aterrissagem (`VisualFeedback`); plataforma treme antes de cair; bolhas na lama.
- **Falta:** tremor de câmera (a opção para reduzi-lo já existe), *squash & stretch* no pulo,
  rastro de velocidade, feedback forte de dano nos espinhos, transições entre telas.
- **Classificação:** Funcional, precisa de polimento.

### 16. Direção de arte

- **Observado:**
  - O menu usa ilustração vetorial com contorno grosso (personagens, comida e cenário de colina).
  - A fase usa **pixel art** nos alimentos, plataformas e placas, e vetor no personagem.
  - O fundo da estufa é claro e lavado: plataformas, lama e itens disputam contraste com o fundo.
  - A arte da estufa é neutra e tingida por fase: a cor muda, a forma não.
  - Letreiros "FAST" em inglês no corredor; "CURTO / LONGO" em português.
- **Problema:** não há um estilo; há três. É a principal razão de o jogo "parecer protótipo"
  nas capturas.
- **Classificação:** Problemático. Proposta em `EVOLUCAO_ART_DIRECTION.md`.

### 17. Áudio

- **Existe:** `AudioService` com catálogo em `Resources/SO_AudioCatalog`, volumes (geral, música,
  efeitos) persistidos; 2 músicas (`mus_menu`, `mus_level01`) e 16 efeitos.
- **Falta:** música por fase ou por intensidade (última volta, janela de 45 s); variação de efeitos
  (o mesmo passo e o mesmo pulo sempre); som distinto por jogador.
- **Qualidade sonora:** **A validar** — os arquivos foram gerados por ferramenta e nunca avaliados
  por uma pessoa ouvindo em partida.
- **Classificação:** Incompleto.

### 18. Estados, eventos e dados

- **Estados:** `GameFlow` (estático) para cena e pausa (`Time.timeScale`); `PlayerStateMachine` e
  `FatigueStateMachine` testadas; `MatchCompletionController` (691 linhas) para chegada, janela
  final, tempo-limite e pontuação.
- **Eventos:** C# `event Action` entre componentes; sem barramento global. Adequado ao tamanho.
- **Dados:** alimentos e faixas em ScriptableObject (bom). Pontuação criada em código, constantes
  do peso em `const` dentro de `FoodData`, parâmetros de fase em `const` no `LevelBootstrap`.
  Ajustar balanceamento exige recompilar.
- **Classificação:** Funcional (estados) · Incompleto (dados).

### 19. Persistência

- **Existe:** `PlayerPrefs` para volumes e "reduzir flashes".
- **Ausente:** recordes por fase, fases concluídas, estatísticas de partida (que também serviriam
  ao playtest).

### 20. Acessibilidade

- **Existe:** "reduzir flashes e tremores"; volumes separados; tolerâncias de pulo (coyote e buffer).
- **Ausente:** remapeamento, controle, ajuste de velocidade do jogo, tamanho de texto, legendas
  visuais para sons, distinção dos jogadores além da cor (azul × roxo é um par fraco para
  daltonismo azul-amarelo — **A validar**).
- **Classificação:** Incompleto.

### 21. Testes, CI e desempenho

- **Testes:** 233 EditMode + 91 PlayMode, todos passando localmente; robô de corrida nas 10 fases
  (~30 min no PlayMode completo).
- **CI:** lint de Markdown, YAML e formatação C#. Os testes do Unity são pulados por falta do
  segredo `UNITY_LICENSE` — check verde não prova nada sobre o jogo.
- **Desempenho:** a fase monta milhares de objetos em `Awake` e o terreno é um colisor único;
  nunca foi medido em máquina fraca nem com perfilador. **A validar.**

## Por que o jogo ainda transmite características de MVP

1. **A decisão central não é decisão.** O curto é pior em tempo, pontos e Bem-Estar.
2. **A vitória é decidida pela coleta, não pela corrida**, e a coleta favorece quem já está na frente.
3. **Tudo se parece:** dois jogadores idênticos, dez fases com a mesma forma, uma música.
4. **Três estilos visuais** e um HUD que fala a língua do programador.
5. **O tema saúde aparece como número**, sem explicação nem consequência que se sinta no controle.

Os cinco pontos estão no código e nos dados; nenhum depende de opinião sobre a arte. O que
depende de pessoas jogando está marcado como **A validar** e entra no plano de playtest.
