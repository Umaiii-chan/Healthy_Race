# Contribuindo com Healthy Race

## Pré-requisitos

- Unity 6 LTS na versão definida em `ProjectSettings/ProjectVersion.txt`.
- Git e Git LFS.
- Uma branch criada a partir de `develop`.

## Branches

Use os prefixos:

- `feature/<descricao>` para novas funcionalidades.
- `fix/<descricao>` para correções.
- `chore/<descricao>` para infraestrutura, documentação e manutenção.
- `release/<versao>` para estabilização de release.
- `hotfix/<descricao>` apenas para correções urgentes em produção.

## Commits

Use Conventional Commits:

- `feat: add wellbeing system`
- `fix: prevent double fatigue trigger`
- `test: add wellbeing edit mode tests`
- `docs: update game rules`
- `chore: update ci workflow`

## Pull Requests

Todo PR deve:

1. Ter escopo pequeno e objetivo.
2. Explicar o que mudou e como foi testado.
3. Estar atualizado com a branch base.
4. Passar por todas as validações de CI.
5. Ter pelo menos uma aprovação quando houver mais de um colaborador.
6. Não conter conflitos, arquivos gerados pela Unity ou segredos.

## Definition of Done

Uma tarefa só está concluída quando código, testes, documentação necessária e CI estiverem verdes.
