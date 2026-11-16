# Evolução — UI/UX e experiência do jogador

Base: `EVOLUCAO_AUDITORIA.md` §2, §12-14, §20 e as capturas em `docs/evidence/visual/`. As
telas existem como prefab gerado (`PF_VIS_Screens`, por `VisualAssetBuilder`) e são controladas
por `GameScreens` + `RaceScreenView`; o HUD é montado em código (`PlayerHUD`, `MatchHUD`).

## 1. Jornada atual, tela a tela

| Momento | Hoje | Problema | Classificação |
| --- | --- | --- | --- |
| Abertura | Cena `MainMenu` | — | Completo |
| Menu principal | Jogar, Como jogar, Áudio, Sair | Sem seleção de fase, sem créditos | Funcional, polir |
| Escolha de jogadores | Não existe | Ninguém confirma que está pronto nem qual lado é seu | Ausente |
| Seleção de fase | Não existe; Jogar = Fase 1 | Fase 7 exige passar por 6 | **Ausente (P0)** |
| Instruções | Tela "Como jogar" com controles | Não explica comida, atalho nem pontos; é leitura, não prática | Incompleto |
| Tutorial | Não existe | A Fase 1 apresenta tudo de uma vez | Ausente |
| Largada | A corrida começa ao carregar a cena | Quem ainda está achando a tecla larga atrás | **Ausente (P0)** |
| HUD | 8 dados por jogador em fonte ~10 px | Lê como depuração; nada perto do personagem | Problemático |
| Energia/Bem-Estar | Número + barra no alto | Fora da linha do olhar | Problemático |
| Fadiga | Texto "Fadiga: EM FADIGA!" no alto | Fora da linha do olhar | Problemático |
| Pontuação | Pontos ao vivo no alto | Irrelevante durante a corrida se D1 for aprovada | Problemático |
| Pausa | Esc: continuar, reiniciar, controles, áudio, menu | Qualquer jogador pausa; ok | Completo |
| Configurações | Volumes + reduzir flashes | Sem controles, assistência, tamanho de texto | Incompleto |
| Vitória/derrota | Tela de resultado em texto | Sem hierarquia nem comemoração; não diz por quê | Problemático |
| Reinício | "Jogar novamente" | Ok; deveria ser o foco padrão | Funcional, polir |
| Entre fases | "Próxima fase" | Sem placar acumulado | Incompleto |

## 2. Arquitetura de navegação proposta

```text
Abertura
  └─ Menu principal
       ├─ Jogar ──► Modo ──┬─ Copa (3 fases) ──► Prontos? ──► 3-2-1 ──► Fase ──► Resultado ──► Placar da copa ─┐
       │                   ├─ Fase livre ──► Seleção de fase ──► Prontos? ──► 3-2-1 ──► Fase ──► Resultado    │
       │                   └─ Treino (fase-tutorial, sem rival obrigatório)                                  │
       ├─ Como jogar (3 cartões ilustrados + "Treinar agora")                                ◄───────────────┘
       ├─ Opções ──┬─ Áudio (existe)
       │           ├─ Controles (remapear P1/P2, dispositivo de cada um)
       │           └─ Acessibilidade (texto maior, reduzir flashes — existe, assistência padrão)
       ├─ Créditos
       └─ Sair
Pausa (Esc ou Start): Continuar · Reiniciar fase · Opções · Sair para o menu
```

Princípios: no máximo 3 cliques do menu até a corrida; "Revanche" sempre em foco depois de
uma fase; qualquer tela navegável por teclado e por controle.

## 3. Telas a redesenhar, por prioridade

### U1 — Contagem de largada "3-2-1" · P0

- **Problema:** a partida começa ao carregar a cena.
- **Solução:** câmera mostra a porta de chegada (2 s) e volta à largada; contagem 3-2-1 com som;
  movimento liberado no "JÁ!". Pular o vídeo da porta com qualquer tecla de pulo.
- **Custo:** 1 dia. **Aceite:** teste PlayMode: entrada ignorada antes do "JÁ!"; cronômetro
  começa no "JÁ!".

### U2 — Seleção de fase · P0

- Grade de 10 cartões com miniatura, nome, elemento dono, estrelas de cada jogador e recorde.
  Fases liberadas progressivamente (fase N+1 libera ao terminar a N) com opção "liberar todas"
  para feira/professor.
- **Custo:** 1,5 dia (+ persistência, `EVOLUCAO_QUALIDADE.md` Q6). **Aceite:** escolher a Fase 7 em ≤ 3 cliques.

### U3 — "Prontos?" · P1

- Dois painéis lado a lado; cada jogador aperta o próprio pulo para confirmar. Mostra os
  controles daquele jogador, o broto e a cor dele, e a assistência (liga/desliga) de cada um.
- Resolve "qual sou eu?" antes da corrida e testa o teclado (efeito *ghosting* aparece aqui,
  não no meio da fase).
- **Custo:** 1,5 dia. **Aceite:** só começa com os dois confirmados; teste de entrada por jogador.

### U4 — HUD mínimo + informação no personagem · P0

