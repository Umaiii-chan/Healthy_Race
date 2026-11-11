# Notas da versão

## Em desenvolvimento — regras novas (Fase B do roadmap)

Não lançada. Só deve ser mesclada depois do teste com pessoas na 1.0.1 (decisão D0).

- **Lanche com pico e queda** (D2): impulso de 2 s e depois sonolência de 4 s; encadear renova o
  pico e alonga a queda. Com Bem-Estar baixo, o pico encurta e a queda alonga. Sai o efeito "pesado".
- **Fruta, água ou refeição acordam** da queda na hora.
- **Comida por jogador** (D3): cada item pode ser pego uma vez por jogador; depois da primeira
  coleta ele fica apagado, na cor de quem falta.
- **Vence quem chega primeiro** (D1), com desempate por Bem-Estar e fadigas se chegarem no mesmo
  instante; até **3 estrelas** por jogador (chegada, Bem-Estar ≥ 60, tempo-alvo). A tela de
  resultado diz em uma frase por que a corrida terminou assim.
- **Contagem de largada** "3, 2, 1, JÁ!"; o tempo de chegada passa a usar o relógio da partida.
- **Fadiga anunciada:** 1 s de aviso com o jogador ainda andando, depois 1,2 s de parada.
- **Feedback de coleta:** nome do alimento em português, mudança de Bem-Estar e efeito sobem do
  personagem.
- **Ainda não feito desta fase:** cesta dourada disputada, bifurcação com ícones e prévia de
  câmera, P1 e P2 com silhuetas diferentes, correção de quina, pulo variável e câmera com limite.
- **Em aberto:** para o robô, o atalho comendo tudo ainda é mais rápido que o longo na maioria
  das fases; ver `TEST_REPORT.md`.

## 1.0.1 — build de playtest (Fase A do roadmap)

Mesmas regras da 1.0.0, preparada para o teste com pessoas (decisão D0 de
`ROADMAP_PRODUTO_FINAL.md`).

### Mudanças

- **Largadas iguais:** o P2 largava 1 tile à frente e a porta do P1 ficava 2 tiles mais perto.
  As portas foram reposicionadas, e um teste confere que os dois jogadores correm a mesma
  distância nas 10 fases.
- **Registro de partida** para o playtest: um arquivo por partida com rota, coletas, liderança
  e resultado. Guia em `docs/PLAYTEST.md`; análise com `tools/playtest/analisar_partidas.py`.
- **Progresso salvo:** fases concluídas e melhor tempo de cada fase (base da futura seleção de fase).
- **Balanceamento em asset** (`Resources/SO_Balance`): pontos, efeito do lanche, fadiga e prazos
  da partida ajustáveis sem recompilar.
- **Botão Interagir removido:** não fazia nada. As alavancas continuam por contato.
- Na tela de resultados, "Puzzle" passou a se chamar "Alavancas".
- Código do protótipo cooperativo removido (caixa, botão de pressão, cena `VerticalSlice_Core`).

## 1.0.0

Primeira versão completa: corrida para dois jogadores em 10 fases.

### O jogo

- **10 fases de corrida** em estufas de vários andares, cada uma com nome, tamanho, mecânica
  em destaque e cores próprias. Cada fase vem de um mapa em texto, e o mesmo construtor monta
  todas.
- **Caminho curto ou longo** em cada bifurcação. O corredor curto não tem pulos, mas o
  fast-food é inevitável e deixa o jogador **pesado** (metade da velocidade por 5 s). O
  caminho longo tem obstáculos e alimentos saudáveis.
- **Cada jogador termina sozinho** na própria porta; nenhum trecho depende do outro. O segundo
  tem 45 s depois da chegada do primeiro, e a fase acaba aos 5 minutos.
- **Obstáculos:** lombadas com rampa, lama, espinhos, plataformas que caem, alavancas com
  portão e elevadores de autoatendimento.
- **Pontuação** pensada para a corrida: chegar primeiro vale 500 (segundo, 250), o bônus de
  tempo cai aos poucos depois do tempo-alvo da fase e cada alavanca puxada vale 150.
- **Personagem articulado** com animações de correr, pular, cair, aterrissar, empurrar, fadiga,
  vitória e derrota; rosto de esforço quando pesado de fast-food.
- **Menu, pausa, reinício e próxima fase**, com câmera compartilhada e HUD numa faixa própria
  no alto da tela.
- **Áudio** com música e efeitos para pulo, coleta, alavanca, portão, checkpoint e chegada.

### Qualidade

- Testes automatizados em EditMode e PlayMode, incluindo um robô que percorre as 10 fases
  pelos dois caminhos com física real. Resultados em [TEST_REPORT.md](TEST_REPORT.md).
- Defeitos encontrados pelo robô e corrigidos antes da versão:
  - pulo bloqueado parado na rampa;
  - elevador que virava degrau, abria buraco no piso ou esmagava quem esperava;
  - plataforma móvel que não andava com um passo de física por quadro;
  - espinhos com área maior que o desenho;
  - animação de empurrar que nunca disparava contra parede.

### Requisitos

- Windows 10 ou 11, 64 bits. Dois jogadores no mesmo teclado.

### Conhecido

- O jogo foi validado pelo robô de testes. Falta uma rodada de partidas com pessoas para
  calibrar o ritmo e os pontos.
- Todas as fases usam a mesma música.
