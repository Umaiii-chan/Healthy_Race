# Rig dos corredores (Zuri e Tom)

Rig de membros em curva aprovado na folha de modelos
(`docs/arte/folha-de-modelos-zuri-tom-2026-11-11.html`, "Rig 1"). Está **em construção**: os
prefabs e os clipes existem e são testados, mas o jogo ainda usa o rig dos brotos (`CharacterRig`).

## O que existe

| Peça | Onde | Papel |
| --- | --- | --- |
| `HoseCurve` | `Scripts/Visuals/HoseCurve.cs` | Geometria: arco de círculo entre âncora e alvo, com comprimento fixo. O arco para em 200°; com o alvo perto demais o membro encurta em vez de se enrolar |
| `HoseLimb` | `Scripts/Visuals/HoseLimb.cs` | Desenha o membro em malha, com pontas redondas e contorno fino no lado da luz e grosso no lado do peso. Refaz a malha em `LateUpdate`, depois do Animator |
| `RunnerRig` | `Scripts/Visuals/RunnerRig.cs` | Vira o personagem, troca o rosto (5) e posiciona as pontas da faixa de cabeça conforme o Bem-Estar |
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
escreve os clipes e monta os prefabs. Para conferir:

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

## O que é provisório e o que falta

- **As peças são desenho por código**, nas proporções da folha de modelos, para o rig funcionar
  e ser testado. O desenho final é do ilustrador, nas mesmas células e com os mesmos pivôs.
- **As animações são um primeiro passe**, escritas por números e conferidas em pranchas de
  quadros parados. Ninguém as viu em movimento nem dentro do jogo.
- **Não ligado ao jogo:** `PlayerPresentation` ainda usa `CharacterRig`. Falta trocar, tirar as
  cinco peles de corpo, ligar o Bem-Estar a `RunnerRig.SetWellbeing` e acionar `Rush` e `Crash`
  a partir do estado do lanche.
- **Número espelhado:** o número da regata faz parte do tronco e sai invertido quando o
  personagem vira para a esquerda.
- **Braço curto na corrida:** com a mão perto do ombro o arco bate no limite e o braço encurta.
- **Tênis afunda no impacto:** o achatamento da raiz no `Land` leva o tornozelo abaixo da altura
  de repouso por um instante (o rig antigo fazia o mesmo com as pernas).

## Armadilha das capturas

Na primeira vez que uma variante de shader é usada, o Editor compila em segundo plano e pula o
desenho daquele objeto. Uma captura de quadro único saía sem braços e pernas. As ferramentas de
captura desligam `ShaderUtil.allowAsyncCompilation` durante o render. Isso é do Editor; numa
build as variantes já vêm compiladas, mas o rig novo ainda não foi visto rodando em build.

Segunda armadilha: com mais de ~80 personagens na mesma imagem, parte das malhas dos membros
deixa de ser desenhada (as pernas das últimas linhas sumiam, qualquer que fosse o clipe). A causa
exata não foi identificada; não depende da posição na cena. Por isso a prancha de animações sai em
duas páginas de 56 personagens. No jogo há dois personagens na tela.
