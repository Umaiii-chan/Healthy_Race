# Rig dos corredores (Zuri e Tom)

Rig de membros em curva aprovado na folha de modelos
(`docs/arte/folha-de-modelos-zuri-tom-2026-11-11.html`, "Rig 1"). **As 10 fases já usam Zuri
(jogador 1) e Tom (jogador 2)** no lugar dos brotos, e o menu principal também. As peças são
provisórias (ver o fim desta nota).

## O que existe

| Peça | Onde | Papel |
| --- | --- | --- |
| `HoseCurve` | `Scripts/Visuals/HoseCurve.cs` | Geometria: arco de círculo entre âncora e alvo, com comprimento fixo. O arco para em 200°; com o alvo perto demais o membro encurta em vez de se enrolar |
| `HoseLimb` | `Scripts/Visuals/HoseLimb.cs` | Desenha o membro em malha, com pontas redondas e contorno fino no lado da luz e grosso no lado do peso. Refaz a malha em `LateUpdate`, depois do Animator |
| Shader do membro | `Art/Shaders/SH_HoseLimb.shader`, `Art/Characters/MAT_HoseLimb.mat` | Cor por vértice, sem textura. O membro **não pode** usar o shader de sprite (ver "Por que o membro tem shader próprio") |
| `RunnerRig` | `Scripts/Visuals/RunnerRig.cs` | Vira o personagem, troca o rosto (5) e posiciona as pontas da faixa de cabeça conforme o Bem-Estar |
| Retratos e poses | `Art/Characters/SP_RunnerPortraits.png` | Por personagem: 5 retratos de cabeça (um por faixa de Bem-Estar) e 1 pose de corrida de corpo inteiro. Render dos próprios prefabs, com fundo transparente |
| Folha de peças | `Art/Characters/SP_RunnerRig.png` | 13 peças × 2 personagens, 256 px por célula e por unidade |
| Prefabs | `Prefabs/Visual/PF_VIS_Runner_Zuri.prefab`, `..._Tom.prefab` | Montados pelo construtor, com `Animator` |
| Clipes | `Animations/Visual/Runner/AN_RUN_<Nome>_<Estado>.anim` | 14 por personagem, escritos em código (`VisualAssetBuilder.RunnerClips.cs`) |
| Controladores | `Animations/Visual/Runner/AC_VIS_Runner_<Nome>.controller` | Um por personagem, com o parâmetro inteiro `Action` |

## Como regenerar

```bash
python tools/art/build_runner_rig.py --preview previa.png
```

Depois, no Unity: menu `Healthy Race › Visuals › Build runner rig (Zuri e Tom)` (ou
`-executeMethod HealthyRace.Editor.Visuals.VisualAssetBuilder.BuildRunners`), que importa a folha,
escreve os clipes, monta os prefabs, renderiza a folha de retratos e poses e aponta as 10 cenas
de fase para os prefabs (`UseRunnersInLevels`). Depois rode `Healthy Race › Visuals › Build
screens and game flow` (`VisualAssetBuilder.BuildFlow`), que refaz o prefab de telas: é ele que
põe as poses no menu. Só os retratos, sem refazer os prefabs: `Build runner portraits (Zuri e
Tom)` (`VisualAssetBuilder.BuildRunnerPortraitSheet`). Para conferir:

- `Healthy Race › Evidence › Capture runners (Zuri e Tom)` grava `docs/evidence/character/Runners.png`
  (poses, rostos, faixa e lado);
- `Healthy Race › Evidence › Capture runner animations` grava
  `docs/evidence/character/Runner-<Nome>-Animations-1.png` e `-2.png` (uma linha por estado).

Os pivôs e as posições de montagem estão em dois lugares: `PARTS` e `RIGS` no script Python e
`RunnerParts` e `Runners` em `VisualAssetBuilder.Runners.cs`. Mudou num, mude no outro.

## Montagem

Origem entre os pés. Medidas em unidades do jogo (1 unidade = 1 tile = 256 px da folha).

