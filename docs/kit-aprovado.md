# Kit aprovado no jogo (HUD 5, Alimentos 1, Cenário 3, Menu 2, Resultado 3)

O que foi aprovado na página de propostas de 11/11/2026 e como entrou no jogo. A arte é **de
apoio**: desenho vetorial por código, no traço da proposta, para o jogo funcionar com a direção
aprovada. O acabamento final é do ilustrador, nos mesmos arquivos e tamanhos.

## De onde vem a arte

`tools/art/build_kit.py` desenha cada peça em SVG (contorno de tinta `#1E2A22`, uma cópia da
forma deslocada como sombra de tinta, cor chapada com um tom claro por dentro) e rasteriza com o
Chrome ou o Edge sem interface, com fundo transparente.

```bash
python tools/art/build_kit.py --preview previa.png
```

Com `--only foods`, `--only hud` ou `--only env` gera só uma família. Precisa do Chrome ou do
Edge instalado; a variável `CHROME` aponta outro caminho.

Depois, no Unity: `Healthy Race › Visuals › Build HUD kit (placas, folhas, trepadeira)`
(`VisualAssetBuilder.BuildHudKit`), que importa as peças e preenche `Resources/SO_HudKit.asset`
e `Resources/SO_SceneryKit.asset`. O HUD e a fase são montados por código, por isso os dois kits
ficam em `Resources`; sem eles o jogo cai na arte anterior, sem erro.

| Família | Arquivos | Quem usa |
| --- | --- | --- |
| Alimentos | `Art/Collectibles/spr_food_<id>.png` (18) | `FoodData` de cada alimento |
| Ícones de alimento | `Art/Kit/SP_FoodIcons.png` | Reserva; ainda sem uso |
| HUD e interface | `Art/Kit/hud_*.png`, `menu_pad.png`, `ui_star_*.png` | `HudKit` |
| Cenário | `Art/Kit/env_*.png` | `SceneryKit` |
| Seis alimentos da proposta | `tools/art/kit/proposal_foods.json` | Fonte de maçã, água, brócolis, hambúrguer, refrigerante e batata |

## Alimentos 1

Os 18 alimentos das fases saíram do pixel art de 32 px, de origem não confirmada, para arte
própria. O natural pousa numa **folha** (base curva) e o lanche vem na **bandeja de papel
listrada** (base reta): a família se lê pelo suporte, sem halo nem selo. Seis desenhos vêm da
proposta; os outros doze foram feitos no mesmo traço.

O alimento não é chamado de ruim em lugar nenhum, e nada nele liga comida a corpo.

## HUD 5

`GreenhouseHud` (em `Scripts/UI`) é só uma vista: lê `PlayerHUD` e `MatchHUD` a cada quadro.

- **Placa pendurada** por jogador: retrato no aro da cor do jogador e o Bem-Estar em **cinco
  folhas**. Cada folha vale 20 pontos; qualquer ponto acima de zero ainda mantém uma.
- **Relógio de madeira** no meio, com o tempo da corrida.
- **Trepadeira da corrida**: os dois retratos andam da largada até a porta.
- **Etiqueta creme** só onde há texto: o nome da fase, a janela final e o aviso de fadiga.

O painel de texto antigo (`PlayerHUD`, `MatchHUD`) continua existindo e recebendo os eventos,
mas fica escondido. Os números dele são os que os testes conferem. A faixa do HUD passou de 64
para 168 px de referência, então a área da fase ficou cerca de 10% mais baixa.

Limite conhecido, já apontado na revisão da proposta: cinco folhas dão passos de 20 pontos, e o
jogador não vê o número exato durante a corrida.

## Cenário 3

O kit de estufa já existia. O que entrou agora:

- **Placas sem texto.** Na bifurcação, seta e alimento de cada caminho (verde para o lado com a
  maçã, amarela para baixo com o hambúrguer e o raio do impulso). Na alavanca, o desenho do
  puxão. O texto do mapa (`[placas]`) continua lá e decide qual placa aparece.
- **Selo do lanche** no corredor, no lugar do letreiro "FAST".
- **Cores de função.** O que se aciona é amarelo (base da alavanca, elevador, capa do portão); o
  que machuca é vermelho (faixa sob os espinhos). Um **cabo tracejado amarelo** liga cada
  alavanca ao seu portão.
- **Profundidade.** Um marco em silhueta por fase (regador, carrinho de mão, girassol, cata-vento,
  caixa d'água), uma faixa de luz pintada que desce até a porta e vasos pendurados no teto.

São cinco marcos para dez fases: cada marco aparece em duas fases, nunca em fases vizinhas.

A identidade do jogador 2 passou de roxo para o laranja do Tom (`#F2994A`).

## Menu 2

O menu é um trecho jogável da estufa, montado pela mesma `LevelBootstrap` das fases com o mapa
`Levels/Menu.txt` (tema `modo menu`). `MenuStage` pendura as opções como placas:

- quem anda até a placa e **pula** escolhe;
- as placas são botões de verdade: **mouse** e **setas + Enter** continuam valendo;
- "Jogar" abre o **"Prontos?"**: cada jogador pula no próprio vaso, e a corrida começa com os dois
  nos vasos por 0,7 s. Enter começa já e Esc volta;
- duas placas de seta escolhem a fase, entre as já liberadas no salvamento.

A tela de menu do prefab virou só o letreiro. `MainMenuBootstrap` não é mais usado pela cena.

## Resultado 3

`RaceTrace` guarda o progresso dos dois a cada 0,25 s. `RaceResultBoard` monta a tela: o
vencedor à esquerda, com a frase do desfecho e as estrelas dos dois; à direita, o gráfico de quem
estava na frente em cada andar, a virada marcada e a frase que aponta o andar.

O título, o motivo e os dois painéis de números da tela antiga viraram a vista **Detalhes**.

O número de andares do gráfico é contado pelas placas de bifurcação do mapa. A posição no
gráfico é o progresso de quem vai na frente, dividido em partes iguais: é uma aproximação do
andar, não uma medida.

## O que não entrou

- **Fases novas.** As dez fases são as mesmas, com a arte nova. O redesenho de conteúdo (um
  elemento próprio por fase, ensino por fase) é a Fase C do roteiro e depende dos tempos do
  playtest humano.
- **Cesta dourada**, placa de bifurcação com prévia de câmera e os demais itens pendentes da
  Fase B.
- **Tela de seleção de fase** própria: a escolha existe só nas placas do "Prontos?".
- **Telas de pausa, controles e áudio**: seguem com o visual anterior.
- **Fonte de título**: a decisão entre Lilita One e Fredoka continua aberta; os textos usam a
  fonte padrão.
- **Galeria de teste** (`Scenes/Test/VisualTest.unity`): segue com os brotos.
