# CHARACTER_DESIGN — personagens: decisão e briefing da folha de modelos

> Proposta para aprovação — nenhum asset definitivo foi produzido

Versão 2.1, 11/11/2026. Este documento deixou de propor personagens. Em 11/11/2026 o dono do
projeto reprovou todos os estudos da rodada 1 e pediu **humanos estilizados: jovens corredores com
atitude, de 12 a 16 anos**. Outro agente conduz a rodada 2 de estudos. Aqui ficam o que a rodada 2
precisa cumprir, como será julgada e, em apêndice, o que foi descartado.

Aplica `ART_BIBLE.md` versão 2.1, seção 5.

## Decisão do dono em 11/11/2026: dupla 4 — Zuri e Tom

A rodada 2 (`docs/arte/personagens-rodada-2-2026-11-11.html`) apresentou cinco duplas de humanos
estilizados. O dono do projeto escolheu a **dupla 4, "membros de mangueira"**.

| Item | O que foi escolhido |
| --- | --- |
| Personagens | **Zuri** (P1, azul): dois coques afro, corre aos saltos. **Tom** (P2, laranja): mais comprido, cabelo bagunçado, corre de tiro |
| Proporção | 3 cabeças; cabeça, mãos e tênis grandes |
| Linguagem | Braços e pernas são tubos curvos, sem cotovelo nem joelho marcados; linha média uniforme; o corpo inteiro faz um arco na pose; pálpebra baixa para dar atitude |
| Cor do P2 | Laranja, como desenhado na dupla (fecha a decisão 4 da seção 7) |
| Estados | O corpo não muda de tamanho e a pele não muda de cor; pico = corpo todo esticado; queda = mole, com bocejo |

O que a escolha deixa em aberto, e que a folha de modelos precisa resolver:

1. **Indicador de Bem-Estar.** No estudo ele é só a postura, e a nota de Gameplay foi 2 de 5: a
   postura se confunde com a pose de corrida. A folha de modelos deve testar um segundo canal que
   se leia em movimento e a 48 px (por exemplo o cabelo — coques de Zuri e mechas de Tom — ou uma
   munhequeira na cor do jogador), sem motivo de planta.
2. **"Infantil demais".** Foi uma das críticas da rodada 1, e esta é a dupla que mais puxa para
   esse lado. A folha de modelos deve segurar a idade aparente em 12 a 16 anos: pálpebra, postura,
   roupa de corrida, proporção da cabeça.
3. **Estilo de época.** Animação de membros de mangueira tem histórico de caricatura; rostos e
   traços ficam neutros e são revisados antes de qualquer asset final.
4. **Animação no Unity.** Membro curvo não funciona com as peças rígidas do rig atual
   (`CharacterRig`, folha de partes gerada por `tools/art/build_character_rig.py`). Alternativas a
   avaliar: membro desenhado como curva (malha ou linha com pontas redondas) entre ombro e mão, com
   alvo de IK; osso com spline e malha deformável (pacote 2D Animation); ou quadro a quadro. O
   custo de animação foi estimado como alto.
5. **Composição.** O exagero das poses pede cenário um pouco mais caricato; conferir a dupla sobre
   o Cenário 3 aprovado antes de produzir em volume.
6. **Acabamento.** O estudo recebeu 2 de 5: mãos, dobras de roupa, peso de linha e folha de modelo
   precisam de ilustrador.

As seções seguintes continuam valendo como regras e critérios para a folha de modelos.

## 1. Por que a rodada 1 foi reprovada

Foram reprovados os cinco conceitos da página de propostas e os três deste documento (broto
lapidado, sementes, vasos). O dono marcou quatro motivos. Cada um vira uma exigência.

