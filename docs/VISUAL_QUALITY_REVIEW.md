# VISUAL_QUALITY_REVIEW — avaliação dos designs existentes

> Proposta para aprovação — nenhum asset definitivo foi produzido

Data: 11/11/2026. Base: capturas e arquivos do repositório `E:\dev\Healthy_Race` (ramo
`feature/fase-a-fundacao`, só leitura) e a página `docs/arte/propostas-2026-11-11.html`, feita por
outro agente e que apareceu durante este trabalho. A seção 12 registra as decisões que o dono do
projeto tomou em 11/11/2026 sobre essa página; as seções 1 a 11 são a avaliação anterior a elas. Os documentos `docs/EVOLUCAO_*.md` não estão
neste ramo: foram lidos do ramo `chore/plano-produto-final`.

As notas de 0 a 5 seguem os oito critérios da diretriz. São ferramenta de revisão, não medida: duas
pessoas dariam notas diferentes. O que vale é a justificativa.

## Resumo

1. **O jogo tem quatro autores visuais que não se conhecem.** O atlas do menu foi gerado por IA
   (`docs/visual-art-prompts.md`); o personagem que corre na fase foi desenhado por código com
   formas exatas (`tools/art/build_character_rig.py`); a estufa também (`build_greenhouse.py`); os
   alimentos da fase são pixel art de 32 px sem origem registrada (`docs/LICENCAS.md`). Cada um tem
   contorno, luz, escala e filtro de textura próprios.
2. **A aparência genérica vem de três causas concretas**, não de falta de detalhe: formas
   geométricas perfeitas (círculo, cápsula, retângulo arredondado), contorno preto de espessura
   única em tudo e o vocabulário de emoji (olho redondo com brilho, boca em "D", reflexo branco no
   canto de cima).
3. **Há marca em potencial**: o broto que murcha com o Bem-Estar, o piso xadrez vermelho e creme
   da lanchonete, a estufa em andares com o atalho passando por baixo, a mola da folha no rig.
   Nenhum deles depende do desenho atual para sobreviver.
4. **Nenhum grupo passa do corte** proposto em `ASSET_STYLE_GUIDELINES.md` (nenhum critério abaixo
   de 3, média de pelo menos 4,0). A média do jogo atual é 1,6.

| Grupo | Média (0-5) |
| --- | --- |
| 1. Personagens | 2,1 |
| 2. Variações P1/P2 | 0,9 |
| 3. Olhos, expressões, proporções | 1,6 |
| 4. Alimentos e itens (fase) | 1,3 |
| 5. Ícones e interface | 1,4 |
| 6. Cenários e plataformas | 2,1 |
| 7. Contorno, sombra e luz | 1,6 |
| 8. Composição e hierarquia | 1,6 |

---

## 1. Personagens

O que se vê: dois desenhos diferentes do mesmo broto. No menu
(`docs/evidence/visual/MainMenu-1280.png`, linha 1 de `Assets/_Game/Art/VisualAtlas/SP_VisualAtlas.png`):
cabeça-bola grande, corpinho com braços abertos, contorno preto com brilho. Na fase
(`docs/evidence/fase-01/baixo-esquerda.png`, `Assets/_Game/Art/Characters/SP_CharacterRig.png`):
oito peças por estado — cabeça, três rostos, folha, tronco, braço, perna — em cinco peles (saudável,
estável, cansado, crítico, salo).

O que faz parecer genérico:

- A cabeça é um círculo exato, o tronco é um quadrado de cantos arredondados, o braço é uma cápsula:
  `build_character_rig.py` desenha com `ellipse`, `rounded` e `capsule`. Nenhuma forma tem decisão
  de desenho; é o kit de primitivas de qualquer editor.
- Contorno de 9 px (constante `OUTLINE`) igual em toda parte, cor `(22,22,22)`: sem lado de luz,
  sem peso.
- O reflexo é o mesmo arco claro no alto à esquerda em todas as cabeças.
- "Cabeça redonda + corpo pequeno + folha no topo" é o molde mais comum de mascote de planta; o
  próprio prompt do atlas pede "round oversized heads, small humanoid bodies".
- O estado crítico é o boneco pintado de rosa e o "salo" é o boneco pintado de vinho: troca de cor,
  não de desenho.

