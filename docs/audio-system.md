# Sistema de Áudio (AUDIO-01)

## Descrição

O Sistema de Áudio centraliza a reprodução de trilhas musicais e efeitos sonoros (SFX) para o Healthy Race, provendo controle de volume hierárquico (Master, Music, SFX), persistência via `PlayerPrefs`, fallback sintético procedural para ambientes de CI/batchmode sem áudio nativo, e integração com todos os sistemas de gameplay da Fase 1 (Corrida do Equilíbrio).

---

## Arquitetura

### 1. Centralização com AudioService

- `AudioService` opera como componente gerenciador único (Singleton acessível via `AudioService.Instance`), instanciando e configurando dois `AudioSource`s dedicados:
  - `MusicSource`: em loop contínuo, responsável pela trilha musical de fundo;
  - `SfxSource`: disparador de sons pontuais com volume proporcional via `PlayOneShot`.
- Evita explosão de `AudioSource`s em GameObjects de cena, reduzindo overhead de CPU e memória.

### 2. Controle de Volume e Persistência

- Fórmulas de volume efetivo:
  - `VolumeMusica = MasterVolume * MusicVolume`
  - `VolumeSFX = MasterVolume * SfxVolume * volumeMultiplier`
- Valores são sempre limitados no intervalo fechado `[0.0, 1.0]`.
- Persistência automática em `PlayerPrefs`:
  - `HealthyRace_MasterVolume` (padrão: 1.0)
  - `HealthyRace_MusicVolume` (padrão: 0.7)
  - `HealthyRace_SfxVolume` (padrão: 0.8)
- Evento reativo: `VolumeChanged(float master, float music, float sfx)`.

### 3. Catálogo de Áudio (18 Assets)

Todos os clipes estão mapeados no enum `AudioClipId`:

| Identificador | Tipo | Arquivo | Gatilho |
| --- | --- | --- | --- |
| `Music_Menu` | Música | `Assets/_Game/Audio/Music/mus_menu.wav` | Menu Principal / Telas Iniciais |
| `Music_Level01` | Música | `Assets/_Game/Audio/Music/mus_level01.wav` | Início da Fase 1 |
| `Sfx_Jump` | SFX | `Assets/_Game/Audio/SFX/sfx_jump.wav` | Pulo bem-sucedido do jogador |
| `Sfx_Step` | SFX | `Assets/_Game/Audio/SFX/sfx_step.wav` | Passos / locomoção |
| `Sfx_PickupHealthy` | SFX | `Assets/_Game/Audio/SFX/sfx_pickup_healthy.wav` | Coleta de alimento saudável |
| `Sfx_PickupUltra` | SFX | `Assets/_Game/Audio/SFX/sfx_pickup_ultra.wav` | Coleta de ultraprocessado |
| `Sfx_WellbeingUp` | SFX | `Assets/_Game/Audio/SFX/sfx_wellbeing_up.wav` | Aumento no nível de Bem-Estar |
| `Sfx_WellbeingDown` | SFX | `Assets/_Game/Audio/SFX/sfx_wellbeing_down.wav` | Queda no nível de Bem-Estar |
| `Sfx_Fatigue` | SFX | `Assets/_Game/Audio/SFX/sfx_fatigue.wav` | Entrada no estado de fadiga |
| `Sfx_Button` | SFX | `Assets/_Game/Audio/SFX/sfx_button.wav` | Placa de pressão/botão acionado |
| `Sfx_Lever` | SFX | `Assets/_Game/Audio/SFX/sfx_lever.wav` | Alavanca cooperativa alternada |
| `Sfx_Door` | SFX | `Assets/_Game/Audio/SFX/sfx_door.wav` | Porta do puzzle aberta |
| `Sfx_PuzzleSolved` | SFX | `Assets/_Game/Audio/SFX/sfx_puzzle_solved.wav` | Quebra-cabeça cooperativo resolvido |
| `Sfx_Checkpoint` | SFX | `Assets/_Game/Audio/SFX/sfx_checkpoint.wav` | Checkpoint ativado pelo jogador |
| `Sfx_Finish` | SFX | `Assets/_Game/Audio/SFX/sfx_finish.wav` | Linha de chegada cruzada |
| `Sfx_Victory` | SFX | `Assets/_Game/Audio/SFX/sfx_victory.wav` | Conclusão da partida com vitória |
| `Sfx_Defeat` | SFX | `Assets/_Game/Audio/SFX/sfx_defeat.wav` | Conclusão da partida com derrota |
| `Sfx_Tie` | SFX | `Assets/_Game/Audio/SFX/sfx_tie.wav` | Conclusão da partida com empate |

### 4. Resiliência e Fallback Sintético

Para garantir execução robusta em pipelines de integração contínua (GitHub Actions) ou execução batchmode sem GPU/placa de som física:

- `AudioService.GetClip(AudioClipId id)` carrega o asset do disco no Editor e o armazena em cache.
- Caso o arquivo não esteja importado ou o teste rode em runtime isolado, `CreateSyntheticFallback(id)` gera um `AudioClip` sintetizado proceduralmente (onda senoidal com envoltória ADSR adequada) via `AudioClip.Create`.
- Garante 0 chamadas a `null` ou `NullReferenceException`.

---

## Integração no Gameplay

No `Phase1Bootstrap`:

1. `Start()`: inicia `AudioClipId.Music_Level01`.
2. Locomoção do jogador (`PlayerMovement.Jumped`): reproduz `Sfx_Jump`.
3. Sistema de fadiga (`FatigueComponent.StateChanged`): reproduz `Sfx_Fatigue` ao fadigar.
4. Bem-Estar (`WellbeingComponent.WellbeingChanged`): reproduz `Sfx_WellbeingUp` ou `Sfx_WellbeingDown`.
5. Coleta de comida (`HandleFoodCollected`): reproduz `Sfx_PickupHealthy` ou `Sfx_PickupUltra`.
6. Quebra-cabeça da Zona 5:
   - Botão: `Sfx_Button`
   - Alavanca: `Sfx_Lever`
   - Porta: `Sfx_Door`
   - Resolução: `Sfx_PuzzleSolved`
7. Checkpoint: `Sfx_Checkpoint`.
8. Chegada: `Sfx_Finish`.
9. Encerramento de partida (`MatchCompletionController.MatchConcluded`): `Sfx_Victory` ou `Sfx_Tie`.

---

## Testes Automatizados

- **EditMode** (`Assets/_Game/Tests/EditMode/AudioServiceTests.cs`):
  - Validação de volumes padrão;
  - Clamping estrito `[0, 1]` em todos os setters de volume;
  - Disparo de eventos `VolumeChanged`;
  - Persistência e restauração de dados via `PlayerPrefs`;
  - Resolução de clipes de áudio não nulos para todos os 18 `AudioClipId`;
  - Atribuição e limpeza de clipe no `MusicSource`;
  - Execução de SFX sem exceções.
- **PlayMode** (`Assets/_Game/Tests/PlayMode/AudioPlayModeTests.cs`):
  - Ciclo de vida assíncrono com frames de simulação;
  - Reprodução de música e parada com limpeza de clipe;
  - Cálculo de volumes efetivos nos `AudioSource`s em tempo de execução;
  - Reprodução sequencial de todos os SFX sem erros de frame.
