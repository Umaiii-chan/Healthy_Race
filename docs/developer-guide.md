# Guia de Desenvolvimento e Boas Práticas

Este documento define o fluxo recomendado de desenvolvimento, versionamento,
organização de arquivos, criação de scripts, Pull Requests, CI/CD e releases do
Healthy Race.

O objetivo é manter o repositório previsível, reduzir conflitos e garantir que
as mudanças cheguem às branches estáveis apenas após revisão e validação.

## 1. Branches do projeto

O projeto utiliza duas branches permanentes:

- `main`: versões estáveis e distribuíveis.
- `develop`: integração das funcionalidades aprovadas.

Branches temporárias:

- `feature/<nome>`: novas funcionalidades.
- `fix/<nome>`: correções de bugs.
- `chore/<nome>`: manutenção, ferramentas e infraestrutura.
- `docs/<nome>`: documentação.
- `test/<nome>`: criação ou manutenção de testes.
- `release/<versao>`: estabilização de uma versão.
- `hotfix/<nome>`: correção urgente originada de `main`.

Fluxo padrão:

```text
develop
  └── feature/player-movement
        └── Pull Request
              └── develop
```

Não envie código diretamente para `main` ou `develop`.

## 2. Ferramentas necessárias

Instale antes de começar:

- Git.
- Git LFS.
- Unity na versão definida pelo projeto.
- Visual Studio, JetBrains Rider ou VS Code com suporte a C#.
- GitHub CLI (`gh`), opcional, mas recomendado.

Validação rápida:

```bash
git --version
git lfs version
gh --version
```

## 3. Configuração inicial do Git

Configure identidade e comportamento básico:

```bash
git config --global user.name "Seu Nome"
git config --global user.email "seu-email@exemplo.com"
git config --global init.defaultBranch main
git config --global core.autocrlf false
```

O repositório utiliza `.gitattributes` para normalizar arquivos de texto com
`LF`.

## 4. Clonando o repositório

```bash
git clone https://github.com/Umaiii-chan/Healthy_Race.git
cd Healthy_Race
git lfs install
git lfs pull
git status
```

## 5. Atualizando referências remotas

Antes de iniciar uma tarefa:

```bash
git fetch --all --prune
```

Esse comando busca novas referências e remove referências locais de branches
remotas que já foram excluídas.

Atualizar `develop`:

```bash
git switch develop
git pull --rebase origin develop
```

Atualizar `main`:

```bash
git switch main
git pull --rebase origin main
```

Prefira `pull --rebase` durante o trabalho diário para evitar merge commits
acidentais.

## 6. Criando branches de trabalho

Nova feature:

```bash
git switch develop
git pull --rebase origin develop
git switch -c feature/player-movement
```

Correção:

```bash
git switch develop
git pull --rebase origin develop
git switch -c fix/fatigue-trigger
```

Documentação:

```bash
git switch develop
git pull --rebase origin develop
git switch -c docs/update-developer-guide
```

Hotfix:

```bash
git switch main
git pull --rebase origin main
git switch -c hotfix/critical-save-error
```

## 7. Comandos Git usados no dia a dia

Ver o estado atual:

```bash
git status
```

Ver alterações ainda não adicionadas ao stage:

```bash
git diff
```

Ver alterações já adicionadas ao stage:

```bash
git diff --staged
```

Adicionar um arquivo:

```bash
git add Assets/_Game/Scripts/Player/PlayerMovement.cs
```

Adicionar todas as alterações intencionalmente:

```bash
git add -A
```

Remover um arquivo do stage:

```bash
git restore --staged caminho/do/arquivo
```

Descartar uma alteração local ainda não commitada:

```bash
git restore caminho/do/arquivo
```

Ver histórico:

```bash
git log --oneline --graph --decorate --all
```

Criar commit:

```bash
git commit -m "feat: implement player movement"
```

Enviar uma branch pela primeira vez:

```bash
git push -u origin feature/player-movement
```

Nos próximos pushes:

```bash
git push
```

Sempre confira `git status` antes de usar `git add -A`.

## 8. Conventional Commits

Formato:

```text
<tipo>: <descrição curta>
```

Tipos adotados:

- `feat`: nova funcionalidade.
- `fix`: correção.
- `refactor`: refatoração sem alteração de comportamento esperado.
- `test`: testes.
- `docs`: documentação.
- `chore`: manutenção.
- `ci`: CI/CD.
- `build`: build ou dependências.
- `perf`: desempenho.

Exemplos válidos:

```text
feat: implement wellbeing system
fix: prevent fatigue from triggering twice
test: add wellbeing edit mode tests
docs: document branch workflow
ci: add Unity test workflow
chore: configure git lfs
```

Evite mensagens vagas como:

```text
fix
ajuste
teste
agora vai
changes
update
```

