# ASSET_STYLE_GUIDELINES — produção e aprovação de assets

> Proposta para aprovação — nenhum asset definitivo foi produzido

Versão 2.1, 11/11/2026. Como um desenho vira asset do Healthy Race e o que ele precisa cumprir para
entrar. Aplica `ART_BIBLE.md` 2.1, `CHARACTER_DESIGN.md` 2.1 e `ENVIRONMENT_ART.md` 2.1, já com as
decisões do dono: aprovados Alimentos 1, Cenário 3, Animações 1, HUD 5, Resultado 3, Menu 2 e Game
design 1; personagens reprovados e em nova rodada (humanos estilizados).

## 1. Fluxo em dez passos

É o fluxo da diretriz, com a saída e o critério de cada passo. Nenhum passo começa antes de o
anterior estar aprovado pelo dono do projeto. Só o passo 10 toca o repositório do jogo.

| # | Passo | Saída | Critério para avançar | Situação em 11/11/2026 |
| --- | --- | --- | --- | --- |
| 1 | Definir a Art Bible | `docs/ART_BIBLE.md` | Aprovada por escrito; pendências da seção 7 respondidas | Versão 2.1 proposta |
| 2 | Estudos de personagens | Conceitos em silhueta e em cor | Um conceito escolhido | **Rodada 1 reprovada. Rodada 2 em andamento (humanos estilizados)** |
| 3 | Aprovar as silhuetas | Folha de silhuetas P1 e P2 em 160, 48 e 32 px | Três pessoas de fora dizem quem é quem | Depende do passo 2 |
| 4 | Paleta e proporções | Tabela de cores; faixa de proporção escolhida | Captura em cinza e simulação de daltonismo | Paleta de base definida; proporção e cor do P2 pendentes |
| 5 | Folha de modelos | Os 13 itens de `CHARACTER_DESIGN.md` 4 | Revisão de ilustrador; notas da seção 5 | Depende do passo 2 |
| 6 | Estudos de cenário | Trecho de fase em camadas | Três faixas de valor em cinza | **Cenário 3 aprovado** como direção |
| 7 | Padrão de plataformas e obstáculos | Kit base em peças | Uma fase real remontada só com o kit | Direção aprovada; peças por fazer |
| 8 | Alimentos e itens | Prancha das famílias e da cesta em 96, 56 e 28 px | Lanche e natural distinguidos em cinza | **Alimentos 1 aprovado** como direção |
| 9 | Padrão de animações | Poses-assinatura e um ciclo de corrida | Vídeo de 10 s aprovado | **Animações 1 aprovada** como regra; depende do personagem |
| 10 | Validar dentro do jogo | Fase 1 com o conjunto representativo (seção 6) | Checklist da seção 5; testes passando | Pendente |

O que não espera a rodada 2: passos 6, 7 e 8, o HUD 5, o Menu 2 e o Resultado 3 podem ser
produzidos e integrados com um personagem provisório (seção 6.1).

## 2. Especificação técnica

Unity 6000.3.23f1, URP 17.3 2D.

### 2.1 O que existe hoje

Lido nos arquivos `.meta` e nos scripts.

| Asset | Pixels por unidade | Filtro | Mipmap | Pivô |
| --- | --- | --- | --- | --- |
| `Art/Characters/SP_CharacterRig.png` (2048×1280, 40 peças) | 256 | Trilinear | sim | por peça (folha em 0,5 / 0,078; braço e perna no topo) |
| `Art/Environment/Greenhouse/SP_GH_*.png` (ladrilhos) | 256 | Trilinear | — | centro |
| `SP_GH_Leaves.png` | 128 | Trilinear | — | centro |
| `SP_GH_Wall.png` (fundo esticado) | 32 | Trilinear | — | centro |
| `Art/VisualAtlas/SP_VisualAtlas.png` (1254×1254, 36 recortes) | 128 | Bilinear | não | centro |
| `Art/Environment/Visual/SP_Background.png` | 100 | Bilinear | — | centro |
| `Art/Collectibles/spr_food_*.png` (32×32) | 32 | Point | não | centro |

