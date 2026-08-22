# Estrutura do projeto Unity

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

Não use `Resources` por padrão. Prefira referências serializadas e, se o projeto realmente precisar de carregamento dinâmico em escala, avalie Addressables posteriormente.