O que tem potencial: a folha que cai e seca com o Bem-Estar (a coluna "folha" do rig é a única
que muda de forma entre estados). A mola da folha em `CharacterRig.cs` (`LeafStiffness`,
`LeafDamping`): já existe um comportamento próprio. A ideia de o estado morar no corpo, não na barra.

| Critério | Nota | Justificativa |
| --- | --- | --- |
| Originalidade | 2 | Só a folha que murcha é do jogo; o resto é o mascote-planta padrão. |
| Consistência | 2 | O broto do menu e o da fase têm proporção, olho e contorno diferentes. |
| Silhueta | 2 | Bola + folha se reconhece como "broto", mas é a mesma em todos os estados e nos dois jogadores. |
| Legibilidade | 3 | Cor chapada e contorno preto leem a 48 px; o estado depende de matiz. |
| Composição | 2 | Verde-limão sobre vidro verde-claro: personagem e fundo na mesma família de cor. |
| Acabamento | 2 | Limpo, mas com cara de marcador de posição de motor de jogo. |
| Personalidade | 1 | Nenhuma pose-assinatura; em `docs/evidence/character/Animations.png` as linhas quase não se distinguem. |
| Gameplay | 3 | A cor do corpo informa o Bem-Estar, mas ocupa o canal que deveria dizer quem é quem. |

## 2. Variações P1/P2

O que se vê: o mesmo prefab duas vezes; uma etiqueta oval "P1" azul e "P2" roxa acima da
cabeça. Na largada de `baixo-esquerda.png` as duas etiquetas se encostam e viram "P1 •P2".

Causa: não há variação: é o caso-limite de "mesmo modelo com cores diferentes", com a cor fora
do corpo. Azul e roxo ficam próximos em valor; em cinza são iguais.

Potencial: nenhum no desenho. Fica a decisão já tomada de dar forma própria a cada jogador.

| Critério | Nota | Justificativa |
| --- | --- | --- |
| Originalidade | 0 | Etiqueta de texto sobre o personagem. |
| Consistência | 3 | São idênticos; a consistência aqui é o defeito. |
| Silhueta | 0 | Idênticas. |
| Legibilidade | 1 | Etiqueta de cerca de 20 px; some com a câmera afastada (`fase-inteira.png`). |
| Composição | 1 | As etiquetas se sobrepõem quando os dois estão perto. |
| Acabamento | 1 | Solução de depuração. |
| Personalidade | 0 | Não existe P1 nem P2 como personagem. |
| Gameplay | 1 | É a origem do "qual sou eu?". |

## 3. Olhos, expressões e proporções

O que se vê: colunas 2 a 4 de `SP_CharacterRig.png`: olho = disco branco com pupila e brilho;
boca = meia-lua com língua; "esforço" = sobrancelha reta inclinada e boca em ponto; piscar = dois
arcos. A pálpebra dos estados baixos é um semicírculo da cor da pele. O personagem tem cerca de uma
cabeça e meia de altura sem a folha, com corpo na metade da largura da cabeça.

Causa: é o alfabeto de emoji: cada estado é um rosto de catálogo (feliz, neutro, sonolento,
triste). O rosto de esforço é o mesmo desenho nas cinco peles. Olho e boca nunca são assimétricos e
nunca mexem na silhueta. No atlas os olhos ocupam um terço da cabeça; no rig, bem menos: duas
proporções para o mesmo personagem.

Potencial: a pálpebra pesada dos estados "cansado" e "crítico" comunica sono sem texto; serve
para a queda do lanche.

| Critério | Nota | Justificativa |
| --- | --- | --- |
| Originalidade | 1 | Rostos de catálogo. |
| Consistência | 2 | Olho do atlas e olho do rig não são o mesmo desenho. |
| Silhueta | 1 | Nenhuma expressão altera o contorno do personagem. |
| Legibilidade | 2 | A 48 px o olho tem 4 px e a boca some. |
| Composição | 2 | Rosto centrado e simétrico, sem direção de olhar. |
| Acabamento | 2 | Traços uniformes; pálpebra como recorte geométrico. |
| Personalidade | 1 | Cinco estados, cinco emojis; nenhuma atitude. |
| Gameplay | 2 | Cansado e crítico se distinguem pela cor do corpo, não pelo rosto. |