Seis combinações de escala e filtro. Não há nenhum Sprite Atlas (`*.spriteatlas`). Não há Sorting
Layers nomeadas: tudo está na camada `Default`, com ordens numéricas (fundo −30, ambiente 0, objetos
10, alimentos 12, personagem 18 a 22, identificadores 24 e 25, rótulos 30, efeitos 35). Material
único: `MAT_VisualUnlit`. Fonte: `LegacyRuntime`. Pacotes 2D instalados: `com.unity.2d.sprite` e
`com.unity.2d.tilemap`.

### 2.2 Padrão proposto

| Tema | Regra |
| --- | --- |
| Autoria | Vetor (Inkscape ou equivalente), prancheta em unidades do jogo, exportação em PNG RGBA de 32 bits. Personagens: desenho de mão, limpo em vetor |
| Pixels por unidade | 256 para tudo que está no plano de jogo e para personagens. 128 para fundo, marco e vasos pendurados. Nenhum outro valor |
| Tamanho do tile | 1 unidade = 256 px |
| Células | Múltiplos de 128 px; margem transparente de 8 px em cada lado |
| Filtro | Trilinear com mipmaps no mundo; Bilinear sem mipmap na interface. Point é proibido |
| Compressão | Sem compressão (RGBA32) ou BC7 de alta qualidade no Windows. Sem Crunch |
| Tamanho máximo de textura | 2048 por atlas |
| Wrap | Clamp, exceto faixas que repetem na horizontal (piso do corredor, parede) |
| Mesh do sprite | Full Rect em peças com 9-slice e em ladrilhos; Tight no resto |
| Material | `MAT_VisualUnlit`. Sem luz 2D da URP; sombra e luz são pintadas, inclusive a faixa de luz do Cenário 3 |
| Cor | sRGB; nenhuma cor fora da paleta da bible |

Conta de resolução: com a câmera mais próxima (10 unidades de altura visível) em 1080p, uma unidade
ocupa 108 px; em 4K, 216 px. 256 px por unidade cobre os dois sem ampliar.

### 2.3 Pivôs

| Tipo | Pivô |
| --- | --- |
| Personagem montado | Entre os pés, na linha do chão |
| Peças do rig | Na articulação: braço no ombro, perna no quadril, indicador na base, rosto no centro da cabeça |
| Item com suporte | Centro da base do suporte (folha ou bandeja) |
| Ladrilho de piso | Centro da célula de 1 tile |
| Objeto apoiado (espinhos, alavanca, placa de bifurcação, portão, porta) | Centro da base |
| Elevador | Centro do tampo |
| Vaso pendurado e placa pendurada | Centro em cima, no ponto da corda (para balançar) |
| Fundo e marco | Centro embaixo |
| Interface | Conforme a âncora do Canvas |

### 2.4 Sprite Atlas

Criar quatro, com Sprite Atlas V2, preenchimento de 4 px, sem rotação e sem empacotamento justo.

| Atlas | Conteúdo |
| --- | --- |
| `ATL_Personagens` | Peças dos dois corredores, todos os estados |
| `ATL_Estufa` | Kit base, perigos, mecanismos, placa de bifurcação, corredor, vasos pendurados |
| `ATL_Itens` | Alimentos com suporte, cesta, ícones de estado no mundo, efeitos quadro a quadro |
| `ATL_Interface` | Placa de madeira (9-slice), corda, etiqueta creme, folhas do medidor, relógio, trepadeira, retratos, ícones |

Fundos e marcos ficam fora de atlas (texturas grandes, uma por fase).

### 2.5 Sorting Layers

Substituir a camada única por camadas nomeadas.

| Sorting Layer | Conteúdo | Ordem interna sugerida |
| --- | --- | --- |
| `Ceu` | Céu, vidro, arcos | 0 a 9 |
| `Fundo` | Folhagem em dois valores, marco, faixa de luz | 0 folhagem, 5 marco, 9 luz |
| `Estrutura` | Vasos pendurados, cordas, hastes, corpo das bancadas, interior do corredor | 0 a 19 |
| `Piso` | Tábuas, rampas, piso do corredor | 0 a 9 |
| `Objetos` | Perigos, mecanismos, cabo tracejado, placas, portas, itens | 0 perigo, 5 mecanismo, 10 item |
| `Personagens` | Peças do rig; P2 com +10 | 0 a 9 |
| `Efeitos` | Poeira, linhas de pico, z, texto flutuante | 0 a 9 |
| `PrimeiroPlano` | Folhas escuras dos cantos | 0 |

