# Evolução — direção de arte e redesign visual

Base: capturas em `docs/evidence/` (menu, fases 1 a 10, resultados, prancha do personagem) e a
origem da arte registrada em `docs/visual-art-prompts.md` e `docs/visual-mvp.md`.

## 1. Diagnóstico visual

| Área | Observado | Efeito |
| --- | --- | --- |
| Estilo | Menu e personagens em **ilustração vetorial de contorno grosso** (atlas `SP_VisualAtlas`, gerado por IA a partir da Art Bible v1.1); alimentos, plataformas e placas da fase em **pixel art** (`Art/Collectibles/spr_food_*.png`) | Três linguagens na mesma tela: a fase parece de outro jogo que o menu |
| Alimentos | Pixel art de ~32 px, sem contorno, pequenos sobre fundo claro | Leitura fraca; lanche e fruta se distinguem pela cor, não pela forma |
| Personagens | P1 e P2 idênticos; só uma etiqueta "P1"/"P2" de ~20 px | "Qual sou eu?" |
| Estufa | Painéis de vidro claros, folhagem lavada, a mesma em todas as fases (só tingida) | Fundo e plano de jogo com o mesmo valor; nenhum marco |
| Corredor curto | Faixa rosa e piso vermelho e branco, letreiros "FAST" em inglês | Funciona como sinal; a palavra está em outra língua |
| Placas | "↓ CURTO / → LONGO", "PUXE A ALAVANCA" em madeira | Tutorial escrito no cenário |
| Tipografia | `LegacyRuntime` (Arial) no HUD e nos resultados; título do menu com fonte própria | O HUD parece ferramenta de depuração |
| Resultados | Duas colunas de texto sobre fundo escurecido | Nenhum momento de vitória |

**O que já é bom e deve ficar:** o personagem broto com folha que murcha conforme o Bem-Estar
(é a imagem do jogo), o menu principal, o piso vermelho e branco como marca da lanchonete, o
rig articulado com 12 clipes.

**Achado de custo baixo:** o atlas vetorial **já tem** 18 alimentos no estilo do menu (maçã,
banana, laranja, uva, morango, cenoura, brócolis, tomate, água, castanha, hambúrguer, pizza,
refrigerante, batata, rosquinha, cachorro-quente, sorvete, chocolate). A fase usa outros sprites.
Faltam no estilo vetorial só abacate, peixe, melancia, bala e salgadinho — ou se troca o cardápio
para os que já existem.

**A validar — origem e licença:** os sprites `spr_food_*.png` entraram no commit `a3e3592`
(FOOD-01) sem registro de origem. Antes de um produto final, registrar fonte e licença de toda
arte, incluindo a nota de que o atlas e o fundo foram gerados por IA (`visual-art-prompts.md`).

## 2. Direções possíveis

### Direção A — "Estufa ilustrada" (estender o estilo do menu) · **recomendada**

- **Conceito:** livro ilustrado de horta: formas redondas, contorno escuro grosso, cores chapadas
  com um brilho suave. É o estilo do menu e do atlas, levado para dentro da fase.
- **Paleta (tokens iniciais, a amostrar da arte do menu):**

  | Token | Uso | Cor inicial |
  | --- | --- | --- |
  | `contorno` | Contorno de tudo que é jogável | `#1E2A22` |
  | `folha` | Saudável, P1/P2 em estado bom, botões principais | `#7BD34A` |
  | `creme` | Painéis, texto sobre escuro | `#F8F3DC` |
  | `ceu` / `vidro` | Fundo da estufa | `#BFE3F2` com 60% de saturação |
  | `lanche` | Corredor, letreiros, lanches | `#E8423A` + `#FFD23F` |
  | `perigo` | Espinhos, bordas de queda | `#D7263D` com listras creme |
  | `p1` / `p2` | Jogadores | `#2F80ED` / `#B054E8` (ver §3 sobre daltonismo) |

- **Personagens:** os brotos do atlas, com duas silhuetas distintas (§3). Estados por cor e folha
  (já existe) + efeitos (§4).
