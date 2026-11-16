# ENVIRONMENT_ART — cenários, plataformas e ambientes

> Proposta para aprovação — nenhum asset definitivo foi produzido

Versão 2.1, 11/11/2026. Aplica `ART_BIBLE.md` 2.1 e as opções aprovadas pelo dono na página de
propostas: **Cenário 3** (kit base da estufa, um marco por fase, luz pintada apontando a saída),
**Alimentos 1**, **HUD 5**, **Menu 2** e **Resultado 3**. O que este documento acrescenta ao que foi
aprovado está marcado como proposta.

O trecho de fase em `docs/arte/redesign-v2.0.html` (c) foi desenhado para a versão 2.0 e está superado nas cores
de função, na placa, nos espinhos e nos personagens.

## 1. A estufa como mundo

O lugar: a estufa-escola de um colégio, de ferro e vidro, em vários andares, cuidada pelos alunos.
No fim da aula, dois corredores da turma disputam o caminho até a porta. Em cada andar há bancadas
de madeira por cima e, por baixo, o corredor da cantina: o atalho.

| Elemento do jogo | O que é no mundo | Por que está ali |
| --- | --- | --- |
| Plataforma | Bancada de mudas sobre estrutura verde | É onde ficam as plantas; por isso há vários níveis |
| Caminho longo | As bancadas de cima | Trajeto de quem cuida das plantas; tem fruta e água |
| Caminho curto | O corredor da cantina, por baixo | Atalho de quem está com pressa; tem lanche |
| Rampa | Prancha de carrinho de mão | Sobe-se com o carrinho de mudas |
| Lama | Terra encharcada sobre a bancada | Alguém regou demais |
| Espinhos | Ponteiras sobre uma base de aviso | Trecho interditado |
| Alavanca e portão | Alavanca ligada por cabo a uma grade | Separa as alas |
| Elevador | Bandeja que sobe numa haste | Leva sementeiras para cima |
| Cesta dourada | A cesta da feira da escola | É de todos; por isso é disputada |
| Porta de chegada | Porta da ala, com a cor de cada corredor | Fim da corrida |
| Placas do menu e do HUD | Placas de madeira penduradas por corda | Sinalização feita pelos alunos |

Fica fora: portal de pedra com brilho, bandeira de checkpoint, placa escrita dentro da fase,
letreiro em inglês, qualquer objeto que não exista numa estufa ou numa cantina.

## 2. Camadas e separação de planos

Conforme o Cenário 3: fundo sem contorno, em dois valores de vidro e folhagem, **30% a 40% mais
claro e menos saturado** que o plano de jogo.

| # | Camada | O que tem | Valor | Contorno | Paralaxe |
| --- | --- | --- | --- | --- | --- |
| 1 | Céu e vidro | Céu `#E3F3E5`, vidro `#D3ECD7`, arcos e caixilhos `#BCDDC3` | 85% a 92% | não | 0,1 |
| 2 | Fundo | Folhagem desenhada folha a folha em dois valores (`#BFE0C4`, `#AED6B4`); **marco da fase** em silhueta; **faixa de luz pintada** | 75% a 82% | não | 0,4 |
| 3 | Estrutura | Vasos pendurados, cordas, hastes, corpo verde das bancadas (`#5E7C4F`), interior do corredor | 45% a 75% | não | 1,0 |
| 4 | Plano de jogo | Pisos, rampas, perigos, mecanismos, itens com suporte, personagens | 35% a 70% | **sim** | 1,0 |
| 5 | Primeiro plano | Folhas grandes e escuras nos cantos de baixo | 20% a 30% | não | 1,3 |

Regras:

1. A camada 4 é a única com tinta e a única com cores de função.
2. O marco fica no valor do fundo: dá para lembrar, não dá para confundir com piso. É a ressalva
   que a própria opção aprovada registra.
3. A faixa de luz aponta para a saída ou para a porta. No máximo duas por tela.
4. O primeiro plano ocupa só os 8% externos do quadro e nunca cruza um caminho.
5. Sem desfoque em camada nenhuma.
6. O corredor tem fundo próprio (`#FBE3D9`), mais quente que o vidro: lê como "dentro".

Teste de aprovação: a captura da fase em escala de cinza mostra três faixas — fundo claro, jogo no
meio com linha, bordas escuras.

## 3. Kit base