| Medida | Zuri | Tom |
| --- | --- | --- |
| Quadril | 0,60 | 0,64 |
| Quadril ao pescoço | 0,34 | 0,36 |
| Pescoço ao centro da cabeça (e raio) | 0,21 | 0,21 |
| Topo do crânio | 1,36 | 1,42 |
| Braço / perna (comprimento) | 0,38 / 0,45 | 0,40 / 0,49 |
| Tornozelo em repouso | 0,18 | 0,18 |

Ordem de desenho, de trás para a frente: braço de trás, mão, perna de trás, tênis, perna da
frente, tênis, tronco, pontas da faixa, cabelo de trás, cabeça, rosto, cabelo da frente, faixa,
braço da frente, mão. O tronco fica na frente das duas pernas para a bermuda cobrir o topo delas
(a folha de modelos previa a perna da frente sobre o tronco).

Cotovelo dobra para trás (`Bend` negativo) e joelho para a frente (positivo). O lado da dobra
acompanha o personagem quando ele vira.

## Animações

Os clipes movem os **alvos** de mão e pé (posição em `Rig/Body`); os membros em curva
acompanham. Squash e stretch ficam só na raiz `Rig/Body`, como na linguagem aprovada (elástico
moderado); a única exceção é a cabeça no "nhac" de comer.

| Ação | Estado | Repete | Observação |
| --- | --- | --- | --- |
| 0 a 11 | `Idle`, `Move`, `Jump`, `Fall`, `Land`, `Push`, `Interact`, `Eat`, `Fatigue`, `Recover`, `Victory`, `Defeat` | conforme o rig antigo | Mesmos nomes e números de `VisualAction` |
| 12 | `Rush` (pico) | sim | Passada rápida (0,34 s), tronco a −22° |
| 13 | `Crash` (queda) | sim | Passo arrastado (1,10 s), cabeça caída |

A corrida é própria de cada um: Zuri corre aos saltos (passada de 0,68 s, o corpo sobe 0,09) e
Tom corre de tiro (0,52 s, tronco a −14°). Na vitória, Zuri pula com os dois braços e Tom dá um
soco no ar. Nas poses de braços para cima as mãos vão para a diagonal, à frente e atrás da cabeça,
para não cobrir o rosto.

O rosto não é animado pelos clipes: quem troca é `RunnerRig.SetFace`, a partir do estado do jogo.

## Ligação ao jogo

`PlayerPresentation` aceita os dois rigs. Com um `RunnerRig` ligado (`ConfigureRunner`), a cada
quadro ela:

- vira o personagem para o lado da entrada (`SetFacing`); parado, mantém o último lado;
- repassa o Bem-Estar para a faixa de cabeça (`SetWellbeing`). Corpo e pele não mudam com o
  estado;
- escolhe a animação (`RunnerAnimationFor`): o número de `VisualAction`, trocado por `Rush` (12)
  ou `Crash` (13) quando o personagem está correndo sob efeito do lanche;
- escolhe o rosto (`RunnerFaceFor`): vitória → olho arregalado; derrota e fadiga → esforço;
  queda → sonolento; pico → arregalado; empurrar, pular e interagir → esforço; resto → neutro.

O personagem pisca sozinho sobre o rosto neutro. Com "reduzir movimento" ligado ele não pisca e
nunca fica de olhos fechados.

### Interface

- **Menu principal:** Zuri e Tom de corpo inteiro, correndo para o mesmo lado
  (`VisualCatalog.RunnerPose`). O terceiro personagem, o broto "salo" ao lado dos lanches, saiu:
  comida não muda o corpo de ninguém.
- **Retrato:** `VisualCatalog.Portrait(jogador, faixa)` devolve a cabeça do personagem com as
  pontas da faixa na altura do Bem-Estar; o rosto e a pele são os mesmos nos cinco. Quem usa hoje
  é só `PlayerHudView`, o HUD-modelo do prefab de telas (visto na galeria de teste).
- **O HUD das fases não tem retrato.** O que aparece durante a corrida é `PlayerHUD`
  (`Scripts/UI`), só texto e barras, montado por código. Pôr o retrato nele é parte do redesenho
  do HUD, que ainda não foi feito.

