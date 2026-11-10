# Relatório de testes — versão 1.0.0

Data: 10/11/2026. Editor: Unity 6000.3.23f1, Windows 11, modo batch (sem som).

## Resumo

| Suíte | Testes | Passaram | Falharam |
| --- | --- | --- | --- |
| EditMode | 233 | 233 | 0 |
| PlayMode | 91 | 91 | 0 |
| Build Windows 64-bit (Mono) | — | sucesso | — |

No GitHub, o CI roda lint de Markdown e YAML, formatação de C# (CSharpier) e validação do
repositório. Os testes do Unity no CI ficam pulados enquanto o repositório não tiver a
licença (`UNITY_LICENSE`); os números acima são da execução local.

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
| 1 | Corrida do Equilíbrio | 125,0 s | 140,6 s | 5980 | -3050 |
| 2 | Horta Suspensa | 95,5 s | 115,7 s | 4810 | -2742 |
| 3 | Pomar das Alavancas | 127,9 s | 139,4 s | 6460 | -3039 |
| 4 | Estufa dos Espinhos | 138,6 s | 153,0 s | 5730 | -3064 |
| 5 | Ponte de Folhas | 142,1 s | 166,9 s | 4860 | -5448 |
| 6 | Feira Livre | 159,2 s | 176,5 s | 6590 | -3676 |
| 7 | Torre das Trepadeiras | 133,3 s | 163,1 s | 6210 | -3551 |
| 8 | Mercado Comprido | 131,4 s | 146,3 s | 4890 | -2807 |
| 9 | Labirinto da Lama | 176,8 s | 185,2 s | 4640 | -4037 |
| 10 | Grande Final | 173,6 s | 209,0 s | 7540 | -4346 |

Partindo de Bem-Estar zero, na Fase 1: longo 126,4 s, curto 141,2 s.

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
