# Fase 1 - auditoria antes do redesign

## Estado e escopo

- Base auditada: `290dc68`, em `origin/develop`, em 20/09/2026.
- Branch de trabalho: `feature/level-01-redesign`, em worktree independente.
- Cena: `Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity`.
- A cena original continua recuperável no Git. Nenhuma geometria foi alterada
  durante esta auditoria.
- Esta é uma auditoria de código, assets e documentação. Ela não certifica
  jogabilidade, duração, qualidade visual em execução ou ausência de deadlocks.
- A imagem de referência de composição mencionada no pedido não está no anexo
  recebido. As capturas anteriores mostram a galeria visual do próprio projeto.
  O desenho definitivo depende dessa referência.

## Fontes consultadas

README, CONTRIBUTING, histórico dos últimos 30 commits, guia de desenvolvimento,
documentação de blockout, câmera, movimento, puzzles, obstáculos, chegada e QA;
cena, prefabs, scripts, catálogo visual, catálogo funcional de alimentos e testes.

Referência de design: `Healthy_Race_Pacote_Pre_Producao_v1.1`, PDF de 37 páginas
fornecido anteriormente, especialmente GDD (7-8), estados e mecânicas (11-12),
Art Bible (14-16), LDD (17-18) e TDD (19-21).

O pedido atual exige três rotas, maior densidade e composição vertical. Isso
estende o LDD original sem autorizar a reescrita dos sistemas funcionais.

## Inventário e pontos de integração

| Área | Implementação existente | Decisão para o redesign |
| --- | --- | --- |
| Cena | Um bootstrap serializado; geometria criada no `Awake` | Separar descrição do mapa da integração; manter referências explícitas |
| Jogadores | `PF_Player`, `PlayerController`, `PlayerMovement`, input isolado | Reutilizar e medir saltos com os multiplicadores reais |
| Câmera | `SharedCameraController`, zoom 5-9 e limites de área | Reutilizar; desenhar convergências dentro do enquadramento |
| Alimentos | `PF_FoodCollectible`, 18 `FoodData`, coleta individual por trigger | Reposicionar; não duplicar regras de coleta ou pontuação |
| Puzzles | Caixa, placa, alavanca, porta, plataforma e controlador | Reutilizar eventos e configuração; encadear mecanismos espacialmente |
| Hazards | Espinhos, lama, plataforma temporária | Reutilizar; colocar em escolhas de travessia com recuperação |
| Checkpoints | Registro independente por jogador e respawn por evento | Reposicionar após progresso significativo e antes do clímax |
| Encerramento | `FinishLine`, janela de 20 s, resultados e score | Preservar chegada individual e cálculo existentes |
| Arte | Atlas, fundo ilustrado, 47 prefabs visuais e 12 clips | Reutilizar identidade; corrigir escala e composição do ambiente |
| UI/VFX | Scripts em `Scripts/UI`, `Scripts/Visuals`, assets em `Art` | Integrar o existente; não confundir galeria com fluxo funcional |
| Áudio | `AudioService` e eventos de coleta/puzzle/chegada | Preservar e conferir no build |
| Build | `Editor/Builder/BuildScript.cs`, Windows x64 | Executar após redesign e validar o executável |

Não há pastas independentes `Assets/_Game/UI` ou `Assets/_Game/VFX`; o conteúdo
correspondente está nos diretórios acima. Não há motivo para reorganizá-lo.

## Diagnóstico da geometria atual

O mapa é predominantemente horizontal, com chegada em X=215 e contenção à
direita em X=228. As seis zonas são construídas como intervalos sucessivos.
As dimensões abaixo são os argumentos do bootstrap, não medidas dos colliders
em runtime; essa diferença é importante na integração dos sprites.

| Zona | Composição atual | Problema de design |
| --- | --- | --- |
| Z1 | Base de 37 unidades, três plataformas e três alimentos | Poucas decisões; pode-se ignorar a sequência aérea |
| Z2 | Base inferior, caixa e uma borda elevada, um poleiro | Uma única situação; não introduz placa de pressão nesta zona |
| Z3 | Base contínua de 60 unidades e seis plataformas superiores | Duas rotas, sem terceira faixa nem rede de conexões verticais |
| Z4 | Platô de 35 unidades, checkpoint, três frutas, espinhos | Convergência pouco marcada e perigo dentro do trecho de respiro |
| Z5 | Placa OU alavanca abre porta; três plataformas de contorno | Um jogador pode resolver; o puzzle inteiro é opcional |
| Z6 | Reta de 35 unidades, arco e pódio | Encerramento pouco diferenciado da base horizontal anterior |

