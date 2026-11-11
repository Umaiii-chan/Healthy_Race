# Origem e licença de artes, sons e fontes

Inventário pedido no roadmap (Fase A, item P0 "registro de origem e licença"). **Antes de
distribuir uma build final, nenhuma linha pode ficar como "a confirmar".**

| Item | Arquivos | Origem | Licença | Situação |
| --- | --- | --- | --- | --- |
| Atlas visual (personagens VITA/SALO, 18 alimentos, cenário e mecanismos do menu) | `Assets/_Game/Art/VisualAtlas/SP_VisualAtlas.png` | Gerado por IA (ImageGen integrada ao Codex), a partir da Art Bible v1.1; prompt em `docs/visual-art-prompts.md` | Termos de uso da ferramenta usada | Registrado; conferir os termos da ferramenta para uso público |
| Fundo do menu | `Assets/_Game/Art/Environment/Visual/SP_Background.png` | Gerado por IA, mesmo processo; prompt em `docs/visual-art-prompts.md` | Idem | Registrado; conferir os termos |
| Personagem articulado (partes do rig) | `Assets/_Game/Art/Characters/SP_CharacterRig.png` | Gerado por código: `tools/art/build_character_rig.py` (Pillow) | Do projeto | OK |
| Estufa, terreno, placas, parallax | `Assets/_Game/Art/Environment/Greenhouse/`, `Arena/` | Gerados por código: `tools/art/build_greenhouse.py`, `build_environment.py` | Do projeto | OK |
| Primitivas de UI e efeitos (estrela, coração, gota, anel) | `Assets/_Game/Art/UI/Visual/*.asset`, `Art/VFX/Visual/*.asset` | Criadas no Editor pelo `VisualAssetBuilder` | Do projeto | OK |
| **Alimentos em pixel art usados na fase** | `Assets/_Game/Art/Collectibles/spr_food_*.png` (18) | Entraram no commit `a3e3592` (FOOD-01, 20/09/2026), sem registro de origem | **A confirmar** | **Pendente.** Serão trocados pelos do atlas (direção de arte A); até lá, confirmar a origem |
| **Músicas** | `Assets/_Game/Audio/Music/mus_menu.wav`, `mus_level01.wav` | Entraram no commit `85847c8` (AUDIO-01), sem registro de origem | **A confirmar** | **Pendente** |
| **Efeitos sonoros** | `Assets/_Game/Audio/SFX/*.wav` (16) | Commit `85847c8`, sem registro de origem | **A confirmar** | **Pendente** |
| Som sintético de reserva | Gerado em tempo de execução (`AudioService.CreateSyntheticFallback`) | Código do projeto | Do projeto | OK |
| Fonte do HUD e das telas | `LegacyRuntime.ttf` (fonte embutida do Unity) | Unity | Licença do Unity | OK; será trocada na Fase D (fonte OFL a escolher) |
| Fonte do título do menu | Desenhada no atlas/fundo gerado | Ver atlas | Ver atlas | Registrado |

## Como registrar um item novo

Toda arte, som ou fonte que entrar no repositório ganha uma linha aqui **no mesmo PR**, com:

- origem: autor, ferramenta ou site, e link;
- licença e atribuição exigida;
- data.

Itens gerados por IA levam o prompt em `docs/visual-art-prompts.md` (ou equivalente para áudio).