| Motivo marcado pelo dono | O que havia nos estudos | Exigência para a rodada 2 |
| --- | --- | --- |
| Genéricos | Cabeça ou corpo de forma simples com rosto, perninhas e folha: a família mais comum de mascote. Rostos de catálogo (olho redondo com brilho, meio-sorriso, sobrancelha arqueada) | Personagem com identidade de pessoa: cabelo, roupa, postura e atitude próprios. Nenhum traço que sirva para "qualquer jogo" |
| Infantis demais | Proporção de cabeça gigante, olhos grandes, carinha simpática. Lê como material de educação infantil, e o público vai até 16 anos | Corpo de adolescente. Atitude antes de fofura. Proporção de 3 cabeças para cima |
| Traço e acabamento fracos | Todos os estudos eram SVG montado em código: curvas corretas e sem vida, mãos e pés em traço, poses ajustadas por números, contorno por truque de deslocamento | Desenho de mão: linha de ação, mãos e pés desenhados, contorno variado à mão. Estudo feito por código não é aceito como prova de acabamento |
| A ideia de planta em si | Broto, semente e vaso como corpo do personagem | Nenhum personagem é planta, fruta, semente ou vaso. A planta fica no cenário, na comida e na interface |

Leitura franca do terceiro motivo: ele vale para qualquer estudo gerado por código, inclusive os
da rodada 2. Um humano desenhado com primitivas vetoriais tende a parecer boneco de ícone. Se a
rodada 2 não tiver desenho de mão, a nota de acabamento não deve passar de 2, e isso precisa ser
dito ao dono antes de ele julgar.

## 2. Requisitos para os humanos estilizados

### 2.1 Quem são

- Dois corredores da mesma turma, entre 12 e 16 anos, numa estufa-escola brasileira.
- Atitude: competitivos e bem-humorados. Rivais que são amigos.
- Cada um com **um adjetivo de movimento** (por exemplo: explosiva e desengonçada; econômico e
  preciso). O adjetivo aparece em todas as poses e é o que separa os dois quando a cor some.
- Nome próprio curto, em português, que não seja de planta nem de comida.

### 2.2 Proporção

A decidir entre duas faixas, com as duas desenhadas lado a lado antes de escolher.

| Faixa | Altura em cabeças | A favor | Contra |
| --- | --- | --- | --- |
| Baixa | 3 a 3,5 | Rosto ainda lê a 48 px (cabeça com cerca de 15 px); pernas curtas simplificam a corrida | Mais perto do que foi chamado de infantil |
| Alta | 3,5 a 4,5 | Lê como adolescente; pernas e braços longos dão corrida com atitude | Cabeça com cerca de 11 a 13 px a 48 px: a expressão tem de ir para a pose |

Fora das faixas: abaixo de 3 cabeças volta ao infantil; acima de 4,5 o rosto some e a animação
encarece. Recomendação de partida: testar 3,5 e 4.

Outras medidas, todas a confirmar na folha de modelos:

- Altura visual de até 1,5 unidade, dentro do colisor atual (há trechos de teto baixo no corredor).
- Mãos e pés um pouco maiores que o natural: são o que desenha a corrida de longe.
- Olho proporcional à cabeça; nada de olho ocupando um terço do rosto.

### 2.3 Como P1 e P2 se diferenciam

Pelo desenho, em pelo menos quatro canais além da cor:

| Canal | Exemplo do tipo de diferença esperada (não é proposta de desenho) |
| --- | --- |
| Cabelo | Massa alta e larga × massa baixa e pontuda |
| Roupa | Volume em cima (moletom, capuz) × volume embaixo (bermuda larga, meião) |
| Postura | Inclinada para a frente × ereta |
| Movimento | O adjetivo de cada um |
| Valor | Um conjunto claro, um escuro |
| Indicador de Bem-Estar | Mesmo tipo, formato próprio de cada um |

Proibido como diferença: tipo de corpo usado como piada ou como código (um magro e um gordo),
gênero ligado a papel (o rápido e a cuidadosa), cor de pele ligada a papel. Os dois são igualmente
capazes, igualmente atraentes e erram igual.

Cor: P1 azul `#2F80ED`; P2 roxo `#B054E8` ou laranja `#F2994A` (decisão pendente em `ART_BIBLE.md`
7.4). A cor do jogador vai na roupa e no indicador; nunca na pele nem no cabelo.

