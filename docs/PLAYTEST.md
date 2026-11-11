# Playtest — guia de aplicação

Roteiro para aplicar os testes com pessoas do roadmap (`ROADMAP_PRODUTO_FINAL.md`, decisão D0) e
das rodadas seguintes. O protocolo completo, as métricas e a origem de cada meta estão em
`EVOLUCAO_QUALIDADE.md` §6; este documento é a parte prática.

## Antes da sessão

1. Use a build da rodada (a de linha de base é a **1.0.1**: regras da 1.0.0, largadas
   igualadas e registro de partida ligado).
2. Rode o jogo uma vez na máquina da sessão, de preferência a da feira, e confira:
   - o teclado registra os dois jogadores ao mesmo tempo: segure `D` e `→` e aperte `W` e `↑`
     juntos;
   - o som está ligado e audível.
3. Apague ou mova os arquivos antigos de `partidas` (ver abaixo) para não misturar rodadas.
4. Imprima uma ficha de observação por partida e um formulário por jogador
   (`EVOLUCAO_QUALIDADE.md` §6).
5. Peça autorização para gravar tela e áudio. O registro do jogo não grava nome nem dado pessoal.

## Durante a sessão

- **Facilitador:** diz só "é um jogo de corrida para dois; joguem como quiserem". Não explica
  regras nem controles. Só ajuda se o par ficar travado por 2 minutos, e anota que ajudou.
- **Observador:** preenche a ficha em silêncio, uma por partida.
- **Roteiro da linha de base (≈ 30 min por par):**

  | Ordem | Cenário | O que jogar |
  | --- | --- | --- |
  | 1 | S1 + S2 | Do menu, sem instrução, até terminar a Fase 1 |
  | 2 | S4 | Fases 2 e 3 |
  | 3 | S3 | Fases 4, 5 e 6 seguidas, contando quem vence mais |
  | 4 | S5 / S6 | Fase 9 |
  | 5 | S8 | Perguntar: "querem jogar mais uma?" e anotar a resposta |

- Ao fim, cada jogador preenche o formulário sozinho.

## Onde ficam os registros

O jogo grava um arquivo por partida, no formato JSON Lines:

```text
%USERPROFILE%\AppData\LocalLow\Healthy Race\Healthy Race\partidas\AAAA-MM-DD_HHMMSS_faseNN.jsonl
```

O progresso salvo (fases concluídas e recordes) fica em `...\Healthy Race\Healthy Race\progresso.json`.
Para começar uma rodada do zero, apague esse arquivo.

Eventos gravados: `largada`, `amostra` (a cada 1 s: posição, progresso na rota, Bem-Estar,
peso, líder), `corredor_entrada` e `corredor_saida`, `coleta`, `fadiga`, `espinho`,
`alavanca`, `chegada`, `resultado`, `fim` e `abandono` (saída para o menu ou reinício).

Para desligar o registro numa máquina, defina a preferência `HealthyRace_MatchLog = 0` (será
uma opção de menu na Fase D). Em modo batch, nos testes automáticos, ele já fica desligado.

## Depois da sessão

```bash
python tools/playtest/analisar_partidas.py "%USERPROFILE%/AppData/LocalLow/Healthy Race/Healthy Race/partidas" --csv resumo.csv
```

O script imprime:

- vitórias de P1 e P2;
- quantas vezes quem chegou primeiro venceu;
- diferença de chegada e duração das partidas;
- trocas de liderança;
- escolha do atalho por bifurcação (`yN-esq` / `yN-dir` = corredor do andar N, na metade
  esquerda ou direita);
- contagens por jogador.

Registre a rodada num relatório (`docs/playtest/AAAA-MM-DD.md`) com:

- números do script;
- respostas do formulário (médias das escalas e transcrição das abertas);
- notas do observador;
- **o que confirmou e o que contrariou** os itens "A validar" de `EVOLUCAO_AUDITORIA.md`.

## Limites do registro

- A bifurcação é identificada pelo andar e pela metade da fase. Em fases com mais de duas
  bifurcações por andar, elas se somam.
- O líder é calculado pela projeção na rota do caminho longo. No corredor curto a projeção é
  aproximada, e duas posições a menos de 0,5% de diferença contam como empate (o líder não muda).
- O registro não vê o teclado: tecla que "não pegou" só aparece na observação e no formulário.
