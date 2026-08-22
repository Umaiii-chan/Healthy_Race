# Guia de Desenvolvimento e Boas Práticas

Este documento define o fluxo de trabalho recomendado para desenvolvimento,
versionamento, organização de arquivos, criação de scripts, Pull Requests,
validações e releases do projeto Healthy Race.

O objetivo é reduzir conflitos, evitar arquivos indevidos no repositório e manter
um padrão previsível para todos os colaboradores.

## 1. Visão geral do fluxo

O projeto utiliza as seguintes branches principais:

- `main`: código estável e versões distribuíveis.
- `develop`: integração das funcionalidades aprovadas.

Branches temporárias devem nascer da branch apropriada e ser integradas por
Pull Request.

Tipos de branch:

- `feature/<nome>`: nova funcionalidade.
- `fix/<nome>`: correção de bug.
- `chore/<nome>`: infraestrutura, ferramentas e manutenção.
- `docs/<nome>`: documentação.
- `test/<nome>`: criação ou manutenção de testes.
- `release/<versao>`: estabilização de uma versão.
- `hotfix/<nome>`: correção urgente originada de `main`.

Exemplo:

```text
develop
  └── feature/player-movement
        └── Pull Request
              └── develop
```

Código não deve ser enviado diretamente para `main` ou `develop` quando a
proteção de branch estiver ativa.

## 2. Ferramentas necessárias

Antes de começar, instale:

- Git.
- Git LFS.
- Unity na versão definida pelo projeto.
- Visual Studio, JetBrains Rider ou VS Code com suporte a C#.
- Opcionalmente, GitHub CLI (`gh`).

Confirme as instalações:

```bash
git --version
git lfs version
gh --version
```

O comando `gh --version` é necessário apenas para quem utilizar GitHub CLI.

## 3. Configuração inicial do Git

Configure seu nome e e-mail:

```bash
git config --global user.name "Seu Nome"
git config --global user.email "seu-email@exemplo.com"
```

Recomenda-se habilitar o uso de `main` como nome padrão para novos
repositórios:

```bash
git config --global init.defaultBranch main
```

No Windows, confirme que o projeto não está alterando automaticamente o padrão
de fim de linha definido pelo repositório:

```bash
git config --global core.autocrlf false
```

O projeto já utiliza `.gitattributes` com `eol=lf` para normalizar arquivos de
texto.

## 4. Clonando o projeto

Clone o repositório:

```bash
git clone https://github.com/Umaiii-chan/Healthy_Race.git
cd Healthy_Race
```

Inicialize o Git LFS:

```bash
git lfs install
```

Baixe os arquivos LFS, se necessário:

```bash
git lfs pull
```

Confira o estado do projeto:

```bash
git status
```

## 5. Atualizando referências remotas

Antes de iniciar qualquer tarefa:

```bash
git fetch --all --prune
```

Esse comando:

- busca commits e branches remotas;
- não altera seus arquivos locais;
- remove referências locais para branches remotas já excluídas.

Para atualizar `develop`:

```bash
git switch develop
git pull --rebase origin develop
```

Para atualizar `main`:

```bash
git switch main
git pull --rebase origin main
```

Prefira `pull --rebase` durante o trabalho diário para evitar merge commits
acidentais em branches locais.

## 6. Criando uma branch de trabalho

Nunca comece uma feature diretamente em `main`.

Para uma feature comum:

```bash
git switch develop
git pull --rebase origin develop
git switch -c feature/player-movement
```

Para uma correção:

```bash
git switch develop
git pull --rebase origin develop
git switch -c fix/fatigue-trigger
```

Para documentação:

```bash
git switch develop
git pull --rebase origin develop
git switch -c docs/update-developer-guide
```

Para hotfix de produção:

```bash
git switch main
git pull --rebase origin main
git switch -c hotfix/critical-save-error
```

## 7. Comandos Git usados no dia a dia

### Ver o estado do repositório