É o kit mínimo que toda fase usa (Cenário 1, que o Cenário 3 herda). Tudo a 256 px por unidade,
encaixando na grade de 1 tile dos mapas em texto de `Assets/_Game/Levels/`.

### 3.1 Pisos

| Peça | Tamanho em tiles | Desenho |
| --- | --- | --- |
| Bancada — ponta esquerda, meio, ponta direita | 1 × 0,5 cada | Tábua de madeira quente `#D98F48` com aresta de luz `#F2B86C`, sobre corpo verde de estrutura |
| Plataforma curta | 1 a 2 × 0,35 | Tábua solta |
| Piso do corredor | 1 × 0,5 | Faixa de blocos vermelhos e creme |

Proposta desta bible sobre o kit aprovado:

- pontas das tábuas chanfradas a 12°;
- quatro variantes do meio (lisa, com emenda, com nó, com emenda dupla), sorteadas com semente fixa
  por fase;
- no máximo um sinal de material a cada 4 tiles; sem rebite.

### 3.2 Rampa

Prancha de madeira sobre um calço da mesma madeira, mais escuro. Duas inclinações (as do mapa),
espelhadas.

### 3.3 Lama

Monte de terra sobre a bancada, marrom `#6B4632`, com borda ondulada e uma ou duas bolhas animadas
(`MudBubbles` já existe). Larguras de 2, 3 e 4 tiles. Proposta: deixar a lama escorrer por uma ou
duas línguas sobre a borda da bancada, para ler como molhado de longe.

### 3.4 Espinhos

Fileira de pontas claras sobre uma **base de perigo**. Na página aprovada a base é vermelha com
listra creme, que é também o par de cores da lanchonete.

**Decisão pendente do dono** (`ART_BIBLE.md` 7.3): a proposta é trocar só a listra — vermelho
`#D7263D` com listra diagonal na cor da tinta `#1E2A22`. Vermelho com creme fica exclusivo da
lanchonete. Este documento descreve o kit já com essa troca; se o dono não aprovar, volta a listra
creme e nada mais muda.

Aviso antes de ferir (`HazardPresentation.PresentWarning`): as pontas tremem por 0,5 s.

### 3.5 Alavanca, cabo e portão

- Alavanca: base e haste escuras com o punho na cor de mecanismo `#FFC23C`. Dois estados.
- **Cabo tracejado** amarelo ligando a alavanca ao portão: é o que substitui a placa "PUXE A
  ALAVANCA". Quando acionado, o tracejado corre em direção ao portão.
- Portão: grade de barras de metal cinza `#55636B` com travessa amarela e preta no topo. Aberto:
  recolhido para cima.
- Amarelo de mecanismo vale para todo objeto que o jogador aciona.

### 3.6 Elevador

Bandeja de madeira que corre numa haste, com um disco amarelo no alto e setas duplas amarelas
indicando o sentido. A bandeja usa 9-slice, como o `SP_GH_Lift` atual.

### 3.7 Entrada do corredor e corredor

| Peça | Desenho |
| --- | --- |
| Entrada | Toldo vermelho e creme na borda do buraco, com um disco pendurado mostrando o ícone do lanche |
| Parede | Rosa-claro `#FBE3D9` com faixas verticais um tom mais escuro |
| Piso | Blocos vermelhos e creme |
| Itens | Lanches na bandeja listrada (seção 5) |

Substitui a faixa rosa lisa e os letreiros "FAST". Nenhuma palavra.

### 3.8 Placa de bifurcação, só com ícones

Placa de madeira num poste, com duas linhas:

- **seta para cima + maçã**: caminho das bancadas, com fruta;
- **seta para baixo + lanche**: corredor, com lanche.

A placa fica a pelo menos 3 tiles antes da bifurcação e tem cerca de 2 tiles de altura. O preço do
lanche (pico e depois sono) não está na placa: o jogador aprende na primeira vez e a tela de
resultado explica. A validar em playtest se seta e ícone bastam.

### 3.9 Suportes — proposta

A diretriz proíbe elemento flutuante sem justificativa, e no estudo aprovado ainda há plataformas
soltas no ar. Proposta, para o construtor de fases aplicar sozinho, na camada de estrutura e sem
tinta:

| Situação | Suporte |
| --- | --- |
| Piso com corpo de bancada embaixo | Já apoiado |
| Plataforma curta com parede atrás | Mão-francesa a cada 3 tiles |
| Plataforma solta no vão | Duas cordas até a viga, como os vasos pendurados |

