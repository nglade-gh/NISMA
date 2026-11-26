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
modèle, mais sans perte. C'est ce qui impose les deux niveaux du §3.

## 2. Principe directeur

1. **Vocabulaire fermé.** Une vingtaine de mots-clés, refus de tout le reste.
2. **Déclaratif, pas impératif.** On décrit ce qui existe et ce qui se passe ; on
   n'ordonne pas des étapes.
3. **Rien n'est implicite.** Toute résolution incertaine (un produit qui pourrait être
   deux grains) est une **erreur**, jamais un choix par défaut.
4. **Le fichier se lit comme un document.** Sections en titres, objets en puces : la
   forme du texte dit sa structure.

## 3. Deux niveaux, une seule cible

La complétude et la lisibilité se contredisent si on les demande au même niveau. Le
langage a donc deux niveaux, et **le premier se compile dans le second** :

**Niveau naturel** (§5 à §11) — ce que le modélisateur écrit : `when`, `finds`,
`spawns`, `with`, tags. Il couvre les modèles courants et masque les identifiants, les
sentinelles et les noms de prédicats.

**Niveau mécanique** (§14) — un décalque du format `.nbd2`, un mot par clé JSON,
formules brutes comprises. Il n'est pas destiné à être écrit à la main, sauf pour un cas
marginal ; il garantit qu'**aucun modèle existant n'est inexprimable**.

Conséquence : l'outil peut convertir un `.nbd2` en script mécanique. On part d'un modèle
existant, jamais d'une page blanche, et la conversion est le meilleur test de complétude
du langage.

## 4. Structure et mise en page

**Un `#` ouvre une section, un `##` ouvre un bloc nommé, un `-` (ou `*`) introduit un
objet.** La hiérarchie est portée par les titres, comme dans un document Markdown ;
l'indentation n'a **aucune valeur syntaxique** et ne sert qu'à la lisibilité.

```
# declare
- agent Cell lime
- agent Virus crimson mob 1

# model
## growth
- Cell spawns when finds 3 Cell
- Cell dies when finds Cell < 2 or finds Cell > 3
```

Quatre sections, dans cet ordre, toutes facultatives sauf `declare` :

| Section | Contenu |
|---|---|
| `# declare` | grille, graine, agents, types, tags, champs, productions |
| `# modifiers` | actions globales, périodiques ou conditionnelles |
| `# model` | comportements, éventuellement groupés en blocs `##` |
| `# start` | état initial |

Règles :

- Les sections ne peuvent pas être déplacées. Une section vide est permise.
- Un objet commence par une puce et peut se poursuivre sur plusieurs lignes, à condition
  que la continuation soit **explicite** (§4bis).
- `-` et `*` sont équivalents ; `-` est recommandé.
- Les commentaires utilisent `//` : le dièse est réservé aux titres.
- Les lignes vides sont ignorées ; deux puces séparées par une ligne vide restent dans
  la même section.
- Un rapport d'erreur cite **la ligne du script** : c'est le script qu'on corrige.

Ce choix a une conséquence utile : l'indentation n'étant pas structurelle, un mélange
d'espaces et de tabulations ne peut pas produire d'erreur invisible — le défaut classique
des langages à indentation significative.

## 4bis. Lignes longues

Une puce peut courir sur plusieurs lignes, mais **jamais par simple indentation** : une
indentation plus forte ne signifie rien, sinon on retombe sur le défaut des langages à
indentation significative. La continuation est décidée par le lexique, selon trois
règles.

**1. La ligne suivante commence par un opérateur de liaison** : `and`, `or`, `;`, `,`,
`)`. La puce se poursuit.

```
- neighbour Virus -> Debris when finds Cell > 0
  and finds Virus
  and IL2 < 0.1
  ; set IL2 0
  ; capture
```

**2. Une parenthèse reste ouverte à la fin de la ligne.** La puce se poursuit jusqu'à la
parenthèse fermante.

```
- Cell -> Debris when (finds Cell < 2
      or finds Virus > 3)
```

**3. Un `\` en fin de ligne.** Échappatoire pour les cas où aucune des deux règles ne
s'applique — rare, mais il en faut une.

Règles de lecture :

- Les lignes vides et les commentaires `//` sont ignorés quand on cherche la
  continuation : ils ne cassent ni ne prolongent un objet.
