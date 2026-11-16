# Healthy Race — Referências complementares e fontes de assets gratuitos

Pesquisa feita em 11/11/2026. Complementa (não repete) as referências já levantadas: Ultimate Chicken Horse, Overcooked, Celeste (Forgiveness / Assist Mode), MDA, Game Accessibility Guidelines, Unity "Level design basics", Kishōtenketsu, Steve Swink, SpeedRunners e Re-Mission.

**Como ler a coluna "Verificado?":**

- **Sim** — a página abriu nesta pesquisa e a informação (licença, conteúdo, princípio) foi lida nela.
- **Parcial** — a página abriu (HTTP 200 e título conferidos), mas o trecho relevante não foi lido, ou veio de resumo de busca.
- **Não** — conhecimento geral, não confirmado em página nesta pesquisa.

Nenhuma arte foi inspecionada visualmente: comentários sobre estilo (contorno, chapado) são expectativa a confirmar ao baixar o pacote.

## Resumo executivo

1. Dá para fechar o jogo inteiro só com licenças CC0, MIT e OFL — sem nenhuma obrigação de atribuição dentro do jogo (basta guardar os textos de licença no build).
2. Kenney segue CC0 em tudo o que foi conferido (22 pacotes lidos, de arte e de áudio); é a base mais segura para UI, ícones, teclas, efeitos e jingles.
3. Arte de personagem e cenário no estilo "livro ilustrado de horta" deve ser autoral (Inkscape, GPL); o que existe pronto serve como base, placeholder ou referência — não achei pacote gratuito que já entregue o estilo aprovado.
4. Achado de licença: **Chewy e Luckiest Guy são Apache 2.0, não OFL**, e só têm o subconjunto "latin"; Fredoka, Baloo 2, Nunito, Lilita One, Sniglet, Bubblegum Sans e DynaPuff são OFL com "latin-ext".
5. Achado de disponibilidade: **FreePD.com fechou em definitivo** (a página diz isso); o substituto CC0 conferido para música é o Music Loop Bundle da Tallbeard/Abstraction (itch.io).
6. Cuidado com share-alike e atribuição: OpenMoji é CC BY-SA 4.0, game-icons.net é CC BY 3.0, Twemoji é CC BY 4.0, ZapSplat gratuito exige crédito e só entrega MP3.
7. Game UI Database (1.994 jogos, 79.453 telas) e Interface In Game existem e têm filtros que cobrem HUD, seleção de fase, escolha de jogadores e tela de resultado.
8. Números de legibilidade para adotar: texto mínimo de 18 px em PC a 1080p (26 px em TV), contraste 4,5:1 (3:1 para texto grande), e nunca depender só de cor (Xbox Accessibility Guidelines 101-103).
9. Para o design: os textos mais aproveitáveis são o de tutoriais do Plants vs. Zombies, o de falha justa do Fall Guys, o guia de pistas do SuperTuxKart (atalho só com risco) e o do F-Zero (velocidade paga com energia).
10. Ficaram sem verificação: licenças do Mixkit, conteúdo do Voiceover Pack da Kenney, pacote Vector Graphics do Unity, DragonBones, metas de loudness (LUFS) e postmortems de Runbow e Boomerang Fu — detalhes na última seção.

---

## 1. Game design — novas referências