- **Cenários:** estufa em três camadas — fundo (vidro e plantas, baixo contraste, sem contorno),
  plano de jogo (madeira e terra com contorno), primeiro plano ocasional (folhas desfocadas nas
  bordas). Cada fase tem uma paleta e **um marco** (regador gigante, caixa d'água, girassol...).
- **Tipografia:** uma sans arredondada e pesada para títulos e números (ex.: Fredoka ou Baloo 2,
  licença OFL — conferir) e uma sans legível para texto corrido (ex.: Nunito). Tudo em
  português; nenhum letreiro em inglês.
- **Referências visuais de princípio:** livros ilustrados infantis de horta; a clareza de
  silhuetas de jogos de sofá como Overcooked (contorno + cor chapada + objetos grandes).
- **Vantagens:** a identidade já existe e foi aprovada no menu; o pipeline de geração e montagem
  (atlas → `VisualAssetBuilder`) já existe; contorno grosso é a melhor legibilidade com câmera
  afastada.
- **Complexidade:** **média**. Trocar sprites da fase, redesenhar plataformas, placas e fundo no
  estilo; 5 alimentos novos; variante do P2.

### Direção B — "Pixel art de estufa"

- **Conceito:** tudo em pixel art 32 px, coerente com os alimentos e as plataformas atuais.
- **Paleta:** limitada (32 cores), alto contraste.
- **Personagens:** brotos redesenhados em pixel, animação quadro a quadro.
- **Vantagens:** coerente com metade do que está na fase; barato por objeto.
- **Desvantagens:** obriga a refazer o menu, o personagem e o rig articulado (que é vetorial e é o
  melhor ativo do jogo); pixel art pequeno perde leitura com a câmera afastada.
- **Complexidade:** **alta** (refazer personagem e animações).

### Direção C — "Feira de papel" (recorte e colagem)

- **Conceito:** cenário e personagens como recortes de papel colorido, textura de cartolina,
  sombras de papel. Conversa com feira escolar e trabalho manual.
- **Vantagens:** muito distintiva; tema escolar forte.
- **Desvantagens:** textura disputa com legibilidade; exige um artista com domínio da técnica;
  nada do que existe é reaproveitado.
- **Complexidade:** **alta**.

### Recomendação

**Direção A.** É a única que reaproveita o melhor que o jogo tem (menu, broto, rig) e corrige o
problema principal (mistura de estilos) com o menor custo. B e C trocam um problema de coerência
por um projeto de arte novo.

## 3. Personagens

| Item | Proposta | Prioridade |
| --- | --- | --- |
| Silhuetas distintas | P1: broto de **uma folha larga**; P2: broto de **duas folhas pontudas**. Mesmo corpo e rig, troca só a peça da folha | **P0** |
| Cor + forma + número | Contorno colorido por jogador e número "1"/"2" no peito ou na folha; não depender só de azul × roxo | **P0** |
| Retratos no HUD e resultados | Mesmos brotos, com as duas silhuetas | P1 |
| Expressões por estado | Pico (olhos arregalados, sorriso), queda (bocejo, pálpebra), fadiga (ofegar), espinho (susto) | P1 |
| Vitória e derrota | Já existem; dar pose e confete na tela de resultado | P1 |
| Variações cosméticas | Chapéus, cores de vaso | P3 |

**Par de cores e daltonismo:** azul (`#2F80ED`) × roxo (`#B054E8`) ficam próximos para tritanopia
(**a validar** com simulador). Como a forma (folha) e o número já diferenciam, a cor pode ficar;
se a simulação mostrar confusão, trocar o P2 para laranja (`#F2994A`).

## 4. Elementos interativos e estados

| Elemento | Linguagem proposta |
| --- | --- |
| Lanches | Ícones vetoriais do atlas, com **aura amarela** e raio pequeno: "energia rápida" |
| Frutas, água, refeição | Ícones vetoriais com **aura verde**; família reconhecível pela forma (fruta redonda, gota, prato) |
| Cesta disputada (gameplay F4) | Cesta dourada, maior, com brilho pulsante |
| Item já coletado por você | 30% de opacidade (coleta por jogador) |
| Perigos | Vermelho com listras creme; espinho com contorno |
| Mecanismos | Amarelo-alavanca consistente (alavanca, botão do elevador, esteira) |
| Bifurcação | Letreiro "LANCHE" + raio no corredor; arco de folhas + fruta na estufa |
| Chegada | Porta com a cor do jogador e retrato; visível de longe |
| Estado pico | Faíscas e linhas de velocidade atrás do personagem |
| Estado queda | "Z" subindo, cor levemente dessaturada, passo arrastado |
| Fadiga | Gotas de suor e ofegar; anel de contagem no chão |

## 5. Cenários e fases

- **Fundo com valor separado:** fundo 30-40% menos saturado e mais claro que o plano de jogo;
  nenhum contorno no fundo. Hoje o fundo e as plataformas têm o mesmo peso.
- **Uma identidade por fase:** paleta (já existe por tingimento) + marco + elemento dono
  (gameplay O1/O2). Exemplos: Horta Suspensa (vasos pendurados), Pomar das Alavancas (macieiras
  com alavancas de madeira), Feira Livre (barracas e esteira).
- **Parallax:** já existe (`ParallaxLayer`); ganhar uma camada de primeiro plano desfocada.
- **Iluminação:** sem luz dinâmica (URP 2D permite, mas custa desempenho e tempo); usar faixas de
  luz pintadas no fundo para destacar a porta e o marco.

## 6. Game feel e polimento

| Classe | Itens | Custo |
| --- | --- | --- |
| **Essencial** (sem isso parece protótipo) | Estilo único (§2-A); P1 ≠ P2; feedback de coleta com ícone; estados pico/queda/fadiga visíveis; HUD e resultados redesenhados (UI/UX) | 10-14 dias |
| **Polimento** (qualidade percebida) | *Squash & stretch*; poeira de aterrissagem; tremor de câmera leve (desligável); transições entre telas; confete na vitória; som por estado | 5-7 dias |
| **Cosmético opcional** | Chapéus, cores, decoração animada extra, fundo animado | 3+ dias |

Regra para todo efeito: respeitar a opção "reduzir flashes e tremores" que já existe, e nunca
cobrir o personagem por mais de 0,3 s.

## 7. Critérios de aceite da direção de arte

1. Em qualquer captura de gameplay, só aparece um estilo (sem pixel art ao lado de vetor).
2. Uma pessoa que nunca viu o jogo distingue P1 de P2 em captura em escala de cinza.
3. Lanche e fruta se distinguem em captura em escala de cinza (pela forma ou aura).
4. Nenhum texto em inglês na tela de jogo.
5. Personagem ≥ 50 px de altura em 1080p no zoom máximo da câmera.
6. Fonte de toda arte e licença registradas em `docs/` antes da build final.
