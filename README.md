# NISMA — NetBioDyn Instructions for Simulation and Model Authoring

**Rôle** : outil de simulation multi-agents (enseignement) compris entre
`NetBioDyn_Simulators` et `centyllion.com`. NISMA capture le modèle objet des
simulations NetBioDyn, sérialise les fichiers JSON, puis fournit un langage de
script `NISMA` pour générer des simulations plus puissantes que l'interface graphique.

**État** : projet développé et fonctionnel (WIP). Code C# compilable, sérialisation
JSON/opérationnelle, tests passés. Nouveau : développement complet par l'assistant.

**Langage** : `LANGAGE.md` (spécification), `GUIDE.md` (mode d'emploi du modélisateur),
`NISMA_Lang/` (prototype du compilateur : script `.ni` → `.nbd2`) et `NISMA_Ide/`
(atelier web avec coloration syntaxique). Lancer l'atelier :
`dotnet run --project NISMA_Ide` puis <http://127.0.0.1:5080>.

---

## 1. Architecture du projet

```
NISMA/
├── log.pdf                      # Journal de développement
├── modelisation.md              # Sémantique du format JSON
├── questions.txt                # Questions de conception
├── XML/
│   └── models.xml               # Schéma XSD (brouillon)
├── models/                      # Fichiers de test et exemples
│   ├── nisma_format.json        # Format NISMA (format de test)
│   ├── sample_test.json         # Modèle de test généré
│   ├── all.json                 # Exemple NetBioDyn (format d'origine)
│   └── ... (.netbiodyn, .json)
├── NISMA_Core/                  # Projet C# console (.NET 8.0)
│   ├── .gitignore
│   ├── Behaviour.cs
│   ├── Field.cs
│   ├── Grain.cs
│   ├── JsonUtils.cs
│   ├── Program.cs
│   ├── nbd.csproj
│   ├── sub classes/
│   │   ├── Database.cs          # Icônes FontAwesome, couleurs CSS, directions
│   │   ├── FieldTools.cs        # Productions/influences/perméabilités
│   │   └── GrainModel.cs        # Modèle de simulation complet
│   └── UML/
│       ├── model.pu
│       └── model uml.png
└── .git/
```

---

## 2. Développement — ce qui a été fait

### 2.1 `Grain.cs` — corrections et améliorations

- **Correction syntaxe** : `[XmlElement\"invisible\"]` → `[XmlElement(\"invisible\")]`
- **Types corrigés** : `uint Id` → `int Id` (centyllion utilise `Int`)
- **Suppression de `colorC`** : passage à un `string Color` direct (plus simple)
- **Ajout de propriétés manquantes** :
  - `AllowedDirections` : `List<string>` (par défaut `Left, Up, Right, Down`)
  - `FieldProductions`, `FieldInfluences`, `FieldPermeable` :
    `Dictionary<int, float>` (champ→valeur)
- **Types corrigés** : `halfLife` et `movementProbability` passés de `uint`/`float`
  à `int`/`double` pour matching avec centyllion
- **Constructeurs** : ajout de constructeurs complets (id, name, color, icon,
  movementProbability)
- **Sérialisation JSON** : `[JsonPropertyName]` ajouté à toutes les propriétés

### 2.2 `Behaviour.cs` — complétion majeure

- **Correction** : `[XmlRoot("grains")]` → `[XmlRoot("behaviours")]`
- **Ajout de propriétés manquantes** :
  - `AgePredicate` : `Predicate` (opérateur + constante)
  - `FieldPredicates` : `List<FieldPredicate>` (champId → prédicat)
  - `Reaction` : `List<Reaction>` (réaction d'un comportement)
  - `FieldInfluences` : `Dictionary<int, float>`
- **Types corrigés** : `probability` passé de `float` à `double`
- **Classes associées** :
  - `Predicate` : `Op` (Operator enum) + `Constant` (double)
  - `FieldPredicate` : `FieldId` + `Predicate`
  - `Reaction` : `ReactiveId`, `ProductId`, `SourceReactive`, `AllowedDirections`
  - `Operator` enum : `Equals, NotEquals, LessThan, LessThanOrEquals, GreaterThan, GreaterThanOrEquals`
    (avec `[JsonStringEnumConverter]` pour la sérialisation)

### 2.3 `Field.cs` — corrections

- `halfLife` : `uint` → `int`
- Ajout de `Invisible` et `Formula` propriétés
- Ajout de constructeurs avec `speed`, `halfLife`

### 2.4 `GrainModel.cs` — nouveau

- Classe `GrainModel` dans `sub classes/` :
  - `Name`, `Description`, `Grains`, `Behaviours`, `Fields`
  - Méthodes utilitaires : `GrainForId`, `FieldForId`, `HasField`
  - `[JsonPropertyName]` pour le matching JSON

### 2.5 `JsonUtils.cs` — amélioration

- `Load<T>` et `Save<T>` pour la sérialisation JSON générique
- Utilise `System.Text.Json` avec `[JsonPropertyName]` pour le mapping

### 2.6 `Program.cs` — tests fonctionnels

- Test 1 : création d'un grain complet
- Test 2 : création d'un comportement avec `agePredicate`, `fieldPredicates`, `reaction`
- Test 3 : création d'un champ avec `formula`
- Test 4 : sérialisation/désérialisation JSON
- Test 5 : chargement d'un modèle (`models/nisma_format.json`)
- Test 6 : sauvegarde d'un modèle (`models/sample_test.json`)
- Test 7 : méta-modèle avec héritage et comportements génériques (cf. 6.4)

### 2.7 `nbd.csproj` — correction

- Suppression de l'ItemGroup dupliqué (`Compile Include`)
- Build OK, avertissements uniquement (SYSLIB1224 sur les attributs JsonSerializable)

### 2.8 `FieldTools.cs` — correction

- Types `uint` → `int` pour correspondre au modèle centyllion

### 2.9 Sauvegarde : conditions aux limites et prédicats de count (2026-11-12)

Alignement de NISMA sur les changements de sauvegarde de CentyllionExtend
(conditions aux limites par grain, champ torique/imperméable, prédicats de
count sur les voisins) :

- `Grain.cs` : nouvelle classe `BoundaryConditions` (`left/right/top/bottom`,
  défaut `true`, `IsToric`) + propriété nullable `Grain.BoundaryConditions`
  + aide `Grain.EffectiveBoundaryConditions`.
- `Field.cs` : propriété `Toric` (nullable, défaut torique) + aide `IsToric`.
- `Behaviour.cs` : nouvelle classe `AgentCountPredicate` (`grainId` défaut -1,
  `op`, `count`) + propriété nullable `Behaviour.AgentCountPredicates`.
- `JsonUtils.cs` (`SaveNbd`) : nettoyage — `boundaryConditions` supprimé si
  toutes les parois sont `true`, `toric` supprimé si `true`,
  `agentCountPredicates` supprimé si vide.

Contrat d'écriture (identique à CentyllionExtend) : `boundaryConditions`
omis si torique partout, sinon les 4 parois explicites ; `toric` écrit
seulement si `false` ; `agentCountPredicates` omis si vide, chaque prédicat
écrit toujours `grainId`/`op`/`count`. Les fichiers legacy à
`boundaryConditions` partiel restent lisibles (parois absentes = `true`).

Vérification : build de la solution NISMA (0 erreur, 0 avertissement) + test
console dédié (16/16 assertions : écriture, nettoyage, round-trip, lecture
legacy). Documentation : README §4.1ter et PROJECT_DOCUMENTATION §2.4.

---

## 3. Modèle objet

### 3.1 `Grain` — entité active

| Propriété | Type | Obligatoire | Plage |
|---|---|---|---|
| `id` | `int` | oui | >= 0 |
| `name` | `string` | oui | — |
| `color` | `string` (CSS) | oui | 145+ couleurs |
| `icon` | `string` | oui | FontAwesome 5.0.9 |
| `invisible` | `bool` | non | booléen |
| `size` | `double` | non | [1, 5] |
| `description` | `string` | non | texte libre |
| `halfLife` | `int` | non | >= 0 |
| `movementProbability` | `double` | non | [0, 1] |
| `allowedDirections` | `List<string>` | non | 8 directions |
| `fieldProductions` | `Dictionary<int,float>` | non | champ→valeur |
| `fieldInfluences` | `Dictionary<int,float>` | non | champ→valeur |
| `fieldPermeable` | `Dictionary<int,float>` | non | champ→valeur |
| `boundaryConditions` | `BoundaryConditions` | non | `{left,right,top,bottom}` (bool, défaut `true` = torique) ; omis si toutes les parois sont ouvertes |

### 3.2 `Behaviour` — règle de réaction

| Propriété | Type | Obligatoire | Plage |
|---|---|---|---|
| `name` | `string` | oui | — |
| `description` | `string` | non | — |
| `probability` | `double` | non | [0, 1] (par défaut 1) |
| `agePredicate` | `Predicate` | non | `{op, constant}` (conservé pour rétrocompatibilité) |
| `agePredicates` | `List<Predicate>` | non | lignes supplémentaires « âge de la simulation » ; omis si vide ; toutes ET entre elles et avec `agePredicate` |
| `formula` | `string` | non | formule de condition, ex. `count(0)>=3 && count(0)<5` |
| `fieldPredicates` | `List<FieldPredicate>` | non | `champId → prédicat` |
| `mainReactiveId` | `int` | oui | >= 0 |
| `mainProductId` | `int` | non | >= 0 |
| `sourceReactive` | `int` | non | >= -1 |
| `fieldInfluences` | `Dictionary<int,float>` | non | champ→valeur |
| `reaction` | `List<Reaction>` | non | liste de réactions |
| `agentCountPredicates` | `List<AgentCountPredicate>` | non | omis si vide ; chaque prédicat `{grainId, op, count}` est toujours écrit en entier (cf. §4.1ter) |

### 3.3 `Field` — structure d'espace

| Propriété | Type | Obligatoire | Plage |
|---|---|---|---|
| `id` | `int` | non | >= 0 |
| `name` | `string` | oui | — |
| `color` | `string` (CSS) | oui | — |
| `invisible` | `bool` | non | booléen |
| `allowedDirections` | `List<string>` | non | 8 directions |
| `speed` | `float` | non | [0, 1] (par défaut 0.8) |
| `halfLife` | `int` | non | >= 0 (par défaut 10) |
| `toric` | `bool` | non | défaut `true` (torique) ; `false` = bordure imperméable ; omis si `true` |
| `formula` | `string` | non | formule (évaluée dynamiquement) |

### 3.4 `GrainModel` — modèle de simulation

```json
{
  "name": "Modèle de test",
  "description": "Description",
  "grains": [ /* Grain[] */ ],
  "behaviours": [ /* Behaviour[] */ ],
  "fields": [ /* Field[] */ ]
}
```

---

## 4. Sérialisation

### 4.1 JSON — `JsonUtils<T>`

```csharp
public class JsonUtils<T>
{
    public T Load(string path);   // Lecture + désérialisation JSON
    public void Save(string path, object obj); // Écriture + sérialisation JSON
}
```

Utilise `System.Text.Json` avec `[JsonPropertyName]` pour le mapping.

### 4.1bis Champ `updatePlan`

Le modèle porte un plan de mise à jour optionnel, stocké dans `model` après `description`
et avant `grains` :

| Valeur | Signification | Sérialisation |
|---|---|---|
| `"Parallel"` (défaut) | Calculs depuis l'état initial du pas | champ absent |
| `"Sequential"` | Applications immédiates, ordre aléatoire | `"updatePlan": "Sequential"` |

- Classe : `Model.UpdatePlan` (`NISMA_Core.Model`), constantes `Model.ParallelUpdatePlan`
  et `Model.SequentialUpdatePlan`, propriété `Model.IsSequential`.
- `JsonUtils.SaveNbd` retire le champ si sa valeur n'est pas `"Sequential"`.

### 4.1ter Contrat de sérialisation des champs de bordure et de count (2026-11-12)

Alignement sur le contrat d'écriture de CentyllionExtend (Kotlin
`@EncodeDefault(ALWAYS)` sur les champs correspondants). Trois nouveaux
membres :

| JSON | Classe C# | Règle d'écriture | Règle de lecture |
|---|---|---|---|
| `grains[].boundaryConditions` | `Grain.BoundaryConditions` (nullable) + classe `BoundaryConditions` (`Left/Right/Top/Bottom`, défaut `true`, `IsToric`) | omis si toutes les parois sont ouvertes ; sinon **les 4 parois sont toujours écrites** | parois absentes = `true` (les fichiers legacy partiels ex. `{"left":false}` restent lisibles) ; `Grain.EffectiveBoundaryConditions` renvoie le défaut torique si nul |
| `fields[].toric` | `Field.Toric` (nullable) + `Field.IsToric` | omis si `true` ; écrit `false` seulement si imperméable | absent = torique |
| `behaviours[].agentCountPredicates` | `Behaviour.AgentCountPredicates` (nullable) + classe `AgentCountPredicate` (`GrainId` défaut -1, `Op` défaut `"GreaterThanOrEquals"`, `Count`) | omis si liste vide ; chaque prédicat présent **écrit toujours** `grainId`, `op` et `count` | liste absente = null |

`grainId < 0` sur un prédicat de count = « tout agent voisin occupé »
(8 directions, bornes 0-8).

`JsonUtils.SaveNbd` applique le nettoyage correspondant : suppression de
`boundaryConditions` si toutes les parois sont `true`, suppression de
`toric` si `true`, suppression de `agentCountPredicates` si vide.

Vérifié par test console (16 assertions) : écriture du nouveau format,
nettoyage, round-trip de (dé)sérialisation et lecture d'un fichier
legacy CentyllionExtend à `boundaryConditions` partiel.

### 4.2 XML — `XmlSerializer`

Les classes portent `[Serializable]` et `[XmlRoot(...)]` pour la compatibilité XML.

---

## 5. Exemple de simulation

Fichier `models/nisma_format.json` :

```json
{
  "name": "Fourmis",
  "description": "Agent bleus et rouges",
  "updatePlan": "Sequential",
  "grains": [
    {
      "id": 0,
      "name": "Bleu",
      "color": "Blue",
      "icon": "square-full",
      "halfLife": 0,
      "movementProbability": 1,
      "allowedDirections": ["Right", "RightUp", "RightDown"]
    }
  ],
  "behaviours": [
    {
      "name": "Bouge",
      "probability": 1,
      "agePredicate": { "op": "GreaterThanOrEquals", "constant": 50 },
      "mainReactiveId": 0,
      "mainProductId": 0,
      "sourceReactive": -1,
      "reaction": [{
        "reactiveId": 0,
        "productId": 0,
        "sourceReactive": -1,
        "allowedDirections": ["Left", "Up", "Right", "Down"]
      }]
    }
  ],
  "fields": [
    {
      "id": 0,
      "name": "F-Bleu",
      "color": "Blue",
      "speed": 0.8,
      "halfLife": 10,
      "formula": "(1+sin(step/25))/2"
    }
  ]
}
```

---

## 6. Construction et test

### 6.1 Prérequis

- .NET 8.0 SDK (installé à `/workspace/dotnet`)
- Variables d'environnement :
  - `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`
  - `PATH=/workspace/dotnet:$PATH`

### 6.2 Commandes

```bash
# Construire
dotnet build NISMA_Core/nbd.csproj

# Exécuter
dotnet run --project NISMA_Core/nbd.csproj
```

### 6.3 Résultats des tests

```text
=== Test 1 : Grain complet ===
Grain créé : { "id": 0, "name": "Agent", "color": "DarkGoldenRod", ... }

=== Test 2 : Comportement complet ===
Comportement créé : { "name": "Cristallisation", "agePredicate": { "op": "GreaterThanOrEquals", "constant": 50 }, ... }

=== Test 3 : Champ ===
Champ créé : { "id": 0, "name": "Champ", "speed": 0.8, "halfLife": 10, "formula": "(1+sin(step/25))/2" }

=== Test 4 : Sérialisation JSON ===
Grain en JSON : { "id": 0, "name": "Agent", ... }

=== Test 5 : Chargement d'un modèle ===
Chargement réussi : 2 grains, 1 comportements, 1 champs
  Grain : Agent (id=0), Grain : Agent 1 (id=1)
  Comportement : Cristallisation (mainReactiveId=0)
  Champ : Champ (id=0, speed=0.8)

=== Test 6 : Modèle complet sauvegardé ===
Modèle sauvegardé dans models/sample_test.json
```

### 6.4 Test 7 : Méta-modèle avec héritage et comportements génériques

**Description** :
Mapper la relation d'héritage entre types d'agents et générer
automatiquement toutes les combinaisons de comportements concrets.

**Exemple** :
- Types abstraits : `lymphocyte` (hérite de rien), `CelluleCible` (hérite de rien)
- Sous-types concrets :
  - `LA`, `LB` → héritiers de `lymphocyte`
  - `A`, `B`, `C` → héritiers de `CelluleCible`
- Comportement générique : `Lymphocyte-Cible` s'applique à la catégorie `["lymphocyte", "CelluleCible"]`

**Résultat** :
- **5 grains concrets générés** : LA (id=0), LB (id=1), A (id=2), B (id=3), C (id=4)
- **6 comportements générés** :
  - LA-A, LA-B, LA-C
  - LB-A, LB-B, LB-C

Chaque comportement généré est une réaction entre le grain actif (LA/LB) et le
grain cible (A/B/C), avec les IDs de réactions et de produits automatiquement assignés.

---

## 7. Schéma XSD (XML/models.xml)

Le fichier `XML/models.xml` est un schéma XSD partiel pour la validation XML des
descripteurs de simulation. Il est encore en brouillon et doit être complété.

### 7.1 Structure actuelle

```xml
<schema xmlns="http://www.w3.org/2001/XMLSchema"
        targetNamespace="http://www.univ-grenoble-alpes.fr/somethingsomething">

  <element name="models" type="nbd:Model"/>
  <complexType name="Model">
    <sequence>
      <element name="grains" type="nbd:Grains" minOccurs="0" maxOccurs="unbounded"/>
      <element name="behaviours" type="nbd:Behaviours" minOccurs="0" maxOccurs="unbounded"/>
      <element name="fields" type="nbd:Fields" minOccurs="0" maxOccurs="unbounded"/>
    </sequence>
  </complexType>
  <!-- ... Types : Grains, Behaviours, Fields, Pairs, react, Formulae ... -->
</schema>
```

### 7.2 Types

- `Grains` : `id`, `name`, `color`, `icon`, `invisible`, `size`, `description`,
  `halfLife`, `movementProbability`, `allowedDirections`, `fieldProductions`,
  `fieldInfluence`, `fieldPermeable`.
- `Behaviours` : `name`, `probability`, `agePredicate`, `mainReactiveId`,
  `mainProductId`, `sourceReactive`, `reaction`, `fieldInfluences`.
- `Fields` : `id`, `name`, `color`, `allowedDirections`, `speed`, `halfLife`,
  `formula`.
- `Pairs` : `id`, `value`.
- `react` : `op`, `constant`.
- `Formulae` : TO DO.

---

## 8. Historique git

```
* 971213e  réorganisation et edits de grains et fieldTools
* 28e352d  test
* a6af342  initial shenanigans
* ddd88f2  minor correction
* 4ddb30e  xsd rough draft, NOT sure of choices but made them anyway
* 7bc361b  mental gymnastics
* dda7e6c  Delete Program.cs
* 157b560  Delete global.json
* 2dd6887  Delete test.csproj
* 0cc2af1  Delete test.sln
* 22e637b  this is better
* cd511b1  initial commit
```

---

## 9. Roadmap

| Tâche | Statut |
|---|---|
| Modèle objet Grain/Behaviour/Field | ✅ Terminé |
| Sérialisation JSON (JsonUtils<T>) | ✅ Terminée et testée |
| Sérialisation XML | ⚠️ Partielle (XSD en brouillon) |
| Méta-modèle avec héritage + comportements génériques | ✅ Terminé (Test 7) |
| Langage NISMA (scripting) | ⏳ À venir |
| Validation / tests | ✅ Terminée (tests passés) |
| Documentation | ✅ Terminée |

---

*Auteur : étudiante (originelle) + développement par l'assistant.*
*Dernière mise à jour : 2026-11-12 — conditions aux limites (`boundaryConditions`, `toric`) et prédicats de count (`agentCountPredicates`) dans le modèle et la sauvegarde.*