### 3.10 Porta de chegada e ponto de retorno

A página aprovada não redesenha os dois. Proposta: porta de ala em madeira com a folha pintada na
cor do jogador e o retrato numa plaquinha, uma por jogador, no lugar dos portais azuis de pedra;
ponto de retorno como estaca de canteiro que levanta uma bandeirola da cor de quem passou.

## 4. Marco, luz e vasos pendurados

O que o Cenário 3 acrescenta ao kit: **um marco por fase**, em silhueta no fundo; **uma faixa de
luz pintada** que aponta a saída; **vasos pendurados** que dão a escala vertical da estufa de vários
andares.

Na Fase 1 o marco aprovado é o **regador gigante**. Os outros nove são proposta deste documento,
com os nomes dos arquivos `Fase01.txt` a `Fase10.txt`.

| Fase | Nome | Marco | Situação |
| --- | --- | --- | --- |
| 1 | Corrida do Equilíbrio | Regador gigante | Aprovado |
| 2 | Horta Suspensa | Móbile de vasos em correntes, em escada | Proposta |
| 3 | Pomar das Alavancas | Macieira que fura o teto de vidro | Proposta |
| 4 | Estufa dos Espinhos | Mandacaru enorme num vaso rachado | Proposta |
| 5 | Ponte de Folhas | Tanque de vitórias-régias, visto de lado | Proposta |
| 6 | Feira Livre | Barraca de feira com toldo e balança de pratos | Proposta |
| 7 | Torre das Trepadeiras | Caixa-d'água com uma trepadeira em espiral | Proposta |
| 8 | Mercado Comprido | Geladeira-vitrine antiga da cantina | Proposta |
| 9 | Labirinto da Lama | Mangueira furada num carretel, esguichando | Proposta |
| 10 | Grande Final | Cúpula de vidro ao anoitecer com um girassol alto | Proposta |

Regras do marco:

1. Um desenho grande (4 a 7 tiles), em silhueta de dois valores, na camada de fundo.
2. No valor do fundo, nunca mais escuro que a folhagem de trás.
3. Nunca atrás de uma bifurcação: compete com a placa.
4. Aparece inteiro pelo menos uma vez no percurso.
5. A miniatura da fase na seleção é o marco.

Vasos pendurados: de barro, com planta em silhueta, presos por corda à viga, em três alturas. Ficam
na camada de estrutura, sem tinta, e nunca na frente de um piso.

Cor de vidro por fase: só a da Fase 1 está aprovada (`ART_BIBLE.md` 7.7).

## 5. Alimentos no cenário

Aprovado: Alimentos 1. Vale o que está em `ART_BIBLE.md` 13.1. Para o cenário, isso significa:

- Fruta, água e refeição **pousam numa folha**. Nas bancadas, a folha pode estar apoiada no piso.
- Todo lanche vem na **bandeja de papel listrada** de vermelho e creme, a mesma do piso do corredor.
- A cesta dourada é maior, trançada, com bandeirola; uma por andar.
- Nenhum item paira com halo. Quando o item fica acima do piso, o suporte vai junto.
- Uma fruta fica logo depois de cada reencontro de bifurcação, num desvio de 1 a 2 s (Game design 1).

## 6. Menu como trecho da estufa

Aprovado: Menu 2. O menu deixa de ser uma tela e passa a ser uma cena montada com o mesmo kit.

| Elemento | Desenho |
| --- | --- |
| Fundo | O da Fase 1, com o logotipo pintado no fundo, no valor do marco |
| Opções | Placas de madeira penduradas por corda, com texto (Jogar, Como jogar, Opções, Sair). A placa escolhida fica na cor de destaque |
| Como escolher | O jogador anda até a placa e pula. Uma legenda no rodapé diz isso |
| "Prontos?" | Cada jogador tem um marcador no chão com a cor dele; pular no próprio marcador confirma. As teclas de cada um aparecem desenhadas ao lado |
| Acessibilidade | Setas e Enter continuam funcionando |

Consequências para o cenário:

- as placas do menu são as mesmas do HUD (uma peça só, `ART_BIBLE.md` 14);
- texto em placa só existe aqui; nas fases, só ícones;
- o trecho do menu precisa de piso contínuo e sem perigo, com as placas ao alcance de um pulo;
- o fundo gerado por IA do menu atual (`SP_Background.png`) sai.

