# Evolução — visão do produto

Base: auditoria em `EVOLUCAO_AUDITORIA.md`. Este documento define **qual experiência** o
Healthy Race entrega; os outros documentos dizem como chegar lá.

## O que o jogo é hoje, em uma frase

Uma corrida de plataforma para duas pessoas no mesmo teclado em que a comida vira pontos e o
atalho é sempre a escolha errada.

## Pitch proposto

> **Uma corrida de plataforma para duas pessoas no sofá em que o que você come muda como você corre.**

Variante para o banner e a loja: *"Pegue o atalho do lanche e voe — até a energia acabar."*

## Público-alvo

| Segmento | Por que | O que exige do jogo |
| --- | --- | --- |
| **Primário:** pares de 10 a 16 anos (irmãos, colegas, amigos) jogando no mesmo computador | É o contexto real de uso: feira de projetos, escola, casa, um teclado | Regras entendidas em 1 minuto, partida de 2 a 3 min, revanche imediata |
| **Secundário:** adulto + criança (família, professor + aluno) | O tema ODS 3 abre a porta da escola e da família | Assistência para o jogador menos hábil sem humilhar ninguém |
| **Contexto de avaliação:** banca e visitantes da feira | Decidem em 30 s se o jogo é bom | Tela inicial e primeira fase precisam vender o conceito sozinhas |

Não é público: jogadores de plataforma de precisão hardcore. O jogo pode recompensar habilidade,
mas não pode exigi-la para ser divertido.

## Perguntas de identidade

**Qual é o diferencial?** A comida é a mecânica, não o colecionável. Jogos de plataforma
competitivos (Speedrunners, Ultimate Chicken Horse, Pico Park) não usam o que o personagem
come como recurso de corrida; jogos sobre alimentação raramente são de ação. O Healthy Race
mora nesse cruzamento. Hoje o diferencial existe só no papel, porque a comida não muda o jogo
de quem fica no caminho longo.

**O que faz voltar?** A revanche com a mesma pessoa ("agora eu pego o atalho na hora certa") e
a variação entre fases. Um placar de "copa" (3 fases seguidas) dá motivo para continuar perdendo
uma fase; recordes por fase dão motivo para jogar sozinho ou repetir.

**Qual emoção principal?** **Rivalidade divertida com reviravolta** — o "quase!" de uma ultrapassagem
no último andar. Emoção secundária: a satisfação de uma escolha que deu certo
("peguei o lanche, voei e cheguei antes").

**Competição, casual, estratégia ou habilidade?** Competição **casual** com **uma decisão
estratégica por bifurcação** e **habilidade que conta, mas não decide sozinha**. Em ordem:
competição > decisão > habilidade.

**Como o tema entra sem virar aula?** Pela regra que se sente no controle, não pelo texto:

- o lanche rápido dá um impulso real **e** cobra depois (o jogador sente a queda);
- frutas, água e refeições equilibradas dão efeitos menores e estáveis;
- o fim da fase mostra, em uma linha, o que a escolha fez ("seu impulso acabou no espinho").

O jogo não chama comida de "boa" ou "ruim". Ele mostra consequências. É o mesmo princípio do
*Guia Alimentar para a População Brasileira* (Ministério da Saúde, 2014), que orienta preferir
alimentos in natura e evitar ultraprocessados sem proibir nada — referência sugerida para a
equipe conferir antes de citar no jogo.

**O que deve tornar o jogo reconhecível?**

- *Mecanicamente:* a bifurcação "corredor da lanchonete × estufa" e a curva "pico e queda" do lanche.
- *Visualmente:* os brotos (VITA) com folha na cabeça que murcha ou cresce conforme o Bem-Estar,
  em duas silhuetas distintas para P1 e P2; a estufa como cenário-tema com uma cor e um marco por fase.

## Pilares de design

As cinco hipóteses do briefing foram avaliadas contra o jogo existente. Três viraram pilares
como estão, duas foram fundidas e uma nova entrou.

| Hipótese do briefing | Decisão | Motivo |
| --- | --- | --- |
| Competição divertida | **Mantida**, como pilar 1 | É o que o jogo já é |
| Estratégia | **Mantida e afinada** como pilar 2 | Hoje a decisão não existe (atalho dominado) |
| Identidade (alimentação como universo) | **Fundida** no pilar 3 | Só faz sentido se a comida for mecânica |
| Habilidade | **Mantida** como pilar 4, com limite | O público é casual; habilidade não pode decidir sozinha |
| Rejogabilidade | **Rebaixada a objetivo**, não pilar | Decorre dos pilares 1-3 + copa e recordes; não guia decisões de mecânica |
| — | **Novo:** legibilidade (pilar 5) | Câmera compartilhada e duas pessoas na mesma tela exigem que tudo se leia de relance |

### Pilar 1 — Corrida lado a lado

