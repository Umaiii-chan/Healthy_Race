# Sistema de obstáculos (HAZ-01)

## Sistemas

A fase reutiliza `SpikeHazard`, `MudHazard` e `FallingPlatform` sem alterar suas regras de dano, lentidão, respawn ou cooldown.

## Distribuição final

### Z3 — Copa superior

- `Upper_FallingA` e `Upper_FallingB` criam pressão de timing na rota de maior recompensa.
- Ambas retornam após a queda, portanto a rota nunca fica permanentemente bloqueada.

### Z3 — Córrego inferior

- `Lower_MudA` e `Lower_MudB` tornam o atalho terrestre mais lento.
- `Lower_SpikesA` e `Lower_SpikesB` interrompem a corrida automática e devolvem o jogador ao Checkpoint 1.
- Alimentos ultraprocessados ficam próximos da linha rápida; alimentos saudáveis aparecem antes de conexões e saídas.

### Z6 — chegada

- `Z6_FinalSpikes` cria o último pico de atenção antes do terraço final e retorna ao Checkpoint 2.

## Leitura visual

Os hazards usam sprites próprios e dimensões visuais iguais às dos colliders. Rótulos técnicos como “ESPINHOS” e “LAMA (-30%)” foram removidos; forma, cor, posição e animação comunicam a função.

## Validação

Além dos testes unitários de penalidade, cooldown, multiplicador e respawn, os testes de layout verificam que o piso e as plataformas ao redor dos hazards respeitam o envelope real de salto do jogador.