## 4. Alimentos e itens

O que se vê: na fase, `Assets/_Game/Art/Collectibles/spr_food_*.png`: 18 sprites de 32×32 em
pixel art, sem contorno, importados a 32 px por unidade com filtro Point. No menu, os 18 alimentos
do atlas: vetor com contorno preto e reflexo branco.

Causa:

- Pixel art ao lado de vetor suave: dois tamanhos de pixel na mesma tela.
- Sem contorno sobre fundo claro; o peixe azul da Fase 5 fica sobre vidro azul
  (`docs/evidence/fases/fase-05.png`).
- Nível de detalhe desigual: a melancia tem rosto, o hambúrguer tem cinco faixas, a maçã é uma
  mancha vermelha; a banana tem um pixel solto fora do desenho.
- Lanche e fruta não têm código de forma: só quem conhece o alimento sabe a família.
- Os do atlas têm o defeito oposto: aparência de pacote de emoji. Mesmo reflexo no mesmo canto,
  degradê suave, contorno que engrossa e afina sem motivo, lata vermelha com onda branca que lembra
  marca conhecida.
- Flutuam sem sombra nem apoio.

Potencial: a lista de alimentos (cotidiano brasileiro de cantina) e a regra já decidida de
separar famílias pela forma. Nenhum desenho.

| Critério | Nota | Justificativa (sprites da fase) |
| --- | --- | --- |
| Originalidade | 1 | Ícones de comida de estoque. |
| Consistência | 0 | Outra técnica, outro filtro, outra escala que o resto do jogo. |
| Silhueta | 3 | Banana, batata e água funcionam; maçã × laranja e morango não. |
| Legibilidade | 2 | 32 px bastam de perto; sem contorno, somem em fundo claro. |
| Composição | 1 | Soltos no ar, sem sombra. |
| Acabamento | 1 | Pixels soltos, detalhe desigual, origem "a confirmar". |
| Personalidade | 1 | Nenhum traço comum entre eles. |
| Gameplay | 1 | A família (lanche × natural) não se lê. |

Os 18 do atlas, como conjunto: originalidade 1, consistência 4 entre si e 1 com a fase, silhueta 4,
legibilidade 4, composição 2, acabamento 3, personalidade 1, gameplay 2.

## 5. Ícones e interface

O que se vê: três interfaces. Menu (`MainMenu-1280.png`): painel verde-escuro de cantos
arredondados, botão verde e botões-pílula creme. Fase (`baixo-esquerda.png`): barra azul-marinho com
oito dados por jogador em fonte de sistema de cerca de 10 px ("Sem faixa", "Fadiga: Normal",
"Ep. 0"). Resultado, controles e pausa (`Results-1280.png`, `Controls-1280.png`, `Pause-1280.png`):
texto cru sobre fundo escurecido, caixas cinza translúcidas.

Causa: fonte `LegacyRuntime` (Arial) sem peso nem escala; retângulo arredondado como única
forma; sombra nenhuma ou transparência; três paletas (verde-escuro, azul-marinho, cinza). Ícones
(estrela, coração, gota, anel) são primitivas geradas no Editor. O resultado é uma planilha: "SCORE
FINAL", doze linhas por jogador, sem vencedor visível além do título.

Potencial: o logotipo do menu: caixa alta pesada, duas cores, sombra deslocada sem desfoque. O
par creme sobre verde-escuro. A opção "reduzir flashes e tremores", que já existe.

| Critério | Nota | Justificativa |
| --- | --- | --- |
| Originalidade | 1 | Componentes padrão de UI. |
| Consistência | 1 | Menu, HUD e resultado parecem de três modelos diferentes. |
| Silhueta | 1 | Tudo é retângulo. |
| Legibilidade | 1 | HUD de cerca de 10 px em 1080p. |
| Composição | 2 | O menu tem hierarquia; o resultado não tem nenhuma. |
| Acabamento | 1 | Texto de depuração na tela de jogo. |
| Personalidade | 2 | Só o logotipo. |
| Gameplay | 2 | A informação existe, longe do olhar e em excesso. |

## 6. Cenários e plataformas