| Fonte | Link | Princípio | Por que funciona | Como adaptar ao Healthy Race | Verificado? |
| --- | --- | --- | --- | --- | --- |
| George Fan, "How I Got My Mom to Play Through Plants vs. Zombies" (GDC 2012) | Resumo: <https://www.gamedeveloper.com/design/gdc-2012-10-tutorial-tips-from-i-plants-vs-zombies-i-creator-george-fan> — vídeo: <https://www.youtube.com/watch?v=fbzhHSexzpY> | 10 dicas de tutorial: misturar o tutorial ao jogo, fazer em vez de ler, espalhar as lições, pouco texto, mensagens passivas e adaptativas, visual que comunica função | O jogador aprende agindo uma vez, sem interrupção; a resistência é maior nos primeiros minutos | Fase 1 só com pulo e uma bifurcação; lanche e fruta apresentados um de cada vez; dica só aparece para quem errou duas vezes; lanche e fruta com silhueta e cor que já dizem "rápido" e "cura" | Sim (artigo lido; vídeo com título conferido) |
| Fall Guys — Q&A com Joe Walsh (Game Developer, 20/10/2020) | <https://www.gamedeveloper.com/design/q-a-designing-for-seasons-in-bumbling-platform-royale-i-fall-guys-i-> | Falha justa: o jogador deve sentir que o erro foi dele; rodadas simples e rejogáveis; dificuldade desigual de propósito | Quem atribui o erro a si mesmo quer tentar de novo; picos de dificuldade viram momentos memoráveis | Espinho e plataforma que cai sempre com aviso (tremor + som) antes de punir; uma fase "vilã" entre as 10; cada fase explicável em uma frase | Sim |
| SuperTuxKart — "Making Tracks: Advanced Gameplay Design" | <https://supertuxkart.net/Making_Tracks:_Advanced_Gameplay_Design> | Caminho principal equilibrado; atalho só existe se tiver risco; recompensa escondida para quem tenta o incomum | Mantém a escolha de rota como decisão, não como resposta certa única | A lanchonete é o atalho: o risco é a queda depois do pico. A estufa é o caminho principal: mais longa, mas previsível. Cesta dourada fora do trajeto óbvio | Sim |
| Open Surge wiki — "Level design" (sintetiza o fio "Sonic Level Design" do Sonic Retro) | <https://github.com/alemart/opensurge/wiki/Level-design> | Rotas alta, média e baixa: a alta é a mais rica em recompensa e mais difícil de manter; a baixa recebe quem cai e não pode ser cruel | O jogador escolhe a dificuldade ao escolher a rota; errar não tira ninguém do jogo | Andares da estufa como rotas: cair de andar leva a um trecho mais lento, nunca a um beco; frutas e água concentradas na rota que exige mais habilidade | Sim |
| F-Zero — boost pago com energia ("Dust and Destruction: The Racing Economy in F-Zero 99", Super Jump) | <https://www.superjumpmagazine.com/dust-and-destruction-the-racing-economy-in-f-zero-99/> | Velocidade agora em troca de um recurso que vai faltar depois; faixas de recarga devolvem o recurso | O ganho é imediato e visível, o custo é adiado — a decisão tem peso sem precisar de texto | É o modelo direto do lanche: pico imediato, queda depois. Fruta e água fazem o papel da faixa de recarga. Mostrar o custo futuro no HUD (ícone de sono enchendo durante o pico) | Parcial (página abriu; conteúdo via resumo de busca) |
| Mario Kart — defesa do casco azul (Kosuke Yabuki, via Game Developer, 24/07/2017) e justificativa original (Hideki Konno, Kotaku, 2011) | <https://www.gamedeveloper.com/design/-sometimes-life-isn-t-fair-i-mario-kart-i-director-defends-blue-shell-game-design> — <https://kotaku.com/the-maker-of-mario-kart-justifies-the-blue-shell-5780082> | Catch-up declarado: a equipe testou o jogo sem o item e achou que faltava algo; o objetivo é todos na disputa até o fim | Corrida decidida cedo deixa de ser divertida para os dois lados | Catch-up visível e nomeado (ex.: quem está atrás encontra mais fruta ou um vento a favor), nunca escondido; evitar punição direta ao líder, que frustra em jogo de 2 | Sim (Game Developer lido); Parcial (Kotaku abriu, conteúdo via busca) |
| Game Maker's Toolkit — "How Games Use Feedback Loops" | <https://www.youtube.com/watch?v=H4kbJObhcHw> | Laço de realimentação positivo (quem ganha, ganha mais) contra negativo (quem perde recebe ajuda) | Dá vocabulário para decidir quanto catch-up usar e onde | Mapear cada mecânica: pico é laço positivo, queda é negativo embutido. Conferir que a soma não torna a liderança irreversível antes da última bifurcação | Parcial (título e canal conferidos; vídeo não assistido) |
| TowerFall — "The magic of TowerFall: depth, simplicity, community" (Game Developer, 28/04/2015) | <https://www.gamedeveloper.com/design/the-magic-of-i-towerfall-i-depth-simplicity-community> | Simplicidade de entrada, profundidade escondida no mesmo botão, personagens só cosméticos, efeitos para o espectador conseguir ler | Novato joga em segundos; veterano descobre técnica; quem assiste entende | Os dois brotos idênticos em mecânica, diferentes só em cor e folha. Rastro e partículas no pico para a plateia da sala ler quem está acelerado | Sim |
| "The challenges and rewards of designing local multiplayer games" (Game Developer, 23/11/2016) | <https://www.gamedeveloper.com/design/the-challenges-and-rewards-of-designing-local-multiplayer-games> | Projetar para o que só o jogo local tem: ver e falar com o outro, tela compartilhada, plateia; testar cedo com arte provisória | A diversão vem da sala, não só da tela | Testar com pares reais de 10 a 16 anos desde o protótipo com placeholders Kenney; pensar em quem está assistindo na fila | Sim |
| Trackmania — medalhas (documentação oficial) + Angry Birds — "The making of an Angry Birds level" (Pocket Gamer) | <https://doc.trackmania.com/play/what-is-a-seasonal-campaign/> — <https://www.pocketgamer.com/angry-birds/exclusive-the-making-of-an-angry-birds-level/> | Quatro degraus (bronze, prata, ouro, autor) no mesmo percurso; a Rovio diz gastar mais tempo calibrando as estrelas do que montando a fase, com base em playtest | Um mesmo conteúdo serve a níveis diferentes de habilidade; o degrau seguinte está sempre à vista | Definir as 3 estrelas por dado de playtest, não por chute: 1 = terminar, 2 = tempo-alvo ou sem queda, 3 = cesta dourada ou tempo de "autor" (o da própria equipe) | Sim |
| "Juice it or lose it" (Jonasson e Purho), "The art of screenshake" (Jan Willem Nijman) e GMTK "Secrets of Game Feel and Juice" | <https://www.youtube.com/watch?v=Fy0aCDmgnxg> — <https://www.youtube.com/watch?v=AJdEqssNZ-U> — <https://www.youtube.com/watch?v=216_5nu4aVQ> | Resposta exagerada a cada ação: squash e stretch, partículas, som, pausa de impacto | Complementa Swink com receita prática e barata | Coleta de fruta com "pop" e som; aterrissagem com poeira; início do pico com esticada do broto. Tremor de tela com parcimônia, porque a câmera é compartilhada | Parcial (títulos e canais conferidos) |
| Tutorial sem texto: "Sequelitis — Mega Man Classic vs. Mega Man X" (Egoraptor), GMTK "Half-Life 2's Invisible Tutorial", e "Level Design Workshop: Designing Celeste" (GDC) | <https://www.youtube.com/watch?v=8FpigqfcvlM> — <https://www.youtube.com/watch?v=MMggqenxuZc> — <https://www.youtube.com/watch?v=4RlpMhBKNr0> | Ensinar pelo desenho da fase: apresentar o elemento em lugar seguro, depois cobrar, depois combinar | O jogador sente que descobriu sozinho | Primeira lombada sem buraco depois; primeira alavanca com o portão visível na mesma tela; primeiro lanche logo antes de uma reta, para o pico ser sentido | Parcial (títulos e canais conferidos). O vídeo do Egoraptor tem linguagem adulta: é referência para a equipe, não para mostrar aos alunos |

### Consultado, com aderência menor

- "Move or Die: Why It's Worth It to Chase Your Tail" (GDC 2018, Nicolae Berbece): <https://gdcvault.com/play/1024982/-Move-or-Die-Why> — a página abriu; o tema é pós-lançamento e comunidade, não design de rodada.
- "Ask the Developer vol. 11 — Super Mario Bros. Wonder", parte 1: <https://www.nintendo.com/us/whatsnew/ask-the-developer-vol-11-super-mario-bros-wonder-part-1/> — abriu, mas a parte 1 não trata de multijogador na mesma tela; as partes 2 a 4 não foram lidas.

---

## 2. UI/UX de jogos

### 2.1 Bancos de referência