## 9. Mantendo a branch atualizada

Antes de abrir um Pull Request para `develop`:

```bash
git fetch origin
git rebase origin/develop
```

Se houver conflitos, resolva os arquivos e execute:

```bash
git add caminho/do/arquivo
git rebase --continue
```

Cancelar o rebase:

```bash
git rebase --abort
```

Se a branch já havia sido enviada antes do rebase:

```bash
git push --force-with-lease
```

Use `--force-with-lease` apenas em branches pessoais de trabalho.
Nunca faça force push em `main` ou `develop`.

## 10. Resolução de conflitos

Marcadores típicos de conflito:

```text
<<<<<<< HEAD
código A
=======
código B
>>>>>>> outra-branch
```

Procedimento:

1. Entenda as duas versões.
2. Defina o comportamento correto.
3. Remova os marcadores.
4. Valide a Scene, Prefab ou script afetado.
5. Execute os testes relacionados.
6. Adicione o arquivo corrigido ao stage.
7. Continue o rebase ou finalize o commit.

O CI bloqueia arquivos que contenham marcadores de conflito.

## 11. Pull Requests

Um Pull Request deve ser pequeno, objetivo e testável.

Criar pelo GitHub CLI:

```bash
gh pr create \
  --base develop \
  --head feature/player-movement \
  --title "feat: implement player movement"
```

Abrir o PR atual no navegador:

```bash
gh pr view --web
```

Ver informações:

```bash
gh pr view
```

Ver checks:

```bash
gh pr checks
```

Baixar um PR para revisão:

```bash
gh pr checkout 123
```

O PR deve informar:

- problema ou necessidade;
- solução implementada;
- forma de teste;
- evidências quando aplicável;
- riscos conhecidos;
- impacto em outras áreas.

## 12. Política de merge

Prefira `Squash and merge` para features, fixes, documentação e manutenção.

Exemplo:

```text
feature/player-movement
  ├── commit 1
  ├── commit 2
  └── Squash and merge
        └── feat: implement player movement
```

O merge só deve ocorrer quando:

- todos os checks obrigatórios estiverem verdes;
- o PR possuir as aprovações necessárias;
- as conversas obrigatórias estiverem resolvidas;
- a branch estiver atualizada com a base;
- não houver credenciais ou arquivos gerados no diff.

## 13. GitHub Actions

Listar execuções recentes:

```bash
gh run list
```

Abrir uma execução:

```bash
gh run view <run-id>
```

Ver apenas logs de falha:

```bash
gh run view <run-id> --log-failed
```

Executar manualmente o workflow de build:

```bash
gh workflow run build.yml
```

Acompanhar uma execução:

```bash
gh run watch
```

## 14. Quality Gate

Pull Requests para `develop` e `main` passam pelo Quality Gate.

Validações previstas:

- validação estrutural do repositório;
- Markdown lint;
- YAML lint;
- formatação de C#;
- testes Unity EditMode e PlayMode quando o projeto e a licença estiverem
  configurados.

Checks atuais:

- `Repository validation`.
- `Markdown and YAML lint`.
- `C# format`.
- `EditMode and PlayMode tests`.

Não desabilite uma regra do CI apenas para obter um merge. Primeiro identifique
a causa da falha.

## 15. Validação local

O CI executa:

```bash
bash .github/scripts/validate-repository.sh
```

Esse script verifica:

- diretórios gerados pela Unity;
- marcadores de conflito;
- possíveis credenciais;
- arquivos maiores que 50 MB fora do Git LFS.

Execute essa validação antes de abrir um Pull Request quando estiver em Linux,
macOS ou Git Bash.

## 16. Estrutura de pastas

Conteúdo próprio do jogo deve permanecer em `Assets/_Game`.

```text
Assets/
└── _Game/
    ├── Art/
    │   ├── Characters/
    │   ├── Collectibles/
    │   ├── Environment/
    │   ├── UI/
    │   └── VFX/
    ├── Audio/
    │   ├── Music/
    │   └── SFX/
    ├── Animations/
    ├── Materials/
    ├── Prefabs/
    │   ├── Characters/
    │   ├── Collectibles/
    │   ├── Obstacles/
    │   └── Puzzles/
    ├── Scenes/
    ├── Scripts/
    │   ├── Core/
    │   ├── Player/
    │   ├── WellBeing/
    │   ├── Collectibles/
    │   ├── Obstacles/
    │   ├── Puzzles/
    │   ├── Scoring/
    │   ├── UI/
    │   └── Audio/
    ├── ScriptableObjects/
    │   ├── Food/
    │   ├── Player/
    │   └── Levels/
    └── Tests/
        ├── EditMode/
        └── PlayMode/
```

## 17. Onde colocar cada tipo de arquivo

Scripts de personagem:

```text
Assets/_Game/Scripts/Player/
```

