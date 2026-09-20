# Origem da arte visual

Ferramenta: ImageGen integrada ao Codex, sem CLI/API externa.
Referência de estilo: figura 4 da Art Bible oficial, página 14 do pacote v1.1.
A imagem da referência foi fornecida como orientação de estilo, não como alvo de edição.

## Atlas

Arquivo: `Assets/_Game/Art/VisualAtlas/SP_VisualAtlas.png`.
Saída recebida: 1254x1254 RGBA com transparência real. Preservada sem editar pixels.
Os gutters reais foram mapeados no Sprite Editor em seis colunas e seis linhas.
A primeira linha é mais alta, acomodando os personagens completos.

Prompt utilizado:

```text
Create a production game sprite atlas for Healthy Race, following the attached Art Bible reference image ONLY for art direction. Output a square 2048x2048 RGBA image with genuinely transparent background. EXACTLY 6 columns and 6 rows of equal square cells, no visible grid, no labels, no typography, no shadows outside each sprite. Every object centered in its cell with 18 percent transparent padding on each edge. Thick near-black outlines, smooth clean cartoon minimalist shapes, flat vibrant colors and small soft highlights. NOT pixel art. All characters have round oversized heads, small humanoid bodies, short arms and legs and a leaf on their heads as in the reference. Each character is full body front/three-quarter view, same height and feet baseline. Row 1 left to right: healthy VITA (bright lime green big leaf happy face), stable VITA (muted green smaller leaf neutral face), tired VITA (yellow green drooping leaf heavy eyelids), critical VITA (pink red wilted brown leaf exhausted face), SALO (dark burnt red round body no fresh leaf orange outline exhausted face), lush small green tree. Row 2: green apple, yellow banana, orange fruit, bunch of purple grapes, strawberry, carrot. Row 3: broccoli, tomato, blue water bottle, pair of chestnuts, hamburger, pizza slice. Row 4: red soda can with white stripe NO text, carton of french fries, pink donut, hot dog, pink ice cream cone, chocolate bar. Row 5: wide grass topped soil ground block (front elevation), wide wooden floating platform (front elevation), stone wall block, small green shrub, red checkpoint flag on pole, blue glowing finish portal in dark stone arch. Row 6: wooden crate with diagonal bracing, red pressure button on gray plinth, lever red handle on gray base, closed wooden door in stone frame, row of three sharp silver spikes, brown mud puddle with bubbles. All environment pieces are game side-view, clearly readable silhouettes; ground and wall fill most of cell without perspective distortion. Transparent gutters must remain empty. No extra objects in cells. No background scene. Exactly 36 separate sprites.
```

## Fundo

Arquivo: `Assets/_Game/Art/Environment/Visual/SP_Background.png`.

Prompt utilizado:

```text
Healthy Race 2D platformer background only, wide 16:9 landscape. Cartoon minimalist flat illustration with clean rounded shapes, soft blue sky, a few cream clouds, layered sage green distant hills, faraway round-canopy trees and subtle warm sunlight. Calm welcoming daytime park. Match thick dark outline simple green vegetation of the attached game art reference, but use much softer low-contrast outlines for this BACKGROUND layer. Empty open center and foreground to keep two players, platforms and HUD readable. No people, characters, food, text, UI, buildings, roads, ground platforms, signs or foreground objects. Continuous landscape, gentle hills on bottom third, sky occupying upper two thirds. Finished production game background, not a mockup.
```

## Elementos de UI e VFX

Estrelas, corações, gotas, anéis, preenchimentos e badges são primitivas simples
criadas no Editor e serializadas como Sprite/Texture2D em arquivos Unity `.asset`.
Não exigem geração de texturas durante o gameplay.