| Fonte | Link | O que oferece | Como filtrar para o Healthy Race | Verificado? |
| --- | --- | --- | --- | --- |
| Game UI Database 2.0 (Edd Coates) | <https://www.gameuidatabase.com/> | 1.994 jogos e 79.453 telas (contagem lida na página), mais vídeos. Busca por jogo, cor (hex) e texto na imagem. Visualizadores embutidos: escala de cinza, alto contraste e simulação de protanopia, deuteranopia e tritanopia | Menu **Screen Categories**: "All Gameplay & HUDs"; em PRE-GAME, "Character Select", "Stage/Level Select", "Choose Players"; em GAME END, "Level Complete", "Results Screen", "Leaderboards & Ranking"; em TITLE, "Title Screen" e "Mode & Screen Select"; em MID-GAME, "Pause" e "Stage Intro". Cruzar com **Genre** (Platformer, Racing), **Theme** (Cartoon, Wholesome), **Age Rating** (ESRB Everyone, PEGI 3/7), **Game Aesthetic** (2D Art), **Camera** (Side View) e **UI Style** (Flat, Art & Vector) | Sim (lido via curl; o WebFetch recebeu 403) |
| Interface In Game | <https://interfaceingame.com/> | Capturas e vídeos de interface por jogo. Categorias de captura com contagem: Menu (6.347), In game (5.366), Overlay (3.021), entre 21 tipos (Settings, Map, Game over…) | Página de capturas com filtro por categoria: <https://interfaceingame.com/screenshots/> ; jogos por gênero e por tema (Cartoon existe como tema). Páginas de jogo que abriram: Overcooked! 2, Fall Guys, Ultimate Chicken Horse, Rayman Legends, It Takes Two, Unrailed, Sonic Mania, Celeste, TowerFall Ascension, Heave Ho, Trackmania Turbo (padrão `interfaceingame.com/games/<slug>/`) | Sim (home lida; páginas de jogo com HTTP 200, capturas não inspecionadas) |

Não existem no Interface In Game (404 nos endereços tentados): Mario Kart 8, Moving Out, Gang Beasts, Cuphead, Among Us, Super Mario Bros. Wonder, Crash Team Racing.

### 2.2 Exemplos concretos — o que observar

| Necessidade | Onde olhar | O que observar | Como aplicar | Verificado? |
| --- | --- | --- | --- | --- |
| HUD mínimo em multijogador de mesma tela | Interface In Game: Rayman Legends, Heave Ho, TowerFall Ascension; Game UI Database: "All Gameplay & HUDs" + Platformer | HUD preso aos cantos por jogador, cor do jogador repetida no personagem e no HUD, quase nenhum número | Jogador 1 no canto esquerdo, jogador 2 no direito; só o estado (pico, queda) e as estrelas parciais; nada no centro, que é onde a ação acontece | Parcial (páginas existem; capturas não vistas) |
| Barra de progresso de corrida com ícones | Game UI Database: Genre "Racing" + "All Gameplay & HUDs"; Interface In Game: Trackmania Turbo, Fall Guys | Trilho horizontal com cabeças dos corredores, marcas de checkpoint e bandeira no fim | Trilho fino no topo com as duas cabeças de broto; marcar as bifurcações com um losango; a cabeça pisca no pico e fica cinza-azulada na queda | Parcial |
| Tela de resultado com estrelas | Interface In Game: Overcooked! 2; Game UI Database: "Results Screen" e "Level Complete" | Estrelas entram uma a uma com som crescente; o critério de cada estrela aparece ao lado | Duas colunas (uma por jogador), estrelas com rótulo curto ("Chegou", "Sem queda", "Cesta dourada"); vencedor destacado por posição e coroa, não só por cor | Parcial |
| Tela "pressione para entrar / pronto" | Interface In Game: TowerFall Ascension, Heave Ho, Ultimate Chicken Horse; Game UI Database: "Choose Players" | Cada jogador tem um painel; estado vazio mostra a tecla a apertar; estado pronto é inconfundível | Dois painéis com o desenho das teclas de cada lado do teclado (Kenney Input Prompts); broto acena ao entrar; "pronto" quando os dois seguram a tecla | Parcial |
| Seleção de fase com estrelas | Game UI Database: "Stage/Level Select" e "World Map"; Interface In Game: Overcooked! 2, Celeste | Caminho ou mapa com nós, estrelas sob cada nó, fase travada com cadeado além de cinza | Caminho de horta com 10 canteiros; sob cada um, as estrelas dos dois jogadores em duas linhas com a cor e o ícone de folha de cada um | Parcial |
| Indicador de jogador fora da tela | SmashWiki, "Magnifying-Glass Damage": <https://www.ssbwiki.com/Magnifying_glass> | Smash mostra o personagem numa lupa presa à borda quando ele sai do enquadramento | Bolha na borda com a cabeça do broto e uma seta; complementa a solução de câmera já estudada no SpeedRunners | Sim (página abriu e trata do tema) |

### 2.3 Guias de legibilidade, contraste e daltonismo

| Fonte | Link | O que diz | Como aplicar | Verificado? |
| --- | --- | --- | --- | --- |
| Xbox Accessibility Guidelines 101 — Text display | <https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/101> | Tamanho mínimo padrão: 26 px a 1080p em console/TV e 18 px a 1080p em PC (52 e 36 px em 4K); medida do topo da ascendente à base da descendente; ícones com texto seguem a mesma regra; permitir escala até 200%; pelo menos uma opção sem serifa; fonte com todos os caracteres do idioma | O jogo é de PC, mas dois jogadores dividem um monitor e ficam mais longe: adotar 26 px como piso do HUD e 18 px como piso absoluto. Conferir os acentos do português na fonte escolhida | Sim |
| Xbox Accessibility Guidelines 102 — Contrast | <https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/102> | 4,5:1 para texto e elementos de tamanho normal; 3:1 para texto grande e elementos inativos; 7:1 em modo de alto contraste; medir contra a parte de menor contraste do fundo; contorno em texto e ícones ajuda | O contorno escuro grosso do estilo aprovado já resolve boa parte: aplicar o mesmo contorno a todo ícone e número do HUD, porque o fundo da estufa varia | Sim |
| Xbox Accessibility Guidelines 103 — Additional channels | <https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/103> | Informação crítica nunca só por cor: somar forma, padrão, ícone ou rótulo; pista visual importante também em áudio e vice-versa | Jogadores distintos por cor **e** por forma da folha; pico = raio + rastro + som; queda = "Zzz" + personagem caído + som. Lanche e fruta distintos pela silhueta | Sim |
| Okabe e Ito — Color Universal Design | <https://jfly.uni-koeln.de/color/> | Paleta de 8 cores pensada para ser distinguível por pessoas com daltonismo | Escolher as cores dos dois jogadores dentro dela (laranja e azul é o par mais seguro; evitar vermelho contra verde, que é justamente o par "comida ruim × planta") | Parcial (página abriu; valores das cores não relidos) |
| "Coloring for Colorblindness" (David Nichols) | <https://davidmathlogic.com/colorblind/> | Ferramenta para testar paleta em simulações de daltonismo | Testar a paleta final do HUD antes de pintar tudo | Parcial (página abriu) |
| Color Oracle | <https://colororacle.org/> | Simulador de daltonismo para a tela inteira, gratuito | Rodar com o jogo aberto em cada fase; o Game UI Database tem filtro equivalente para as referências | Parcial (página abriu) |
| "Designing for Xbox and TV" (Microsoft) | <https://learn.microsoft.com/en-us/windows/apps/design/devices/designing-for-tv> | Princípios da "experiência de 3 metros": pouca informação por tela, foco inconfundível | Menus com poucos itens grandes e foco bem marcado, navegáveis pelas teclas dos dois jogadores | Parcial (princípios lidos; números de área segura não localizados) |

