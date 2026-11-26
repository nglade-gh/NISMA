# NISMA — langage de description de simulations

Proposition de langage, à discuter avant toute implémentation.

## 1. Objectif et non-objectifs

**Objectif** : décrire une simulation complète — agents, champs, comportements, état
initial, réglages — dans un texte court et lisible, et produire un fichier `.nbd2`
valide pour NetBioDyn. Le langage apporte ce que NetBioDyn ne sait pas exprimer :
des **types abstraits** et des **tags**, qui donnent du polymorphisme aux
comportements.

**Non-objectifs** : exécuter la simulation (c'est NetBioDyn qui le fait), remplacer
les formules de NetBioDyn (elles restent la cible), permettre une expressivité
arbitraire (le langage est volontairement fermé).

**Contrainte de complétude** : tout modèle `.nbd2` existant doit pouvoir être exprimé
dans ce langage — pas de façon unique, plusieurs scripts pouvant produire le même
modèle, mais sans perte. C'est ce qui impose les deux niveaux décrits au §3bis : un
niveau naturel pour écrire, un niveau mécanique pour tout représenter.

## 2. Principe directeur

Trois règles, dont tout le reste découle :

1. **Vocabulaire fermé.** Une vingtaine de mots-clés, refus de tout le reste. Un
   langage « presque naturel » qui accepte des formulations libres devient ambigu
   sans prévenir.
2. **Déclaratif, pas impératif.** On décrit ce qui existe et ce qui se passe ; on
   n'ordonne pas des étapes. L'ordre d'exécution appartient à NetBioDyn.
3. **Rien n'est implicite.** Toute résolution incertaine (un produit qui pourrait
   être deux grains) est une **erreur**, jamais un choix par défaut.

## 3. Structure d'un fichier

Un fichier contient quatre sections, dans cet ordre. Chacune peut être vide sauf
la première.

```
declarations      agents, types abstraits, tags, champs, productions
comportements     ce qui se transforme, quand, avec quelle probabilité
etat initial      qui est où au premier pas
reglages          grille, plan de mise à jour, graine
```

## 3bis. Deux niveaux, une seule cible

La complétude et la lisibilité se contredisent si on les demande au même niveau. Le
langage a donc deux niveaux, et **le premier se compile dans le second** :

**Niveau naturel** (§4 à §8) — ce que le modélisateur écrit : `when`, `any`,
`becomes`, `has only`, tags. Il couvre les modèles courants et masque les identifiants,
les sentinelles et les noms de prédicats.

**Niveau mécanique** (§13) — un décalque du format `.nbd2`, un mot par clé JSON,
formules brutes comprises. Il n'est pas destiné à être écrit à la main, sauf pour un
cas marginal ; il garantit qu'**aucun modèle existant n'est inexprimable**.

Conséquence pratique : l'outil peut convertir un `.nbd2` en script mécanique. On part
toujours d'un modèle existant, jamais d'une page blanche, et la conversion est le
meilleur test de complétude du langage.

## 4. Déclarations

```
agent Cell colour lime
agent Virus colour crimson mobility 1.0
agent Debris colour grey

abstract Lymphocyte is a Cell
abstract Macrophage is a Cell

tag Active
tag Infected

agent ActiveLymphocyte is a Lymphocyte with tag Active
agent InfectedCell is a Cell with tag Infected

field IL2 diffusion 0.3 half-life 4 directions 8
field IFN diffusion 0.2 half-life 6 opacity 60

Cell produces IL2 1.0
Cell absorbs IL2 0.5
```

- `agent` : un grain **concret**, seul type qui apparaît dans le fichier produit.
  Attributs facultatifs : `colour`, `mobility`, `half-life`, `description`.
- `abstract` : un type qui n'est **jamais instancié** et n'apparaît pas dans le
  `.nbd2`. Il sert uniquement à regrouper des agents concrets.
- `tag` : une caractéristique énumérée, sans contenu. Un agent concret peut en
  porter plusieurs.
- Un agent est soit racine, soit sous-type d'**un seul** type abstrait. Pas
  d'héritage multiple : les conflits de résolution coûteraient plus qu'ils ne
  rapportent.
- `field` : `diffusion` (0–1), `half-life` (pas), `directions` (4 ou 8),
  `opacity`, `toric` (défaut vrai).
- `produces` / `absorbs` : production et influence d'un champ par un agent.

## 5. Comportements

Quatre formes, et quatre seulement.

### 5.1 Transformation

```
when a Cell has exactly 3 neighbours of type Cell
    a Cell appears

when a Cell has fewer than 2 or more than 3 neighbours of type Cell
    the Cell disappears

when IL2 at the cell is above 1.0
    any Lymphocyte becomes ActiveLymphocyte
```

- `a <Type> appears` : la cellule visée est vide, l'agent apparaît (génération
  spontanée).
- `the <agent> disappears` : l'agent meurt.
- `<A> becomes <B>` : transformation sur place ; `A` est le réactif principal,
  `B` le produit. `it` désigne l'agent principal si le nom est omis.

