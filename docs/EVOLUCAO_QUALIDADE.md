# Evolução — arquitetura, qualidade técnica, acessibilidade e playtest

Base: `EVOLUCAO_AUDITORIA.md` §1, §18-21. Este documento diz o que a base técnica precisa para
sustentar as propostas de gameplay, UI/UX e arte, e como validar o produto com pessoas.

## 1. Avaliação da arquitetura

| Aspecto | Situação | Suporta a evolução? |
| --- | --- | --- |
| Fases como dados (`LevelGrid` + texto) | Excelente; testado por parâmetro nas 10 fases | Sim — é a base de O1 |
| Robô de corrida (`LevelPlayModeTests`) | Valida rotas com física real | Sim — é a ferramenta de balanceamento (E1-E4) |
| Acoplamento | `LevelBootstrap` (1 624 linhas) monta mapa, câmera, HUD, partida e efeitos | Parcial — HUD, câmera e partida vão mudar |
| Responsabilidade dos scripts | Pequenos e focados, exceto `LevelBootstrap` e `MatchCompletionController` (691) | Sim |
| ScriptableObjects | Comida e faixas sim; pontuação, efeitos e parâmetros de fase não | Não para playtest: ajustar exige recompilar |
| Estados | `GameFlow` estático, máquinas de estado testadas | Sim |
| Eventos | `event Action` entre componentes | Sim; barramento global desnecessário |
| Persistência | `PlayerPrefs` para áudio | Não para recordes, estrelas, fases liberadas |
| Cenas | Uma por fase, geradas | Sim |
| Assets | Gerados por script (Python + `VisualAssetBuilder`) | Sim; trocar estilo é regerar |
| Entrada | Input System, um mapa por jogador, só teclado | Não para controle e remapeamento |
| UI | `UnityEngine.UI.Text` com fonte embutida | Parcial — fonte e tamanho de texto pedem TextMeshPro |
| Código morto | Cooperativo (`CooperativePuzzleController`, `PressureButton`, `PushableBox`), `VerticalSliceBootstrap`, `MatchResultsPanel` | Atrapalha leitura; `VerticalSlice_Core` ainda na build |

Conclusão: **a arquitetura suporta a evolução sem reescrita.** Faltam três peças pequenas
(efeitos do jogador, estado de coleta por jogador, dados de balanceamento em asset) e dois
serviços (salvamento e registro de partida).

## 2. Refatorações

### Indispensáveis (bloqueiam propostas P0/P1)

| # | Refatoração | Bloqueia | Custo |
| --- | --- | --- | --- |
| Q1 | **Efeitos do jogador**: trocar `ApplySluggishness` por um componente `PlayerEffects` com efeitos temporizados (pico, queda, firme) e multiplicador combinado; `PlayerMovement` lê um número só | F1, F2, B1 | 1,5 dia |
| Q2 | **Coleta por jogador**: `FoodCollectible` guarda quem coletou; apresentação translúcida por jogador; item disputado como variante | F4 | 1,5 dia |
| Q3 | **Balanceamento em asset**: `SO_Balance` com pontuação, efeitos de comida, fadiga, janelas de partida; `ScoreConfig.CreateForRace` passa a ler dele (item B-11 da auditoria anterior) | Iteração de playtest | 1 dia |
| Q4 | **Extrair do `LevelBootstrap`** a montagem da câmera e do HUD (`LevelCameraSetup`, `LevelHudSetup`), mantendo a montagem do mapa onde está | U4, C5 | 1,5 dia |

### Recomendadas

| # | Refatoração | Por quê | Custo |
| --- | --- | --- | --- |
| Q5 | Remover código cooperativo morto e `VerticalSlice_Core` da build (B-18) | Menos código, build menor, nomes certos ("mecanismo", não "puzzle") | 0,5 dia + ajustar testes que carregam a cena |
| Q6 | **Salvamento** em JSON (`Application.persistentDataPath`): fases liberadas, estrelas, recordes, opções; versão do formato no arquivo | U2, estrelas, recordes | 1 dia |
| Q7 | **Registro de partida** (JSONL local, desligável): bifurcação e rota escolhida, coletas, estados, posição a cada 1 s, resultado | Playtest E5-E8 | 1 dia |
| Q8 | **Entrada**: esquemas por dispositivo, atribuição de dispositivo por jogador, *rebinding* salvo | U3, U7, acessibilidade | 2,5 dias |
| Q9 | **TextMeshPro** + fonte da direção de arte no HUD e nas telas | Legibilidade, "texto maior" | 2 dias |
| Q10 | CI com testes do Unity (segredo `UNITY_LICENSE` ou *runner* próprio); corridas do robô em categoria separada, noturna | Check verde passar a significar algo | 1 dia + licença |