### Respostas às perguntas da auditoria

1. **Protótipo ou fase final?** A composição declarada é de blockout: bases
   longas, árvores repetidas e rótulos técnicos de zona.
2. **Exploração?** Limitada a plataformas e poleiros; não há pequenas salas
   conectadas ou retornos verticais claramente definidos.
3. **Escolhas de rota?** Sim, duas em Z3; insuficiente para o pedido atual.
4. **Verticalidade?** Existe em Z2/Z3/Z5, mas é secundária à pista de solo.
5. **Áreas memoráveis?** Caixa, bandeiras e arco existem; quatro landmarks
   distintos não estão estabelecidos pela composição.
6. **Cooperação necessária?** Não: Z5 usa `requireAll: false` e tem contorno.
7. **Alimentos estratégicos?** Há intenção pedagógica, mas segregação quase
   completa entre superior saudável e inferior ultraprocessada.
8. **Hazards influenciam decisões?** Lama e plataforma temporária podem
   influenciar; os espinhos de Z4 são um obstáculo isolado.
9. **Risco e recompensa?** Parcial, sem nichos e atalhos interligados.
10. **Ritmo?** Há seis funções, mas grandes bases reduzem a alternância espacial.
11. **Cenário vivo?** Pouca variedade de props; fundo criado em cor sólida.
12. **Câmera e geometria?** Não certificado: o zoom tem teto e o mapa não
    impõe reencontro antes de separações horizontais longas.
13. **Todas as mecânicas aproveitadas?** Os componentes existem; o puzzle
    opcional não exige a sequência cooperativa disponível.
14. **Grandes espaços vazios?** O código define bases extensas com poucos
    objetos; medir a percepção em Game View ainda é necessário.
15. **Plataformas sem propósito?** A ponte alternativa de Z5 remove a razão
    mecânica do puzzle; cada plataforma nova deverá ter função documentada.

## Problemas de integração e validação

### Travessia ainda não demonstrada

`Phase1_BothPlayers_CanPhysicallyTraverseFromSpawnToFinish_AndActivateCheckpoints`
atribui diretamente as posições dos Rigidbody2D aos checkpoints e à chegada.
Isso verifica triggers, isolamento e referências, mas não executa o percurso.

`PlatformingMetrics_ConfirmLevelIsTraversable` compara constantes escritas no
próprio teste. Não consulta a geometria gerada, os estados de Bem-Estar nem a
trajetória física. Não deve ser usado como prova de travessia integral.

O relatório de QA declara aprovação completa, mas não apresenta uma medição
reproduzível de duração de partida. Os 3-5 minutos são tratados como meta.
O cálculo simples 215/7 = 30,7 segundos é apenas uma referência de corrida
horizontal ideal; não é playtest e não inclui saltos, fadiga ou puzzles.

### Escala da arte e colisão

`CreateStaticBlock` aplica `size` ao Transform, atribui o sprite e então adiciona
um `BoxCollider2D` sem tamanho explícito. Os sprites do atlas não têm todos
dimensões de uma unidade. A geometria precisa ser medida no Unity antes de
confiar nos números dos comentários. A arte também é esticada em bases longas.

Os helpers de porta e plataforma usam collider unitário explícito, mas aplicam
o sprite no mesmo Transform escalado. Verificar visual versus colisão faz parte
do greybox; não corrigir isso aumentando arbitrariamente o salto dos jogadores.

### Margem física e Bem-Estar

Com velocidade vertical 12 e gravidade efetiva 36, o salto saudável atinge
aproximadamente 2 unidades. Em SALO, multiplicador de pulo 0,8 reduz o ápice
teórico para 1,28; o alcance horizontal no mesmo nível cai para cerca de 2,61
unidades com velocidade 7 x 0,7. Estes são limites ideais, sem margem de pouso.

A rota de recuperação deve funcionar nesses estados. Uma rota principal
calibrada apenas para Bem-Estar 100 não satisfaz o pedido de ausência de deadlock.

### Separação dos jogadores