### 2.4 O que nunca muda

- **Corpo e pele não mudam com o estado.** Nem tamanho, nem largura, nem cor. Pele não recebe
  tinta de cansaço, de pico nem de queda.
- Comida nunca aparece ligada a corpo maior ou a peso. A palavra "pesado" não entra em arte,
  interface nem documento.
- Mesma caixa e mesmo colisor para os dois.

## 3. Bem-Estar, pico e queda num humano

Sem a folha na cabeça, o jogo perde o medidor que morava no corpo. O HUD aprovado (cinco folhas na
placa) mostra o número; falta o sinal **no personagem**, que é onde o olhar está.

### 3.1 Indicador diegético de Bem-Estar

Exigências:

1. É um objeto que o personagem usa ou carrega, com motivo no mundo (corrida escolar, estufa).
2. **Não é planta.**
3. Ocupa pelo menos 20% da altura da silhueta e muda de **postura**, não só de cor: três posturas
   (alto, médio, baixo), mais uma para pico e uma para queda.
4. Lê a 48 px e em cinza.
5. Leva a cor do jogador.
6. Cabe no lugar que a folha ocupa no rig atual (uma peça com mola), para aproveitar o código.

Candidatos para a rodada 2 testar. São hipóteses escritas, não desenhos.

| Candidato | Como mostra Bem-Estar | A favor | Contra |
| --- | --- | --- | --- |
| Flâmula de corredor presa às costas, com o número | Em pé e esticada; tombando; caída | Grande, leva cor e número (responde "qual sou eu?"), movimento secundário natural | Pode ler como bandeira de bicicleta; precisa de desenho para não parecer acessório solto |
| Catavento de papel preso ao boné ou à mochila | Gira rápido e fica em pé; gira devagar; para e pende | Feito à mão, combina com a estufa-escola; o giro é leitura de energia | Em captura parada o giro não se vê; depende da postura da haste |
| Faixa de cabeça ou lenço comprido | Esvoaça alto; pende; arrasta | Atitude de corredor; simples de animar | Fino: pode sumir a 48 px |
| Capuz do moletom | Para trás e cheio de vento; meio caído; sobre os olhos | Faz parte da roupa; na queda vira "capuz sobre os olhos", que diz sono | É roupa, e a mudança é pequena na silhueta |
| Só postura e rosto, sem objeto | Ombros, cabeça, passo | Custo zero de peça | Fraco a 48 px; tudo recai no HUD |

A rodada 2 pode trazer outro. O que não pode é ficar sem indicador ou usar cor de pele.

### 3.2 Estados

| Estado | Indicador | Rosto | Corpo e pose | Efeito | Não pode |
| --- | --- | --- | --- | --- | --- |
| Saudável | Posição alta | Atitude do personagem | Ereto, pronto | — | — |
| Cansado | Posição média | Pálpebra a meia altura, boca reta | Ombros um pouco caídos | — | Mudar cor da pele |
| Crítico | Posição baixa | Sobrancelha de preocupação | Curvado, mãos nos joelhos quando parado | Uma gota de suor | Corpo menor, pele pálida ou vermelha |
| Pico (lanche) | Esticado ao máximo | Olhos abertos, dentes ou sorriso largo | Inclinado para a frente, passada larga | Linhas de velocidade em tinta e amarelo | Brilho, halo, raio colorido no corpo |
| Queda / sonolento | Caído | Olhos semicerrados, bocejo | Passo arrastado, cabeça pendendo, braços soltos | "z" subindo | **Barriga, inchaço, corpo maior, qualquer sinal de peso** |
| Fadiga (ofegar) | Sobe e desce com a respiração | Boca aberta | Mãos nos joelhos | Gota | — |
| Susto (espinho) | Arrepia | Olhos arregalados | Encolhe 10% por 0,1 s, pela raiz | — | Sangue, ferimento |
| Vitória | Alto, maior | Riso | Salto ou pose própria do personagem | Confete de papel | Dança de marca ou gesto de outro jogo |
| Derrota | Baixo | Olhar para baixo, cara de "ufa" | Ombros caídos, sentado ou apoiado | — | Choro em jorro, humilhação |
| Pronto (menu) | Alto | Olhar para o outro | Pulo curto no próprio marcador | — | — |