O rig dos brotos (`CharacterRig`) continua no projeto, com seus testes e a galeria de teste
(`Scenes/Test/VisualTest.unity`); saiu das fases e do menu.

## Por que o membro tem shader próprio

O shader de sprite do URP (`Sprite-Unlit-Default`) multiplica a cor por `unity_SpriteColor` e
espelha a posição por `unity_SpriteProps`. Esses dois valores são por objeto e só um
`SpriteRenderer` os define. O membro é uma malha comum (`MeshRenderer`): com aquele shader ele
herda o que sobrou do desenho anterior, e **aparece ou some conforme a ordem de desenho do
quadro**.

Foi assim que os membros falharam, sempre em silêncio, sem erro nem aviso:

- na Fase 1, Zuri e Tom apareciam sem braços nem pernas, com os tênis soltos (a captura
  `docs/evidence/fase-01` do commit `fc76927` mostra isso);
- na folha de retratos, a pose de corpo inteiro saía sem membros;
- nas pranchas de poses, com muitos personagens, os membros apareciam.

Com `HealthyRace/HoseLimb` (cor por vértice, passes `Universal2D` e `UniversalForward`) o membro
não depende de nada que outro objeto tenha deixado. Os testes
`RunnerInterfaceTests.Limbs_UseTheirOwnShader_NotTheSpriteOne` e
`Limbs_AreDrawn_AloneAndAmongTheSprites` guardam isso: o primeiro pelo nome do shader, o segundo
fotografando o prefab e contando a tinta na área de cada membro.

## O que é provisório e o que falta

- **As peças são desenho por código**, nas proporções da folha de modelos, para o rig funcionar
  e ser testado. O desenho final é do ilustrador, nas mesmas células e com os mesmos pivôs.
- **As animações são um primeiro passe**, escritas por números e conferidas em pranchas de
  quadros parados. Os testes PlayMode conferem que elas tocam na fase (o pé sai do chão na
  corrida, pico e queda trocam de estado), mas ninguém as viu em movimento com os próprios olhos.
- **Cor do jogador 2:** o Tom é laranja (como na folha de modelos e na Art Bible), mas a
  identidade do jogador 2 no jogo ainda é roxa (`VisualCatalog.PlayerTwo` e a cor do HUD em
  `LevelBootstrap`). A etiqueta "P2" do menu fica roxa embaixo de um personagem laranja.
- **Braço da frente cobre o número:** parado, o braço pende na frente da regata.
- **Número espelhado:** o número da regata faz parte do tronco e sai invertido quando o
  personagem vira para a esquerda.
- **Braço curto na corrida:** com a mão perto do ombro o arco bate no limite e o braço encurta.
- **Tênis afunda no impacto:** o achatamento da raiz no `Land` leva o tornozelo abaixo da altura
  de repouso por um instante (o rig antigo fazia o mesmo com as pernas).

## Armadilhas das capturas

Duas falhas antigas das capturas foram atribuídas a outras causas antes de o problema do shader
aparecer. As duas proteções continuam no código, mas **não foi refeito o teste para saber se
ainda são necessárias** com o shader próprio:

- **Compilação de shader em segundo plano.** Na primeira vez que uma variante de shader é usada,
  o Editor compila em segundo plano e pula o desenho daquele objeto. As ferramentas de captura
  desligam `ShaderUtil.allowAsyncCompilation` durante o render. Isso é do Editor; numa build as
  variantes já vêm compiladas.
- **Muitos personagens na mesma imagem.** Com mais de ~80 personagens parte dos membros deixava
  de ser desenhada, qualquer que fosse o clipe. O sintoma é o mesmo do shader de sprite (membro
  some conforme o que mais está na cena), então é provável que a causa fosse a mesma. A prancha
  de animações segue saindo em duas páginas de 56 personagens.

A folha de retratos recorta o fundo sem depender do alfa do alvo de render: fotografa a cena
sobre preto e sobre branco, e a diferença entre as duas fotos dá a transparência de cada pixel.