---

## 3. Sprites e arte

### 3.1 Kenney — tudo CC0, licença lida em cada página

A página de suporte (<https://kenney.nl/support>) diz que os assets são de domínio público (CC0), uso comercial permitido, atribuição não exigida (crédito a "Kenney" é bem-vindo) e pede para não usar o logotipo Kenney isoladamente.

| Fonte | Link | O que serve | Licença | Atribuição | Verificado? |
| --- | --- | --- | --- | --- | --- |
| New Platformer Pack (2025, 440 arquivos) | <https://kenney.nl/assets/new-platformer-pack> | Tiles, personagens e fundos de plataforma; melhor base de placeholder e de proporção | CC0 | Não exigida | Sim |
| Platformer Art Deluxe (930 arquivos) | <https://kenney.nl/assets/platformer-art-deluxe> | Maior acervo de tiles e objetos de plataforma | CC0 | Não exigida | Sim |
| Simplified Platformer Pack (90) / Abstract Platformer (370) / Jumper Pack (110) / Scribble Platformer (100) | <https://kenney.nl/assets/simplified-platformer-pack> — <https://kenney.nl/assets/abstract-platformer> — <https://kenney.nl/assets/jumper-pack> — <https://kenney.nl/assets/scribble-platformer> | Blocos simples para greybox das 10 fases | CC0 | Não exigida | Sim |
| Toon Characters (270) / Shape Characters (100) | <https://kenney.nl/assets/toon-characters-1> — <https://kenney.nl/assets/shape-characters> | Referência de poses e de animação por partes | CC0 | Não exigida | Sim |
| Foliage Pack (100) / Background Elements Remastered (90) | <https://kenney.nl/assets/foliage-pack> — <https://kenney.nl/assets/background-elements-redux> | Plantas, árvores e elementos de fundo para a estufa | CC0 | Não exigida | Sim |
| UI Pack (430, versão 2.0 refeita) | <https://kenney.nl/assets/ui-pack> | Botões, painéis, sliders | CC0 | Não exigida | Sim |
| Game Icons (105) / Board Game Icons (250) / Emotes Pack (480) | <https://kenney.nl/assets/game-icons> — <https://kenney.nl/assets/board-game-icons> — <https://kenney.nl/assets/emotes-pack> | Ícones de interface e balões de emoção | CC0 | Não exigida | Sim (licença e contagem; lista de ícones não conferida) |
| Input Prompts (1.500, versão 1.5a) | <https://kenney.nl/assets/input-prompts> | Glifos de teclado e mouse e de controles, para a tela de entrada e o tutorial | CC0 | Não exigida | Sim |
| Particle Pack (80) | <https://kenney.nl/assets/particle-pack> | Partículas para pico, coleta e aterrissagem | CC0 | Não exigida | Sim |
| Food Kit (200) | <https://kenney.nl/assets/food-kit> | Comida — **é 3D**, não serve direto; só como referência de lista de itens | CC0 | Não exigida | Sim |

Kenney não tem pacote 2D de comida entre os que foram conferidos.

### 3.2 Outras fontes de arte e ícones

| Fonte | Link | O que serve | Licença | Atribuição | Verificado? |
| --- | --- | --- | --- | --- | --- |
| Glitch (Tiny Speck) no OpenGameArt | <https://opengameart.org/content/glitch-sprite-assets-huge-collection> — <https://opengameart.org/content/glitch-misc-assets-svg> | Arte vetorial de um jogo comercial encerrado; o pacote SVG tem ao menos 500 itens (sementes, ferramentas, ingredientes, troféus); os sprites vêm em dois arquivos 7z de cerca de 185 MB | CC0 | Não exigida; pedem, se quiser, link para glitchthegame.com | Sim (páginas do OpenGameArt lidas). A página oficial glitchthegame.com/public-domain-game-art devolveu 404 nesta pesquisa |
| OpenGameArt (busca geral) | <https://opengameart.org/art-search-advanced> | Busca com filtro de licença (CC0, CC-BY 3.0/4.0, CC-BY-SA, OGA-BY, GPL) e tipo de arte | Varia por item | Formato sugerido no FAQ: "nome do asset" por autor, licença, URL. O FAQ avisa que CC-BY e CC-BY-SA não podem ser distribuídos com DRM; OGA-BY remove essa restrição | Sim (FAQ lido via curl) |
| Openclipart | <https://openclipart.org/> (FAQ: <https://openclipart.org/share>) | Mais de 180 mil cliparts vetoriais; buscar frutas, lanches, plantas, ferramentas de jardim | Domínio público (o FAQ diz que o acervo é inteiro de domínio público) | Não exigida | Sim (frase do FAQ lida) |
| itch.io — assets gratuitos | <https://itch.io/game-assets/free/tag-vector> — filtro CC0: <https://itch.io/game-assets/assets-cc0> | Pacotes vetoriais de autores independentes | **Não há licença única**: cada página define a sua | Conforme a página do asset | Parcial (as duas listagens abriram; nenhum asset individual conferido) |
| CraftPix freebies | <https://craftpix.net/freebies/> (licença: <https://craftpix.net/file-licenses/>) | Cenários, personagens e GUIs em estilo cartoon | Licença própria | Não exigida. Proibido revender ou redistribuir os arquivos-fonte e proibido usar para treinar IA; exige login | Sim (licença lida) |
| OpenMoji | <https://openmoji.org/> | 4.495 emojis em SVG e PNG, versão colorida e versão só contorno preto; é o conjunto mais próximo do contorno escuro | CC BY-SA 4.0 | Exigida, e derivados (ícone redesenhado a partir dele) ficam sob a mesma licença | Sim |
| Twemoji (fork mantido por jdecked) | <https://github.com/jdecked/twemoji> | Emojis chapados | Gráficos CC BY 4.0; código MIT | Exigida; o projeto aceita menção numa tela "Sobre" ou nos créditos | Sim |
| Noto Emoji | <https://github.com/googlefonts/noto-emoji> | Emojis chapados | Fontes OFL 1.1; imagens e ferramentas Apache 2.0 | Não exigida no jogo; manter o texto da licença ao redistribuir | Sim |
| Fluent Emoji (Microsoft) | <https://github.com/microsoft/fluentui-emoji> | Emojis em várias variantes | MIT (rótulo do repositório) | Manter aviso de copyright e licença | Parcial (rótulo MIT lido; arquivo LICENSE não aberto) |
| Game-icons.net | <https://game-icons.net/> (sobre: <https://game-icons.net/about.html>) | Cerca de 4.180 ícones SVG; bons para silhuetas de HUD | CC BY 3.0 | Exigida: "Icons made by {autor}. Available on <https://game-icons.net"> | Sim |
| Phosphor Icons | <https://phosphoricons.com/> (repo: <https://github.com/phosphor-icons/core>) | 1.248 ícones em 6 pesos; **Bold** e **Fill** combinam com contorno grosso | MIT | Manter aviso de copyright e licença junto ao build | Sim |
| Tabler Icons | <https://github.com/tabler/tabler-icons> | 6.287 ícones: 5.233 de contorno e 1.054 preenchidos | MIT | Idem | Sim |
| Lucide | <https://lucide.dev/license> | Ícones de contorno fino (menos compatível com o estilo) | ISC; parte derivada do Feather sob MIT | Manter aviso de copyright e permissão | Sim |

### 3.3 Fontes tipográficas

Licença e subconjuntos lidos no arquivo `METADATA.pb` de cada família no repositório <https://github.com/google/fonts> (o diretório — `ofl/` ou `apache/` — já indica a licença). A página de licença do Google Fonts não devolveu o texto ao leitor automático.

| Fonte | Link | O que serve | Licença | Atribuição | Verificado? |
| --- | --- | --- | --- | --- | --- |
| Fredoka | <https://fonts.google.com/specimen/Fredoka> | Arredondada, variável em peso e largura; títulos, HUD e botões | OFL | Não exigida no jogo; incluir o `OFL.txt` no build; não vender a fonte isolada | Sim (OFL; latin e latin-ext) |
| Baloo 2 | <https://fonts.google.com/specimen/Baloo+2> | Arredondada e pesada, variável em peso; títulos | OFL | Idem | Sim (OFL; latin e latin-ext) |
| Nunito | <https://fonts.google.com/specimen/Nunito> | Arredondada e mais neutra; textos corridos e menus | OFL | Idem | Sim (OFL; latin e latin-ext) |
| Lilita One | <https://fonts.google.com/specimen/Lilita+One> | Peso único, muito pesada; logotipo e "3-2-1-JÁ" | OFL | Idem | Sim (OFL; latin e latin-ext) |
| Sniglet / Bubblegum Sans / DynaPuff | <https://fonts.google.com/specimen/Sniglet> — <https://fonts.google.com/specimen/Bubblegum+Sans> — <https://fonts.google.com/specimen/DynaPuff> | Alternativas arredondadas e infantis | OFL | Idem | Sim (OFL; latin e latin-ext) |
| Chewy | <https://fonts.google.com/specimen/Chewy> | Display | **Apache 2.0** (não é OFL) | Manter o texto da licença e avisos | Sim (Apache 2.0; só subconjunto "latin") |
| Luckiest Guy | <https://fonts.google.com/specimen/Luckiest+Guy> | Display pesada | **Apache 2.0** | Idem | Sim (Apache 2.0; só subconjunto "latin") |
| Kenney Fonts (11 arquivos) | <https://kenney.nl/assets/kenney-fonts> | Fontes de jogo; a etiqueta da página fala em "pixel" | CC0 | Não exigida | Sim (licença); estilo não conferido |

Em todas, vale digitar "ÁÉÍÓÚ ÂÊÔ ÃÕ Ç À áéíóú âêô ãõ ç à" no Unity (TextMesh Pro) antes de adotar: o subconjunto "latin" costuma cobrir o português, mas isso não foi conferido glifo a glifo, sobretudo em Chewy e Luckiest Guy.

### 3.4 Ferramentas gratuitas

| Fonte | Link | O que serve | Licença | Atribuição | Verificado? |
| --- | --- | --- | --- | --- | --- |
| Inkscape | <https://inkscape.org/> | Desenho vetorial com contorno grosso e cor chapada; exporta PNG e SVG. É a ferramenta central do estilo aprovado | GPL (v2 ou posterior no código; binários sob GPL v3 ou posterior) | Nenhuma sobre a arte produzida | Sim (arquivo COPYING lido no GitLab; a página de licença do site deu 403) |
| Krita | <https://krita.org/> | Pintura e rascunho; animação quadro a quadro; exporta PSD em camadas | GPL v3; a página afirma uso livre para qualquer fim, inclusive trabalho comercial | Nenhuma sobre a arte produzida | Sim |
| Unity 2D Animation | <https://docs.unity3d.com/Packages/com.unity.2d.animation@latest> | Ossos e skinning dentro do Unity: animar o broto por partes (cabeça, folha, corpo, pés) sem redesenhar quadros | Pacote oficial do Unity | — | Parcial (página do pacote abriu; conteúdo não lido) |
| Unity 2D PSD Importer | <https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest> | Importa PSD em camadas (do Krita) já separado em partes para o rig | Pacote oficial do Unity | — | Parcial |
| Unity Vector Graphics | <https://docs.unity3d.com/Packages/com.unity.vectorgraphics@latest> | Importa SVG direto | Pacote oficial do Unity | — | Parcial (página abriu; situação do pacote no Unity 6 não conferida). Caminho mais simples e seguro: exportar PNG do Inkscape no dobro da resolução-alvo |

Aseprite e Spine são pagos e não entram. DragonBones não foi verificado (ver última seção).

---

## 4. Efeitos sonoros e música

### 4.1 Geradores

| Fonte | Link | O que serve | Licença | Atribuição | Verificado? |
| --- | --- | --- | --- | --- | --- |
| jsfxr | <https://sfxr.me/> | Gerador no navegador; presets: pickup/coin, powerup, hit/hurt, jump, click, blip/select, explosion, laser, synth, tone; exporta WAV | Código sob Unlicense; a tabela da página lista uso comercial irrestrito também no plano gratuito | Não exigida | Sim |
| sfxr original (DrPetter) | <https://www.drpetter.se/project_sfxr.html> | Versão de desktop original | Código MIT; o autor diz que é livre para qualquer uso | Não exigida | Sim |
| ChipTone (SFBGames) | <https://sfbgames.itch.io/chiptone> | Gerador mais flexível (HTML5, Windows, macOS) | A página declara os sons gerados livres para qualquer fim, comercial ou não, sob CC0 | Não exigida | Sim |
| Bfxr | <https://www.bfxr.net/> (código: <https://github.com/increpare/bfxr2>) | Variante do sfxr com mais parâmetros; inclui o gerador de passos Footsteppr | Código MIT; **a página não fala dos direitos sobre os sons gerados** | Não informada | Parcial |
| LabChirp | <https://labbed.net/software/labchirp/> | Gerador para Windows com até 8 canais e gravação em lote | Gratuito (aceita doação); sem licença nomeada e **sem declaração sobre os sons gerados** | Não informada | Parcial |

O timbre desses geradores é "retrô". Com o estilo de livro ilustrado, convém usar ondas suaves (senoide, triângulo), ataques curtos e combinar com os sons orgânicos da Kenney.

### 4.2 Bibliotecas

| Fonte | Link | O que serve | Licença | Atribuição | Verificado? |
| --- | --- | --- | --- | --- | --- |
| Kenney — Interface Sounds (100) | <https://kenney.nl/assets/interface-sounds> | Cliques, confirmações, navegação de menu | CC0 | Não exigida | Sim |
| Kenney — UI Audio (50) | <https://kenney.nl/assets/ui-audio> | Botões, chaves, cliques (alavanca) | CC0 | Não exigida | Sim |
| Kenney — Impact Sounds (130) | <https://kenney.nl/assets/impact-sounds> | Impactos e foley: aterrissagem, queda de plataforma, batida | CC0 | Não exigida | Sim |
| Kenney — Digital Audio (60) | <https://kenney.nl/assets/digital-audio> | Tons sintéticos: coleta, power-up | CC0 | Não exigida | Sim |
| Kenney — Music Jingles (85) | <https://kenney.nl/assets/music-jingles> | Vinhetas curtas: vitória, derrota, estrela, checkpoint | CC0 | Não exigida | Sim |
| Kenney — RPG Audio (50) | <https://kenney.nl/assets/rpg-audio> | Foley e passos | CC0 | Não exigida | Sim |
| Kenney — Voiceover Pack (90) | <https://kenney.nl/assets/voiceover-pack> | Vozes masculina e feminina; candidato para a contagem | CC0 | Não exigida | Sim (licença); **lista de falas e idioma não conferidos** |
| Kenney — Casino Audio, Sci-fi Sounds, Voiceover Pack (Fighter) | <https://kenney.nl/assets/category:Audio> | Menos úteis aqui | CC0 (Casino lido; os outros dois só vistos na listagem) | Não exigida | Parcial |
| Freesound | <https://freesound.org/> (FAQ: <https://freesound.org/help/faq/>) | Sons orgânicos difíceis de sintetizar: bocejo, portão, elevador, mordida | Por som: CC0, CC BY ou CC BY-NC. A busca tem filtro de licença | CC BY: nome do som, usuário, link e licença, numa linha ou numa página de créditos; o site gera a lista dos sons baixados em freesound.org/home/attribution/. Evitar CC BY-NC | Sim |
| Pixabay (sound effects e música) | <https://pixabay.com/sound-effects/> (licença: <https://pixabay.com/service/license-summary/>) | Efeitos e músicas variados | Pixabay Content License: uso gratuito, modificável; proibido redistribuir o arquivo isolado | Não exigida | Sim (resumo da licença lido; a página de efeitos bloqueou o acesso automático) |
| Sonniss — GDC Game Audio Bundle | <https://sonniss.com/gameaudiogdc/> — 2026: <https://gdc.sonniss.com/> | Pacotes profissionais anuais (2015-2024, de 9 a 48 GB cada; 2026 com 7,47 GB) | Royalty-free, uso comercial, projetos ilimitados; proibido redistribuir como biblioteca de sons e proibido treinar IA | Não exigida | Sim |
| ZapSplat | <https://www.zapsplat.com/> (licença: <https://www.zapsplat.com/license-type/standard-license/>) | Biblioteca grande de efeitos e música | Licença própria. Conta gratuita: só MP3, com limite de downloads | **Exigida** na conta gratuita: crédito claro a "ZapSplat" nos créditos. Proibido redistribuir e treinar IA | Sim (via curl; o WebFetch recebeu 403) |
| Mixkit | <https://mixkit.co/free-sound-effects/> (licenças: <https://mixkit.co/license/>) | Efeitos e músicas | "Sound Effects Free License" e "Stock Music Free License" — **o texto abre numa janela carregada por script e não foi lido** | Não confirmada | Parcial |
| Incompetech (Kevin MacLeod) | <https://incompetech.com/music/royalty-free/> (FAQ: <https://incompetech.com/music/royalty-free/faq.html>) | Músicas em muitos estilos | CC BY 4.0 | Exigida, em três linhas: `Título Kevin MacLeod (incompetech.com)` / `Licensed under Creative Commons: By Attribution 4.0` / `<https://creativecommons.org/licenses/by/4.0/`> — em lugar fácil de achar | Sim |
| Tallbeard Studios / Abstraction — Music Loop Bundle | <https://tallbeard.itch.io/music-loop-bundle> | Loops de música feitos para jogo (etiquetas da página: seamless, upbeat, chiptune, ambient) | CC0 (declarado na página) | Não exigida; se quiser, creditar "Abstraction" com link. O autor pede para não usar em NFT, IA ou revenda | Sim |
| Soundimage.org (Eric Matyas) | <https://soundimage.org/> (licença: <https://soundimage.org/sample-page/>) | Músicas e efeitos para jogos | "Soundimage International Public License" (licença própria) | Há página "Attribution Info": tratar como exigida | Parcial (existência da licença lida; termos não lidos por inteiro) |
| OpenGameArt (música e sons) | <https://opengameart.org/art-search-advanced> | Música e efeitos com filtro de licença | Varia; preferir CC0 | Conforme o item | Sim (filtro conferido) |
| FreePD | <https://freepd.com/> | — | **Site encerrado**: a página informa fechamento definitivo (2008-2025) | — | Sim |

### 4.3 Sons que o jogo precisa e onde achar

| Som | Fonte sugerida | Observação |
| --- | --- | --- |
| Pulo | jsfxr (preset Jump) ou ChipTone | Um som por jogador, com alturas diferentes |
| Aterrissagem | Kenney Impact Sounds | Volume baixo; 3 ou 4 variações |
| Passo | Kenney RPG Audio (footstep) ou Footsteppr do Bfxr | Opcional; em corrida rápida costuma poluir |
| Coleta de fruta | jsfxr (Pickup/coin) ou Kenney Digital Audio | Tom ascendente, "limpo" |
| Coleta de lanche | jsfxr (Powerup) + mordida do Freesound (filtro CC0) | Mais gordo e chamativo que o da fruta: a tentação tem de soar bem |
| Início do pico (whoosh, faísca) | jsfxr (Powerup, varredura para cima); whoosh no Freesound CC0 ou Sonniss | Acompanhar com rastro visual |
| Início da queda (bocejo, desaceleração) | Varredura descendente no ChipTone; bocejo no Freesound CC0 | Dá para gravar o bocejo com o celular: fica autoral e sem licença a gerir |
| Cura da queda | Kenney Digital Audio ou jsfxr (Powerup curto), arpejo ascendente | Parente do som da fruta |
| Espinho, dano | jsfxr (Hit/hurt) ou Kenney Impact Sounds | Curto e seco, sem assustar |
| Plataforma tremendo e caindo | Kenney Impact Sounds: tremor em loop curto + queda | O tremor é o aviso — tem de vir antes da queda |
| Alavanca | Kenney UI Audio (switch) | — |
| Portão abrindo | Freesound CC0 (gate, door slide) ou Sonniss | — |
| Elevador | Loop mecânico do Freesound CC0 + "ding" da Kenney Interface Sounds | — |
| Checkpoint | Kenney Music Jingles (vinheta curta) ou Interface Sounds | — |
| Contagem 3-2-1-JÁ | Três bipes iguais e um mais agudo (jsfxr, Blip/select); voz: gravar a própria equipe em português | O Voiceover Pack da Kenney não teve falas nem idioma conferidos |
| Chegada | Kenney Music Jingles | — |
| Vitória / derrota | Kenney Music Jingles (uma ascendente, uma descendente) | Derrota leve, sem humilhar: os dois estão na mesma sala |
| Estrela ganha | Kenney Interface Sounds ou Digital Audio, com altura subindo a cada estrela | Ver boas práticas |
| Botão de menu | Kenney Interface Sounds / UI Audio | Um para navegar, um para confirmar, um para voltar |
| Cesta dourada | Kenney Music Jingles ou arpejo no ChipTone | O som mais "rico" do jogo |
| Música de menu | Tallbeard/Abstraction Music Loop Bundle (CC0) | Faixa calma |
| Música de fase (alegre, 100-130 bpm) | Mesmo pacote; opção com crédito: Kevin MacLeod | O bpm das faixas não foi conferido: medir ao escolher |
| Variação de "reta final" | A mesma faixa com `AudioSource.pitch` entre 1,06 e 1,12, ou uma camada de percussão extra | Sugestão desta pesquisa, não de uma fonte |

A correspondência entre cada som e o pacote é a aposta mais provável pelo nome e pelas etiquetas dos pacotes; os arquivos não foram ouvidos.

### 4.4 Boas práticas curtas de áudio

| Prática | Referência | Verificado? |
| --- | --- | --- |
| Variar altura e volume a cada repetição e não tocar o mesmo clipe duas vezes seguidas; o Unity 6 tem o Audio Random Container para isso sem código | <https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioRandomContainer.html> — <https://gamedevbeginner.com/how-to-use-the-random-audio-container-in-unity/> — <https://www.gamedeveloper.com/audio/the-power-of-pitch-shifting> | Parcial (as três páginas abriram; a do manual foi lida em parte) |
| Separar grupos Música, Efeitos e Interface no Audio Mixer, com volumes ajustáveis pelo jogador | <https://docs.unity3d.com/6000.0/Documentation/Manual/AudioMixer.html> | Parcial (página abriu) |
| Toda pista importante em dois canais: som e imagem. O jogo pode ser jogado em sala barulhenta ou sem som | XAG 103 (link no bloco 2.3) | Sim |
| Identidade sonora por jogador: mesma família de sons, alturas diferentes, e leve panorâmica (jogador 1 um pouco à esquerda, jogador 2 à direita) | Sugestão desta pesquisa, coerente com XAG 103 e com o texto do TowerFall sobre legibilidade | Não (não vem de uma fonte) |
| Som com resposta imediata e exagerada para cada ação | "Juice it or lose it" (link no bloco 1) | Parcial |
| Música abaixo dos efeitos; baixar a música (ducking) em eventos como a contagem e a chegada | Conhecimento geral; a página do efeito Duck Volume do manual do Unity não foi localizada | Não |

---

## Recomendação de kit mínimo gratuito

O conjunto mais simples e seguro: **só CC0, MIT e OFL**. Nenhum item exige crédito na tela; basta uma pasta de licenças no build.

| Necessidade | De onde tirar | Licença | Obrigação |
| --- | --- | --- | --- |
| Personagens, cenário, comida | Arte autoral no Inkscape, no estilo aprovado. Greybox e proporções com Kenney New Platformer Pack; ideias de itens no pacote SVG do Glitch e no Openclipart | Própria / CC0 / domínio público | Nenhuma |
| Ícones do HUD (raio, sono, coração, estrela, gota, folha) | Phosphor Icons nos pesos Bold ou Fill, redesenhados com o contorno do jogo; complemento com Kenney Game Icons e Emotes Pack | MIT / CC0 | Incluir o texto da licença MIT do Phosphor |
| Teclas na tela de entrada e no tutorial | Kenney Input Prompts | CC0 | Nenhuma |
| Painéis e botões | Desenho próprio no mesmo estilo; Kenney UI Pack como base | CC0 | Nenhuma |
| Fonte | Fredoka para títulos, HUD e botões; Nunito para textos | OFL | Incluir o `OFL.txt` de cada uma |
| Efeitos sonoros | Kenney Interface Sounds, UI Audio, Impact Sounds, Digital Audio e RPG Audio + sons gerados no jsfxr e no ChipTone + gravações próprias (bocejo, contagem em português) | CC0 / sem restrição | Nenhuma |
| O que faltar (portão, elevador, mordida) | Freesound **somente com filtro CC0** | CC0 | Nenhuma; anotar o link de cada som |
| Vinhetas (vitória, derrota, estrela, checkpoint, cesta dourada) | Kenney Music Jingles | CC0 | Nenhuma |
| Música de menu, de fase e reta final | Tallbeard/Abstraction Music Loop Bundle; reta final com a mesma faixa acelerada | CC0 | Nenhuma (crédito a "Abstraction" é cortesia) |

**O que deixar de fora para manter o kit simples:** OpenMoji (CC BY-SA, obriga derivados à mesma licença), game-icons.net e Twemoji (crédito obrigatório), ZapSplat gratuito (crédito e só MP3), CraftPix (licença própria e login), Mixkit (licença não lida), Chewy e Luckiest Guy (Apache e cobertura de acentos não conferida).

**Se quiserem uma música mais elaborada:** uma faixa de Kevin MacLeod, com as três linhas de crédito do FAQ na tela de créditos.

### Higiene de licença (vale para qualquer escolha)

- Manter no repositório um `CREDITOS.md` com uma linha por asset: nome, autor, link, licença e data do download.
- Guardar junto de cada pacote o arquivo de licença que vem no zip.
- Tela de créditos no jogo, mesmo sem obrigação: agradecer Kenney, Abstraction, Phosphor e os autores das fontes.
- Antes de usar qualquer asset do itch.io ou do OpenGameArt, ler a licença **na página daquele asset**.

---

## Não consegui verificar

### Página não abriu ou não entregou o conteúdo

- **Mixkit** — a página de licenças abriu, mas os textos da "Sound Effects Free License" e da "Stock Music Free License" carregam por script e não foram lidos. Não usar sem ler.
- **Iwata Asks — New Super Mario Bros. Wii** (multijogador na mesma tela): erro de certificado TLS (`unable to verify the first certificate`). Nenhuma fonte alternativa foi aberta.
- **GameSpot — "Fall Guys: Here's How The Developers Make Its Levels"**: 403. Substituído pelo Q&A do Game Developer, que abriu.
- **GamesRadar — entrevista do Fall Guys** (mistura de caos e habilidade): só o título veio; a proporção "80% habilidade, 20% caos" aparece apenas em resumo de busca e **não entrou** na tabela.
- **Super Mario Wiki — distribuição de itens do Mario Kart 8 Deluxe**: 404 no endereço tentado.
- **Paul Tol — paletas para daltonismo** (personal.sron.nl): sem resposta. Substituído por Okabe-Ito e davidmathlogic.
- **Página oficial da arte do Glitch** (glitchthegame.com/public-domain-game-art): 404; os espelhos no OpenGameArt abriram e trazem a licença CC0.
- **Inkscape, página de licença do site**: 403; a licença foi lida no arquivo COPYING do GitLab oficial.
- **Google Fonts, páginas de licença**: não devolvem o texto ao leitor automático; a licença foi lida no repositório google/fonts.
- **Pixabay, página de efeitos sonoros**: bloqueio a acesso automático (403); o resumo da licença foi lido.
- **Unity, página do efeito Duck Volume**: 404 nos dois endereços tentados.

### Abriu, mas o ponto específico não foi conferido

- **Estilo visual de qualquer pacote** (contorno, traço, se é vetor): nenhuma imagem foi inspecionada.
- **Conteúdo dos arquivos de áudio**: nenhum som foi ouvido; a tabela 4.3 casa som e pacote pelo nome e pelas etiquetas.
- **Kenney Voiceover Pack**: licença conferida; lista de falas (se tem "3, 2, 1, go") e idioma, não.
- **Kenney Game Icons, Board Game Icons, Emotes Pack**: licença e contagem conferidas; presença de raio, sono, coração e estrela, não.
- **Kenney Sci-fi Sounds e Voiceover Pack (Fighter)**: vistos só na listagem da categoria.
- **Capturas do Interface In Game e do Game UI Database**: as páginas existem; o que cada captura mostra (barra de progresso, tela de entrada) não foi visto.
- **Bfxr e LabChirp**: as páginas não dizem nada sobre os direitos dos sons gerados.
- **Soundimage.org**: licença própria identificada, termos não lidos por inteiro.
- **Fluent Emoji**: rótulo MIT lido no repositório; arquivo LICENSE não aberto.
- **Okabe-Ito**: a página abriu; os valores das 8 cores não foram relidos.
- **Unity 2D Animation, PSD Importer e Vector Graphics**: as páginas dos pacotes abriram; compatibilidade e situação de cada um no Unity 6 não foram conferidas.
- **Vídeos do YouTube** (GMTK, GDC, Juice it or lose it, Art of Screenshake, Sequelitis): título e canal conferidos; conteúdo não assistido — os princípios descritos são conhecimento geral sobre cada vídeo.
- **Artigo do F-Zero 99 (Super Jump) e Kotaku (Konno)**: abriram; o conteúdo usado veio de resumo de busca.
- **Bpm das músicas** do Music Loop Bundle e de Kevin MacLeod.
- **Cobertura de acentos do português** em cada fonte, glifo a glifo.

### Não encontrado

- Postmortem ou palestra de design de **Runbow** e de **Boomerang Fu** (só resenhas e um AMA).
- Palestra da GDC sobre level design do **Fall Guys**.
- Texto de designer sobre a **filosofia das medalhas do Trackmania** (só a documentação de como funcionam).
- Texto de designer sobre **sistema de três estrelas** além da entrevista da Rovio.
- Vídeo do GMTK dedicado a **risco e recompensa em atalhos**.
- Pacote 2D gratuito de **comida** no estilo de contorno grosso.

### Conhecimento geral, sem confirmação nesta pesquisa

- **DragonBones** (situação atual e licença do editor).
- **Metas de loudness** para jogos (valores em LUFS).
- Obrigação exata da **OFL** e da **MIT** ao embutir num jogo: a orientação de incluir os textos de licença no build é a prática corrente; não foi relida nos textos das licenças.
- Aseprite e Spine serem pagos (premissa do pedido, não reconferida).
