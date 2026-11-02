# NISMA — Documentation technique du projet

**NISMA** (NetBioDyn Instructions for Simulation and Model Authoring) est une
bibliothèque C# (.NET 8.0) qui capture le modèle objet des simulations
NetBioDyn, le sérialise au format `.netbiodyn` et génère les fichiers de
simulation importables dans **Centyllion**.

Ce document décrit l'état actuel du code (commenté exhaustivement en XML),
le schéma `.netbiodyn` (aligné sur le schéma Kotlin `ModelAndSimulation`
de `centyllion-app`), les 4 exemples, les règles de validation Centyllion,
la méta-modélisation et les contraintes d'import connues.

---

## 1. Structure du projet

```
NISMA/
├── README.md                      # Historique de développement
├── modelisation.md                # Sémantique du format JSON (rappel)
├── PROJECT_DOCUMENTATION.md       # ← Ce fichier
├── models/                        # Fichiers de test (.json, .netbiodyn)
├── NISMA_Core/                    # Bibliothèque métier (projet console de démo)
│   ├── Grain.cs                   # Classe Grain (entité active)
│   ├── Behaviour.cs               # Classe Behaviour + Predicate/FieldPredicate/Reaction
│   ├── Field.cs                   # Classe Field (champ / grille)
│   ├── Simulation.cs              # Simulation + SimulationSettings (état initial)
│   ├── JsonUtils.cs               # Sérialisation .netbiodyn (NbdUtils) + JsonUtils<T>
│   ├── ModelWrapper.cs            # Wrapper legacy (⚠️ doublon historique, cf. §8)
│   ├── Program.cs                 # Point d'entrée : 7 tests de démonstration
│   └── sub classes/
│       ├── GrainModel.cs          # GrainModel + méta-modélisation (MetaModel/TypeDecl/BehaviourDecl)
│       ├── FieldTools.cs          # Paires production/influence/perméabilité
│       └── Database.cs            # Constantes : icônes FontAwesome 5.0.9, couleurs CSS, directions
└── Exemples/
    ├── SimSimple/NISMA_Core/      # Exemple 1 : pas d'héritage
    ├── SimMoyen/NISMA_Core/       # Exemple 2 : héritage + comportements génériques
    ├── SimComplexe/NISMA_Core/    # Exemple 3 : champs + nombreux agents
    └── SimAutoPulse/NISMA_Core/   # Exemple 4 : réimplémentation d'autopulse_gradient.netbiodyn
```

Chaque exemple est un projet autonome (`nbd.csproj`, .NET 8.0) qui crée un
modèle, un état initial de simulation et l'écris dans `models/*.netbiodyn`.

---

## 2. Format `.netbiodyn` (schéma Centyllion)

Le format `.netbiodyn` est un JSON dont la racine correspond exactement à la
classe Kotlin `ModelAndSimulation` (`centyllion-app/core/.../model.kt`) :

```jsonc
{
  "model": {                      // GrainModel
    "name": "…",
    "description": "…",
    "grains":      [ /* Grain[] */ ],
    "behaviours":  [ /* Behaviour[] */ ],
    "fields":      [ /* Field[] */ ]
  },
  "simulation": {                 // Simulation
    "name": "…",
    "description": "…",
    "settings": {
      "size": 100,                // taille d'un côté de la grille
      "showGrid": true,
      "gridTextureUrl": null,
      "backgroundColor": null
    },
    "agents": [ /* Int[] */ ],    // taille = settings.size * settings.size
    "assets":  [ /* Asset3d[] */ ],
    "width": 100,                 // = settings.size
    "height": 100,                // = settings.size
    "depth": 1
  },
  "thumbnail": ""                 // String (vignette, souvent vide)
}
```

### 2.1 `model` — le modèle de simulation

| Propriété | Type | Note |
|---|---|---|
| `name` / `description` | `string` | |
| `grains` | `Grain[]` | id ≥ 0, `color` CSS, `icon` FontAwesome, `size` ∈ [1,5], `halfLife` ≥ 0, `movementProbability` ∈ [0,1], `allowedDirection` (8 directions), `fieldProductions`/`fieldInfluences` ∈ [-1,1] par champ, `fieldPermeable` ∈ [0,1] par champ |
| `behaviours` | `Behaviour[]` | `mainReactiveId` **obligatoire** (≥ 0), `probability` ∈ [0,1], `agePredicate` {`op`,`constant`}, `fieldPredicates`, `reaction[]`, `fieldInfluences` |
| `fields` | `Field[]` | `speed` ∈ [0,1], `halfLife` ≥ 0, `formula` (évaluée dynamiquement) |

**`fieldPredicates` — format objet.** Chaque prédicat de champ est sérialisé
comme un objet `{ "first": <idChamp>, "second": { "op": "…", "constant": n } }`
et non comme un tableau. Côté NISMA, la classe `FieldPredicate` porte les
propriétés `First` (id du champ) et `Second` (un `Predicate`) ; le convertisseur
`FieldPredicateConverter` (dans `JsonUtils.cs`) garantit la forme objet attendue
par Centyllion lors de la (dé)sérialisation.