Sistema de Bem-Estar:

```text
Assets/_Game/Scripts/WellBeing/
```

Coletáveis:

```text
Assets/_Game/Scripts/Collectibles/
Assets/_Game/Prefabs/Collectibles/
Assets/_Game/ScriptableObjects/Food/
```

Obstáculos:

```text
Assets/_Game/Scripts/Obstacles/
Assets/_Game/Prefabs/Obstacles/
```

Puzzles:

```text
Assets/_Game/Scripts/Puzzles/
Assets/_Game/Prefabs/Puzzles/
```

Interface:

```text
Assets/_Game/Scripts/UI/
Assets/_Game/Art/UI/
```

Áudio:

```text
Assets/_Game/Scripts/Audio/
Assets/_Game/Audio/Music/
Assets/_Game/Audio/SFX/
```

Testes:

```text
Assets/_Game/Tests/EditMode/
Assets/_Game/Tests/PlayMode/
```

## 18. Boas práticas para scripts em `C#`

Prefira um conceito principal por arquivo:

```text
PlayerMovement.cs
PlayerWellBeing.cs
PlayerScore.cs
```

O nome do arquivo deve corresponder ao tipo principal.

Exemplo:

```csharp
public class PlayerMovement : MonoBehaviour
{
}
```

O arquivo correspondente deve ser `PlayerMovement.cs`.

Use namespaces:

```csharp
namespace HealthyRace.Player
{
    public class PlayerMovement : MonoBehaviour
    {
    }
}
```

Namespaces recomendados:

```text
HealthyRace.Core
HealthyRace.Player
HealthyRace.WellBeing
HealthyRace.Collectibles
HealthyRace.Obstacles
HealthyRace.Puzzles
HealthyRace.Scoring
HealthyRace.UI
HealthyRace.Audio
```

Prefira componentes pequenos e especializados.

Evite um único `PlayerManager` responsável por movimento, input, Bem-Estar,
pontuação, animação, áudio e interação.

Prefira composição:

```text
Player
├── PlayerController
├── PlayerMovement
├── PlayerInput
├── PlayerWellBeing
├── PlayerScore
├── PlayerInteraction
└── PlayerVisual
```

Evite singletons sem necessidade. Um singleton global só deve existir quando
houver uma justificativa arquitetural clara.

## 19. Convenções de nomenclatura em `C#`

Classes, métodos públicos, propriedades e enums usam `PascalCase`.

```csharp
public class PlayerMovement
public float CurrentSpeed { get; private set; }
public void ApplyFatigue()
```

Variáveis locais e parâmetros usam `camelCase`.

Campos privados usam `_camelCase`.

```csharp
private float _moveSpeed;
```

Booleanos devem indicar uma condição:

```text
isGrounded
isFatigued
canMove
hasFinished
```

Evite nomes genéricos como `flag`, `value`, `data2`, `temp1` ou `thing`.

## 20. Campos configuráveis no Inspector

Prefira campos privados serializados:

```csharp
[SerializeField] private float _moveSpeed = 5f;
```

Evite tornar todos os campos públicos apenas para exibi-los no Inspector.

Quando uma referência obrigatória puder causar erro em runtime, valide-a:

```csharp
private void Awake()
{
    if (_rigidbody == null)
    {
        Debug.LogError("Rigidbody2D is required.", this);
    }
}
```

## 21. Arquivos `.meta`

Arquivos `.meta` devem ser versionados.

Eles armazenam GUIDs utilizados pela Unity para relacionar scripts, prefabs,
assets, materiais e cenas.

Nunca adicione `*.meta` ao `.gitignore`.

Ao mover ou renomear assets, prefira fazer a operação pelo Unity Editor.

## 22. Arquivos proibidos no Git

Diretórios gerados pela Unity não devem ser commitados:

```text
Library/
Temp/
obj/
Build/
Builds/
Logs/
UserSettings/
MemoryCaptures/
Recordings/
```

Também não devem ser versionados arquivos locais de IDE, artefatos de testes ou
segredos.

Se um arquivo ignorado já estiver sendo rastreado:

```bash
git rm -r --cached caminho
```

Depois faça commit da remoção.

## 23. Git LFS

O projeto usa Git LFS para arquivos binários e assets grandes.

Extensões configuradas incluem:

```text
.psd
.psb
.png
.jpg
.jpeg
.tga
.fbx
.blend
.wav
.mp3
.ogg
.mp4
.mov
.ttf
.otf
```

Ver padrões rastreados:

```bash
git lfs track
```

Ver arquivos LFS atuais:

```bash
git lfs ls-files
```

Após clonar:

```bash
git lfs install
git lfs pull
```

## 24. Scenes e Prefabs

Evite que várias pessoas editem simultaneamente a mesma Scene grande.

Prefira:

- cenas menores;
- prefabs reutilizáveis;
- Prefab Variants quando apropriado;
- composição de objetos;
- comunicação prévia quando uma Scene crítica estiver em edição.

Arquivos `.unity`, `.prefab`, `.asset`, `.mat`, `.anim` e `.controller` estão
preparados para `unityyamlmerge` quando a ferramenta estiver configurada
localmente.

## 25. ScriptableObjects

Use ScriptableObjects para dados compartilhados e configurações de gameplay.

Exemplo conceitual:

```text
FoodData
├── displayName
├── type
├── score
├── wellbeingModifier
├── sprite
└── collectSound
```

Eles são adequados para balanceamento porque permitem alterar dados sem alterar
a lógica dos sistemas.

Evite armazenar estado mutável específico de uma partida diretamente em um
asset persistente sem uma estratégia explícita de reset ou clonagem.

## 26. Testes

Use EditMode para lógica independente da execução completa de uma Scene.

Exemplos:

- cálculo de pontuação;
- regras de Bem-Estar;
- regras de fadiga;
- validação de dados.

Use PlayMode para comportamentos dependentes do jogo em execução.

Exemplos:

- movimento;
- colisões;
- coleta de itens;
- ativação de puzzles;
- conclusão de fases.

Ao corrigir um bug relevante, sempre que possível crie um teste que reproduza o
problema.

## 27. Segredos e credenciais

Nunca envie para o Git:

- senhas;
- tokens;
- API keys;
- licenças;
- credenciais externas.

Configure segredos em:

```text
Settings
└── Secrets and variables
    └── Actions
```

Secrets Unity previstos:

```text
UNITY_LICENSE
UNITY_EMAIL
UNITY_PASSWORD
```

Arquivos `.env` são ignorados. Quando for necessário documentar variáveis, use
`.env.example` sem valores sensíveis.

## 28. Revertendo alterações

Editar o último commit local:

```bash
git commit --amend
```

Desfazer de forma segura um commit já compartilhado:

```bash
git revert <commit-sha>
```

Guardar alterações temporariamente:

```bash
git stash push -m "work in progress"
```

Listar stashes:

```bash
git stash list
```

Restaurar o último stash:

```bash
git stash pop
```

Não use stash como armazenamento permanente.

## 29. Releases e tags

O projeto utiliza Semantic Versioning.

Exemplos:

```text
v0.1.0
v0.2.0
v0.2.1
v1.0.0
```

Fluxo:

```text
develop
  └── release/0.1.0
        ├── validação final
        ├── Pull Request para main
        └── tag v0.1.0
```

Criar tag:

```bash
git tag -a v0.1.0 -m "Healthy Race v0.1.0"
```

Enviar tag:

```bash
git push origin v0.1.0
```

Tags no formato `vX.Y.Z` podem acionar o workflow de build.

## 30. Checklist antes do push

Antes de `git push`, confirme:

- `git status` contém apenas alterações esperadas;
- não existem diretórios gerados da Unity;
- nenhum segredo foi adicionado;
- o código compila;
- testes relacionados passam;
- arquivos estão em pastas corretas;
- arquivos `.meta` necessários estão presentes;
- assets grandes usam Git LFS;
- a branch possui nome adequado;
- commits possuem mensagens claras.

## 31. Checklist antes do Pull Request

1. Execute `git fetch origin`.
2. Faça `git rebase origin/develop` quando a base for `develop`.
3. Resolva conflitos.
4. Rode os testes locais aplicáveis.
5. Execute a validação do repositório.
6. Revise o diff completo.
7. Faça push da branch.
8. Abra o Pull Request.
9. Aguarde o CI.
10. Corrija todas as falhas antes do merge.

Revisar o que será enviado:

```bash
git diff origin/develop...HEAD
```

## 32. Fluxo completo de exemplo

```bash
git switch develop
git pull --rebase origin develop

git switch -c feature/player-movement

# desenvolver e testar

git status
git diff

git add Assets/_Game/Scripts/Player/PlayerMovement.cs
git commit -m "feat: implement player movement"

git fetch origin
git rebase origin/develop

git push -u origin feature/player-movement

gh pr create \
  --base develop \
  --head feature/player-movement \
  --title "feat: implement player movement"

gh pr checks
```

Depois da aprovação e dos checks verdes, use `Squash and merge`.

## 33. Princípio de organização

Antes de adicionar uma nova pasta, Manager global, dependência, singleton,
pipeline ou padrão arquitetural, responda:

1. Qual problema concreto isso resolve?
2. Já existe uma solução no projeto?
3. Essa decisão facilita ou dificulta os testes?
4. Outro desenvolvedor saberá onde encontrar essa implementação?
5. Essa decisão aumenta o acoplamento sem necessidade?

A estrutura do repositório deve permanecer simples, previsível e orientada aos
domínios do gameplay.