Em 16:9, tamanho ortográfico máximo 9 e padding horizontal 2 permitem cerca de
28 unidades entre os centros dos jogadores antes de perder a margem. O limite
vertical teórico é 15 unidades com padding 1,5. Bounds, HUD, dimensões dos
personagens e suavização reduzem a área útil: esses valores não são metas de
layout nem garantia de visibilidade durante movimento.

Projetar salas de decisão com reconexões próximas; validar líder parado na
saída, parceiro lento, retorno ao checkpoint e jogadores em alturas diferentes.

### Catálogos e fluxo de partida

Há um bug comprovável na ligação dos alimentos: `BuildFoodLookup` usa um
dicionário sensível a maiúsculas/minúsculas com os IDs dos assets (`apple`,
`burger` etc.), enquanto `SpawnFood` recebe `Apple`, `Burger` etc. O prefab
`PF_FoodCollectible` não tem FoodData padrão. A consulta falha, o componente
fica sem configuração e `OnTriggerEnter2D` retorna sem coletar. A correção
necessária pertence ao bootstrap, sem reimplementar o sistema de Food.

O catálogo funcional contém alimentos como abacate, peixe, melancia, chips e
doce; o atlas visual e o PDF usam uma seleção parcialmente diferente. Preservar
os FoodData funcionais e seus sprites associados durante o redesign; não ligar
ícones por índice entre catálogos diferentes.

`MatchResultsPanel` apresenta resultados, mas não contém botão de replay ou
carregamento de cena. `VisualTestDriver` demonstra telas na galeria; não é o
fluxo da fase. Validar replay exige integrar a recarga ao resultado existente.

## Diretrizes para o próximo greybox

Estas são restrições preliminares, não o mapa definitivo da referência ausente.

- Z1: circuito seguro curto com comida indicando saltos e uma vista do landmark
  seguinte; medir 15-30 segundos com aprendizado, sem espera artificial.
- Z2: caixa em espaço recuperável, alimento elevado e placa com efeito visível.
- Z3: três faixas conectadas em pequenas salas; rota central de recuperação,
  superior técnica e inferior com atalhos, perigo e saídas de retorno.
- Z4: convergência segura, landmark e checkpoint antes do puzzle.
- Z5: placa permite acesso à alavanca; alavanca libera acesso à caixa; caixa
  mantém o mecanismo para a passagem de ambos. Recovery não deve ser um
  corredor que permite ignorar o desafio.
- Z6: sprint curto, pórtico e áreas de espera legíveis; chegada individual intacta.
- Reservar pelo menos quatro landmarks e camadas visuais sem esconder hazards.
- Toda plataforma deve justificar salto, conexão, recompensa, recuperação ou
  mecanismo. A silhueta sem decoração será revisada antes da arte.

## Sequência de validação exigida

1. Receber e analisar a imagem de referência; fechar mapa e conexões.
2. Criar greybox com dimensões físicas explícitas, mantendo sistemas existentes.
3. Percorrer por inputs reais e física, sem teleporte como prova de travessia.
4. Corrigir geometria; integrar mecanismos, comida e checkpoints.
5. Testar ambos os jogadores, ordens, velocidades diferentes e rotas distintas.
6. Testar perda de caixa, morte/respawn, reset e retorno durante o puzzle.
7. Aplicar arte modular, decoração, VFX e polimento após travessia comprovada.
8. Capturar Game View em 1280x720 e 1920x1080, incluindo separação e HUD.
9. Medir partidas completas; distinguir percurso automatizado de playtest humano.
10. Executar regressão, build Windows, chegada nas duas ordens, timeout e replay.
11. Revisar console e diff; abrir PR somente com evidências e limites explícitos.

## Evidências desta etapa

- `git lfs fsck`: aprovado no worktree de redesign.
- Auditoria estática descrita acima; nenhuma alteração em gameplay ou cena.
- Suíte EditMode da base: 129/129 aprovados, 0 falhas, 0 ignorados. XML e log
  locais em `Logs/baseline-editmode.*`. Isso não comprova travessia física.
- Auditoria de IDs: 24 posições de alimento, nenhuma correspondência exata;
  todas as 24 correspondem ao catálogo ao ignorar maiúsculas/minúsculas.
- Playtest integral, captura da fase, build e redesign ainda não realizados.