O que se vê: `docs/evidence/fase-01/fase-inteira.png` e `docs/evidence/fases/fase-05.png`,
`fase-10.png`: parede de vidro com caixilhos e manchas de folhagem desfocadas
(`SP_GH_Wall.png`, `SP_GH_Leaves.png`); tábuas com rebites; rampas em triângulo de tijolo; vasos com
planta de três círculos (`SP_GH_Pot.png`); trepadeira de elipses iguais (`SP_GH_Vine.png`); corredor
de faixa rosa com piso vermelho e branco e letreiros "FAST"; placas "↓ CURTO / → LONGO" e "PUXE A
ALAVANCA"; portão azul-piscina em gomos; bandeiras vinho; portais azuis de pedra.

Causa:

- Fundo e plano de jogo no mesmo valor; a folhagem é a mesma textura repetida e borrada
  (`ImageFilter` no script).
- Plataformas flutuam sem apoio em todos os andares.
- Vegetação feita de círculos e elipses repetidos: exatamente o que a diretriz proíbe.
- Dez fases são o mesmo cenário tingido: `Fase05.txt` só troca `parede`, `tijolo` e `folhagem`.
- Objetos de mundos diferentes: portal de pedra com brilho azul (fantasia), bandeira de checkpoint,
  portão de tubo, placa de madeira com tipografia de interface.
- Texto no cenário faz o trabalho que o desenho deveria fazer, e parte dele está em inglês.

Potencial: a estrutura espacial é do jogo: estufa em andares, com a lanchonete correndo por
baixo de cada um. O piso xadrez vermelho e creme lê de longe e em cinza. O azul-piscina do portão é
a única cor que só aparece em mecanismo.

| Critério | Nota | Justificativa |
| --- | --- | --- |
| Originalidade | 2 | A ideia espacial é própria; a execução é genérica. |
| Consistência | 2 | Quatro tratamentos de contorno e de detalhe no mesmo andar. |
| Silhueta | 3 | Plataforma, rampa e espinho se reconhecem. |
| Legibilidade | 3 | O piso da lanchonete lê muito bem; lama e tábua são marrons parecidos. |
| Composição | 1 | Sem planos, sem marco, sem ponto de descanso. |
| Acabamento | 2 | Correto e vazio. |
| Personalidade | 1 | Nenhuma fase tem imagem própria. |
| Gameplay | 3 | O atalho é claramente outro lugar; a bifurcação depende de ler. |

## 7. Contorno, sombra e luz

O que se vê: rig: preto, 9 px, uniforme. Estufa: verde-escuro `(30,46,34)`, fino. Alimentos da
fase: nenhum contorno. Atlas: preto irregular. Sombra: nenhuma projetada; um degradê suave nos itens
do atlas. Luz: arco claro na cabeça, faixa clara no topo de tijolos e do piso, nada nos alimentos.

Causa: não existe regra. O contorno não diz nada (o fundo não tem, mas os alimentos também não;
vasos decorativos têm, e não se pisa neles). Não há direção de luz comum, então cada objeto parece
iluminado no seu próprio arquivo.

Potencial: o contorno escuro esverdeado da estufa mostra que tinta colorida funciona melhor que
preto. O fato de o fundo já não ter contorno.

| Critério | Nota | Justificativa |
| --- | --- | --- |
| Originalidade | 1 | Contorno preto uniforme é o padrão. |
| Consistência | 1 | Quatro contornos, três luzes. |
| Silhueta | 3 | Onde há contorno, a forma fecha bem. |
| Legibilidade | 3 | Idem; onde falta, o objeto some. |
| Composição | 1 | Sem luz comum, nada assenta no cenário. |
| Acabamento | 1 | Espessura única é o sinal mais forte de arte automática. |
| Personalidade | 1 | Nenhuma. |
| Gameplay | 2 | O contorno não separa o que se toca do que é enfeite. |

## 8. Composição e hierarquia visual

O que se vê: em `fase-inteira.png`, mais de sessenta objetos pequenos de peso igual sobre fundo
texturizado; os dois jogadores são os menores elementos saturados. Os portais azuis são o único
ponto que salta. No menu, o personagem "salo", um hambúrguer e uma rosquinha ficam no gramado sem
relação com o resto.