O HUD de placas penduradas fica em Canvas, acima de tudo. No menu jogável, as placas de opção são
objetos de cena na camada `Objetos`: o jogador pula nelas.

Risco: criar Sorting Layers muda `ProjectSettings/TagManager.asset` e todos os prefabs gerados;
fazer num PR só, com o `VisualAssetBuilder` regenerando tudo.

### 2.6 Nomes de arquivo

Padrão: `SP_<grupo>_<Nome>[_<Variante>][_<Estado>].png`, sem acento, em PascalCase. Mantém o prefixo
`SP_` que o projeto já usa; aposenta `spr_food_*`.

| Grupo | Exemplo |
| --- | --- |
| `CH` personagem | `SP_CH_<Nome>_Rig.png` (nomes a definir na rodada 2) |
| `IT` item | `SP_IT_Maca_Folha.png`, `SP_IT_Hamburguer_Bandeja.png`, `SP_IT_CestaDourada.png` |
| `EV` kit da estufa | `SP_EV_Bancada_Meio_02.png`, `SP_EV_Rampa_2x3.png`, `SP_EV_Lama_3.png`, `SP_EV_VasoPendurado_01.png` |
| `PG` perigo | `SP_PG_Espinhos.png` |
| `MC` mecanismo | `SP_MC_Alavanca_Aberta.png`, `SP_MC_Portao.png`, `SP_MC_Elevador.png`, `SP_MC_Cabo.png` |
| `CT` corredor | `SP_CT_Piso.png`, `SP_CT_Toldo.png`, `SP_CT_Parede.png` |
| `BG` fundo e marco | `SP_BG_F01_Vidro.png`, `SP_BG_F01_Marco_Regador.png`, `SP_BG_F01_Luz.png` |
| `FX` efeito | `SP_FX_Poeira_01.png` a `_05.png` |
| `UI` interface | `SP_UI_Placa.png`, `SP_UI_Etiqueta.png`, `SP_UI_Folha_Viva.png`, `SP_UI_Relogio.png`, `SP_UI_Trepadeira.png` |

Fonte vetorial fora de `Assets/` ou em pasta ignorada pela importação, com o mesmo nome e extensão
`.svg`.

### 2.7 Personagem: rig ou spritesheet

Rig de recortes, com troca de sprite onde o desenho precisa mudar. A decisão não depende de quem
são os personagens; as peças dependem.

| Parte | Técnica | Motivo |
| --- | --- | --- |
| Cabeça com cabelo, tronco, braços, pernas | Peças do rig, animadas por rotação e posição | Dois estudantes mantêm; cada pose nova não custa desenho |
| Deformação | **Só na raiz** (escala do transform principal), até ±15% | Animações 1, aprovada |
| Rosto | Troca de sprite: dez estados, mais piscar | Expressão desenhada, não deformada |
| Indicador de Bem-Estar | Cinco posturas, na peça que hoje é a folha, com a mola do `CharacterRig` | Reaproveita `LeafStiffness` e `LeafDamping` |
| Poeira, linhas de pico, z, gota | Spritesheet quadro a quadro, 4 a 6 quadros | Efeito curto; só tinta e creme |
| Vitória na tela de resultado | Uma ilustração inteira por personagem | Momento de retrato |

O rig atual tem oito peças por pele (cabeça, rosto, rosto piscando, rosto de esforço, folha,
tronco, braço, perna). Um humano com braço e perna de peça única cabe nas mesmas oito; com cotovelo
e joelho, passa a doze e o `CharacterRig` e o `VisualAssetBuilder.Characters` precisam mudar. É
decisão da rodada 2 (`CHARACTER_DESIGN.md` 4, item 5).

Não usar spritesheet inteira do personagem: dois corredores, quinze estados e oito poses passam de
200 desenhos, e qualquer ajuste de proporção refaz todos.

Hoje o rig tem cinco peles, uma por faixa de Bem-Estar, cada uma com a cor do corpo trocada. Com a
regra de corpo e pele fixos, isso vira **uma pele por personagem**, e o estado troca só o rosto e o
indicador.

### 2.8 Estados de animação

