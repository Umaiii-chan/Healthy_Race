# Healthy Race

Healthy Race é um jogo 2D competitivo de plataforma para dois jogadores no mesmo teclado, desenvolvido em Unity com C#. Inspirado no ODS 3 — Saúde e Bem-Estar, é uma corrida em 10 fases: em cada bifurcação o jogador escolhe o caminho **curto**, um corredor de lanchonete onde o fast-food deixa o personagem pesado, ou o **longo**, com obstáculos e alimentos saudáveis. As escolhas alimentares afetam a velocidade, a fadiga e a pontuação.

- Como se joga: [docs/GAMEPLAY.md](docs/GAMEPLAY.md)
- Controles: [docs/CONTROLS.md](docs/CONTROLS.md)
- Relatório de testes: [docs/TEST_REPORT.md](docs/TEST_REPORT.md)
- Notas da versão: [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md)

## Jogar (Windows)

Baixe o `.zip` da [última release](https://github.com/Umaiii-chan/Healthy_Race/releases),
extraia e abra `HealthyRace.exe`. Não precisa instalar nada.

## Stack

- Unity 6 LTS
- C#
- URP 2D
- Unity Input System
- Tilemap
- ScriptableObjects
- Git + GitHub + Git LFS

## Abrindo o projeto

Este repositório é um projeto Unity completo e deve ser aberto pela raiz, que
contém `Assets/`, `Packages/` e `ProjectSettings/`.

- Editor exigido: **Unity 6000.3.23f1 (Unity 6.3 LTS)**.
- Build Windows: a build usa o backend **Mono**, que já vem com o Editor
  (`Healthy Race › Build › Windows 64-bit`, ou
  `-executeMethod HealthyRace.Editor.Build.BuildScript.BuildStandaloneWindows64`).
  O módulo IL2CPP não é necessário.
- O Unity Package Manager restaura automaticamente as dependências de
  `Packages/manifest.json` ao abrir o projeto. Se a restauração falhar, feche o
  Editor, remova apenas `Library/` e abra novamente pela raiz.

O projeto usa URP 2D e o Unity Input System. Os diretórios gerados
`Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `Builds/`, `UserSettings/` e
`MemoryCaptures/` nunca devem ser versionados.

Se o Editor abrir em Safe Mode, leia o primeiro erro do Console. Confirme a
versão do Editor acima e execute a restauração de packages antes de tentar
alterar scripts ou arquivos de ProjectSettings manualmente.

## Testes

No Editor: `Window › General › Test Runner` (EditMode e PlayMode). Por linha de comando:

```bash
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults em.xml
```

O PlayMode inclui um robô que percorre as 10 fases pelos dois caminhos com física real.
Em modo batch o jogo fica sem som.

## Fases

Cada fase é um mapa em texto em `Assets/_Game/Levels/` (formato em
[docs/levels/fase-01.md](docs/levels/fase-01.md)). A Fase 1 vem de
`tools/levels/fase01_map.py`, e as fases 2 a 10 de `tools/levels/build_levels.py`. Depois de
mudar um mapa, rode `Healthy Race › Visuals › Build greenhouse levels` para regerar as cenas.

## Fluxo de desenvolvimento

- `main`: branch estável/release.
- `develop`: integração das features aprovadas.
- `feature/*`, `fix/*`, `chore/*`: trabalho isolado via Pull Request.

Nenhum código deve chegar a `main` sem Pull Request e CI aprovado.

Consulte `CONTRIBUTING.md` e `docs/ci-cd.md` antes de contribuir.