### 5.2 Réaction avec un voisin

```
when a Cell has at least 1 neighbour of type Virus
    a Virus neighbour becomes Debris
```

Une ligne de réaction : un voisin d'un type donné devient un autre type. Le
qualificatif est `a` (un voisin quelconque) ou `every` (tous les voisins
correspondants). L'âge du produit est hérité du voisin transformé, comme dans
NetBioDyn.

### 5.3 Conditions

Les conditions se cumulent avec `and`, s'alternent avec `or`, et se nient avec
`not`. Trois familles :

```
has exactly 3 neighbours of type Cell
has at least 2 neighbours of any type
has no neighbour of type Debris
has only neighbours of type Cell, Debris
IL2 at the cell is above 1.0
IL2 at a neighbour is below 0.2
age of the agent is above 10
age of the agent is a multiple of 5
step is 10
```

Les deux dernières formes de comptage (`has only`, `has exactly` avec plusieurs
types) s'appuient sur les prédicats `hasOnly` / `hasExactly` de NetBioDyn : c'est
précisément ce que le langage rend utilisable sans écrire de formule.

### 5.4 Actions

```
when a Virus has at least 3 neighbours of type Virus
    a Virus neighbour becomes Debris
    and set IL2 at the cell to 0
    and add 2 Virus at random in the square (10,10)-(20,20)
```

Actions disponibles : `set <field> at <where> to <value>`, `add <field> ... by`,
`add N <agent> at random in the square ...`, `clear <field> ...`. Les méta-actions
de NetBioDyn (`Pause`, `Capture`, `Select`…) sont accessibles par leur nom, en
minuscules : `pause`, `capture`, `select`.

### 5.5 Probabilité

```
when ... probability 0.3
```

Par défaut 1. Une probabilité s'applique **par comportement**, jamais par agent
touché.

## 6. Polymorphisme par les tags

C'est la raison d'être du langage.

```
abstract Cell
tag Active
agent RestingCell is a Cell
agent ActiveCell is a Cell with tag Active

when IL2 at the cell is above 1.0
    any Cell becomes Active

when a Cell has at least 3 neighbours of type Active
    a Cell neighbour becomes Infected
```

**`any Cell`** désigne n'importe quel agent concret de l'extension de `Cell` —
c'est-à-dire `RestingCell` et `ActiveCell`. Le comportement est **étendu** en un
comportement par agent concret.

**`becomes Active`** exige un produit unique. Deux cas :

- un seul agent concret porte le tag → il est choisi ;
- plusieurs agents le portent → **erreur**, avec la liste des candidats :

```
error line 12: "becomes Active" is ambiguous: ActiveCell and ActiveTcell both
carry the tag Active. Name the agent you mean, or give the tag to a single one.
```

### Règles d'expansion

1. Seuls le réactif principal et les réactifs de ligne sont polymorphes. Un
   **produit ne l'est jamais** : c'est la source de l'ambiguïté, donc une erreur.
2. Un comportement polymorphe produit autant de comportements concrets que
   l'extension le demande, et le compilateur **annonce le compte** :
   `behaviour at line 12 expands to 3 concrete behaviours`.
3. La probabilité est conservée telle quelle : une cellule ne portant qu'un grain,
   un seul des comportements étendus peut s'appliquer à elle par pas.
4. Les identifiants sont attribués **après expansion**, dans l'ordre de
   déclaration. Deux compilations du même fichier donnent le même `.nbd2`.
5. Les types abstraits et les tags ne sont **jamais** écrits dans le fichier.

## 7. État initial

```
start with 200 Cell at random
start with Cell at (5,5)
start with Virus from stencil "glider.rle" at (10,10)
```

Trois formes : densité aléatoire, position explicite, motif importé (RLE, `.cells`
ou matrice — les formats que NetBioDyn accepte déjà). Aucun autre placement n'est
inventé : s'il faut un motif complexe, on l'écrit dans un fichier de stencil.

## 8. Réglages

```
grid 100 x 100
update parallel
seed 42
```

`update` vaut `parallel` (défaut) ou `sequential`. `seed` rend la génération
reproductible ; sans elle, NISMA choisit une graine et l'écrit dans le fichier.

## 9. Compilation

```
1. Analyse      texte → arbre de syntaxe ; toute faute de vocabulaire est refusée ici
2. Résolution   noms → ensembles d'agents concrets (tags, types abstraits)
3. Expansion    comportements polymorphes → comportements plats
4. Émission     modèle NISMA → JSON .nbd2
5. Validation   règles de Centyllion sur le modèle étendu ; refus si non valide
```

Une erreur arrête la compilation. Aucun fichier n'est écrit tant que les cinq
phases n'ont pas réussi. Les messages portent le **numéro de ligne du script**,
pas celui du JSON.

## 10. Ce que le langage ne fera pas en v1

- Héritage de corps de comportements (un type abstrait qui porterait lui-même des
  comportements hérités par ses sous-types).
