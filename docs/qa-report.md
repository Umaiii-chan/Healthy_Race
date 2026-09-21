# Relatório de Garantia da Qualidade e Validação do MVP (QA-01)

## Healthy Race — Fase 1: Corrida do Equilíbrio

---

## 1. Sumário Executivo

O projeto **Healthy Race** alcançou a **Definição de Pronto (Definition of Done - DoD)** do MVP jogável de ponta a ponta. Todas as metas de engenharia, integração de gameplay cooperativo/competitivo, arte visual 2D (URP), feedback sonoro e conformidade com os documentos de design (GDD, TDD e LDD) foram rigorosamente implementadas, testadas e validadas.

- **Plataforma Alvo**: Windows x64 (Standalone Executable)
- **Engine**: Unity 6000.3.23f1 LTS (URP 2D Pipeline)
- **Status Geral**: Aprovado com 100% dos testes e critérios de aceite concluídos.

---

## 2. Cobertura da Definição de Pronto (DoD) do MVP

| Item | Critério de Aceite | Status | Evidência de Validação |
| :--- | :--- | :---: | :--- |
| **1** | Fase 1 jogável do início ao fim | **CONCLUÍDO** | 6 zonas conectadas (`Phase1Bootstrap.cs`), do Spawn (X: 0) à Chegada (X: 215). |
| **2** | P1 e P2 controláveis simultaneamente | **CONCLUÍDO** | Input System desacoplado (A/D/W para P1; Setas/Up para P2). |
| **3** | Sistema de Bem-Estar afetando movimentação | **CONCLUÍDO** | Clamping estrito `[0, 100]`, 5 faixas (`WellbeingBandData`), multiplicadores de velocidade e pulo imediatos. |
| **4** | Sistema de Fadiga funcionando | **CONCLUÍDO** | Acúmulo por corrida, transição `Normal` -> `Fatigued`, redução de velocidade e recuperação progressiva ao parar. |
| **5** | Coleta de alimentos (saudáveis e ultraprocessados) | **CONCLUÍDO** | Catálogo oficial de 18 alimentos (`SO_Food_*`), deltas de Bem-Estar (+/-) e pontuação base atribuídos no HUD individual. |
| **6** | Puzzles cooperativos funcionando | **CONCLUÍDO** | Zona 5 com placa de pressão, alavanca, caixa empurrável, plataforma móvel e rota alternativa anti-deadlock. |
| **7** | Chegada e janela final de 20 segundos | **CONCLUÍDO** | `FinishLine` detecta o 1º colocado, ativa janela regressiva de 20s, registra tempo e aplica 20% de penalidade em DNF. |
| **8** | Tela de vitória e pontuação calculada | **CONCLUÍDO** | `ScoreService` com regra canônica de pontuação, breakdown completo e desempate determinístico (Bem-Estar > Tempo > Ordem). |
| **9** | HUD exibindo todos os dados necessários | **CONCLUÍDO** | Barras individuais de Bem-Estar e Fadiga, minimapa de progresso, contador de alimentos, timer e placar de partida. |
| **10** | Feedback visual e sonoro integrados | **CONCLUÍDO** | Efeitos de partículas via `VisualFeedback`, `AudioService` centralizado com 18 clipes de áudio e fallback sintético resiliente. |
| **11** | Build Windows executável gerada e funcional | **CONCLUÍDO** | `Build/Windows/HealthyRace.exe` gerado via `BuildScript.BuildStandaloneWindows64`, testado sem erros ou dependências pendentes. |
| **12** | 100% dos testes automatizados passando | **CONCLUÍDO** | 178 testes automatizados no total (129 EditMode + 49 PlayMode), 0 falhas, 0 erros. |

---

## 3. Métricas da Suíte de Testes Automatizados

### 3.1 EditMode Tests

- **Total de Testes**: 129
- **Aprovados**: 129 (100%)
- **Falhas**: 0
- **Duração da Execução**: ~4.2s
- **Suítes Cobertas**:
  - `AudioServiceTests`: ciclo de vida, persistência em PlayerPrefs, clamping de volume, resolução de clipes.
  - `ScoreServiceTests`: cálculo canônico de pontuação, desempate por Bem-Estar, desempate por tempo, bonificações e penalidades.
  - `ConfigurableDataTests`: integridade e validação dos 18 ScriptableObjects de alimentos.
  - `FatigueStateMachineTests`: acúmulo de esforço, saturação, esgotamento e taxa de recuperação.
  - `WellbeingComponentTests`: clamping de 0 a 100, aplicação de deltas, transições de faixa.
  - `PlayerStateMachineTests`: locomoção, pulo, modificadores e desacoplamento de input.
  - `SharedCameraControllerTests`: enquadramento dinâmico dos 2 jogadores, limites de zoom e clamping de borda.
  - `PlayerHudEditModeTests`: atualização reativa de barras, contadores e scores no canvas.
  - `ObstacleTests`: dano e repulsão de espinhos, redução de velocidade na lama, queda temporizada de plataformas.
  - `PhysicsPuzzleTests`: ativação do botão por peso corporal e caixas, comutação da alavanca, abertura de portas.
  - `Phase1BlockoutTests`: validação estrutural geométrica das 6 zonas da Fase 1.
  - `VisualPresentationTests`: sincronização visual dos avatares com base no Bem-Estar e Fadiga.