### Podem esperar (ou não fazer)

- Barramento de eventos global, injeção de dependência: o tamanho do projeto não pede.
- Addressables: 12 cenas pequenas; carregamento atual basta.
- IL2CPP: só se o desempenho medido pedir (Mono atende hoje).
- Sistema de localização: o jogo é só em português; basta não deixar texto em inglês.
- **Não fazer:** reescrever física, trocar pipeline de render, migrar para ECS.

Compatibilidade: tudo acima funciona no Unity 6000.3 LTS e nas dependências atuais (Input System,
URP 2D, Test Framework). TextMeshPro já vem no Unity 6 (pacote `ugui`).

## 3. Testes e qualidade de código

- **Manter** o padrão atual: cada mecânica nova com teste EditMode (regra) + PlayMode (física).
- **Robô:** modos novos para os experimentos — "curto sem efeitos", "um atalho", "lanche + fruta".
  Cada mudança de física ou de gerador roda as 10 corridas antes do merge.
- **Teste de equidade:** largadas equidistantes da primeira bifurcação e portas equidistantes do
  último elevador, nas 10 fases (EditMode, sobre o mapa).
- **Teste de legibilidade automático:** `VisualEvidence` gera capturas em 1280×720 e 1920×1080;
  acrescentar um teste que mede o tamanho em pixels do personagem no zoom máximo (≥ 50 px).
- **Tempo de suíte:** PlayMode completo ~30 min por causa do robô. Separar categoria `Corrida` e
  rodar só nas mudanças de fase/física e à noite.

## 4. Desempenho

Nunca medido fora da máquina de desenvolvimento. Plano:

| Medida | Como | Meta inicial (a confirmar) | Origem da meta |
| --- | --- | --- | --- |
| Quadros por segundo | Build com `Profiler` / contador na tela de depuração, Fase 10, dois jogadores separados | 60 fps estáveis em 1080p | `Application.targetFrameRate = 60` já está no código |
| Hardware alvo | Notebook com vídeo integrado (classe Intel UHD 620) | 60 fps em 1080p ou 720p | Hipótese: laboratório escolar e feira; confirmar com a máquina real da feira |
| Tempo de carga de fase | Cronômetro de `LoadScene` até o "JÁ!" | < 3 s | Abaixo de 3 s, a revanche parece imediata |
| Lixo por quadro (GC) | Profiler | ~0 B em corrida | Picos de GC causam engasgos visíveis |

## 5. Acessibilidade

Checklist a partir do Game Accessibility Guidelines (ver `EVOLUCAO_REFERENCIAS.md` §5):

| Item | Proposta | Prioridade |
| --- | --- | --- |
| Remapear controles | Q8 + U7 | P1 |
| Mais de um dispositivo | Controle por jogador (Q8) | P1 |
| Não depender só de cor | P1/P2 por forma e número; lanche/fruta por forma e aura | P0 |
| Tamanho de texto legível | ≥ 24 px em 1080p; opção "texto maior" | P0 / P1 |
| Informação na linha do olhar | Estados perto do personagem (U4) | P0 |
| Informação não só por som | Todo som de estado tem ícone | P1 |
| Ajuste de velocidade / assistência | Por jogador (U7) | P1 |
| Treino sem falha | Fase-tutorial e modo treino | P1 |
| Reduzir flashes e tremores | Já existe; aplicar a todo efeito novo | Mantido |
| Configurações salvas | Já existe para áudio; estender (Q6) | P1 |
| *Ghosting* do teclado | Teste no "Prontos?" (U3) e esquemas alternativos | P1 |

## 6. Plano de playtest

### Protocolo

- **Participantes:** pessoas que não participaram do desenvolvimento, em **pares**. Mínimo de 5
  pares por rodada (número usual para achar os problemas mais frequentes de usabilidade; não serve
  para estatística). Misturar: 2 pares iniciantes, 2 experientes, 1 par misto (adulto + criança ou
  experiente + iniciante).
- **Ambiente:** a máquina e o teclado reais da feira, se possível.
- **Papéis:** um facilitador (explica só "é um jogo de corrida para dois; joguem como quiserem")
  e um observador silencioso com a ficha de observação. Não ajudar, exceto se travarem por 2 min.
- **Gravação:** tela + áudio da conversa (com autorização), e o registro de partida (Q7).
- **Rodadas:** antes das mudanças (linha de base da v1.0.0), depois da Fase B (gameplay) e antes
  do release.

### Cenários reproduzíveis