- Héritage multiple, ou agents appartenant à plusieurs types abstraits.
- Formules libres dans le **niveau naturel** : tout y passe par le vocabulaire. Le
  niveau mécanique (§13) accepte au contraire les formules brutes, sans quoi les
  modèles existants ne seraient pas exprimables.
- Fonctions, variables, boucles : un script décrit un modèle, pas un calcul.

## 11. Correspondance avec le fichier produit

| Script | `.nbd2` |
|---|---|
| `agent Cell` | `model.grains[]` |
| `abstract`, `tag` | rien — résolus à la compilation |
| `field IL2 diffusion 0.3` | `model.fields[]` (`speed` 0.3) |
| `Cell produces IL2 1.0` | `grains[].fieldProductions[0] = 1.0` |
| `when ... a Cell appears` | `behaviours[]` avec `mainReactiveId = -1` |
| `the Cell disappears` | `mainProductId = -1` |
| `A becomes B` | `mainReactiveId`/`mainProductId` |
| `a neighbour of type X becomes Y` | `reaction[]` |
| `has at least 3 neighbours` | `agentCountPredicates` ou `formula` |
| `has only neighbours of type ...` | `formula` : `hasOnly({...})` |
| `probability 0.3` | `behaviours[].probability` |
| `start with ...` | `simulation.agents[]` |
| `grid`, `update`, `seed` | `simulation.settings`, `model.updatePlan` |

## 12. Exemple complet

```
grid 100 x 100
update parallel
seed 42

agent Cell colour lime
agent Virus colour crimson mobility 1.0
agent Debris colour grey

abstract Immune is a Cell
tag Active

agent RestingImmune is an Immune
agent ActiveImmune is an Immune with tag Active

field IL2 diffusion 0.3 half-life 4 directions 8
Cell produces IL2 1.0

when a Cell has exactly 3 neighbours of type Cell
    a Cell appears

when a Cell has fewer than 2 or more than 3 neighbours of type Cell
    the Cell disappears

when IL2 at the cell is above 1.0
    any Immune becomes ActiveImmune

when a Cell has at least 1 neighbour of type Virus
    a Virus neighbour becomes Debris
    and set IL2 at the cell to 0

start with 500 Cell at random
start with 20 Virus at random
```

## 13. Niveau mécanique — la garantie de complétude

Un mot par clé du format, aucune élégance recherchée. Ce niveau existe pour une seule
raison : qu'aucun modèle existant ne soit inexprimable.

```
model "coeur" description "modèle de référence"
update sequential

agent 0 "C_a" colour lime icon square-full size 1 mobility 0
    produces 0 1.0
    influence 0 0.2
    permeable 0 0.2
    boundary left open right open top wall bottom wall

agent 1 "C_i" colour dodgerblue

field 0 "pa" colour orange speed 0.3 half-life 4 opacity 100 toric yes
    directions left up right down leftup leftdown rightup rightdown

behaviour 1 "Birth" enabled yes probability 1
    main -1 -> 0
    count 0 is 3
    reactions
        0 "C_a" -> 0 directions right left
    actions "SetField(0, 0, 0, 1)"

modifier 0 "Seed" enabled yes probability 1
    formula "step == 10"
    actions "AddRect(1, 50, 10, 10, 20, 20)"

grid 50 x 50 agents "…42 caractères de la grille…"
```

Règles :

- Toute clé JSON du format a sa forme ici. Une clé non couverte est un **bug du
  langage**, pas une limite assumée.
- Les identifiants sont explicites, y compris les sentinelles (`-1` case vide,
  `-2` cases vides en comptage, `-3` n'importe quel agent, `0` aucun produit).
- `formula` et `actions` acceptent le texte NetBioDyn tel quel : c'est l'échappatoire
  qui rend la complétude possible.
- Ce niveau n'étend rien : un comportement écrit ici vaut un comportement, sans
  polymorphisme. Les tags et types abstraits n'y ont pas cours.

### Le test de complétude

Le critère d'acceptation du langage tient en une boucle sur les modèles existants :

```
pour chaque .nbd2 de NetBioDyn_Models/ (44 fichiers) :
    script  = convertir(model)          # lecture .nbd2 → niveau mécanique
    sortie  = compiler(script)          # niveau mécanique → .nbd2
    comparer(sortie, model)             # égalité sémantique du JSON
```

- 44/44 doivent passer. Un échec nomme la clé manquante, donc le travail à faire.
- La comparaison est **sémantique** (arbre JSON), pas textuelle : l'ordre des clés et
  les espaces ne font pas partie du format.
- Ce convertisseur est aussi une fonctionnalité utile en soi : partir d'un modèle
  existant plutôt que d'une page blanche.

### Le test de fidélité du niveau naturel

Pour les modèles qui n'utilisent ni tags ni types abstraits, compiler le script
naturel doit produire **le même JSON** que la conversion mécanique du même fichier.
C'est ce test qui garantit que le sucre du niveau naturel ne change pas la sémantique.
