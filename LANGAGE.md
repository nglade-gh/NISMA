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

## 3. Trois sections, dans l'ordre

Comme un programme Pascal sépare ses déclarations de son corps, un script sépare ce qui
**existe**, ce qui **agit sur la simulation**, et ce qui **modélise**. L'ordre est
imposé ; une section peut être vide, mais elle ne peut pas être déplacée.

```
declare         ce qui existe : agents, types, tags, champs, caractéristiques
modifiers       ce qui pilote la simulation : actions périodiques, pauses, captures
model           ce qui modélise : productions de champs et comportements
```

Les en-têtes sont facultatifs ; en leur absence, l'ordre des sections fait foi.

La séparation n'est pas cosmétique : les deux premières sections parlent de **support**
(noms, couleurs, formes, cadence d'actions globales), la troisième parle de **biologie**.
Un modélisateur qui ne lit que `model` doit comprendre le modèle ; un développeur qui ne
lit que `declare` doit pouvoir changer une couleur sans toucher au sens.

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

## 4. declare — ce qui existe

```
declare
  grid 100 100
  update parallel
  seed 42

  agent Cell lime
  agent Virus crimson mob 1
  agent Debris grey

  abstract Immune < Cell
  agent RestImmune < Immune
  agent ActImmune < Immune with Active
  tag Active

  field IL2 diff 0.3 life 4 dirs 8
  field IFN diff 0.2 life 6 dirs 4 opacity 60
```

| Forme | Sens |
|---|---|
| `grid L H` | taille de la grille |
| `update parallel\|sequential` | plan de mise à jour |
| `seed N` | graine aléatoire |
| `agent NOM COULEUR` | grain concret |
| `abstract NOM < PARENT` | type abstrait, absent du fichier produit |
| `agent NOM < PARENT with T1, T2` | sous-type concret, un seul parent |
| `tag NOM` | caractéristique énumérée |
| `field NOM diff D life L dirs 4\|8` | champ |

**Attributs facultatifs** — `mob` (mobilité), `life` (demi-vie du grain), `icon`,
`size`, `desc`. Aucun n'a d'effet sur la sémantique : ce sont des attributs de
présentation, et c'est précisément pour cela qu'ils vivent dans `declare` et non dans
`model`. `opacity` et `wall` (paroi imperméable) sont les seuls attributs de champ qui
changent quelque chose, et ils restent dans `declare` parce qu'ils décrivent le support.

## 5. modifiers — ce qui pilote la simulation

Des actions globales, déclenchées par le temps ou par une condition d'ensemble. Elles
deviennent des `modifiers` dans le fichier produit.

```
modifiers
  every 50 steps: add 20 Virus in square (10,10)-(20,20)
  at step 10: set IL2 0 everywhere
  at step 200: capture
  when IL2 < 0.01 everywhere: pause
```

| Déclencheur | Sens |
|---|---|
| `every N steps` | tous les N pas |
| `at step N` | au pas N exactement |
| `when CONDITION everywhere` | dès que la condition tient sur toute la grille |

Les actions sont celles des comportements, plus `pause`, `capture`, `stop`,
`activate`/`deactivate` d'un comportement. Une seule action par modificateur ; deux
lignes pour deux actions.

## 6. model — ce qui modélise

```
model
  Cell makes IL2 1.0

  Cell spawns when finds 3 Cell
  Cell dies when finds Cell < 2 or finds Cell > 3
  Immune -> ActImmune when IL2 > 1
  neighbour Virus -> Debris when finds Virus
  Cell -> Debris p 0.1 when age > 10
  neighbour Virus -> Debris when finds Virus; set IL2 0
```

| Forme | Sens |
|---|---|
| `Cell makes F V` | production du champ F |
| `Cell absorbs F V` | influence du champ F |
| `Cell lets F V` | perméabilité au champ F |
| `Cell spawns` | une cellule vide devient Cell |
| `Cell dies` | l'agent disparaît |
| `A -> B` | A devient B ; forme longue `A reacts into B` |
| `neighbour A -> B` | un voisin A devient B (ligne de réaction) |

- Un type abstrait en sujet s'étend de lui-même en un comportement par grain concret
  (§7). `any` reste disponible pour insister.
- Le produit d'une transformation doit être un grain **concret** : sinon erreur, avec la
  liste des candidats.
- `p 0.3` vaut 1 par défaut.
- Les actions suivent `;`, séparées par `;`.

## 7. Conditions

`finds` compte les voisins, `∈` teste l'appartenance à un ensemble :

| Forme | Sens |
|---|---|
| `finds 3 Cell` | exactement trois voisins de type Cell |
| `finds Cell < 2` | moins de deux |
| `finds Cell` | au moins un |
| `finds nothing` | aucun voisin occupé |
| `finds only Cell, Virus` | aucun type hors de ceux-là |
| `IL2 > 1` | champ sur la cellule courante |
| `IL2 in near > 0.2` | champ chez un voisin |
| `age > 10` | âge de l'agent |
| `at step 10` | numéro de pas |
| `Cell ∈ around` | forme symbolique de `finds Cell` |
| `not`, `and`, `or`, parenthèses | combinaison |

Les comptages portent sur les huit voisins, la case centrale n'est jamais comptée.
`finds only` s'appuie sur `hasOnly` de NetBioDyn.

## 8. État initial

En fin de fichier, après `model` :

```
start 200 Cell random
start Cell in (5,5)
start Virus from "glider.rle" in (10,10)
```

Trois formes seulement. Un motif plus complexe s'écrit dans un fichier de stencil, pas
dans le script.

## 8bis. Blocs et portées

Un bloc regroupe des règles sous un nom. Il ne crée ni variable ni sous-programme : il
donne une **unité de lecture** et une **unité de pilotage**.

```
model
  Cell makes IL2 1.0

  infection:
    p 0.2
    neighbour Virus -> Debris when finds Virus
    Cell -> Debris when finds 3 Virus

  growth:
    Cell spawns when finds 3 Cell
    Cell dies when finds Cell < 2 or finds Cell > 3
```

1. `nom:` ouvre un bloc ; les lignes indentées lui appartiennent. **Pas d'imbrication**
   en v1 : NetBioDyn ne connaît pas les groupes, et une hiérarchie de portées poserait
   des questions sans réponse (une probabilité de groupe s'applique-t-elle à chaque
   règle ou au groupe entier ?).
2. Une règle hors bloc appartient au bloc implicite `main`.
3. Un `p` en tête de bloc s'applique à **chaque règle du bloc, indépendamment** — c'est
   du sucre, pas une probabilité de groupe. Une règle portant son propre `p` l'emporte.
4. Le nom du bloc préfixe les comportements produits : `infection/1`, `infection/2`. Le
   `.nbd2` reste lisible et un groupe reste identifiable à la relecture.
5. `activate infection` et `deactivate infection` depuis `modifiers` basculent tous les
   comportements du bloc — c'est la raison d'être principale des blocs.

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
declare
  grid 100 100
  seed 42

  agent Cell lime
  agent Virus crimson mob 1
  agent Debris grey

  abstract Immune < Cell
  agent RestImmune < Immune
  agent ActImmune < Immune with Active
  tag Active

  field IL2 diff 0.3 life 4 dirs 8

modifiers
  every 50 steps: add 20 Virus in square (10,10)-(20,20)
  at step 500: capture

model
  Cell makes IL2 1.0

  growth:
    Cell spawns when finds 3 Cell
    Cell dies when finds Cell < 2 or finds Cell > 3

  immunity:
    Immune -> ActImmune when IL2 > 1
    neighbour Virus -> Debris when finds Virus; set IL2 0

start 500 Cell random
start 20 Virus random
```

Vingt-quatre lignes : deux grains plus un type abstrait et un tag, un champ, deux blocs
nommés, un modificateur périodique, l'état initial.

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
