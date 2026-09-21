# Perfis de Jogador (assimetria P1 / P2)

> Status: **proposta**. A Parte 1 (orçamento derivado) está implementada; os perfis
> descritos aqui dependem do aceite do mapa da arena. Ver `docs/phase1-blockout.md`.

## Por que assimetria

Com P1 e P2 idênticos, todo portal cooperativo degenera no mesmo padrão: um
jogador segura o botão e o outro anda. Um joga, o outro espera. A cooperação só
vira mecânica de verdade quando os dois têm capacidades diferentes e cada um
destrava algo que o outro não alcança.

A assimetria é amarrada ao tema de bem-estar (ODS 3): agilidade e resistência são
duas formas legítimas de estar bem, e nenhuma das duas é superior.

## Os dois perfis

| Perfil | Jogador | Multiplicadores | Habilidade | Fraqueza |
| :-- | :-- | :-- | :-- | :-- |
| **Ágil** | P1 | pulo × 1.15 | Alcança desníveis e vãos que P2 não alcança | Na lama: velocidade 0.4× e fadiga acelerada |
| **Resistente** | P2 | pulo × 0.85 | Imune à lama; único que empurra a caixa pesada | Pulo curto: depende de mecanismos para subir |

## Orçamento resultante

Derivado por `JumpBudget` a partir da física viva, nunca fixado em literal.
Com a gravidade da arena ($g = 36\ \text{m/s}^2$) e $v_0 = 12$:

| | Ápice | Alcance | Degrau seguro | Vão seguro |
| :-- | :-- | :-- | :-- | :-- |
| Base | 2.00 m | 4.67 m | 1.80 m | 4.00 m |
| **P1 Ágil** | **2.65 m** | **5.37 m** | 2.45 m | 4.70 m |
| **P2 Resistente** | **1.45 m** | **3.97 m** | 1.25 m | 3.30 m |

Os valores de degrau e vão seguros descontam as margens de `JumpBudget`
(`StepSafetyMargin` e `GapSafetyMargin`).

## Regras de projeto para a geometria

| Desnível | Quem passa |
| :-- | :-- |
| ≤ 1.2 m | ambos |
| 1.6 – 2.4 m | só P1 |
| ≥ 2.8 m | ninguém sozinho, exige mecanismo |

| Vão | Quem passa |
| :-- | :-- |
| ≤ 3.2 m | ambos |
| 3.8 – 4.7 m | só P1 |
| ≥ 5.4 m | ninguém |

## Composição de multiplicadores

`PlayerMovement` já usa `_jumpMultiplier` e `_movementMultiplier` para o sistema
de fadiga e para a lama. O perfil **não** pode sobrescrever esses campos, senão a
fadiga zera a assimetria ou vice-versa. A composição correta é:

```text
efetivo = base × perfil × situacional (fadiga, lama)
```

O perfil entra como um terceiro fator, aplicado em `PlayerMovement` antes dos
multiplicadores situacionais.

## Dados

`PlayerProfileData` (ScriptableObject em `Assets/_Game/ScriptableObjects/Player/`)
guarda multiplicador de pulo, multiplicador de movimento, imunidade à lama e
força de empurrar. Nenhum desses números fica espalhado pelo bootstrap.

- `MudHazard` consulta o perfil antes de aplicar a penalidade.
- `PushableBox` ganha um peso e recusa empurrão de quem não tem força suficiente:
  P1 encosta na caixa pesada e ela não anda.

## Testes

- `Mud_PenalizesAgileOnly` (PlayMode): P1 desacelera na lama, P2 não.
- `HeavyBox_MovesOnlyForResilient` (PlayMode): a caixa pesada só anda com P2.
- `JumpBudget_MatchesMeasuredPhysics_ForBothProfiles` (PlayMode): mede ápice e
  alcance de cada perfil em runtime, tolerância de 5%.