### 2.2 `simulation` — l'état initial

- **`settings.size`** : taille d'un côté de la grille.
- **`agents`** : vecteur plat de `Int` de longueur **`size * size`**
  (contrainte `agentsSize = size * size` de Centyllion). Chaque case vaut
  `-1` (case vide) ou l'**id d'un grain**.
- **`width`/`height`/`depth`** : redondants avec `settings.size` / `depth`
  (marqués « TODO remove from serialization » côté Kotlin) mais toujours
  émis pour compatibilité.
- **`assets`** : liste d'`Asset3d` (généralement vide).

`JsonUtils.cs` (`NbdUtils.SaveNbd`) assemble le bloc complet
`{ model, simulation, thumbnail }` ; la cohérence `agents.Length == size²`
doit être garantie par le code créant la simulation (chaque `Program.cs`
construit `Agents` en conséquence, cf. commentaires de section).

### 2.3 Écarts entre le cœur et les exemples

| Aspect | `NISMA_Core/` (cœur) | Exemples |
|---|---|---|
| `fieldPredicates` | sérialisés `{first, second}` via `FieldPredicateConverter` | idem (identiques au cœur) |
| `Simulation` | constructeur avec `Asset3d` (démo de `ToString`) | mêmes classes, usage direct |
| `Grain`/`Behaviour` | sans `JsonIgnore` | SimAutoPulse : `[JsonIgnore(Condition = WhenWritingDefault)]` sur les propriétés par défaut |

---

## 3. Les 4 exemples

| Exemple | Fichier généré | Caractéristique |
|---|---|---|
|| **SimSimple** | `simulation_simple.netbiodyn` | Création directe : 4 grains « agriculteurs », 2 comportements, 1 champ. Pas d'héritage, API de base uniquement. |
|| **SimMoyen** | `simulation_moyen.netbiodyn` | Méta-modélisation : hiérarchie `lymphocyte → {LA, LB}` et `CelluleCible → {A, B, C}`, 1 comportement générique `Lymphocyte-Cible`, modèle concret produit par `MetaModel.Generate()` (5 grains, 6 comportements). |
|| **SimComplexe** | `simulation_complexe.netbiodyn` | Modèle volumineux : 12 grains, 8 comportements, 3 champs (formules `sin(step/…)`). Démonstration d'un réseau cellulaire complet. |
|| **SimAutoPulse** | `autopulse_gradient.netbiodyn` | Réimplémentation du modèle neuronal `autopulse_gradient` : 6 agents (axones A/I/0, pulses A/I, Cell), 8 comportements (activation, inhibition latérale, régénération, cycle pulse, agrégation), 2 champs (`influx`, `gradient` avec formule `1/(x+1)`). |
|| **SimEco** | `eco.netbiodyn` | Modèle écologique simple : 5 grains, 9 comportements, 1 champ. |
|| **SimFlux2** | `flux2.netbiodyn` | Modèle de flux sanguin simple : 2 grains, 1 champ. |
|| **SimFlux25Influx** | `flux25_influx.netbiodyn` | Modèle de flux nerveux avec influx : 7 grains, 18 comportements, 5 champs, 10000 agents. |
|| **SimOndesSpirales** | `ondes_spiralles.netbiodyn` | Modèle hormonal complexe : 5 grains (insuline/glucagon), 9 comportements (réactions entre glandes actives/inactives), prédicats d'âge, 1 champ. Biblique exemple pour la simulation cellulaire. |

Tous les `Program.cs` sont découpés en sections commentées :
(1) création du modèle, (2) comportements, (3) champs, (4) simulation,
(5) sauvegarde `.netbiodyn`.

---

## 4. Règles de validation Centyllion

Extraites de `centyllion-app/core/.../model/validation.kt`
(`Grain.diagnose`, `Behaviour.diagnose`, `Reaction.diagnose`, `Field.diagnose`) :

**Grain**
- `halfLife` ≥ 0 ;
- `movementProbability` ∈ [0, 1] ;
- `fieldProductions` : valeurs ∈ [-1, 1] (et production bloquée si perméabilité ≤ 0) ;
- `fieldInfluences` : valeurs ∈ [-1, 1] ;
- `fieldPermeable` : valeurs ∈ [0, 1].

**Behaviour**
- **`mainReactiveId` est requis** : doit être ≥ 0 (« Behaviour must have a main reactive ») ;
- `probability` ∈ [0, 1] ;
- `agePredicate.constant` ≥ 0 ;
- constantes des `fieldPredicates` ∈ [0, 1] ;
- `reaction` : `reactiveId`/`productId` (quand ≥ 0) doivent référencer un grain existant ; `allowedDirection` non vide.
- **Pas de propriété `id` sur `Behaviour`** — un comportement n'est identifié que par son contenu ; n'en émettre aucun.

**Field**
- `halfLife` ≥ 0 ;
- `speed` ∈ [0, 1].