```bash
git status
```

Use antes de qualquer commit.

### Ver alterações não commitadas

```bash
git diff
```

Para alterações já adicionadas ao stage:

```bash
git diff --staged
```

### Adicionar um arquivo ao stage

```bash
git add Assets/_Game/Scripts/Player/PlayerMovement.cs
```

Evite usar `git add .` sem antes conferir `git status`.

Quando desejar adicionar todas as alterações intencionalmente:

```bash
git add -A
```

### Remover um arquivo do stage

```bash
git restore --staged caminho/do/arquivo
```

O arquivo continua no disco, apenas deixa de fazer parte do próximo commit.

### Descartar uma alteração local

```bash
git restore caminho/do/arquivo
```

Atenção: esse comando remove alterações ainda não commitadas no arquivo.

### Ver histórico

```bash
git log --oneline --graph --decorate --all
```

### Criar commit

```bash
git commit -m "feat: implement player movement"
```

### Enviar a branch pela primeira vez

```bash
git push -u origin feature/player-movement
```

Depois disso, basta:

```bash
git push
```

## 8. Padrão de commits

O projeto utiliza Conventional Commits.

Formato:

```text
<tipo>: <descrição curta>
```

Tipos mais usados:

- `feat`: nova funcionalidade.
- `fix`: correção.
- `refactor`: alteração estrutural sem nova funcionalidade.
- `test`: testes.
- `docs`: documentação.
- `chore`: manutenção e ferramentas.
- `ci`: CI/CD.
- `build`: sistema de build ou dependências.
- `perf`: melhoria de desempenho.

Exemplos:

```text
feat: implement wellbeing system
fix: prevent fatigue from triggering twice
test: add wellbeing edit mode tests
docs: document branch workflow
ci: add Unity test workflow
chore: configure git lfs
```

Evite mensagens como:

```text
fix
ajuste
teste
agora vai
changes
update
```

Um commit deve representar uma alteração lógica e compreensível.

## 9. Atualizando sua branch com `develop`

Antes de abrir um Pull Request, atualize sua branch:

```bash
git fetch origin
git rebase origin/develop
```

Se houver conflitos, o Git indicará os arquivos afetados.

Após resolver cada conflito:

```bash
git add caminho/do/arquivo
git rebase --continue
```

Para cancelar o rebase:

```bash
git rebase --abort
```

Se sua branch já havia sido enviada ao GitHub antes do rebase, será necessário
atualizar o remoto:

```bash
git push --force-with-lease
```

Use `--force-with-lease`, nunca `--force`, em branches pessoais de trabalho.

Nunca force push em `main` ou `develop`.

## 10. Resolvendo conflitos

Um conflito normalmente contém marcadores como:

```text
<<<<<<< HEAD
código A
=======
código B
>>>>>>> outra-branch
```

Não faça commit com esses marcadores.

Procedimento recomendado:

1. Entenda as duas versões.
2. Defina qual comportamento deve permanecer.
3. Remova todos os marcadores.
4. Abra o projeto e valide o resultado.
5. Execute os testes.
6. Adicione o arquivo resolvido ao stage.
7. Continue o rebase ou finalize o commit.

O CI possui uma validação específica contra marcadores de conflito.

## 11. Criando Pull Requests

Um Pull Request deve ser pequeno, objetivo e testável.

Pelo site do GitHub, selecione sua branch e crie o PR para `develop`.

Usando GitHub CLI:

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

Ver informações do PR:

```bash
gh pr view
```

Ver checks:

```bash
gh pr checks
```

Fazer checkout de um PR para revisão:

```bash
gh pr checkout 123
```

O PR deve explicar:

- problema ou necessidade;
- solução implementada;
- como testar;
- evidências quando aplicável;
- riscos conhecidos;
- impacto em outras partes do projeto.

## 12. Política de merge

Para features, fixes e documentação, prefira `Squash and merge`.

Exemplo:

```text
feature/player-movement
  ├── commit 1
  ├── commit 2
  ├── commit 3
  └── Squash and merge
        └── feat: implement player movement
```

Isso mantém o histórico das branches principais legível.

O merge só deve ocorrer quando:

- os checks obrigatórios estiverem verdes;
- o PR possuir as aprovações necessárias;
- todas as conversas obrigatórias estiverem resolvidas;
- a branch estiver atualizada com a base;
- não houver arquivos gerados ou credenciais no diff.

## 13. Verificando GitHub Actions

Listar execuções recentes:

```bash
gh run list
```

Visualizar uma execução:

```bash
gh run view <run-id>
```

Visualizar apenas logs que falharam:

```bash
gh run view <run-id> --log-failed
```

Executar manualmente um workflow que suporte `workflow_dispatch`:

```bash
gh workflow run build.yml
```

Acompanhar a execução:

```bash
gh run watch
```

## 14. Quality Gate

Pull Requests para `develop` e `main` passam pelo Quality Gate.

As validações incluem:

- validação estrutural do repositório;
- Markdown lint;
- YAML lint;
- formatação de C#;
- testes Unity EditMode e PlayMode quando o projeto Unity e a licença do CI
  estiverem configurados.

Os checks atualmente definidos são:

- `Repository validation`.
- `Markdown and YAML lint`.
- `C# format`.
- `EditMode and PlayMode tests`.

Falha em um check deve ser corrigida na branch do PR. Não se deve desabilitar uma
regra de CI apenas para conseguir fazer merge sem analisar a causa.

## 15. Validação local do repositório

O CI executa:

```bash
bash .github/scripts/validate-repository.sh
```

Desenvolvedores em Linux, macOS ou Git Bash podem executar a mesma validação
antes do push.

Ela verifica principalmente:

- diretórios gerados pela Unity;
- marcadores de conflitos Git;
- possíveis credenciais;
- arquivos maiores que 50 MB fora do Git LFS.

## 16. Organização das pastas

A estrutura de código e conteúdo do jogo deve permanecer dentro de
`Assets/_Game`.

Estrutura principal:

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

## 17. Onde colocar cada arquivo

### Scripts de personagem

```text
Assets/_Game/Scripts/Player/
```

Exemplos:

```text
PlayerController.cs
PlayerMovement.cs
PlayerInteraction.cs
PlayerVisual.cs
```

### Sistema de Bem-Estar

```text
Assets/_Game/Scripts/WellBeing/
```

Exemplos:

```text
PlayerWellBeing.cs
WellBeingState.cs
FatigueController.cs
```

### Coletáveis

```text
Assets/_Game/Scripts/Collectibles/
Assets/_Game/Prefabs/Collectibles/
Assets/_Game/ScriptableObjects/Food/
```

### Obstáculos

```text
Assets/_Game/Scripts/Obstacles/
Assets/_Game/Prefabs/Obstacles/
```

### Puzzles

```text
Assets/_Game/Scripts/Puzzles/
Assets/_Game/Prefabs/Puzzles/
```

### UI

```text
Assets/_Game/Scripts/UI/
Assets/_Game/Art/UI/
```

### Áudio

```text
Assets/_Game/Scripts/Audio/
Assets/_Game/Audio/Music/
Assets/_Game/Audio/SFX/
```

### Testes

```text
Assets/_Game/Tests/EditMode/
Assets/_Game/Tests/PlayMode/
```

## 18. Boas práticas para scripts C#

### Um conceito principal por arquivo

Prefira:

```text
PlayerMovement.cs
PlayerWellBeing.cs
PlayerScore.cs
```

Evite um arquivo genérico contendo dezenas de classes sem relação clara.

### Nome do arquivo igual ao tipo principal

Se a classe é:

```csharp
public class PlayerMovement : MonoBehaviour
{
}
```

O arquivo deve ser:

```text
PlayerMovement.cs
```

### Use namespaces

Exemplo:

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