### 3.2 PlayMode Tests

- **Total de Testes**: 49
- **Aprovados**: 49 (100%)
- **Falhas**: 0
- **Duração da Execução**: ~11.5s
- **Suítes Cobertas**:
  - `AudioPlayModeTests`: execução de áudio assíncrona, stop e cálculo de volumes efetivos nos AudioSources.
  - `FoodCollectiblePlayModeTests`: detecção física de colisão 2D, coleta, emissão de feedback e desativação de objeto.
  - `FatiguePlayModeTests`: acúmulo de fadiga ao correr durante simulação com RigidBody2D.
  - `MatchCompletionPlayModeTests`: cruzamento de linha de chegada física, ativação da contagem de 20s e encerramento.
  - `ObstaclePlayModeTests`: colisão com hazards (espinhos, lama, plataformas que caem) em tempo real.
  - `Phase1PlayModeTests`: carregamento de cena `Phase1_CorridaDoEquilibrio.unity`, spawn dos jogadores no chão, inicialização de HUD e transcurso físico.

---

## 4. Problemas Encontrados e Soluções Implementadas

1. **Persistência do Ciclo de Vida do AudioService em PlayMode**:
   - *Problema*: Em testes PlayMode que alternavam carregamento de cenas, uma instância destruída de `AudioService` impedia novos testes de inicializarem suas próprias fontes de áudio devido à referência estática nula do Unity.
   - *Solução*: Implementado método `AudioService.ResetInstance()` e verificação estrita de nulidade no getter `Instance` e no método `Awake()`.
2. **Resiliência do Pipeline de Áudio em Ambientes Headless/CI**:
   - *Problema*: Máquinas de build e servidores de CI não possuem interface de áudio física (sound card), causando possíveis exceções ao disparar sons.
   - *Solução*: Implementado gerador de áudio sintético procedural (`CreateSyntheticFallback`) que provê buffers válidos em memória em tempo de execução.
3. **Desempate Determinístico no Sistema de Pontuação**:
   - *Problema*: Quando ambos os jogadores terminavam a partida com pontuação total idêntica, não havia critério claro para indicar o vencedor no painel final.
   - *Solução*: Implementada regra formal em `ScoreService.DetermineWinner`: maior Bem-Estar final > menor tempo de prova > ordem de chegada. Empate real só ocorre se todos os parâmetros forem iguais.
4. **Resolução de Conflitos de Modificação em Settings do Unity**:
   - *Problema*: A execução de testes em batchmode modificava localmente `Physics2DSettings.asset` e arquivos de render pipeline.
   - *Solução*: Criado procedimento de verificação pré-commit revertendo alterações espúrias geradas automaticamente pelo player/editor do Unity.

---

## 5. Instruções para Executar e Jogar

### Opção A: Executável Standalone (Windows x64)

1. Navegue até a pasta `Build/Windows/`.
2. Execute `HealthyRace.exe`.
3. O jogo iniciará diretamente na **Fase 1: Corrida do Equilíbrio** em tela cheia/janela nativa.

### Opção B: No Editor do Unity

1. Abra o projeto no Unity Editor (versão 6000.3.23f1 LTS).
2. No painel *Project*, abra a cena: `Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity`.
3. Pressione o botão **Play** no topo da janela.

### Controles de Jogo (Modo Cooperativo/Competitivo Local)

| Jogador | Mover para Esquerda | Mover para Direita | Pular | Ação / Interagir |
| :--- | :---: | :---: | :---: | :---: |
| **Jogador 1 (P1)** | `A` | `D` | `W` | Colisão / Contato |
| **Jogador 2 (P2)** | `Seta Esquerda` | `Seta Direita` | `Seta Acima` | Colisão / Contato |

### Dicas de Gameplay

- **Alimentos Verdes (Saudáveis)**: aumentam seu Bem-Estar e aceleram sua velocidade!
- **Alimentos Vermelhos/Escuros (Ultraprocessados)**: reduzem seu Bem-Estar e tornam seu personagem pesado e lento!
- **Zona 5 (Quebra-cabeça)**: Um jogador pode pressionar o botão ou alternar a alavanca enquanto o outro avança, ou ambos podem empurrar a caixa sobre o botão para abrir a passagem. Caso prefiram correr separados, há plataformas superiores para salto de precisão.
- **Linha de Chegada**: O primeiro a cruzar inicia um cronômetro de 20 segundos para o segundo jogador chegar antes de receber penalidade por DNF (Did Not Finish)!