Os 12 clipes atuais são Idle, Move, Jump, Fall, Land, Push, Interact, Eat, Fatigue, Recover,
Victory, Defeat. O commit `bdebd8e` do ramo atual tirou o código cooperativo e a ação Interagir,
então Push e Interact ficam sem uso (conferir antes de apagar os clipes); entram pico, sonolento,
susto e pronto.

| Estado | Poses-chave | Duração | Repete |
| --- | --- | --- | --- |
| Parado | 4 | 2,0 s | sim |
| Corrida | 8 | 0,5 s na velocidade base | sim |
| Salto, com antecipação | 3 | 0,12 s | não |
| Ápice | 1 | enquanto durar | — |
| Queda | 2 | enquanto durar | sim |
| Aterrissagem | 3 | 0,20 s | não |
| Comer | 4 | 0,25 s | não |
| Pico (corrida larga) | 8 | 0,4 s | sim |
| Sonolento (andar arrastado e bocejo) | 8 e 6 | 0,8 s e 1,0 s | sim |
| Fadiga (ofegar) | 4 | 1,2 s | sim |
| Recuperar | 3 | 0,3 s | não |
| Susto (espinho) | 3 | 0,3 s | não |
| Vitória | 8 | 1,0 s | sim |
| Derrota | 6 | 1,5 s | sim |
| Pronto (pulo no marcador) | 4 | 0,4 s | não |

Curvas de entrada e saída suaves. A antecipação do pulo é desenho e não pode atrasar a física; a
validar na Unity. O modo "reduzir movimento" usa durações maiores e amplitudes menores.

### 2.9 Interface aprovada, do ponto de vista técnico

| Tela | O que exige |
| --- | --- |
| HUD 5 | Placa em 9-slice com duas cordas até a borda de cima; cinco sprites de folha por jogador, cada uma com três estados (proposta); relógio de madeira; trepadeira como linha com dois marcadores e a porta. Substitui `PlayerHUD` e `MatchHUD` montados em código com `LegacyRuntime` |
| Menu 2 | Uma cena jogável montada com o kit, em vez do prefab de telas; placas com colisor de gatilho; legenda; navegação por setas e Enter mantida |
| "Prontos?" | Dois marcadores no chão com gatilho; glifos das teclas de cada jogador (Kenney Input Prompts, CC0) |
| Resultado 3 | **Registro de quem estava na frente em cada andar** e do evento decisivo. O registro de partida do ramo atual é o ponto de partida; o gráfico são duas linhas sobre a etiqueta creme |

## 3. Uso de ferramentas de IA

Pode: pesquisa visual, painel de referências, variações de silhueta para discussão, estudo de
composição, apoio para escrever e revisar documentos e código de montagem.

Não pode:

1. Imagem gerada entrar no jogo, no menu, no banner ou na loja como asset final. O atlas atual
   (`SP_VisualAtlas.png`) e o fundo do menu (`SP_Background.png`) estão nessa situação e saem; o
   Menu 2 aprovado já dispensa o fundo.
2. Imagem gerada ser limpa ou vetorizada automaticamente e chamada de autoral. Refino significa
   redesenhar segundo a bible, com a folha de modelos ao lado.
3. Pedir a uma ferramenta "no estilo de" um jogo, estúdio ou artista, ou a partir de foto de pessoa
   real. Vale em dobro para personagens humanos.
4. Usar asset cuja licença ou cujos termos de uso não foram lidos.
5. Apresentar estudo feito por código ou por IA como prova de acabamento. "Traço e acabamento
   fracos" foi motivo de reprovação da rodada 1.

Registro obrigatório em `docs/LICENCAS.md`, no mesmo PR: ferramenta, data, para que serviu (estudo,
não asset), e o prompt em `docs/visual-art-prompts.md`. Os estudos em SVG de `docs/arte/` e da página de
propostas foram escritos por assistentes de IA: são material de discussão e entram no registro como
tal.

Pendências que já existem em `docs/LICENCAS.md`: origem dos 18 `spr_food_*.png`, das músicas e dos
efeitos sonoros ("a confirmar"); termos de uso da ferramenta que gerou o atlas.

## 4. Quem faz o quê