### Prefira componentes pequenos

Evite criar um `PlayerManager` responsável simultaneamente por:

- movimento;
- input;
- Bem-Estar;
- pontuação;
- animação;
- áudio;
- interação.

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

### Evite singletons sem necessidade

Um singleton global só deve existir quando houver um motivo arquitetural claro.
Não use singleton como solução padrão para acesso fácil aos objetos.

### Dados configuráveis devem ficar fora do código quando possível

Valores como:

- pontos de alimentos;
- modificadores de Bem-Estar;
- velocidade base;
- força de pulo;
- duração de fadiga;

podem ser armazenados em ScriptableObjects quando fizer sentido.

Isso reduz números mágicos e facilita balanceamento.

## 19. Convenções de nomenclatura C#

Classes, métodos públicos, propriedades e enums:

```text
PascalCase
```

Exemplos:

```csharp
public class PlayerMovement
public float CurrentSpeed { get; private set; }
public void ApplyFatigue()
```

Variáveis locais e parâmetros:

```text
camelCase
```

Campos privados:

```text
_camelCase
```

Exemplo:

```csharp
private float _moveSpeed;
```

Booleanos devem expressar uma condição:

```text
isGrounded
isFatigued
canMove
hasFinished
```

Evite nomes como:

```text
flag
value
data2
temp1
thing
```

## 20. Componentes e Inspector

Quando um campo precisa ser configurado no Inspector, prefira campo privado
serializado:

```csharp
[SerializeField] private float _moveSpeed = 5f;
```

Evite tornar todos os campos públicos apenas para aparecerem no Inspector.

Use validações quando uma configuração inválida puder causar erro em runtime.

Exemplo:

```csharp
private void Awake()
{
    if (_rigidbody == null)
    {
        Debug.LogError("Rigidbody2D is required.", this);
    }
}
```

## 21. Arquivos `.meta` da Unity

Arquivos `.meta` devem ser versionados.

Eles armazenam GUIDs utilizados pela Unity para relacionar assets, prefabs,
scripts, materiais e cenas.

Nunca adicione `*.meta` ao `.gitignore`.

Ao mover ou renomear assets, prefira fazer a operação dentro do Unity Editor.
Isso reduz o risco de perder referências.

## 22. Arquivos que não devem ser commitados

O `.gitignore` do projeto exclui diretórios gerados como:

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

Também não devem ser commitados arquivos locais de IDE, artefatos de testes ou
segredos.

Se um arquivo ignorado já estiver sendo rastreado pelo Git, adicionar uma regra
ao `.gitignore` não é suficiente.

Remova-o apenas do índice:

```bash
git rm -r --cached caminho
```

Depois faça commit da remoção.

## 23. Git LFS

Arquivos binários e assets grandes são rastreados pelo Git LFS conforme
`.gitattributes`.

Exemplos configurados:

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

Ver padrões LFS:

```bash
git lfs track
```

Ver arquivos atualmente rastreados:

```bash
git lfs ls-files
```

Depois de clonar:

```bash
git lfs install
git lfs pull
```

Não remova manualmente as regras de LFS sem avaliar o impacto no histórico do
repositório.

## 24. Scenes e Prefabs

Evite que várias pessoas editem simultaneamente a mesma Scene grande.

Prefira:

- cenas pequenas;
- prefabs reutilizáveis;
- Prefab Variants quando apropriado;
- composição em vez de concentrar tudo em uma única Scene.

Antes de editar uma Scene crítica, comunique a equipe quando houver risco de
conflito.

Arquivos `.unity`, `.prefab`, `.asset`, `.mat`, `.anim` e `.controller` já estão
marcados para uso de `unityyamlmerge` quando essa ferramenta estiver configurada
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

Isso permite alterar balanceamento sem modificar a lógica do sistema.

Evite colocar estado mutável específico de uma partida em um asset persistente
sem uma estratégia explícita para clonagem ou reset.

## 26. Testes