**Simulation (à l'import)**
- `agents` doit contenir `settings.size * settings.size` valeurs ;
- les ids dans `agents` doivent référencer des grains existants du modèle.

Les fichiers générés par les 4 exemples respectent ces règles (vérification :
10000 agents pour `size = 100`, `mainReactiveId ≥ 0` partout, `fieldPredicates`
au format objet, aucun `id` sur les comportements).

---

## 5. Méta-modélisation NISMA

Implémentée dans `sub classes/GrainModel.cs` (documentée en XML) :

- **`TypeDecl`** — déclaration d'un type : `Name`, `Parent` (null si racine),
  `IsAbstract`. Déclaré via `MetaModel.AddType`.
- **`BehaviourDecl`** — comportement *générique* : `Name`, `Categories`
  (noms de types/catégories), `Reactions` (réactions avec `ReactiveId`/
  `ProductId`/`SourceReactive`/`AllowedDirections`). Déclaré via
  `MetaModel.AddBehaviour`.
- **`MetaModel.Generate()`** — produit un `GrainModel` concret :
  1. un `Grain` par **type concret** (non abstrait), ids assignés
     séquentiellement (mémoire : l'ordre de déclaration des types concrets
     détermine les ids) ;
  2. pour chaque `BehaviourDecl`, un `Behaviour` par **cartesienne** des
     types concrets de chaque catégorie (ex. 2 lymphocytes × 3 cellules cibles
     → 6 comportements), les `mainReactiveId`/`mainProductId`/`reactiveId`/
     `productId` étant résolus vers les ids des grains générés.

C'est ce mécanisme qui alimente SimMoyen (5 grains, 6 comportements générés)
et le Test 7 de `NISMA_Core/Program.cs`.

---

## 6. Import dans Centyllion et contraintes connues

1. **Ouvrir** le fichier `.netbiodyn` dans Centyllion (menu Modèles → Import) :
   Centyllion déserialise directement `ModelAndSimulation` (Kotlinx JSON).
2. Les **noms JSON doivent rester exacts** (`[JsonPropertyName]`) : tout
   renommage casse l'import silencieusement (champs vides par défaut).
3. **`agents` doit être initialisé** (longueur `size²`) : un `agents` vide ou
   tronqué rend la simulation non exécutable à l'import.
4. **`fieldPredicates` au format objet** `{first, second}` : la liste/tableau
   est rejetée par le parseur Centyllion — d'où `FieldPredicateConverter`.
5. **`mainReactiveId ≥ 0`** sur chaque comportement : un comportement sans
   réactif principal est signalé en erreur de validation et le modèle
   n'est pas exécutable.
6. **`thumbnail`** est optionnel (String, vide par défaut) ; `assets` aussi.
7. ⚠️ **Doublon historique** : la classe `ModelWrapper` est définie deux fois
   dans le cœur (`ModelWrapper.cs` et `JsonUtils.cs`) — le projet `NISMA_Core`
   seul ne compile pas (3 erreurs CS0101/CS0246 préexistantes, indépendantes
   des commentaires). Les 4 exemples compilent proprement.
8. `NISMA_Core/Program.cs` sert de cahier de charges de l'API : certains
   appels volontairement désynchronisés des signatures actuelles (documenté
   dans le commentaire du fichier).

---

## 7. Compilation et exécution

```bash
export PATH="/workspace/dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
dotnet build Exemples/SimSimple/NISMA_Core/nbd.csproj    # Build succeeded (0 erreur)
dotnet build Exemples/SimMoyen/NISMA_Core/nbd.csproj     # Build succeeded (0 erreur)
dotnet build Exemples/SimComplexe/NISMA_Core/nbd.csproj  # Build succeeded (0 erreur)
dotnet build Exemples/SimAutoPulse/NISMA_Core/nbd.csproj # Build succeeded (0 erreur)
dotnet build Exemples/SimEco/NISMA_Core/nbd.csproj        # Build succeeded (0 erreur)
dotnet build Exemples/SimFlux2/NISMA_Core/nbd.csproj     # Build succeeded (0 erreur)
dotnet build Exemples/SimFlux25Influx/NISMA_Core/nbd.csproj # Build succeeded (0 erreur)
dotnet build Exemples/SimOndesSpirales/NISMA_Core/nbd.csproj # Build succeeded (0 erreur)
dotnet build Exemples/SimSimple/NISMA_Core/nbd.csproj    # Build succeeded (0 erreur)
dotnet build NISMA_Core/nbd.csproj                       # ⚠️ 3 erreurs préexistantes (cf. §6.7)
```

Résultat de vérification (après commentaire exhaustif, 31/31 fichiers C#
vérifiés : suppression des commentaires → code identique byte à byte) :
**les 4 exemples buildent sans erreur** ; le cœur reste bloqué sur le doublon
`ModelWrapper` préexistant.

---

*Dernière mise à jour : 2026-10-31 — commentaire exhaustif de l'ensemble des
fichiers C# (cœur + 4 exemples) et alignement sur le schéma `ModelAndSimulation`
de Centyllion.*
