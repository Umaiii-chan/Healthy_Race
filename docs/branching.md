# Estratégia de branches

## Branches permanentes

### `main`

Contém apenas versões estáveis e potencialmente distribuíveis.

### `develop`

Branch de integração. Features aprovadas entram aqui primeiro.

## Branches temporárias

- `feature/<nome>` → nasce de `develop`, retorna por PR para `develop`.
- `fix/<nome>` → nasce de `develop`, retorna por PR para `develop`.
- `chore/<nome>` → infraestrutura/documentação, normalmente para `develop`.
- `release/<versao>` → nasce de `develop`; aceita apenas correções de estabilização; merge em `main` e de volta em `develop`.
- `hotfix/<nome>` → nasce de `main`; merge em `main` e `develop`.

## Fluxo padrão

```text
develop
   └── feature/player-movement
          └── Pull Request
                 └── develop
                        └── release/0.1.0
                               ├── main -> tag v0.1.0
                               └── develop
```

## Política de merge

Prefira `Squash and merge` para features e fixes. O título do PR deve seguir Conventional Commits para produzir um histórico limpo.