Use EditMode para lógica que não depende da execução completa de uma Scene.

Exemplos:

- cálculo de pontuação;
- transições de Bem-Estar;
- regras de fadiga;
- validação de dados.

Use PlayMode para comportamentos que dependem da execução do jogo.

Exemplos:

- movimento do personagem;
- colisões;
- interação com coletáveis;
- ativação de puzzles;
- fluxo de conclusão da fase.

Ao corrigir um bug importante, sempre que possível crie primeiro um teste que
reproduza o problema.

## 27. Segredos e credenciais

Nunca envie para o Git:

- senhas;
- tokens;
- API keys;
- licenças;
- credenciais de serviços externos.

Use GitHub Secrets para CI/CD.

No repositório:

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

Arquivos `.env` são ignorados pelo Git. Caso seja necessário documentar
variáveis, use `.env.example` sem valores sensíveis.

## 28. Revertendo erros com segurança

### Corrigir o último commit sem alterar seu conteúdo

```bash
git commit --amend
```

Se o commit já foi compartilhado, evite reescrever histórico sem necessidade.

### Reverter um commit compartilhado

```bash
git revert <commit-sha>
```

`git revert` cria um novo commit que desfaz o anterior e é normalmente a opção
mais segura para branches compartilhadas.

### Recuperar alterações guardadas temporariamente

Guardar:

```bash
git stash push -m "work in progress"
```

Listar:

```bash
git stash list
```

Aplicar e remover do stash:

```bash
git stash pop
```

Não use stash como armazenamento permanente de trabalho.

## 29. Releases

O projeto utiliza Semantic Versioning.

Exemplos:

```text
v0.1.0
v0.2.0
v0.2.1
v1.0.0
```

Fluxo recomendado:

```text
develop
  └── release/0.1.0
        ├── validação final
        ├── Pull Request para main
        └── tag v0.1.0
```

Criar uma tag local:

```bash
git tag -a v0.1.0 -m "Healthy Race v0.1.0"
```

Enviar a tag:

```bash
git push origin v0.1.0
```

Tags `vX.Y.Z` podem acionar o workflow de build configurado no projeto.

## 30. Checklist antes do push

Antes de executar `git push`, confirme:

- `git status` contém apenas alterações esperadas;
- não existem arquivos `Library`, `Temp`, `Build` ou similares;
- nenhum segredo foi adicionado;
- código compila;
- testes relacionados passam;
- novos arquivos possuem nomes e pastas corretos;
- arquivos `.meta` necessários estão presentes;
- assets grandes estão sob Git LFS;
- a branch possui nome adequado;
- commits possuem mensagens compreensíveis.

## 31. Checklist antes do Pull Request

Antes de abrir o PR:

1. Execute `git fetch origin`.
2. Faça `git rebase origin/develop` quando a base for `develop`.
3. Resolva conflitos.
4. Rode os testes locais aplicáveis.
5. Execute a validação do repositório.
6. Revise `git diff origin/develop...HEAD`.
7. Faça push da versão final da branch.
8. Abra o Pull Request.
9. Aguarde os checks do CI.
10. Corrija qualquer falha antes de solicitar merge.

Comando útil para revisar tudo que será enviado:

```bash
git diff origin/develop...HEAD
```

## 32. Fluxo completo de exemplo

Nova funcionalidade de movimento:

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

Depois da aprovação e dos checks verdes, realize `Squash and merge`.

## 33. Princípio geral

Antes de adicionar uma nova pasta, Manager global, dependência, singleton,
pipeline ou padrão arquitetural, responda:

1. Qual problema concreto isso resolve?
2. Já existe uma solução no projeto?
3. Essa decisão torna o código mais fácil ou mais difícil de testar?
4. Outro desenvolvedor saberá onde encontrar essa implementação?
5. Essa decisão aumenta acoplamento desnecessariamente?

A estrutura do repositório deve continuar simples, previsível e orientada aos
domínios do gameplay.