| Fica no alto | Vai para perto do personagem | Sai |
| --- | --- | --- |
| Retrato e Bem-Estar (barra com a cor da faixa) de cada jogador | Ícone de estado: pico, queda, fadiga (com contagem) | "Fadiga: Normal", "Ep. 0" |
| Tempo (centro) | Texto flutuante da coleta: ícone + "+12" | "Saud. N · Ultra N" (vai para o resultado) |
| **Barra de corrida**: uma linha com os dois brotos mostrando o progresso de cada um até a porta | Seta na borda da tela quando o jogador sai do quadro | Pontos ao vivo (se D1 aprovada) |
| Nome da fase só nos 3 primeiros segundos | | "Sem faixa", "%" de progresso em texto |

- Tamanhos mínimos em 1080p: números 32 px, rótulos 24 px; escalam com a opção "texto maior".
- Fonte: a da direção de arte (sans arredondada), não `LegacyRuntime`.
- **Custo:** 3 dias. **Aceite:** captura em 1280×720 e 1920×1080 sem sobreposição; nenhum texto
  < 24 px em 1080p; teste EditMode dos elementos.

### U5 — Resultado que explica · P0

Hierarquia, de cima para baixo:

1. **Vencedor** grande, com o broto em pose de vitória e confete (o perdedor ao lado, menor, em pose de derrota).
2. **Uma frase de "por quê"**, gerada a partir dos eventos da partida. Exemplos:
   - "P2 chegou 3,2 s antes."
   - "O impulso do lanche no 4º andar colocou P1 na frente."
   - "P1 perdeu 6 s sonolento na lama."
3. **Estrelas** de cada jogador (★ chegou, ★ Bem-Estar ≥ 60, ★ tempo ≤ alvo), com animação.
4. **Detalhes** recolhidos atrás de um botão: tempo, Bem-Estar final, comidas por família, atalhos usados.
5. Botões: **Revanche** (foco padrão) · Próxima fase · Menu. Na copa: **Continuar** e o placar.

- **Custo:** 3 dias (frase de "por quê" usa os eventos que já existem: coleta, chegada, fadiga,
  estado do lanche). **Aceite:** em playtest, perguntados "por que ganhou?", jogadores respondem
  com a frase da tela ou equivalente.

### U6 — Como jogar em 3 cartões + treino · P1

Três cartões ilustrados, sem parágrafos:

1. **Corra até a sua porta.** (broto, seta, porta da cor do jogador)
2. **Lanche = impulso agora, sono depois.** (hambúrguer → raio → "Z")
3. **Fruta e água acordam.** (fruta cortando o "Z")

Botão "Treinar agora" abre a fase-tutorial. O texto dos controles fica em Opções › Controles.
**Custo:** 1 dia. **Aceite:** pessoa nova explica as três regras depois de ver os cartões (pergunta do playtest).

### U7 — Opções: controles, assistência, texto · P1

- **Controles:** remapear Mover/Pular de cada jogador; escolher dispositivo (teclado esquerdo,
  teclado direito, controle 1, controle 2). O Input System já suporta *rebinding* e esquemas.
- **Assistência por jogador** (padrão, mudável no "Prontos?"): velocidade do jogo 80%, sem fadiga,
  espinho só empurra. Resultado mostra um selo discreto.
- **Acessibilidade:** texto maior (×1,25), reduzir flashes e tremores (existe), vibração do controle.
- Tudo persistido (`PlayerPrefs` já é usado).
- **Custo:** 3 dias. **Aceite:** remapeamento sobrevive a reiniciar o jogo; teste de cada assistência.

### U8 — Créditos · P2

Autores, docente (se houver), fontes, ferramentas, origem e licença das artes e sons. Custo 0,5
dia. Obrigatório antes de distribuir.

## 4. Princípios de interface (regras para qualquer tela nova)

| Princípio | Regra verificável |
| --- | --- |
| Hierarquia | Uma informação principal por tela, com o dobro do tamanho da segunda |
| Feedback imediato | Toda ação do jogador tem resposta visual em ≤ 100 ms e sonora quando relevante |
| Linha do olhar | Estado do jogador (pico, queda, fadiga) aparece a ≤ 2 tiles do personagem |
| Pouca poluição | HUD com ≤ 3 elementos por jogador durante a corrida |
| Consistência | Mesmos botões (forma, cor, foco) em todas as telas; cor de P1 e P2 igual em HUD, porta, retrato |
| Legibilidade | Texto ≥ 24 px em 1080p; contraste ≥ 4,5:1 sobre o fundo |
| Controles simples | Toda tela navegável com setas/Enter, WASD/Espaço e controle |
| Sem texto em outra língua | Nenhuma palavra em inglês na interface |

## 5. Explicação dos efeitos dos alimentos

A explicação vem em três camadas, nesta ordem, para não virar aula:

1. **No controle:** o efeito é sentido (pico, queda) — gameplay F1.
2. **No instante:** ícone flutuante na coleta (raio, "Z", coração) — gameplay F5.
3. **Depois:** a frase de "por quê" no resultado e os cartões de "Como jogar".

Nenhuma tela entre fases com texto educativo, quiz ou lição (ver `EVOLUCAO_REFERENCIAS.md` §7.5).

## 6. Critérios de aceite do conjunto

- Um par de pessoas que nunca jogou sai do menu e começa a primeira corrida em menos de 60 s,
  sem ajuda.
- Ninguém pergunta "qual sou eu?" depois do "Prontos?".
- Depois de uma partida, os dois explicam o que o lanche faz.
- Todas as telas capturadas em 1280×720 e 1920×1080 sem sobreposição (atualizar `VisualEvidence`).