- La puce est la seule à porter le `-` ; les lignes de continuation n'en portent pas.
  Une seconde puce, elle, commence bien un nouvel objet.
- Les erreurs citent la **première ligne** de l'objet, et l'analyse reprend à sa dernière
  ligne — une règle multiligne ne décale donc pas les numéros suivants.
- Le style recommandé aligne la continuation sous le texte de la puce, mais cette
  indentation n'est qu'une convention de lecture.

Pourquoi pas la règle inverse — « la ligne suivante est plus indentée donc elle
continue » ? Parce qu'elle rend le sens dépendant d'un caractère invisible, et qu'une
reformatage malheureux changerait alors la signification du fichier sans rien signaler.

## 5. declare — ce qui existe

```
# declare
- grid 100 100
- update parallel
- seed 42

- agent Cell lime
- agent Virus crimson mob 1
- agent Debris grey

- abstract Immune < Cell
- agent RestImmune < Immune
- agent ActImmune < Immune with Active
- tag Active

- field IL2 diff 0.3 life 4 dirs 8
- field IFN diff 0.2 life 6 dirs 4 opacity 60
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

**Attributs de présentation** — `mob` (mobilité), `life` (demi-vie du grain), `icon`,
`size`, `desc`. Ils n'ont aucun effet sur la simulation, et c'est pour cela qu'ils vivent
dans `declare` : on peut changer une couleur sans relire le modèle. `opacity` et `wall`
(paroi imperméable) sont les seuls attributs de champ qui changent quelque chose, et ils
restent ici parce qu'ils décrivent le support.

## 6. modifiers — ce qui pilote la simulation

Des actions globales, déclenchées par le temps ou par une condition d'ensemble. Elles
deviennent des `modifiers` dans le fichier produit.

```
# modifiers
- every 50 steps: add 20 Virus in square (10,10)-(20,20)
- at step 10: set IL2 0 everywhere
- at step 200: capture
- when IL2 < 0.01 everywhere: pause
- at step 300: deactivate growth
```

| Déclencheur | Sens |
|---|---|
| `every N steps` | tous les N pas |
| `at step N` | au pas N exactement |
| `when CONDITION everywhere` | dès que la condition tient sur toute la grille |

Actions : celles des comportements, plus `pause`, `capture`, `stop`, `activate BLOC` et
`deactivate BLOC`. Une action par puce ; deux puces pour deux actions.

## 7. model — ce qui modélise

```
# model
- Cell makes IL2 1.0

## growth
- Cell spawns when finds 3 Cell
- Cell dies when finds Cell < 2 or finds Cell > 3

## immunity
- Immune -> ActImmune when IL2 > 1
- neighbour Virus -> Debris when finds Virus; set IL2 0
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
  (§11). `any` reste disponible pour insister.
- Le produit d'une transformation doit être un grain **concret** : sinon erreur, avec la
  liste des candidats.
- `p 0.3` vaut 1 par défaut.
- Les actions suivent `;`, séparées par `;`.

## 8. Conditions

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

## 9. start — état initial

```
# start
- 200 Cell random
- Cell in (5,5)
- Virus from "glider.rle" in (10,10)
```

Trois formes seulement. Un motif plus complexe s'écrit dans un fichier de stencil, pas
dans le script.

## 10. Blocs et portées

Un `##` ouvre un bloc nommé dans `model`. Il ne crée ni variable ni sous-programme : il
donne une **unité de lecture** et une **unité de pilotage**.

```
# model
## infection
- p 0.2
- neighbour Virus -> Debris when finds Virus
- Cell -> Debris when finds 3 Virus

## growth
- Cell spawns when finds 3 Cell
```

