# Evolução — referências e benchmarking

Pesquisa feita em 10/11/2026. Marcação usada:

- **[Lido]** — veio da página citada, aberta nesta pesquisa.
- **[Geral]** — conhecimento geral, não conferido numa fonte nesta pesquisa.
- **[Proposta]** — adaptação ao Healthy Race.

**Limitação:** os sites oficiais de Ultimate Chicken Horse, Overcooked e Celeste não abriram
neste ambiente (erro de certificado TLS na rede, não dos sites). Usei fontes secundárias,
indicadas em cada item. Nenhuma mecânica, arte ou fase deve ser copiada: o objetivo é entender
o princípio e adaptar.

## Resumo: o que cada referência resolve no Healthy Race

| Problema do Healthy Race (ver auditoria) | Referência | Princípio |
| --- | --- | --- |
| Atalho sempre errado | UCH, MDA | Risco tem de pagar às vezes; medir a escolha |
| Coleta decide, corrida não; bola de neve | MDA (exemplo do Monopoly), UCH, Mario Kart | Feedback claro de quem ganha; recuperação declarada, nunca escondida |
| Pulo de altura fixa, controle pouco expressivo | Celeste, Swink | Gravidade no ápice, pulo variável, correção de quina, polimento |
| Câmera afasta demais | Overcooked, SpeedRunners | Layout compacto; regra explícita para quem sai da tela |
| Fases iguais, tudo apresentado de uma vez | Kishōtenketsu, Unity Level Design | Uma mecânica por fase, em quatro tempos; marcos visuais |
| Tema como número, risco de sermão | Re-Mission, "brócolis com chocolate" | Comportamento de saúde dentro da mecânica |
| Teclado compartilhado, só cor diferencia | Game Accessibility Guidelines | Remapear, mais de um dispositivo, não depender só de cor |

## 1. Ultimate Chicken Horse

