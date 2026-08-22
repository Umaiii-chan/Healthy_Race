# CI/CD e governança

## Objetivo

Impedir que mudanças quebradas ou não revisadas cheguem às branches estáveis.

## Pipeline de Pull Request

Todo PR para `develop` ou `main` executa:

1. Validação do repositório: arquivos gerados, conflitos, credenciais e arquivos grandes.
2. Lint de Markdown e YAML.
3. Validação de formatação C# com CSharpier.
4. Testes Unity EditMode e PlayMode quando o projeto e a licença do CI estiverem configurados.

## Pipeline de build

Builds distribuíveis são executados:

- manualmente via `workflow_dispatch`; ou
- ao criar uma tag SemVer no formato `vX.Y.Z`.

O build Windows x64 é publicado como artifact do GitHub Actions.

## Secrets necessários

Configure em `Settings > Secrets and variables > Actions`:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Para licença Unity Pro, adapte o workflow para `UNITY_SERIAL` conforme o modelo de licenciamento utilizado.

## Proteção recomendada

### `main`

- Require a pull request before merging.
- Require at least 1 approval quando houver equipe.
- Dismiss stale approvals when new commits are pushed.
- Require conversation resolution.
- Require branches to be up to date before merging.
- Require status checks: `Repository validation`, `Markdown and YAML lint`, `C# format`, `EditMode and PlayMode tests`.
- Block force pushes and deletions.
- Prefer Squash Merge.

### `develop`

- Require pull request.
- Require status checks.
- Block force pushes and deletions.

## Releases

Use Semantic Versioning:

- `v0.1.0`: primeiro protótipo jogável.
- `v0.2.0`: nova funcionalidade compatível.
- `v0.2.1`: correção.
- `v1.0.0`: primeira entrega considerada estável.
