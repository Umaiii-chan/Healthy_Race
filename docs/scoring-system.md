# Sistema de Pontuação e Desempate (SCORE-01)

## Descrição

O sistema de pontuação (**ScoreService**) do Healthy Race provê um cálculo determinístico, desacoplado e auditável do desempenho de cada jogador ao término da partida. Além dos pontos brutos obtidos na coleta e nos desafios, o sistema premia o equilíbrio fisiológico (Bem-Estar) e a cooperação, penalizando o consumo de ultraprocessados, a sobrecarga física (fadiga) e o abandono de prova (DNF).

---

## Fórmulas de Pontuação e Breakdown

### 1. Pontos de Alimentos Saudáveis (`HealthyPoints`)

Soma dos `BasePoints` definidos nos ScriptableObjects `FoodData` dos alimentos saudáveis coletados durante a corrida:

$$\text{HealthyPoints} = \sum \text{BasePoints}_{\text{healthy}}$$

### 2. Pontos de Puzzles Cooperativos (`PuzzlePoints`)

Recompensa por puzzles cooperativos resolvidos (ex: Zona 5):

$$\text{PuzzlePoints} = N_{\text{puzzles}} \times 150\text{ pts}$$

### 3. Bônus de Tempo (`TimeBonus`)

Concedido se o jogador concluiu a prova dentro do tempo-alvo ($T \le 180\text{s}$):

$$\text{TimeBonus} = 600\text{ pts} \quad (\text{se } \text{Finished} \text{ e } T \le 180\text{s})$$

### 4. Bônus de Chegada (`ArrivalBonus`)

Ordem de chegada na faixa oficial:

- 1º Colocado: $+200\text{ pts}$
- 2º Colocado (dentro da janela de 20s): $+100\text{ pts}$
- DNF (Não concluiu): $0\text{ pts}$

### 5. Bônus de Bem-Estar Final (`WellbeingBonus`)

Multiplicador educativo que valoriza terminar a corrida com saúde e equilíbrio:

$$\text{WellbeingBonus} = \text{FinalWellbeing} \times 5\text{ pts}$$

### 6. Penalidade de Alimentos Ultraprocessados (`UltraPenalty`)

Penalidade por ingestão de ultraprocessados:

$$\text{UltraPenalty} = N_{\text{ultra}} \times 100\text{ pts}$$

### 7. Penalidade de Episódios de Fadiga (`FatiguePenalty`)

Penalidade aplicada quando o jogador entra no estado de fadiga por exaustão:

$$\text{FatiguePenalty} = N_{\text{fatigue\_episodes}} \times 200\text{ pts}$$

### 8. Penalidade de Não Conclusão (`DnfPenalty`)

Aplicada se o jogador não cruzar a linha antes do término da janela final:

$$\text{DnfPenalty} = 500\text{ pts}$$

---

## Cálculo do Score Final

$$\text{FinalScore} = \text{HealthyPoints} + \text{PuzzlePoints} + \text{TimeBonus} + \text{ArrivalBonus} + \text{WellbeingBonus} - \text{UltraPenalty} - \text{FatiguePenalty} - \text{DnfPenalty}$$

---

## Hierarquia Determinística de Desempate

O desempate entre P1 e P2 não utiliza nenhum fator pseudo-aleatório (`Random`):

1. **Maior Pontuação Final (`FinalScore`)**: o jogador com maior score vence;
2. **Maior Bem-Estar Final (`FinalWellbeing`)**: se os pontos forem iguais, vence quem estiver com maior Bem-Estar;
3. **Menor Tempo de Conclusão (`FinishTime`)**: se pontos e Bem-Estar forem iguais, vence quem completou mais rápido (concluído tem prioridade sobre DNF);
4. **Mais Alimentos Saudáveis (`HealthyFoodsCount`)**: quem coletou mais alimentos saudáveis;
5. **Menos Episódios de Fadiga (`FatigueEpisodesCount`)**: quem sofreu menos episódios de fadiga;
6. **Empate Absoluto (`WinnerPlayerIndex = 0`)**: empate formal da partida.

---

## Componentes Arquiteturais

1. **`ScoreInput.cs` (`HealthyRace.Scoring`)**: Estrutura imutável de entrada contendo métricas puras da partida.
2. **`PlayerScoreResult.cs` (`HealthyRace.Scoring`)**: Estrutura imutável de breakdown detalhado e conversão para visualização.
3. **`MatchScoreResult.cs` (`HealthyRace.Scoring`)**: Estrutura contendo os resultados individuais, vencedor e razão de desempate.
4. **`ScoreConfig.cs` (`HealthyRace.Scoring`)**: ScriptableObject serializável para balanceamento de valores.
5. **`ScoreService.cs` (`HealthyRace.Scoring`)**: Serviço estático e determinístico sem dependência de MonoBehaviour ou UI.
6. **`MatchCompletionController.cs` (`HealthyRace.Level`)**: Orquestra o encerramento, reúne os dados dos jogadores e aciona o `ScoreService`.
7. **`MatchResultsPanel.cs` (`HealthyRace.UI`)**: Exibe na tela final de resultados a colocação, tempo e breakdown de pontuação.