Os dois jogadores devem se ver, se alcançar e se ultrapassar. A vitória precisa se decidir
na corrida, não na contagem de itens.

*Teste do pilar:* em playtest, a maioria das partidas tem pelo menos uma troca de liderança e
quem cruza a porta primeiro vence na maioria das vezes (ver metas em `EVOLUCAO_QUALIDADE.md`).

### Pilar 2 — Escolha com preço visível

Cada bifurcação oferece uma vantagem imediata e um custo depois. Nenhuma opção pode ser sempre
a certa. O jogador vê o preço antes de pagar.

*Teste do pilar:* nenhuma rota é escolhida em mais de ~75% das bifurcações por jogadores que já
entenderam as regras; o robô mede que o curto é mais rápido sem efeitos e mais lento se o jogador
errar o tempo do impulso.

### Pilar 3 — Comida é poder, não sermão

Todo alimento muda o personagem de um jeito que se sente no controle. Os efeitos são simples,
consistentes e coerentes com o tema, sem afirmações nutricionais.

*Teste do pilar:* depois de uma partida, o jogador consegue dizer o que o lanche e a fruta fazem,
sem ter lido nada.

### Pilar 4 — Controle que dá orgulho

O movimento responde na hora e permite jogar melhor (pulo curto e longo, aterrissagem precisa),
mas as fases não exigem precisão de especialista para chegar ao fim.

*Teste do pilar:* um jogador que nunca jogou plataforma termina a Fase 1 sem ajuda; um experiente
encontra linhas mais rápidas.

### Pilar 5 — Tudo se lê de relance

Em qualquer instante, cada jogador sabe onde está, quem está na frente, o que acabou de comer e
o que isso fez. Informação importante fica perto do personagem, não no canto da tela.

*Teste do pilar:* em playtest, nenhuma pessoa pergunta "qual sou eu?" depois do primeiro minuto.

## Experiência desejada (uma partida)

1. **0:00** — Os dois saem lado a lado, cada broto com sua cor e forma. A primeira bifurcação
   aparece em 5 segundos.
2. **0:10** — Um pula para a estufa; o outro cai no corredor, come um hambúrguer, dispara com o
   impulso e passa na frente. Os dois veem o placar de posição mudar.
3. **0:40** — O impulso acaba no meio de um trecho de lama: quem pegou o atalho fica lento, o
   outro alcança. Primeira reviravolta.
4. **1:30** — Último andar, os dois perto. Uma decisão final (atalho ou não) decide quem chega.
5. **2:00** — Tela de resultado mostra quem venceu e **por quê** em uma frase, com destaque para
   a escolha decisiva. Botão de revanche em foco.

## Proposta de valor

- **Para quem joga:** uma rivalidade curta, fácil de entender e com reviravolta, para jogar com
  alguém ao lado.
- **Para a escola e a feira:** um jogo sobre o ODS 3 que comunica pela experiência — o
  jogador sente a diferença entre energia rápida e energia estável, em vez de ler sobre ela.
- **Para a equipe:** um projeto com base técnica sólida (fases como dados, robô de testes)
  onde cada fase nova custa pouco.

## Diferenciais frente a outros plataformas competitivos

| Jogo | O que faz | O que o Healthy Race faz diferente |
| --- | --- | --- |
| Speedrunners | Corrida com eliminação pela borda da câmera e itens de ataque | Sem eliminação nem ataque; a disputa vem da escolha de rota e de recurso |
| Ultimate Chicken Horse | Jogadores constroem a fase uns contra os outros | Fases autorais; a escolha é de rota e alimentação, não de construção |
| Pico Park | Cooperação obrigatória | Competição: ninguém depende do outro |
| Mario Kart (referência de gênero) | Itens aleatórios e ajuda a quem está atrás | Efeitos determinísticos e escolhidos pelo jogador; ajuda a quem está atrás só se o playtest mostrar que é preciso |

## Decisões desta visão (aprovadas em 11/11/2026, opção recomendada)

| # | Decisão | Recomendação | Alternativa |
| --- | --- | --- | --- |
| V1 | O jogo é uma **corrida** (vence quem chega) ou um **placar** (vence quem soma mais)? | **Corrida.** Pontos viram estrelas de desempenho e placar de copa | Manter placar, rebalanceado para a chegada pesar ≥ 50% |
| V2 | O atalho deve ser **às vezes certo**? | **Sim**: impulso agora, queda depois | Manter como castigo e tirar o discurso de "escolha" |
| V3 | Os dois jogadores disputam a **mesma comida**? | **Não** nos itens comuns (cada um tem a sua cópia); **sim** num item raro por andar | Manter compartilhada e medir a bola de neve no playtest |
| V4 | Público principal de 10 a 16 anos | **Sim** | Público geral, sem ajuste de leitura e assistência |

Detalhe das alternativas e dos custos em `EVOLUCAO_GAMEPLAY.md` e `EVOLUCAO_BALANCEAMENTO.md`.
