# Rig dos corredores (Zuri e Tom)

Rig de membros em curva aprovado na folha de modelos
(`docs/arte/folha-de-modelos-zuri-tom-2026-11-11.html`, "Rig 1"). Está **em construção**: os
prefabs existem e são testados, mas o jogo ainda usa o rig dos brotos (`CharacterRig`).

## O que existe

| Peça | Onde | Papel |
| --- | --- | --- |
| `HoseCurve` | `Scripts/Visuals/HoseCurve.cs` | Geometria: arco de círculo entre âncora e alvo, com comprimento fixo. O arco para em 200°; com o alvo perto demais o membro encurta em vez de se enrolar |
| `HoseLimb` | `Scripts/Visuals/HoseLimb.cs` | Desenha o membro em malha, com pontas redondas e contorno fino no lado da luz e grosso no lado do peso. Refaz a malha em `LateUpdate`, depois do Animator |
| `RunnerRig` | `Scripts/Visuals/RunnerRig.cs` | Vira o personagem, troca o rosto (5) e posiciona as pontas da faixa de cabeça conforme o Bem-Estar |
| Folha de peças | `Art/Characters/SP_RunnerRig.png` | 13 peças × 2 personagens, 256 px por célula e por unidade |
| Prefabs | `Prefabs/Visual/PF_VIS_Runner_Zuri.prefab`, `..._Tom.prefab` | Montados pelo construtor, na pose de repouso |

## Como regenerar

```bash
python tools/art/build_runner_rig.py --preview previa.png
```

Depois, no Unity: menu `Healthy Race › Visuals › Build runner rig (Zuri e Tom)` (ou
`-executeMethod HealthyRace.Editor.Visuals.VisualAssetBuilder.BuildRunners`) e, para a prancha
de conferência, `Healthy Race › Evidence › Capture runners (Zuri e Tom)`, que grava
`docs/evidence/character/Runners.png`.

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

## O que é provisório e o que falta

- **As peças são desenho por código**, nas proporções da folha de modelos, para o rig funcionar
  e ser testado. O desenho final é do ilustrador, nas mesmas células e com os mesmos pivôs.
- **Sem clipes:** os alvos de mão e pé estão parados na pose de repouso. A passada da prancha de
  conferência foi montada à mão. Faltam os 12 clipes reescritos para mover os alvos e os dois
  novos (pico e queda).
- **Não ligado ao jogo:** `PlayerPresentation` ainda usa `CharacterRig`. Falta trocar, tirar as
  cinco peles de corpo e ligar o Bem-Estar a `RunnerRig.SetWellbeing`.
- **Número espelhado:** o número da regata faz parte do tronco e sai invertido quando o
  personagem vira para a esquerda.
- **Braço esticado:** com o alvo além do comprimento o braço fica reto; os clipes devem manter as
  mãos dentro do alcance para a curva aparecer.

## Armadilha das capturas

Na primeira vez que uma variante de shader é usada, o Editor compila em segundo plano e pula o
desenho daquele objeto. Uma captura de quadro único saía sem braços e pernas. As ferramentas de
captura desligam `ShaderUtil.allowAsyncCompilation` durante o render. Isso é do Editor; numa
build as variantes já vêm compiladas, mas o rig novo ainda não foi visto rodando em build.
