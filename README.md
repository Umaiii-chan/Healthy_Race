# Healthy Race

Healthy Race é um jogo 2D competitivo de plataforma e puzzles para dois jogadores, desenvolvido em Unity com C#. Inspirado no ODS 3 — Saúde e Bem-Estar, combina corrida, estratégia e escolhas alimentares que afetam diretamente o desempenho, a fadiga e a pontuação dos jogadores.

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
- Módulo necessário: **Windows Build Support (IL2CPP)** para gerar builds
  Windows. O Editor e a documentação da API são suficientes para editar e
  executar testes.
- O Unity Package Manager restaura automaticamente as dependências de
  `Packages/manifest.json` ao abrir o projeto. Se a restauração falhar, feche o
  Editor, remova apenas `Library/` e abra novamente pela raiz.

O projeto usa URP 2D e o Unity Input System. Os diretórios gerados
`Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `Builds/`, `UserSettings/` e
`MemoryCaptures/` nunca devem ser versionados.

Se o Editor abrir em Safe Mode, leia o primeiro erro do Console. Confirme a
versão do Editor acima e execute a restauração de packages antes de tentar
alterar scripts ou arquivos de ProjectSettings manualmente.

## Fluxo de desenvolvimento

- `main`: branch estável/release.
- `develop`: integração das features aprovadas.
- `feature/*`, `fix/*`, `chore/*`: trabalho isolado via Pull Request.

Nenhum código deve chegar a `main` sem Pull Request e CI aprovado.

Consulte `CONTRIBUTING.md` e `docs/ci-cd.md` antes de contribuir.