1. Les puces qui suivent appartiennent au bloc jusqu'au `##` suivant ou à la section
   suivante. **Pas d'imbrication** : NetBioDyn ne connaît pas les groupes, et une
   hiérarchie poserait des questions sans réponse (une probabilité de groupe
   s'applique-t-elle à chaque règle, ou au groupe entier ?).
2. Une règle placée avant le premier `##` appartient au bloc implicite `main`.
3. Un `p` en tête de bloc s'applique à **chaque règle du bloc, indépendamment** : c'est
   du sucre, pas une probabilité de groupe. Une règle portant son propre `p` l'emporte.
4. Le nom du bloc préfixe les comportements produits : `infection/1`, `infection/2`. Le
   `.nbd2` reste lisible et un groupe reste identifiable à la relecture.
5. `activate infection` / `deactivate infection` depuis `modifiers` bascule tous les
   comportements du bloc — c'est la raison d'être principale des blocs.

## 11. Polymorphisme par les tags

```
# declare
- abstract Cell
- tag Active
- agent RestingCell < Cell
- agent ActiveCell < Cell with Active

# model
- Immune -> ActiveCell when IL2 > 1
```

**Un type abstrait en sujet désigne l'ensemble de ses grains concrets** et le
comportement est **étendu** en un comportement par grain.

**Le produit doit être unique.** Deux cas :

- un seul agent concret porte le tag → il est choisi ;
- plusieurs le portent → **erreur**, avec la liste :

```
error line 14: "-> Active" is ambiguous: ActiveCell and ActiveTcell both carry the
tag Active. Name the agent you mean, or give the tag to a single one.
```

### Règles d'expansion

1. Seuls le réactif principal et les réactifs de ligne sont polymorphes. Un **produit ne
   l'est jamais** : c'est la source de l'ambiguïté, donc une erreur.
2. Un comportement polymorphe produit autant de comportements concrets que l'extension le
   demande, et le compilateur **annonce le compte** :
   `block immunity, line 3: expands to 3 concrete behaviours`.
3. La probabilité est conservée telle quelle : une cellule ne portant qu'un grain, un
   seul des comportements étendus peut s'appliquer à elle par pas.
4. Les identifiants sont attribués **après expansion**, dans l'ordre de déclaration. Deux
   compilations du même fichier donnent le même `.nbd2`.
5. Types abstraits et tags ne sont **jamais** écrits dans le fichier.

## 12. Compilation

```
1. Analyse      texte → arbre de syntaxe ; toute faute de vocabulaire est refusée ici
2. Résolution   noms → ensembles d'agents concrets (tags, types abstraits)
3. Expansion    comportements polymorphes → comportements plats
4. Émission     modèle NISMA → JSON .nbd2
5. Validation   règles de Centyllion sur le modèle étendu ; refus si non valide
```

Une erreur arrête la compilation. Aucun fichier n'est écrit tant que les cinq phases
n'ont pas réussi.

## 13. Ce que le langage ne fera pas en v1

- Héritage de corps de comportements.
- Héritage multiple, ou agents appartenant à plusieurs types abstraits.
- Formules libres dans le **niveau naturel** : tout y passe par le vocabulaire. Le niveau
  mécanique (§14) les accepte, sans quoi les modèles existants ne seraient pas
  exprimables.
- Fonctions, variables, boucles : un script décrit un modèle, pas un calcul.

## 14. Niveau mécanique — la garantie de complétude

Un mot par clé du format, aucune élégance recherchée. Il existe pour une seule raison :
qu'aucun modèle existant ne soit inexprimable.

```
# declare
- model "coeur" description "modèle de référence"
- update sequential
- agent 0 "C_a" colour lime icon square-full size 1 mobility 0
- agent 1 "C_i" colour dodgerblue
- field 0 "pa" colour orange speed 0.3 half-life 4 opacity 100 toric yes

# model
## behaviour 1 "Birth"
- enabled yes
- probability 1
- main -1 -> 0
- count 0 is 3
- neighbour 0 "C_a" -> 0 directions right left
- actions "SetField(0, 0, 0, 1)"

# modifiers
- modifier 0 "Seed" enabled yes probability 1
- formula "step == 10"
- actions "AddRect(1, 50, 10, 10, 20, 20)"

# start
- grid 50 x 50 agents "…la grille, 2500 caractères…"
```

Règles :

- `##` ouvre toujours une **portée nommée** ; seule la charge change : groupe de règles au
  niveau naturel, comportement ou modificateur au niveau mécanique. La syntaxe reste la
  même, l'analyseur n'a pas de cas particulier.
- Toute clé JSON du format a sa forme ici. Une clé non couverte est un **bug du
  langage**, pas une limite assumée.
- Les identifiants sont explicites, sentinelles comprises (`-1` case vide, `-2` cases
  vides en comptage, `-3` n'importe quel agent, `0` aucun produit).
- `formula` et `actions` acceptent le texte NetBioDyn tel quel : c'est l'échappatoire qui
  rend la complétude possible.
- Ce niveau n'étend rien : un comportement écrit ici vaut un comportement, sans
  polymorphisme. Tags et types abstraits n'y ont pas cours.

### Le test de complétude

```
pour chaque .nbd2 de NetBioDyn_Models/ (44 fichiers) :
    script = convertir(model)      // lecture .nbd2 → niveau mécanique
    sortie = compiler(script)      // niveau mécanique → .nbd2
    comparer(sortie, model)        // égalité sémantique du JSON
```

44/44 exigés. Un échec nomme la clé manquante. La comparaison est **sémantique** (arbre
JSON), pas textuelle : l'ordre des clés n'appartient pas au format.

### Le test de fidélité du niveau naturel

Pour les modèles sans tags ni types abstraits, compiler le script naturel doit produire
**le même JSON** que la conversion mécanique du même fichier. C'est ce test qui garantit
que le sucre ne change pas la sémantique.

## 15. Exemple complet

```
# declare
- grid 100 100
- seed 42
- agent Cell lime
- agent Virus crimson mob 1
- agent Debris grey
- abstract Immune < Cell
- agent RestImmune < Immune
- agent ActImmune < Immune with Active
- tag Active
- field IL2 diff 0.3 life 4 dirs 8

# modifiers
- every 50 steps: add 20 Virus in square (10,10)-(20,20)
- at step 500: capture

# model
- Cell makes IL2 1.0

## growth
- Cell spawns when finds 3 Cell
- Cell dies when finds Cell < 2 or finds Cell > 3

## immunity
- Immune -> ActImmune when IL2 > 1
- neighbour Virus -> Debris when finds Virus
  and finds Cell > 0
  ; set IL2 0
  ; capture

# start
- 500 Cell random
- 20 Virus random
```

Vingt-cinq lignes utiles : deux grains plus un type abstrait et un tag, un champ, deux
blocs nommés, une règle multiligne, un modificateur périodique, l'état initial.

## 16. État de l'implémentation

Un prototype du **niveau naturel** vit dans `NISMA_Lang/` ; `GUIDE.md` en est le mode
d'emploi. Deux scripts compilent et ont été vérifiés :

- `scripts/game_of_life.ni` — 2 comportements ; les tables de vérité sont identiques à
  celles du modèle de référence `GoL Engine.nbd2`, et la grille animée 60 générations par
  les seuls prédicats produits suit Conway sans divergence ;
- `scripts/immunity.ni` — type abstrait, tag, champ, blocs ; `Immune -> ActImmune`
  s'étend en 2 comportements, `Cell -> Debris` en 3 (un agent concret qui a des sous-types
  désigne aussi ceux-ci).

### Ce que l'implémentation a précisé

1. **`abstract NOM` sans parent** est autorisé : un type racine est utile et le forcer à
   hériter de quelque chose n'apporte rien.
2. **Un agent concret peut être le parent d'un type abstrait.** `abstract Immune < Cell`
   est la formulation naturelle du modèle immunitaire, et rien n'interdit qu'un agent soit
   à la fois instanciable et général.
3. **Un nom qui désigne plusieurs agents concrets s'étend** : `Cell -> Debris` produit un
   comportement par sous-type, et un comptage sur un type devient la somme des comptages
   (`(count(0)+count(3))<2`). Le compilateur annonce chaque expansion.
4. **Une règle `neighbour` n'a pas de réactif principal.** L'implémentation a montré qu'il
   fallait choisir : `neighbour Virus -> Debris` a d'abord produit un comportement dont le
   réactif principal *et* la ligne de réaction étaient Virus. Le réactif principal est
   désormais vide — sinon la règle dirait deux choses à la fois.
5. **Les tags sont résolus avant les agents** : l'ordre de déclaration ne contraint plus
   l'écriture.
6. **`every N steps` s'écrit `step % N == 0`** dans le fichier produit : `%` existe dans
   les formules de Centyllion, donc aucune mécanique nouvelle n'est nécessaire.

### Écarts assumés avec le texte ci-dessus

- `IL2 in near` n'est pas compilé : la formule équivalente existe, la forme naturelle
  attend son tour.
- Le niveau mécanique (§14) est spécifié mais pas encore compilé ; c'est lui qui portera
  la garantie de complétude, et son test est la conversion des 44 modèles de référence.