| # | Cenário | Roteiro | O que observar |
| --- | --- | --- | --- |
| S1 | Primeira partida | Menu → jogar sem instrução | Tempo até a primeira corrida; "qual sou eu?"; entendem a bifurcação? |
| S2 | Aprendizado dos controles | Fase-tutorial (ou Fase 1) | Erros de pulo nos primeiros 60 s; teclas que "não pegam" |
| S3 | Disputa | Copa de 3 fases | Trocas de liderança; reações; quem chega primeiro vence? |
| S4 | Uso dos alimentos | Fases 1-3 | Escolha do atalho por bifurcação; uso da fruta para cortar a queda |
| S5 | Energia/Bem-Estar | Fase com mais lanches (9) | Percebem a queda e a fadiga? Entendem por quê? |
| S6 | Obstáculos | Fases 4 e 5 | Quedas e mortes por obstáculo; frustração |
| S7 | Conclusão | Fase 10 | Conclui? Tempo; janela de 45 s acionada? |
| S8 | Repetição | Após S3, perguntar "querem jogar mais uma?" | Escolha espontânea de revanche |

### Métricas

| Métrica | Fonte | Meta inicial | De onde vem a meta |
| --- | --- | --- | --- |
| Tempo médio de partida (fase) | Registro | 1,5-3 min | Decisão do usuário (fases de 1-2 min) + leitura de tela; ajustar |
| Taxa de conclusão por fase | Registro | ≥ 90% nas fases 1-5 | Fases iniciais não devem barrar ninguém (pilar 4) |
| Quedas e mortes por fase | Registro | Comparar entre fases (curva) | Sem meta absoluta na primeira rodada |
| Escolha do atalho por bifurcação | Registro | Entre 25% e 75% | Nenhuma opção obrigatória nem inútil (pilar 2) |
| Distribuição de vitórias | Registro | Nenhum lado (P1/P2) > 60% em 20+ partidas | Equidade de largada e teclado |
| Vitória de quem chega primeiro | Registro | Se D1-A: 100% por regra; se D1-B: ≥ 70% | Pilar 1 |
| Diferença de chegada | Registro | Mediana < 5 s | Corridas apertadas mantêm tensão; ajustar |
| Trocas de liderança | Registro (posição a cada 1 s) | ≥ 1 em ≥ 60% das partidas | Pilar 1 |
| Reinícios por fase | Registro | Comparar | Indica frustração ou competição |
| Mecânicas pouco usadas | Registro | Toda mecânica usada em ≥ 1 de cada 3 partidas | Mecânica não usada é custo sem retorno |
| Dificuldades relatadas | Formulário + observação | — | Qualitativo |
| Confusões de interface | Ficha de observação | Nenhuma pergunta "qual sou eu?" após "Prontos?" | Pilar 5 |

Todas as metas são **iniciais**. Depois da rodada de linha de base, cada meta é revista com os
dados: se a linha de base já cumpre, a meta sobe; se ninguém chega perto, investigar antes de
mexer no jogo.

### Formulário para cada jogador (após a sessão)

Escala de 1 (discordo totalmente) a 5 (concordo totalmente), salvo indicação.

1. **Diversão:** "Me diverti jogando."
2. **Diversão:** "Teve um momento em que eu disse 'quase!' ou comemorei." (sim/não; qual?)
3. **Clareza:** "Eu sabia qual personagem era o meu o tempo todo."
4. **Clareza:** "Entendi o que acontecia quando eu pegava o caminho da lanchonete."
5. **Clareza:** "Em poucas palavras, o que o lanche faz com o personagem? E a fruta?" (aberta)
6. **Dificuldade:** "A dificuldade foi: muito fácil · fácil · na medida · difícil · muito difícil."
7. **Dificuldade:** "Teve uma parte em que eu fiquei travado(a)? Qual?" (aberta)
8. **Competitividade:** "A corrida foi justa."
9. **Competitividade:** "Mesmo atrás, eu sentia que podia virar."
10. **Controles:** "O personagem fez o que eu queria."
11. **Controles:** "Alguma tecla não funcionou quando eu apertei?" (sim/não; qual?)
12. **Identidade visual:** "O jogo parece bonito / bem acabado."
13. **Identidade visual:** "Descreva o jogo em três palavras." (aberta)
14. **Vontade de jogar de novo:** "Eu jogaria de novo com a mesma pessoa."
15. **Tema:** "O jogo me fez pensar em alimentação sem parecer uma aula."
16. Idade (faixa), frequência com que joga videogame, já jogou plataforma?

### Ficha do observador (por partida)

Hora · fase · quem venceu · trocas de liderança percebidas · perguntas feitas ao facilitador ·
momentos de frustração (onde) · momentos de comemoração (onde) · atalhos pegos de propósito ·
comentários espontâneos sobre comida.