## 7. Vegetação sem "círculos repetidos"

O Cenário 3 pede folhagem "desenhada folha a folha". Regras para cumprir isso:

1. Cada espécie nasce de uma folha-mãe desenhada à mão, com a ponta cortada, repetida ao longo de
   um caule com rotação e escala irregulares.
2. Toda planta tem uma folha grande, algumas médias, poucas pequenas.
3. No máximo três espécies por fase, de uma lista de oito: lança, coração, leque recortado, fita,
   redonda com talo, serrilhada, pendente, palma.
4. O contorno de um arbusto nunca é simétrico.
5. Nenhuma repetição regular de mais de três elementos iguais em linha.
6. No fundo, silhueta em dois valores, sem nervura.
7. Planta decorativa sobre bancada vai para a camada de estrutura, sem tinta.
8. Os vasos de três círculos (`SP_GH_Pot`) e a trepadeira de elipses (`SP_GH_Vine`) saem.

## 8. Composição para câmera que afasta

| Regra | Número | Como conferir |
| --- | --- | --- |
| Personagem mínimo na tela | 48 px de altura em 1080p | Captura no afastamento máximo |
| Três tamanhos de leitura | Massa (lê a 48 px), detalhe médio (128 px), detalhe fino (só de perto) | Prancha de tamanhos |
| Detalhe fino no afastamento máximo | Funde na massa sem cintilar; mipmaps ligados | Vídeo com a câmera afastando |
| Listras da bandeja | No máximo cinco na largura do item | Prancha a 28 px |
| Densidade | No máximo 1 objeto com tinta a cada 2 tiles de piso, fora os pisos | Contar por andar |
| Respiro | Cada andar tem um trecho de 4 tiles só com piso | Mapa |
| Foco | A faixa de luz e o marco não disputam: a luz aponta a saída, o marco fica longe dela | Captura |
| Placas | Menores que duas vezes a altura do personagem | Medir |
| Bordas | Nada importante nos 8% externos do quadro; o HUD de placas penduradas ocupa o topo | Captura |
| HUD e piso | As placas do HUD nunca ficam no mesmo valor da madeira do piso | Captura em cinza |

Nada troca de sprite com o zoom: o desenho é feito para o menor tamanho e ganha detalhe de perto.

## 9. Antes e depois, em palavras

| Hoje (`docs/evidence/fase-01/baixo-esquerda.png`) | Aprovado ou proposto |
| --- | --- |
| Vidro com folhagem borrada no valor do jogo | Vidro claro em dois valores, folhagem folha a folha (aprovado) |
| Dez fases tingidas | Um marco por fase e luz pintada (aprovado); cor de vidro por fase (proposta) |
| Tábuas flutuando, com rebites | Bancadas sobre estrutura (aprovado); suportes e chanfro (proposta) |
| Triângulo de tijolo verde | Prancha sobre calço (aprovado) |
| Faixa rosa + "FAST" | Corredor com toldo e ícone de lanche (aprovado) |
| "↓ CURTO / → LONGO" | Placa com seta e ícone (aprovado) |
| "PUXE A ALAVANCA" | Cabo tracejado (aprovado) |
| Espinho de metal cinza | Pontas sobre base de perigo (aprovado); listra em tinta (pendente) |
| Alimentos em pixel art soltos no ar | Folha e bandeja listrada (aprovado) |
| Portais de pedra com brilho | Porta de ala com a cor do jogador (proposta) |
| Menu com fundo gerado por IA | Trecho jogável da estufa (aprovado) |

## 10. Limites

- As opções aprovadas foram lidas nos textos e nos scripts da página e vistas em capturas
  reduzidas; os desenhos são estudos, com nota de acabamento 2 dada pela própria página.
- Os marcos das fases 2 a 10, os suportes, o chanfro, a porta e o ponto de retorno são propostas
  deste documento, não vistas pelo dono.
- A colocação automática de suportes e a cena de menu jogável exigem mudanças no
  `VisualAssetBuilder` e no fluxo de telas, não avaliadas em detalhe.
- A leitura da placa só com ícones é hipótese até o playtest.
- Os personagens que aparecem nos estudos de cenário são os reprovados; o cenário precisa ser
  conferido de novo com os da rodada 2.
