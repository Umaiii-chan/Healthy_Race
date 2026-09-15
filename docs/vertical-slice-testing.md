# Teste do Vertical Slice Core (FIX-VERTICAL-SLICE)

## Descrição

A cena `VerticalSlice_Core.unity` integra os sistemas fundamentais de Healthy Race (jogadores P1 e P2, itens coletáveis de comida, caixas empurráveis, HUD e câmera compartilhada). Este documento registra a estrutura da cena, a correção aplicada nas referências de serialização e os procedimentos de validação automatizada e manual.

## Causa Raiz e Correção de Serialização

### Causa Raiz

Os prefabs `PF_FoodCollectible.prefab` e `PF_PushableBox.prefab` foram originalmente serializados com o identificador reservado `fileID: 100100000` atribuído diretamente aos seus objetos `GameObject`. No formato YAML da Unity, `100100000` é reservado para a raiz de instâncias de prefab (`PrefabInstance` / `Prefab`), fazendo com que o pipeline de importação rejeitasse os assets com a mensagem `Prefab file is corrupt: it contains an object using fileID 100100000`.

Adicionalmente, na cena `VerticalSlice_Core.unity`, os campos `_collectiblePrefab`, `_playerPrefab` e `_pushableBoxPrefab` do componente `VerticalSliceBootstrap` apontavam para o `fileID: 100100000`, provocando erros de incompatibilidade de tipo (`Serialized reference type mismatch`) e falha ao resolver as instâncias dos prefabs em tempo de execução.

### Correção Aplicada

1. **Reserialização dos Prefabs**:
   - `PF_FoodCollectible.prefab` e `PF_PushableBox.prefab` foram reserializados através das APIs oficiais do Unity Editor (`PrefabUtility.SaveAsPrefabAsset`), gerando fileIDs 64-bit legítimos e materiais compatíveis com o URP 2D.
2. **Reatribuição na Cena**:
   - As propriedades de `VerticalSliceBootstrap` em `VerticalSlice_Core.unity` foram reatribuídas diretamente aos prefabs e assets de dados oficiais (`PF_Player.prefab`, `PF_FoodCollectible.prefab`, `PF_PushableBox.prefab`, `SO_Food_Apple.asset`, `SO_Food_Burger.asset`).
3. **Validação de Isolamento de Testes**:
   - Limpeza e isolamento de cena implementados em `VerticalSlicePlayModeTests` para descarregar a cena após a execução dos testes em PlayMode.

## Roteiro de Validação Manual

1. Abra o projeto no Unity Editor (versão 6000.3.23f1 LTS).
2. Abra a cena `Assets/_Game/Scenes/VerticalSlice_Core.unity`.
3. Verifique o console do Unity: nenhum erro de prefab corrompido ou referência nula deve ser emitido.
4. No Inspector do GameObject `VerticalSlice_Core`, confirme que o componente `VerticalSliceBootstrap` possui todas as referências preenchidas:
   - `_collectiblePrefab`: `PF_FoodCollectible`
   - `_playerPrefab`: `PF_Player`
   - `_pushableBoxPrefab`: `PF_PushableBox`
   - `_appleData`: `SO_Food_Apple`
   - `_burgerData`: `SO_Food_Burger`
5. Pressione Play:
   - Os personagens P1 e P2 devem surgir nas posições iniciais.
   - A câmera compartilhada enquadra ambos os personagens com suavização.
   - Os coletáveis de maçã e hambúrguer aparecem nos pontos configurados.
   - A caixa empurrável repousa sobre a geometria do cenário.
   - As interfaces de HUD de P1 e P2 aparecem nos cantos superiores da tela.

## Testes Automatizados

### EditMode (`VerticalSliceSceneTests.cs`)

- `ReusablePrefabs_LoadSuccessfullyAndAreValid`: valida o carregamento de `PF_Player.prefab`, `PF_FoodCollectible.prefab` e `PF_PushableBox.prefab` via `AssetDatabase` e a presença dos componentes essenciais (`FoodCollectible`, `PushableBox`).
- `VerticalSlice_SceneOpens_HasBootstrapWithValidReferencesAndNoMissingScripts`: abre a cena em modo de pré-visualização, inspeciona o componente `VerticalSliceBootstrap` via `SerializedObject` garantindo que nenhuma referência seja nula, e valida a ausência de scripts ausentes (`Missing Script`).

### PlayMode (`VerticalSlicePlayModeTests.cs`)

- `VerticalSlice_LoadsAndBootstrapsCoreElementsSuccessfully`: carrega assincronamente a cena em PlayMode e valida a instanciação e inicialização em runtime de P1, P2, caixas, coletáveis, câmera e HUDs.