Fontes: [suporte oficial da Clever Endeavour, "Custom rules and presets"](https://cleverendeavourgames.freshdesk.com/support/solutions/articles/32000028991-custom-rules-and-presets),
[Wikipedia](https://en.wikipedia.org/wiki/Ultimate_Chicken_Horse). Site pedido:
<https://www.cleverendeavourgames.com/ultimate-chicken-horse> (não abriu).

1. **Solução que merece atenção:** a pontuação por tipo de feito. [Lido] Há pontos por chegar,
   por ser o único a chegar (*Solo*), por chegar depois de morrer, por armadilha, por moeda, pela
   ordem de chegada e um ponto de *Comeback* para quem não chega há 2 rodadas. [Lido] Se todos
   ou ninguém chegam, ninguém pontua (presets "Too easy, no points!" e "No winners, no points!").
2. **Por que funciona [Geral]:** a regra do "fácil demais = zero" empurra o jogo para o ponto
   em que o risco paga. O bônus Solo premia a escolha contrária à do grupo. O Comeback é
   **declarado**: todo mundo sabe que existe, então não parece trapaça.
3. **O que aprender:** uma escolha só é escolha se às vezes paga; recuperação aceitável é a que
   está nas regras visíveis.
4. **Adaptação [Proposta]:**
   - registrar a rota escolhida em cada bifurcação (telemetria local) e rebalancear a que passar
     de ~75-80% para um lado — limiar de partida, a calibrar;
   - bônus de "rota diferente": quem escolhe o caminho oposto ao do rival e chega primeiro naquele
     trecho ganha uma estrela de fase;
   - qualquer ajuda a quem está atrás aparece na tela antes da largada.
5. **Custo · impacto:** telemetria de rota baixo · alto; bônus declarados baixo · médio.

## 2. Overcooked

Fontes: [postmortem na MCV/Develop, 26/06/2017](https://mcvuk.com/development-news/the-develop-post-mortem-overcooked/),
[Wikipedia](https://en.wikipedia.org/wiki/Overcooked). Site pedido:
<https://www.team17.com/games/overcooked> (não abriu).

1. **Solução que merece atenção:**
   - [Lido] O tema escolhido porque a cozinha é uma boa analogia para coordenação: o tema *é* a mecânica.
   - [Lido] O protótipo tinha vidas; a equipe trocou por cronômetro depois de ver o público em feiras.
   - [Lido] Layouts compactos de propósito, **para a câmera não recuar demais**.
   - [Lido] Quando um jogador resolvia a fase repetindo uma tarefa, a equipe mudava o layout.
   - [Lido] Três estrelas por limiar de pontos em cada fase; cada cozinha tem um elemento próprio
     (faixa de pedestres, caminhões, iceberg).
2. **Por que funciona [Geral]:** ninguém lê que trabalho em equipe importa; a cozinha força isso.
   As estrelas dão meta de repetição que não depende de vencer o outro. O elemento próprio de cada
   cozinha torna cada fase memorável.
3. **O que aprender:** (a) o tema precisa forçar o comportamento, não descrevê-lo; (b) feira e
   desconhecidos jogando são o teste que importa; (c) a câmera compartilhada manda no layout.
4. **Adaptação [Proposta]:**
   - cada fase com **um elemento próprio** e um marco visual (ver `EVOLUCAO_GAMEPLAY.md` §5);
   - bifurcações que **reconvergem dentro do enquadramento máximo** da câmera;
   - três estrelas por jogador por fase, na tela de resultado.
5. **Custo · impacto:** estrelas baixo · médio; geometria das bifurcações médio · alto.

## 3. Celeste

Fontes: [Maddy Thorson, "Celeste & Forgiveness"](https://maddymakesgames.com/articles/celeste_and_forgiveness/),
[Vice, 16/09/2019, sobre o Assist Mode](https://www.vice.com/en/article/celeste-assist-mode-change-and-accessibility),
[GAG, estudo de caso do Assist Mode](https://gameaccessibilityguidelines.com/celeste-assist-mode).
Site pedido: <https://www.celestegame.com/> (não abriu).

1. **Solução que merece atenção [Lido]:** os truques de tolerância que a autora descreve —
   coyote time, buffer de pulo, **meia gravidade no ápice enquanto o botão está segurado**,
   **correção de quina ao bater a cabeça**, plataforma semi-sólida que "sobe" quem a atravessa de
   lado, herança de impulso de plataforma móvel. O texto resume a intenção: o jogo quer que você
   consiga. E o Assist Mode (velocidade do jogo, invencibilidade e outros), cujo texto foi revisto
   para não tratar quem usa como menos capaz.
2. **Por que funciona [Geral]:** o jogador sente que acertou o que quis fazer; o erro é dele, não
   do controle. Reinício quase instantâneo torna o erro barato.
3. **O que aprender:** o Healthy Race já tem coyote time (0,10 s) e buffer (0,12 s). Falta o que
   torna o pulo **expressivo**: altura variável e um ápice mais demorado, e a correção de quina.
4. **Adaptação [Proposta]:**
   - pulo variável (soltar cedo corta a subida) e gravidade menor no ápice com o botão segurado;
   - correção de quina de até ~0,25 tile;
   - espinho: volta ao checkpoint em menos de 1 s, sem tela de morte (já é assim; manter);
   - **assistência por jogador**, não global: pai e filho jogam juntos com ajustes diferentes;
     a tela de resultado mostra um selo discreto, sem proibir.
5. **Custo · impacto:** movimento baixo · alto (com risco médio: muda as rotas, os testes pegam);
   assistência por jogador médio · médio-alto.

## 4. MDA — Mechanics, Dynamics, Aesthetics

Fonte: [Hunicke, LeBlanc e Zubek, PDF](http://users.cs.northwestern.edu/~hunicke/MDA.pdf) (o link
pedido redireciona para este). Lido inteiro.

1. **Solução que merece atenção [Lido]:** mecânicas são regras e dados; dinâmicas, o
   comportamento que surge delas com o jogador ao longo do tempo; estéticas, as respostas
   emocionais. O designer vê M → D → A; o jogador, A → D → M. O artigo lista oito estéticas
   (sensação, fantasia, narrativa, desafio, companheirismo, descoberta, expressão, passatempo),
   sem pretender esgotá-las, e usa "competição" no exemplo do Quake.
2. **Por que serve [Lido]:** o artigo diz que, num jogo competitivo, o jogador precisa ver
   claramente quem está ganhando, e usa o Monopoly como exemplo de líder que fica cada vez mais
   forte até a tensão acabar; as correções sugeridas são subsídio para quem está atrás ou taxa
   para o líder.
3. **O que aprender:** o Healthy Race tem **dois laços de reforço** como o do Monopoly: (a) comida
   compartilhada — quem está na frente pega tudo; (b) Bem-Estar baixo → mais lento → mais erros →
   fadiga. Os dois punem quem já está perdendo.
4. **Adaptação [Proposta]:** estéticas-alvo: **competição, desafio e companheirismo** (de sofá).
   Cada mecânica proposta nos outros documentos traz a sua linha M → D → A. Exemplo atual:

   | Mecânica | Dinâmica observada | Estética resultante |
   | --- | --- | --- |
   | Fast-food: −Bem-Estar, peso 5 s, −100 pontos | Ninguém entra no corredor de propósito | Nenhuma: escolha morta |
   | Comida única e compartilhada | Líder coleta, perseguidor encontra vazio (a validar) | Frustração de quem está atrás |
   | Alavanca abre o portão para os dois | Perseguidor aproveita o portão aberto | Alívio involuntário |

5. **Custo · impacto:** método, custo baixo · impacto alto.

## 5. Game Accessibility Guidelines

Fonte: [lista completa](https://gameaccessibilityguidelines.com/full-list/). Itens básicos e
intermediários que se aplicam a um jogo 2D local no teclado [Lido]:

| Área | Nível | Item (texto original) | Situação no Healthy Race |
| --- | --- | --- | --- |
| Motor | Basic | Allow controls to be remapped / reconfigured | Ausente |
| Motor | Basic | Include an option to adjust the game speed | Ausente |
| Motor | Intermediate | Support more than one input device | Ausente (só teclado) |
| Motor | Intermediate | Avoid / provide alternatives to requiring buttons to be held down | Atende |
| Visão | Basic | Ensure no essential information is conveyed by a fixed colour alone | **Não atende**: P1/P2 só por cor |
| Visão | Basic | Use an easily readable default font size | **Não atende**: HUD com ~10 px |
| Visão | Basic | Provide high contrast between text/UI and background | Parcial |
| Visão | Intermediate | Avoid placing essential temporary information outside the player's eye-line | **Não atende**: estado só no HUD do alto |
| Audição | Basic | Ensure no essential information is conveyed by sounds alone | Parcial |
| Audição | Basic | Provide separate volume controls… | Atende |
| Cognição | Basic | Include interactive tutorials | Ausente |
| Cognição | Basic | Avoid flickering images and repetitive patterns | Atende ("reduzir flashes") |
| Cognição | Intermediate | Include a means of practicing without failure… | Ausente |
| Geral | Basic | Offer a wide choice of difficulty levels | Ausente |
| Geral | Basic | Ensure that all settings are saved/remembered | Atende |

Observações [Lido]: nenhum item básico ou intermediário trata de multijogador local; precisão de
tempo só aparece no nível avançado.

- **Adaptação [Proposta]:** remapear por jogador, suporte a controle (Input System já permite),
  P1 e P2 por forma e número além da cor, estado do jogador perto do personagem, fase de treino
  sem falha, assistências por jogador.
- **Risco específico [Geral]:** *ghosting* — teclados de membrana não registram algumas combinações
  de 3 ou mais teclas. Com dois jogadores no mesmo teclado vira "o jogo não pulou". Testar no
  teclado da feira.
- **Custo · impacto:** baixo a médio · alto.

## 6. Unity — Level design basics

Fonte: [blog da Unity](https://unity.com/blog/level-design-basics) [Lido].

1. **Solução que merece atenção:** caminho visível na largada; contraste e cor consistente para
   o que é interativo; linhas-guia só onde há escolha real; trilhas de coletáveis como guia;
   marcos grandes e distintos; mecânica nova em três passos (mostrar, testar em lugar seguro,
   desafiar); testar cedo observando outras pessoas.
2. **Por que funciona [Geral]:** o jogador lê o espaço antes de ler o texto.
3. **O que aprender:** o Healthy Race usa placas de texto onde a fase deveria falar sozinha
   ("↓ CURTO / → LONGO", "PUXE A ALAVANCA").
4. **Adaptação [Proposta]:**
   - a porta de chegada (ou um marco no topo da estufa) visível ou anunciada na largada;
   - saudáveis como trilha do caminho longo — já são, falta contraste;
   - **as duas saídas da bifurcação visíveis 1 a 2 s antes da decisão**, com a vantagem e o custo
     desenhados (ícone de lanche + relógio, ícone de fruta + obstáculo), não escritos;
   - cada obstáculo novo apresentado primeiro sem risco.
5. **Custo · impacto:** baixo · alto.

## 7. Referências adicionais

### 7.1 Kishōtenketsu (Super Mario 3D Land)

Fonte: [Christian Nutt, Game Developer, 13/04/2012](https://www.gamedeveloper.com/design/the-structure-of-fun-learning-from-i-super-mario-3d-land-i-s-director).
[Lido] Koichi Hayashida descreve fases em quatro tempos: apresentar, desenvolver, uma reviravolta
e demonstrar domínio.

- **Adaptação [Proposta]:** cada fase é "dona" de um elemento, apresentado nos quatro tempos
  (ex.: plataforma que cai sobre o chão → sobre espinhos → junto com o elevador → tudo no último andar).
- **Custo · impacto:** baixo (organização de conteúdo no gerador) · alto.

### 7.2 Steve Swink — Game Feel

Fonte: [Game Developer, 23/11/2007](https://www.gamedeveloper.com/design/game-feel-the-secret-ingredient).
[Lido] Seis componentes: entrada, resposta, contexto, polimento, metáfora e regras; polimento é a
"impressão interativa de fisicalidade" (partículas, tremor, *squash and stretch*).

- **Adaptação [Proposta]:** os estados do personagem (impulso, queda, saciado, fadiga) precisam
  ser sentidos — animação, poeira, som abafado — e não só um número de velocidade.
- **Custo · impacto:** médio (arte e áudio) · alto.

### 7.3 SpeedRunners — câmera compartilhada em corrida

Fonte: [Wikipedia](https://en.wikipedia.org/wiki/SpeedRunners). [Lido] A câmera segue o líder e
quem sai da área é eliminado; a área encolhe com o tempo; vence quem sobra.

- **Adaptação [Proposta]:** eliminar é duro demais para o público do Healthy Race. Escala
  sugerida: afastar até um limite (não a fase inteira) → seta na borda indicando o jogador fora
  da tela → em último caso, trazer o retardatário para a borda com pequena perda de tempo.
- **Custo · impacto:** médio · alto.

### 7.4 Mario Kart e a recuperação de quem está atrás

Fonte: só trechos de busca de análises (Extra Credits, Nintendo World Report) — tratar como
**[Geral]**. O sorteio de itens favorece quem está atrás; a crítica recorrente é que o líder
vira alvo previsível e se sente punido por jogar bem.

- **Lição:** recuperação escondida irrita; declarada é aceita (ver UCH).
- **Adaptação [Proposta]:** primeiro medir se há bola de neve; se houver, subsídio visível para
  quem está atrás, nunca punição ao líder. Não recomendado aplicar antes do playtest.

### 7.5 Re-Mission e o "brócolis com chocolate"

Fontes: trechos de busca sobre Kato et al., *Pediatrics* 122(2), 2008 — ensaio randomizado em que
o jogo melhorou adesão ao tratamento e autoeficácia; conferir no original antes de citar números.
"Chocolate-covered broccoli" é a crítica a jogos educativos que embrulham conteúdo chato em
diversão (atribuída a Amy Bruckman, 1999, e popularizada por Brenda Laurel; as fontes divergem).

- **O que aprender:** no Re-Mission o comportamento de saúde está dentro da mecânica. No Healthy
  Race, hoje, está no placar.
- **Riscos para o Healthy Race [Proposta]:**
  - Se o caminho saudável for só o mais longo e penoso, o jogo ensina "saudável = sofrimento".
    O caminho longo tem de ser o mais **divertido**.
  - **"Pesado" associa comida a peso corporal** e pode soar gordofóbico para um público escolar.
    Recomendação: trocar o enquadramento por um estado agudo e temporário — "empanzinado" ou
    "sonolento depois do lanche" (bocejo, olhos pesados), nunca corpo maior. Decisão em
    `ROADMAP_PRODUTO_FINAL.md`.
  - Nada de quiz nem texto entre fases. No máximo uma frase na tela de resultado, ligada ao que
    aconteceu na corrida.
- **Custo · impacto:** baixo · alto na recepção.

## Prioridade sugerida a partir das referências

| Ação | Custo | Impacto | Origem |
| --- | --- | --- | --- |
| Telemetria de rota e rebalanceamento da bifurcação | Baixo | Alto | UCH, MDA |
| Pulo variável, ápice mais longo, correção de quina | Baixo | Alto | Celeste |
| Remapeamento, controle, P1/P2 sem depender de cor | Baixo-médio | Alto | GAG |
| Uma mecânica por fase em quatro tempos + marco visual | Baixo-médio | Alto | Kishōtenketsu, Unity, Overcooked |
| Regra de câmera para quem se afasta | Médio | Alto | Overcooked, SpeedRunners |
| Reenquadrar "pesado" como estado temporário | Baixo | Alto | Re-Mission / brócolis com chocolate |
| Estrelas por jogador por fase | Baixo | Médio | Overcooked |
| Assistência por jogador | Médio | Médio-alto | Celeste, GAG |
| Polimento dos estados do personagem | Médio | Alto | Swink |
