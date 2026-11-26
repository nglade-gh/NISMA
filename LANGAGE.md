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

Un mot par chose, pas d'articles. Les valeurs par défaut sont celles de NetBioDyn et
s'écrivent seulement si elles diffèrent.

```
grid 100 100
update parallel
seed 42

agent Cell lime
agent Virus crimson mob 1
agent Debris grey

abstract Immune < Cell
agent RestImmune < Immune
agent ActImmune < Immune +Active
tag Active

field IL2 diff 0.3 life 4 dirs 8
field IFN diff 0.2 life 6 dirs 4 opacity 60

Cell makes IL2 1.0
Cell absorbs IL2 0.5
Cell lets IL2 0.2
```

| Forme | Sens |
|---|---|
| `agent NOM COULEUR` | grain concret ; `mob`, `life`, `icon`, `desc` en option |
| `abstract NOM < PARENT` | type abstrait, jamais écrit dans le fichier |
| `agent NOM < PARENT +Tag1 +Tag2` | sous-type concret, un seul parent |
| `tag NOM` | caractéristique énumérée |
| `field NOM diff D life L dirs 4\|8` | champ ; `opacity`, `wall` en option |
| `A makes F V` | production du champ F par A, valeur V |
| `A absorbs F V` | influence du champ F sur A |
| `A lets F V` | perméabilité de A au champ F |

## 5. Comportements

Une ligne par comportement. La forme est toujours la même :

```
SUJET VERBE [OBJET] [if CONDITIONS] [p PROBABILITE] [; ACTIONS]
```

| Sujet | Verbe | Sens |
|---|---|---|
| `Cell` | `spawn` | génération spontanée : une cellule vide devient Cell |
| `Cell` | `die` | l'agent disparaît |
| `A -> B` | — | transformation : A devient B |
| `neighbour A -> B` | — | une ligne de réaction : un voisin A devient B |

```
Cell spawn if 3 Cell around
Cell die if Cell around < 2 or Cell around > 3
Immune -> ActImmune if IL2 > 1
neighbour Virus -> Debris if Virus around >= 1
Cell -> Debris p 0.1 if age > 10
neighbour Virus -> Debris if Virus around >= 1; set IL2 0
```

- `Immune` étant abstrait, `Immune -> ActImmune` s'étend en un comportement par grain
  concret de `Immune` (§6). Le mot `any` reste disponible pour insister.
- Le produit d'une transformation doit être un grain **concret** ; sinon erreur.
- `p 0.3` vaut 1 par défaut.
- Les actions suivent `;`, séparées par `;`.

## 6. Conditions

Le vocabulaire tient en une douzaine de formes :

| Forme | Sens |
|---|---|
| `3 Cell around` | exactement 3 voisins de type Cell |
| `Cell around < 2` | moins de deux |
| `Cell around >= 1` | au moins un |
| `Cell around` | au moins un (raccourci) |
| `around Cell Virus only` | aucun type hors de ceux-là |
| `IL2 > 1` | valeur du champ sur la cellule courante |
| `IL2@near > 0.2` | valeur du champ chez un voisin |
| `age > 10` | âge de l'agent |
| `step is 10` | numéro de pas |
| `not` , `and` , `or` , parenthèses | combinaison |

Les comptages portent sur les huit voisins ; la case centrale n'est jamais comptée.
`around Cell Virus only` s'appuie sur `hasOnly` de NetBioDyn, `3 Cell around` sur
`count(0) == 3`.

## 7. État initial

```
start 200 Cell random
start Cell at 5 5
start Virus from "glider.rle" at 10 10
```

Trois formes seulement : densité aléatoire, position explicite, motif importé (RLE,
`.cells`, matrice — les formats que NetBioDyn accepte déjà). Un motif plus complexe
s'écrit dans un fichier de stencil, pas dans le script.

## 8. Réglages

`grid`, `update` et `seed` figurent en tête du fichier (§4). `update` vaut `parallel`
par défaut, `sequential` sinon. Sans `seed`, NISMA en tire une et l'écrit dans le
fichier, pour que deux générations du même script soient identiques.

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
grid 100 100
update parallel
seed 42

agent Cell lime
agent Virus crimson mob 1
agent Debris grey

abstract Immune < Cell
agent RestImmune < Immune
agent ActImmune < Immune +Active
tag Active

field IL2 diff 0.3 life 4 dirs 8
Cell makes IL2 1.0

Cell spawn if 3 Cell around
Cell die if Cell around < 2 or Cell around > 3
Immune -> ActImmune if IL2 > 1
neighbour Virus -> Debris if Virus around >= 1; set IL2 0

start 500 Cell random
start 20 Virus random
```

Vingt lignes, deux comportements polymorphes, un champ, une action. Le même modèle en
`.nbd2` fait plusieurs kilo-octets de JSON, dont l'essentiel est de la ponctuation.

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
