# Fase 1: Corrida do Equilíbrio — layout final

## Visão geral

- Cena: `Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity`
- Construção: `HealthyRace.Level.Phase1Bootstrap`
- Extensão: X=-5 a X=228; chegada em X=215
- Jogadores: dois jogadores locais e câmera compartilhada
- Meta de ritmo: 3–5 minutos em exploração normal; uma corrida conhecida pode ser mais rápida

A fase preserva as seis funções do LDD, mas as apresenta como espaços conectados. A referência externa foi usada para densidade, sobreposição de rotas e composição. Sprites, paleta, alimentos, personagens e linguagem visual permanecem os de Healthy Race.

## Zonas

| Zona | Composição | Gameplay |
| --- | --- | --- |
| Z1 — Bosque de largada | chão seguro, três alturas baixas e árvore de entrada | movimento, salto e alimentos guiando o primeiro arco |
| Z2 — Jardim da Grande Árvore | pátio inferior, parede, elevador e jardim superior | caixa sobre placa aciona o elevador; reset recupera a caixa |
| Z3 — Rede de rotas | copa superior, ponte central e córrego inferior, com quatro conexões | escolhas entre técnica/recompensa, estabilidade e velocidade/risco |
| Z4 — Clareira do Reencontro | área segura reconhecível e descida comum | convergência, recuperação e Checkpoint 2 |
| Z5 — Torre do Equilíbrio | placa e caixa no piso, elevador, alavanca alta e portão | placa eleva acesso; placa + alavanca abrem o portão |
| Z6 — Pavilhão do Equilíbrio | último hazard, terraço, arco, pódio e árvore | clímax curto, chegada individual, janela final e resultados |

## Rotas e alimentos

- **Copa superior:** saltos maiores, duas plataformas que caem e alimentos saudáveis de maior valor.
- **Ponte central:** oito plataformas largas, vãos curtos e mistura de alimentos. É a rota de recuperação.
- **Córrego inferior:** menos saltos, duas áreas de lama, dois conjuntos de espinhos e alimentos ultraprocessados próximos dos atalhos.
- Alimentos formam arcos sobre saltos, indicam mudanças de altura e tornam o risco legível antes do comprometimento.

## Física verificada

Parâmetros do jogador: velocidade horizontal de 7 m/s, impulso vertical de 12 m/s e gravidade efetiva de 36 m/s². O envelope teórico é 2 m de subida e 4,67 m de alcance horizontal.

Os testes consultam os `BoxCollider2D` gerados para as três rotas e verificam cada vão e subida, em vez de repetir números fixos do documento. A rota inferior também possui piso contínuo suficiente ao redor dos hazards.

## Câmera e checkpoints

As rotas variam aproximadamente entre Y=-3,4 e Y=5 e se conectam perto de X=86, X=111 e X=138. Pares de jogadores em rotas diferentes entre conectores sucessivos permanecem dentro do zoom máximo da câmera compartilhada. Checkpoint 1 marca a entrada da rede; Checkpoint 2 fica na clareira antes do puzzle.

## Apresentação

O background ilustrado é repetido em módulos espelhados para evitar emendas bruscas. Plataformas usam `SpriteDrawMode.Tiled`, e seus tamanhos visuais coincidem com os colliders. Árvores, arbustos, nascente e arquitetura ficam atrás do gameplay. Rótulos de protótipo foram removidos de caixa, placa, portão, lama e espinhos; permanecem apenas nomes curtos de lugares, checkpoints e mecanismos que precisam de orientação.

## Resultado e replay

A chegada preserva ordem individual, janela final, DNF, pontuação e painel de resultados. O painel agora oferece **Jogar novamente**, que recarrega a cena ativa.

## Evidências

Capturas em 1280×720 e 1920×1080 ficam em `docs/evidence/level-redesign/`. O relatório consolidado e as limitações do playtest estão em `docs/phase1-redesign.md`.
