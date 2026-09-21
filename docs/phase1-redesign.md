# Fase 1 - redesign da Corrida do Equilíbrio

## Conceito

O redesign usa a referência externa como base de composição: espaços compactos,
plataformas em camadas, pequenas salas, atalhos e conexões verticais. A arte,
os personagens, os alimentos, a paleta e os mecanismos continuam pertencendo
ao Healthy Race e seguem a Art Bible oficial.

A fase mantém o eixo de progressão da esquerda para a direita para preservar o
HUD e a chegada existentes. Dentro desse eixo, o jogador circula por três
alturas e reencontra o outro jogador antes do clímax cooperativo.

## Estrutura final Z1-Z6

| Zona | Espaço | Função |
| --- | --- | --- |
| Z1 | Bosque de largada | Movimento, salto e leitura de P1/P2 em área segura |
| Z2 | Jardim da Grande Árvore | Caixa, placa de pressão e plataforma acionada |
| Z3 | Rede de rotas | Três caminhos conectados e escolhas de risco e recompensa |
| Z4 | Clareira do Reencontro | Convergência, recuperação e segundo checkpoint |
| Z5 | Torre do Equilíbrio | Sequência cooperativa obrigatória com recuperação |
| Z6 | Pavilhão do Equilíbrio | Sprint final, último risco, chegada e espera |

## Rotas da Z3

### Copa das árvores

É a rota superior e técnica. Possui maior verticalidade, plataformas temporárias
e alimentos saudáveis de alto valor. Os saltos formam arcos legíveis e retornam
à rota central em dois pontos.

### Ponte central

É a rota de recuperação. Plataformas largas e vãos curtos continuam percorríveis
com os multiplicadores mínimos do estado SALO. Mistura alimentos saudáveis e
ultraprocessados, evitando uma resposta moral óbvia baseada apenas na altura.

### Córrego inferior

É a rota rápida e arriscada. Lama reduz velocidade, espinhos punem trajetórias
apressadas e os ultraprocessados ficam próximos dos atalhos. Existem alimentos
saudáveis de recuperação antes das conexões verticais.

## Risco e recompensa

- A rota superior exige timing e oferece os maiores ganhos de Bem-Estar.
- A rota central é mais previsível, mas alterna recompensas e tentações.
- A rota inferior reduz a quantidade de saltos, porém cobra velocidade, risco
  de respawn e perda de Bem-Estar.
- Alimentos desenham curvas de salto, marcam saídas de salas e indicam pontos de
  troca entre rotas.
- A rota principal de recuperação foi dimensionada com o pulo e a velocidade de
  SALO, sem depender do estado inicial saudável.

## Puzzle final

O puzzle deixa de aceitar `placa OU alavanca` e passa a exigir os dois sinais:

1. a caixa é colocada na placa;
2. a placa eleva a plataforma de acesso;
3. um jogador usa a plataforma para alcançar a alavanca;
4. placa e alavanca juntas abrem o portão;
5. ambos atravessam enquanto a caixa mantém a placa pressionada.

A alavanca de recuperação devolve a caixa ao ponto inicial e redefine o estado
do mecanismo. As plataformas de recuperação ficam do lado de entrada e não
atravessam o portão, portanto resolvem quedas sem anular o puzzle.

## Landmarks

1. árvore de largada;
2. Grande Árvore do jardim;
3. arco de pedra da rota central;
4. nascente azul no caminho inferior;
5. árvore da Clareira do Reencontro;
6. Torre do Equilíbrio;
7. Pavilhão do Equilíbrio e árvore de celebração.

## Composição visual

- O background ilustrado da galeria visual passa a integrar a fase funcional.
- Plataformas usam repetição modular do sprite, evitando esticar uma peça única
  por dezenas de unidades.
- O tamanho visual de blocos, hazards e mecanismos acompanha o BoxCollider2D.
- Elementos técnicos de blockout deixam de dominar a tela; permanecem apenas
  nomes diegéticos curtos em landmarks e pontos de orientação.
- Background, decoração, gameplay, VFX e HUD usam ordens de desenho separadas.

## Métricas e câmera

- A área principal varia de aproximadamente Y=-3,4 a Y=5.
- As rotas possuem conexões intermediárias próximas de X=88 e X=112.
- A separação vertical cabe no zoom máximo da câmera compartilhada em 16:9.
- A fase conserva dois checkpoints: entrada das rotas e reencontro antes do
  puzzle final.
- O progresso continua baseado em X=0 até X=215, preservando o HUD existente.

Esses limites precisam ser confirmados em Game View com P1/P2 separados; os
cálculos são restrições de projeto, não substituem o playtest.

## Validação automatizada

Os testes do redesign verificam:

- presença das três rotas e de quatro conectores verticais;
- uso vertical superior a sete unidades;
- pelo menos cinco landmarks;
- puzzle final com placa e alavanca obrigatórias;
- ausência das antigas plataformas de bypass;
- correspondência entre dimensões visuais e colliders;
- resolução correta dos dados e sprites dos alimentos colocados.

O teste histórico que teletransporta jogadores continua útil para validar
checkpoints e chegada, mas não é tratado como evidência de travessia completa.

## Validação executada

- 137/137 testes de EditMode;
- 49/49 testes de PlayMode;
- estados do portão validados com só placa, só alavanca e ambos;
- vãos das três rotas medidos diretamente nos colliders;
- elevador da Z2 validado no lado acessível da parede e alinhado ao jardim;
- enquadramento entre conectores validado contra o zoom máximo da câmera;
- 16 capturas em Game View: oito regiões em 1280x720 e 1920x1080;
- build Windows x64 concluído com zero warnings e zero erros;
- executável iniciado em smoke test headless sem exceções no log;
- replay exposto no painel e coberto por teste de evento.

O teste automatizado histórico de progressão usa teleporte para conferir
checkpoints e chegada. Durante uma tentativa adicional com movimento físico, a
instrumentação encontrou dois bloqueios reais — a linha de largada com collider
e o elevador sobreposto à parede — que foram corrigidos e protegidos por testes
estruturais. Não foi registrada uma cronometragem humana confiável; a meta de
3–5 minutos permanece como faixa para o playtest presencial, sem inventar uma
medição automatizada como se fosse tempo de jogador.