Causa: tudo tem a mesma saturação e o mesmo tamanho; não há área de respiro nem linha que
conduza à porta; a câmera afastada reduz personagens a cerca de 45 px enquanto placas de texto
ocupam o dobro.

Potencial: os portais mostram que um único objeto de cor exclusiva organiza a tela. A fase em
andares cria faixas horizontais naturais.

| Critério | Nota | Justificativa |
| --- | --- | --- |
| Originalidade | 1 | Disposição de grade. |
| Consistência | 2 | A grade é consistente; os pesos não. |
| Silhueta | 2 | A fase inteira não forma desenho nenhum. |
| Legibilidade | 2 | Placas maiores que jogadores. |
| Composição | 1 | Sem foco, sem respiro. |
| Acabamento | 2 | Arrumado, sem intenção. |
| Personalidade | 1 | As dez fases são a mesma imagem. |
| Gameplay | 2 | Só a porta de chegada se destaca. |

---

## 9. Elementos com potencial de virar marca

| Elemento | Onde está hoje | Como aproveitar |
| --- | --- | --- |
| Broto que murcha | Coluna "folha" do rig | Vira o medidor de Bem-Estar do jogo inteiro: postura e cor do broto, corpo com cor fixa. |
| Mola da folha | `CharacterRig.cs` | Base do movimento de personalidade de cada corredor. |
| Xadrez vermelho e creme | `SP_GH_FastFloor.png` | Assinatura da cantina: piso, toldo e embalagem de todo lanche. |
| Atalho por baixo | Mapas `Fase01-10.txt` | A bifurcação é espacial; a placa só precisa de pictograma. |
| Azul-piscina | `SP_GH_Gate.png` | Cor exclusiva de mecanismo. |
| Sombra deslocada sem desfoque | Logotipo do menu | Vira a "tinta deslocada" de toda a interface. |
| Creme sobre escuro | Painel do menu | Par de base da interface. |

---

## 10. Avaliação da página de propostas (`docs/arte/propostas-2026-11-11.html`)

A página apareceu durante este trabalho. Tem oito seções com cinco opções cada (personagens,
alimentos, cenário, animações, HUD, resultado, menu, regra central) e a própria tabela de notas.
Vi as imagens de personagens, alimentos, cenário, a opção 5 de animação, as opções 1 e 2 de HUD, as
opções 3 a 5 de resultado, as opções 1 e 5 de menu e o começo de game design; o restante li só pelos
títulos e textos.

### O que a página acerta

- Abandona halo, brilho e selo luminoso; adota contorno de peso variável, sombra chapada e uma só
  forma de painel (etiqueta chanfrada).
- Placa de bifurcação só com ícones; cabo ligando alavanca e portão; entrada do corredor sem texto.
- Mostra cada item em cor, em cinza e em silhueta, e cada personagem a 48 px.
- Família de lanche pela embalagem (opção 3 de alimentos): regra verdadeira, que vale para qualquer
  lanche novo.
- "Marco e luz pintada" (opção 3 de cenário) dá imagem própria à fase com um desenho só.
- É honesta sobre o acabamento: dá 2 a todas as opções.

### Onde a página ainda parece genérica ou se contradiz

- **A combinação recomendada nunca aparece junta.** HUD, resultado e menu usam os personagens da
  opção 1 "apenas como base de comparação"; a recomendação é personagens 5, alimentos 3,
  cenário 3 e HUD 1. A nota de consistência de cada opção foi dada sem a tela que a provaria.
- **Menu, opção 1 (recomendada)** mantém o fundo gerado por IA — céu em degradê, sol, nuvens,
  morros redondos — e os painéis do "Prontos?" são retângulos arredondados azul e roxo saturados.
  São dois itens da lista de proibições da diretriz.
- **Cor de jogador muda entre opções**: azul e roxo nas opções 1 e 3, azul e laranja nas 2, 4 e 5.
  Roxo é também a cor de alguns alimentos e de sombras.
- **Vermelho e branco em dois papéis**: listra de perigo sob os espinhos e piso/saquinho da
  lanchonete. Amarelo em três: mecanismo, cesta dourada e linhas de velocidade.
- **Personagens, opção 1**: a diferença entre Bento e Lia está na folha, na altura e na "roupa" de
  casca azul ou roxa; o rosto usa sobrancelha arqueada e meio-sorriso, a expressão de atitude mais
  comum da animação comercial. Continua perto do mascote atual.