| Papel | Estudante A — arte e direção | Estudante B — integração e animação |
| --- | --- | --- |
| Dono de | Bible, paleta, itens, ícones, placas e etiquetas | `VisualAssetBuilder`, atlas, Sorting Layers, rig, clipes, construtor de fases, cena de menu |
| Produz | Vetores e PNG exportados | Prefabs, cenas, capturas de evidência |
| Revisa | Animação e capturas de B | Arte de A contra a especificação técnica |
| Juntos | Checklist de aprovação; nenhum asset entra com a assinatura de um só | Idem |

Onde vale buscar um ilustrador, contratado ou voluntário:

| Tarefa | Por quê | Tamanho |
| --- | --- | --- |
| Folha de modelos dos dois corredores humanos | Humano perdoa menos que mascote, e acabamento foi motivo de reprovação. É a tarefa em que o ilustrador mais pesa | 5 a 8 dias |
| Poses-assinatura e as ilustrações de vitória | Linha de ação e atitude não saem de fórmula | 3 a 4 dias |
| Logotipo | Letreiro é ofício próprio | 1 a 2 dias |
| Dez marcos de fase | Um desenho grande por fase, visível o tempo todo | 4 a 6 dias |

Combinar por escrito a cessão de uso e o crédito, e registrar em `docs/LICENCAS.md`.

Onde um pacote licenciado serve (fontes conferidas em `docs/EVOLUCAO_REFERENCIAS_ASSETS.md`):

| Necessidade | Fonte | Licença |
| --- | --- | --- |
| Glifos de tecla e de controle | Kenney Input Prompts | CC0 |
| Partículas-base | Kenney Particle Pack, redesenhadas em tinta e creme | CC0 |
| Referência de leitura de ícones | Phosphor (peso Bold ou Fill), redesenhados | MIT |
| Blocos para montar e testar fases antes da arte | Kenney New Platformer Pack | CC0 |
| Fontes | Lilita One, Nunito | OFL |

Onde pacote não serve: personagens, alimentos, kit da estufa, placas de interface. São a identidade.
Personagem humano de pacote, em particular, devolve o jogo ao genérico na hora.

## 5. Checklist de aprovação

### 5.1 Notas

Cada asset ou conjunto pequeno recebe nota de 0 a 5 em cada critério, com uma linha de
justificativa, de duas pessoas. Com diferença de 2 pontos ou mais, conversam e refazem.

| Critério | Pergunta | 3 significa | 5 significa |
| --- | --- | --- | --- |
| Originalidade | Parece pertencer só ao Healthy Race? | Segue a bible | Só poderia ser deste jogo |
| Consistência | Combina com os demais assets? | Mesma tinta, luz e escala | Indistinguível da mão dos outros |
| Silhueta | Reconhece-se sem detalhes? | Reconhecível em preto | Reconhecível em preto a 32 px |
| Legibilidade | Funciona em tamanho reduzido? | Lê a 48 px | Lê a 28 px e em cinza |
| Composição | Está integrado ao ambiente? | Assenta no cenário | Ajuda a organizar a tela |
| Acabamento | Parece final? | Sem defeito visível em 1080p | Aguenta ampliação de retrato |
| Personalidade | É memorável? | Tem um traço próprio | Alguém descreve de memória |
| Gameplay | Comunica a função? | Função clara depois de ver uma vez | Função clara na primeira vez |

Nota mínima para entrar no jogo:

- nenhum critério abaixo de 3;
- Legibilidade, Gameplay e Consistência com 4 ou mais;
- média de 4,0 ou mais.

Personagens têm corte mais alto (`CHARACTER_DESIGN.md` 6.1). Asset de fundo, sem contorno, fica
dispensado de Silhueta e Personalidade; o marco de fase não. É ferramenta de revisão, não garantia:
nota alta com justificativa fraca volta.

### 5.2 Verificações objetivas

- [ ] Nenhum degradê, brilho, halo ou desfoque; nenhuma moldura arredondada genérica.
- [ ] Contorno em `#1E2A22`, duas espessuras, só se o objeto é tocável.
- [ ] Uma sombra e uma luz, de cima e da esquerda.
- [ ] Só cores da paleta; cor de função no papel certo.
- [ ] Natural sobre folha; lanche sobre bandeja listrada; nenhum item com aura.
- [ ] Vermelho com creme só na lanchonete (depende da decisão pendente).
- [ ] Nenhum texto dentro da fase; nenhuma palavra em inglês.
- [ ] Nenhum estado altera corpo ou pele de personagem.
- [ ] Nenhum personagem é planta.
- [ ] 256 ou 128 px por unidade; filtro, pivô, nome e atlas conforme a seção 2.
- [ ] Prancha de tamanhos (96, 56 ou 48, 28 px) em cor e em cinza anexada ao PR.
- [ ] Captura dentro do jogo em 1280×720 e 1920×1080, câmera perto e longe.
- [ ] Origem e licença em `docs/LICENCAS.md`.
- [ ] Testes EditMode e PlayMode passando.