Regras do jogo que os estados representam (Game design 1, aprovado): o lanche dá impulso de ×1,3
por 2 s e depois sonolência de ×0,6 por 4 s; fruta ou água encerram a queda na hora; a cesta dourada
dá impulso de 3 s sem queda. Pico e queda se somam ao Bem-Estar: um personagem cansado em pico tem
o indicador esticado e a postura de cansado.

### 3.3 Animação

Vale a opção aprovada (Animações 1): deformação só na raiz, até ±15%; antecipação clara antes do
pulo; o indicador e o cabelo atrasam um pouco; curvas suaves. Com humano, a raiz deforma menos do
que deformaria um mascote: acima de 15% o corpo parece de borracha.

## 4. Folha de modelos necessária

Para cada personagem, numa mesma grade com linha de chão, linha de topo de cabeça e a caixa do
colisor:

1. Proporção: as duas faixas (seção 2.2) lado a lado, antes da escolha.
2. Giro: três quartos (vista de jogo), frente (retrato, HUD, resultado) e perfil.
3. Construção: massas, eixo, onde corta, onde curva; medidas em cabeças.
4. Silhueta preta das vistas, ao lado da do outro personagem, em 160, 48 e 32 px.
5. Peças do rig com pivôs: cabeça com cabelo, rosto, tronco, braço, perna, indicador. Dizer se
   braço e perna são peça única ou têm cotovelo e joelho.
6. Mãos: três formas (aberta, fechada, apontando). Pés: dois (apoio, impulso).
7. Rostos: os dez estados da tabela 3.2, mais piscar.
8. Indicador: cinco posturas, com a cor do jogador.
9. Poses-assinatura, uma por estado de animação: parado, corrida (contato e passagem), salto,
   ápice, queda, aterrissagem, comer, pico, sonolento, fadiga, susto, vitória, derrota, pronto.
10. Tabela de cores com hex, incluindo pele e cabelo, e a regra de sombra e luz.
11. Prancha de tamanhos: 160, 96, 48 e 32 px, em cor e em cinza, sobre vidro, corredor e fundo escuro.
12. P1 e P2 lado a lado em cada pose, para conferir altura e leitura.
13. O que não fazer: três erros desenhados (rosto infantil, corpo alterado por comida, contorno
    uniforme).

Entregar em vetor e em PNG a 256 px por unidade.

## 5. Riscos

| Risco | Por que importa aqui | Como reduzir |
| --- | --- | --- |
| Representação | Dois personagens "representam" a turma inteira. Dois iguais apagam gente; um de cada, mal feito, vira cota | Tratar cabelo, pele e traços com o mesmo cuidado e a mesma qualidade de desenho nos dois; mostrar a alunos da faixa de idade e ouvir |
| Estereótipo | Jogo sobre comida com gente: é fácil cair em "o que come lanche", "a aplicada", "o atleta" | Os dois pegam atalho e os dois comem fruta; nenhum traço físico ou de origem ligado a escolha de comida ou a desempenho |
| Corpo | Público de 10 a 16 anos, tema de saúde. Qualquer leitura de corpo certo ou errado faz dano | Corpo fixo; estados só em indicador, rosto e postura; queda como sono; revisão por alguém de fora, de preferência educador |
| Idade e exposição | Personagens adolescentes | Roupa de educação física comum; nenhuma pose ou enquadramento que sexualize |
| Semelhança | Humano estilizado esbarra em personagens conhecidos e em pessoas reais | Não usar foto de pessoa real como base; não pedir "no estilo de" a ferramenta nenhuma; conferir contra os jogos de referência |
| Custo de animação | Humano tem mais peças e o olho perdoa menos | Membros de peça única ou roupa que esconde articulação; deformação só na raiz; poses-assinatura antes dos ciclos |
| Leitura a 48 px | Rosto humano some antes do rosto de mascote | Indicador grande; cabelo e roupa como massas; pose carrega o estado |
| Acabamento | Foi motivo de reprovação | Folha de modelos por ilustrador; estudo por código só para decidir proporção e silhueta |
| Marca do jogo | Sem o broto, o jogo perde a imagem que tinha | O indicador e a estufa precisam carregar a identidade; banner e menu mudam |