- **Personagens, opção 5 (recomendada, vasos)**: melhor leitura de Bem-Estar da página, mas o rosto
  pintado em corpo rígido limita expressão, barro e lata não aceitam *squash & stretch* sem parecer
  borracha, e são dois esqueletos. A personalidade teria de vir toda da planta.
- **Alimentos, opção 3**: o saquinho cobre metade do lanche; hambúrguer, refrigerante e batata
  viram quase a mesma silhueta (a própria página aponta).
- **Cenário**: plataformas ainda flutuam no canto superior direito; a folhagem de fundo ainda é de
  massas arredondadas; o corredor mantém a faixa rosa.
- **Tipografia** não é definida; os mock-ups usam fonte de sistema.

### Notas para as opções que a página recomendava

As notas dela estão na própria página; estas são as desta revisão.

| Critério | Personagens 5 (vasos) | Alimentos 3 (embalagem) | Cenário 3 (marco e luz) | HUD 1 (etiquetas) |
| --- | --- | --- | --- | --- |
| Originalidade | 4 — vaso-personagem é raro; lembra "planta em vaso com rosto" de papelaria | 3 — embalagem listrada é comum, a regra é boa | 3 — o marco é próprio, o kit não | 3 — etiqueta chanfrada é uma forma, falta fonte |
| Consistência | 3 — não foi visto com o HUD nem com os itens | 4 — repete o piso do corredor | 3 — perigo usa as cores da lanchonete | 3 — mostrado com outros personagens |
| Silhueta | 5 — baixo e largo × alto e estreito | 3 — os três lanches se confundem | 4 — rampa, perigo e mecanismo distintos | 4 — painel reconhecível |
| Legibilidade | 5 — a planta lê a 48 px | 4 — listras leem em cinza | 4 — fundo recua | 4 — três elementos por jogador |
| Composição | 3 — sem cena de teste | 3 — saquinho precisa de base | 4 — luz conduz à porta | 3 — ocupa os dois cantos de cima |
| Acabamento | 2 — estudo | 2 — estudo | 2 — estudo | 2 — estudo |
| Personalidade | 3 — rosto pintado, corpo rígido | 3 — funcional | 4 — "a fase do regador" | 3 — correto |
| Gameplay | 5 — Bem-Estar grande e no topo | 4 — família na hora | 4 — aponta a saída | 4 — responde "quem está na frente" |
| **Média** | **3,8** | **3,3** | **3,5** | **3,3** |

### Convergências com a proposta do redesign

As duas chegam, por caminhos separados, a: contorno de peso
variável, sombra dura deslocada, painel de canto cortado, placa sem texto, cabo ou cano ligando
alavanca e portão, lanche identificado pela embalagem, regador gigante como marco, sementes como
personagens (opção 3 da página). É bom sinal: são as respostas diretas ao diagnóstico.

### Divergências que existiam antes da decisão do dono

A seção 12 registra como cada uma ficou.

1. Personagem: a página recomenda vasos; esta proposta recomenda sementes (ver `CHARACTER_DESIGN.md`).
2. Segundo corredor: a página tem "Gira", semente de girassol listrada, que lê como semente melhor
   que o grão de milho "Milo" desta proposta. Vale considerar o par feijão + girassol.
3. Nome: "Fava" é P2 roxo na página e P1 anil aqui.
4. Cor de mecanismo: amarelo lá, azul-piscina aqui. Cor de perigo: vermelho listrado lá, roxo aqui.
5. Fonte: Fredoka na pesquisa de referências, Lilita One aqui.

## 11. Limites desta revisão

- Não rodei o jogo: avaliei capturas e arquivos. Animação foi julgada pela prancha
  `Animations.png`, não em movimento.
- As capturas de `docs/evidence/visual/` são da cena de teste visual, não de uma fase real; as de
  fase são anteriores às mudanças do ramo `feature/fase-b-gameplay`, que não inspecionei.
- Daltonismo não foi simulado.
- A página de propostas tem mais de 26 mil pixels de altura; parte dela foi vista em escala
  reduzida.

## 12. Decisões do dono em 11/11/2026