## 6. Conjunto representativo mínimo

Produzido e integrado na Fase 1 antes de qualquer volume. Está em dois blocos porque os personagens
dependem da rodada 2.

### 6.1 Bloco A — pode começar agora

Usa um personagem provisório: a caixa do colisor com a cor do jogador e um número, sem rosto. Serve
para medir leitura do cenário e do HUD sem contaminar a avaliação com o personagem reprovado.

| # | Asset | O que prova | Origem |
| --- | --- | --- | --- |
| 1 | Bancada: ponta esquerda, meio em 4 variantes, ponta direita, sobre corpo de estrutura | Kit base; nada flutua | Cenário 3 |
| 2 | Rampa e lama de 3 tiles | Mesmo material em peças diferentes | Cenário 3 |
| 3 | Espinhos com base de perigo | Perigo separado da lanchonete (depende da decisão pendente) | Cenário 3 |
| 4 | Alavanca, cabo tracejado e portão | Mecanismo sem texto | Cenário 3 |
| 5 | Elevador | Estrutura sem tinta ao lado de piso com tinta | Cenário 3 |
| 6 | Corredor: piso, toldo de entrada, parede; e a placa de bifurcação com seta e ícone | Assinatura da lanchonete; pictogramas | Cenário 3 |
| 7 | Fundo da Fase 1: céu e vidro, folhagem em dois valores, regador gigante, faixa de luz, três vasos pendurados | Separação de planos; marco; luz que aponta a saída | Cenário 3 |
| 8 | Quatro itens: maçã e água sobre folha, hambúrguer na bandeja listrada, cesta dourada com bandeirola | Duas famílias pelo suporte; item disputado | Alimentos 1 |
| 9 | HUD: duas placas penduradas com cinco folhas, relógio de madeira, trepadeira de corrida, nas fontes novas | Interface como objeto da estufa | HUD 5 |
| 10 | Trecho de menu: três placas penduradas com texto e dois marcadores de "Prontos?" | Mesma placa em HUD e menu | Menu 2 |

### 6.2 Bloco B — depois da rodada 2

| # | Asset | O que prova |
| --- | --- | --- |
| 11 | Corredor P1: rig completo, parado e corrida | Humano estilizado na linguagem da bible |
| 12 | Corredor P2: idem | P1 e P2 em silhueta |
| 13 | Indicador de Bem-Estar em cinco posturas e rostos de saudável, cansado, crítico, pico e sonolento, dos dois | Estado sem mexer em corpo nem pele; queda como sono |
| 14 | Tela de resultado com o gráfico por andar e uma ilustração de vitória | Resultado 3 com os personagens finais |

Estimativa do bloco A: 10 a 12 dias de arte e 6 a 8 de integração para duas pessoas. Bloco B: depende
do personagem; com folha de modelos de ilustrador, 6 a 8 dias de arte e 4 a 5 de integração. São
estimativas, não medições.

Fica para depois da aprovação do conjunto: os outros 14 alimentos, as fases 2 a 10 (marco e cor de
vidro), seleção de fase, efeitos, vitória e derrota ilustradas.

## 7. Limites

- A especificação técnica foi lida nos `.meta` e nos scripts; nada foi aberto na Unity.
- A escolha de compressão e o custo de memória dos atlas não foram medidos.
- Sorting Layers novas, suportes automáticos, a cena de menu jogável e o registro de liderança por
  andar exigem mudanças de código que não foram detalhadas nem estimadas com cuidado.
- O número de peças do rig humano e, portanto, o custo de integração dependem da rodada 2.
- Os tempos de animação são ponto de partida; o ramo `feature/fase-b-gameplay` pode ter mudado a
  lista de estados.
- As estimativas de dias supõem ilustrador para a folha de modelos; sem ele, o risco é de
  acabamento, que já foi motivo de reprovação.