## 6. Critérios de aprovação da rodada 2

### 6.1 Notas

Os oito critérios da diretriz, de 0 a 5, por duas pessoas (`ASSET_STYLE_GUIDELINES.md` 5). Para
personagem, o corte é mais alto:

- nenhum critério abaixo de 3;
- Originalidade, Silhueta, Legibilidade, Personalidade e Gameplay com 4 ou mais;
- média de 4,0 ou mais;
- Acabamento com 3 só é aceito em estudo; para entrar no jogo, 4.

### 6.2 Testes

| Teste | Passa se |
| --- | --- |
| Os quatro motivos da reprovação | Para cada motivo, o dono responde "resolvido" |
| Idade percebida | Cinco pessoas de fora estimam a idade; a mediana fica entre 12 e 16 |
| Silhueta | Três pessoas de fora dizem quem é quem em preto, a 48 px |
| Escala de cinza | P1 e P2 distinguidos sem cor |
| Estados | Pessoa de fora ordena saudável, cansado e crítico só pelo personagem, a 48 px |
| Queda | Ninguém descreve o estado de queda com palavras de corpo ou de peso |
| Corpo fixo | Sobrepondo todos os estados, o contorno do corpo coincide, fora a pose |
| Estereótipo | Revisão escrita de alguém de fora da equipe |
| Atitude | Três pessoas descrevem cada personagem com um adjetivo; os adjetivos não coincidem |
| Na fase | Captura dentro do jogo, câmera perto e longe, sobre o Cenário 3 |

## 7. Decisões que a rodada 2 precisa devolver ao dono

1. Faixa de proporção (baixa ou alta).
2. Par de personagens e nomes.
3. Indicador de Bem-Estar.
4. Cor do P2 (roxo ou laranja).
5. Vista de jogo (três quartos ou perfil).
6. Quem desenha a folha de modelos.

## Apêndice — conceitos descartados em 11/11/2026

Guardados só para registro. Nenhum deve ser retomado.

| Conceito | O que era | Destino |
| --- | --- | --- |
| A — Broto VITA lapidado | A cabeça-bola atual redesenhada em forma de semente; P1 com uma folha larga, P2 com duas pontudas | Descartado: genérico e planta |
| B — Sementes (Fava e Milo) | Um feijão anil em vírgula e um grão de milho âmbar em cunha, com o broto como medidor. Era a recomendação da versão 2.0 | Descartado: planta, infantil, acabamento |
| C — Vasos corredores | Vaso alto com folhas-lança e tigela baixa com folhas redondas | Descartado: planta |
| Página de propostas, opções 1 a 5 | Brotos de linha viva, recorte de papel, sementes corredoras, jardineiros de estufa, mudas em vaso | Todas reprovadas. A opção 4 (jardineiros) era humana e também foi reprovada: carregava uma muda nas costas como medidor e tinha traço de estudo |

O que se aproveita das duas rodadas, como aprendizado e não como desenho:

- A regra "o corpo diz quem é, o indicador diz como está" continua certa; só o indicador deixa de
  ser planta.
- Silhuetas opostas (uma massa curva e uma reta, uma alta e uma larga) funcionaram em todos os
  estudos para separar P1 de P2.
- Um medidor grande no topo da silhueta foi o que melhor leu a 48 px.
- A opção humana da página já previa que a pele nunca recebe cor de estado. Vira regra.

Os desenhos dos conceitos A, B e C estão em `docs/arte/redesign-v2.0.html`, seções (a) e (b), que não foi refeito.