Tomadas sobre a página `docs/arte/propostas-2026-11-11.html`, depois desta revisão.

### Aprovado

| Seção | Opção | O que define |
| --- | --- | --- |
| Alimentos | 1 — Folha × bandeja | Natural pousa numa folha; lanche vem na bandeja de papel listrada de vermelho e creme, a mesma do piso do corredor; cesta dourada maior, trançada, com bandeirola |
| Cenário | 3 — Marco e luz pintada | Kit base da estufa, um marco por fase em silhueta (regador gigante na Fase 1), faixa de luz pintada apontando a saída, vasos pendurados |
| Animações | 1 — Elástico moderado | Deformação só na raiz, antecipação clara antes do pulo, elemento solto que atrasa, curvas suaves |
| HUD | 5 — Placas penduradas | Placas de madeira presas por corda, Bem-Estar em cinco folhas, relógio de madeira, corrida numa trepadeira |
| Resultado | 3 — Linha da corrida | Vencedor à esquerda; gráfico de quem liderou em cada andar; frase ancorada no andar decisivo |
| Menu | 2 — Estufa jogável | O jogador anda até a placa e pula; em "Prontos?", cada um pula no próprio marcador |
| Game design | 1 — Pico e queda | Lanche dá impulso e depois sono; fruta ou água cortam a queda; vence quem chega; comida por jogador; cesta dourada disputada |

Três dessas não eram as recomendadas pela página nem as que esta revisão pontuou na seção 10:
Alimentos 1, HUD 5 e Menu 2. Pontos de atenção de cada uma, que a própria página registra:

- Alimentos 1: o suporte alarga o item e as listras finas precisam engrossar para ler a 48 px.
- HUD 5: cinco folhas dão passo de 20 pontos, e a madeira das placas compete com a das plataformas.
- Menu 2: custo alto, mais lento para quem só quer jogar, e a navegação por setas precisa continuar.

### Reprovado

Todos os personagens das cinco opções da página **e a ideia de planta como personagem** (broto,
semente, vaso). Motivos marcados pelo dono: genéricos, infantis demais, traço e acabamento fracos,
e a ideia de planta em si.

Com isso ficam descartados também os três conceitos do redesign (broto lapidado, sementes Fava e
Milo, vasos), inclusive o que esta revisão e o `CHARACTER_DESIGN.md` anterior recomendavam. A nova
direção é **humanos estilizados: jovens corredores com atitude, de 12 a 16 anos**; a rodada 2 de
estudos está com outro agente.

O diagnóstico das seções 1 a 3 continua valendo e pesa mais agora: os motivos da reprovação são os
mesmos que derrubaram o personagem atual (molde genérico, rosto de catálogo, acabamento de
marcador de posição).

### Como ficou cada divergência da seção 10

| Divergência | Resultado |
| --- | --- |
| Vasos ou sementes | Nenhum dos dois: planta reprovada como personagem |
| Milho "Milo" ou girassol "Gira" | Sem objeto |
| Nome "Fava" | Sem objeto |
| Mecanismo amarelo ou azul-piscina | Amarelo, o da página aprovada |
| Perigo vermelho listrado ou roxo | Vermelho, o da página; fica pendente só a cor da listra (ver abaixo) |
| Fredoka ou Lilita One | Não decidido |

### O que ainda depende do dono

1. **Vermelho e creme em dois papéis.** A base dos espinhos e a lanchonete (piso, toldo e agora a
   bandeja de todo lanche) usam o mesmo par. Proposta de menor mudança em `ART_BIBLE.md` 7.3: a
   listra do perigo passa a ser tinta escura em diagonal; vermelho com creme fica só na lanchonete.
2. **Cor do P2**: roxo ou laranja.
3. **Fonte de títulos**: Lilita One (proposta) ou Fredoka (pesquisa de referências).
4. **Personagens**: faixa de proporção, indicador de Bem-Estar sem planta, par e nomes — saem da
   rodada 2 (`CHARACTER_DESIGN.md` 7).
5. **Ajustes propostos sobre opções aprovadas**: placa com pontas chanfradas em vez de cantos
   arredondados; folha do medidor em três passos; suportes para plataformas soltas; marcos das fases
   2 a 10.
6. **Ilustrador** para a folha de modelos dos humanos.
