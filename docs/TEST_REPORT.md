# Relatório de testes — versão 1.0.1

Data: 11/11/2026. Editor: Unity 6000.3.23f1, Windows 11, modo batch (sem som).

## Resumo

| Suíte | Testes | Passaram | Falharam |
| --- | --- | --- | --- |
| EditMode | 243 | 243 | 0 |
| PlayMode | 83 | 83 | 0 |
| Build Windows 64-bit (Mono) | — | sucesso | — |

No GitHub, o CI roda lint de Markdown e YAML, formatação de C# (CSharpier) e validação do
repositório. Os testes do Unity no CI ficam pulados enquanto o repositório não tiver a
licença (`UNITY_LICENSE`); os números acima são da execução local.

## Mudança na contagem desde a 1.0.0

A 1.0.0 tinha 233 testes EditMode e 91 PlayMode. Na 1.0.1:

- **saíram 18 testes** do protótipo cooperativo removido (caixa empurrável, botão de pressão,
  controlador cooperativo e a cena `VerticalSlice_Core`): 10 EditMode e 8 PlayMode;
- **entraram 20 testes EditMode**: distância igual para P1 e P2 nas 10 fases (10), balanceamento
  em asset (3), progresso pela rota, registro de partida e progresso salvo (7).

## O que é testado

- **Mapas das 10 fases** (`LevelTests`, EditMode), para cada fase:
  - moldura fechada, largadas, portas e tema;
  - 1,5 tile livre acima de cada piso;
  - fast-food só no corredor curto e saudáveis só fora dele;
  - cada jogador chega sozinho à própria porta;
  - o caminho longo existe sem tocar o corredor;
  - cada fast-food leva à porta;
  - não dá para pular dentro do corredor.

  Também confere que os 10 mapas são diferentes entre si.
- **Rotas** (`LevelRoutes`): busca na grade com o orçamento de pulo do pior Bem-Estar
  (degrau e vão de 1 tile); pulos com vão exigem o arco inteiro livre.
- **Montagem** (`LevelBootstrap`): contagem de cada elemento do mapa, tudo dentro do
  retângulo da fase, terreno num colisor único, parallax ladrilhado, placas legíveis.
- **Mecanismos com física** (`LevelPlayModeTests`): rampa subida andando, elevador que se
  entra andando e que leva ao andar de cima, alavanca que credita o mecanismo e abre o portão
  depois da espera, peso do fast-food que passa depois de 5 s.
- **Corrida completa nas 10 fases**: um robô leva o P1 pelo caminho longo e o P2 pelo curto
  comendo todo o fast-food, com os mesmos comandos de um jogador (andar, pular, esperar o
  elevador). Ele refaz a rota quando cai de uma plataforma. O teste exige que os dois
  terminem e que o curto chegue depois do longo.
- **Sistemas:** Bem-Estar, fadiga, pontuação e desempate, áudio, HUD, fluxo de menu, pausa e
  próxima fase, fim de partida garantido, personagem e animações.

## Corridas medidas

Tempos de jogo do robô (ele para no centro de cada tile antes de pular; uma pessoa faz o
longo mais rápido).

| Fase | Nome | Longo (P1) | Curto (P2) | Pontos longo | Pontos curto |
| --- | --- | --- | --- | --- | --- |
| 1 | Corrida do Equilíbrio | 125,7 s | 141,4 s | 5980 | -3058 |
| 2 | Horta Suspensa | 96,2 s | 116,1 s | 4810 | -2747 |
| 3 | Pomar das Alavancas | 128,6 s | 139,7 s | 6460 | -3043 |
| 4 | Estufa dos Espinhos | 139,3 s | 153,3 s | 5730 | -3067 |
| 5 | Ponte de Folhas | 142,7 s | 167,1 s | 4860 | -5450 |
| 6 | Feira Livre | 159,9 s | 176,9 s | 6589 | -3679 |
| 7 | Torre das Trepadeiras | 133,9 s | 163,5 s | 6210 | -3554 |
| 8 | Mercado Comprido | 132,1 s | 146,9 s | 4890 | -2811 |
| 9 | Labirinto da Lama | 177,4 s | 185,6 s | 4640 | -4040 |
| 10 | Grande Final | 174,2 s | 209,1 s | 7540 | -4346 |

Partindo de Bem-Estar zero, na Fase 1: longo 127,1 s, curto 141,5 s.

## Defeitos encontrados pelo robô e corrigidos

| Defeito | Correção |
| --- | --- |
| Parado em rampa de 45° depois de um pulo, o jogador não contava como no chão e não pulava | O sensor de chão aceita uma folga extra só sobre piso inclinado |
| Elevador em repouso 0,3 acima do piso, virando degrau | Repousa rente ao piso |
| Elevador deixava buraco no piso ao subir e esmagava quem esperava ao descer | Piso fixo embaixo, plataforma de mão única e sensor que só conta quem está em cima |
| Plataforma móvel parada com um passo de física por quadro | Lê a posição do corpo, não do transform interpolado |
| Gatilho dos espinhos maior que o desenho | Do tamanho das pontas |
| Último andar baixo demais para pular; porta do P2 sobre um vão | Fase com 2 linhas a mais; piso sob as portas |
| Animação de empurrar nunca disparava contra parede | A sonda cruza a borda do corpo |
| Espinhos com renascimento padrão em (0, 0) | A largada é calculada antes dos perigos |
| Lama apagava o efeito do Bem-Estar na velocidade | Multiplicador de terreno separado |

## Não coberto

- Partidas com pessoas: ritmo, legibilidade e diversão não são medidos pelo robô.
- Desempenho em máquinas mais fracas.
- Testes do Unity no CI (falta a licença no repositório).
