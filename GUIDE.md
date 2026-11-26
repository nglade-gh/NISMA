# NISMA — guide du modélisateur

Ce guide décrit le langage NISMA tel qu'il est **aujourd'hui implémenté**, pas tel qu'il
devrait être un jour. Chaque exemple de ce document a été compilé et exécuté.

Un script décrit une simulation complète ; le compilateur en produit un fichier `.nbd2`
que NetBioDyn ouvre et fait tourner. Vingt lignes de texte remplacent plusieurs
kilo-octets de JSON.

```
# declare
- grid 50 50
- agent Cell [Lime]

# model
- Cell spawns when finds 3 Cell
- Cell dies when finds Cell < 2 or finds Cell > 3

# start
- 178 Cell random
```

## 1. Premier essai

```
cd NISMA
/workspace/dotnet/dotnet run --project NISMA_Lang -- scripts/game_of_life.ni -o /tmp/gol.nbd2
```

Le compilateur affiche ce qu'il a compris :

```
écrit /tmp/gol.nbd2
1 grain(s), 0 champ(s), 2 comportement(s), 0 modificateur(s), 2500 cellule(s)
```

Ouvrez `/tmp/gol.nbd2` dans Centyllion : le modèle se charge, les deux comportements sont
dans l'explorateur, la grille est peuplée. Le fichier produit est un `.nbd2` ordinaire —
rien dans NetBioDyn ne sait qu'il vient d'un script.

Si le script contient une faute, le compilateur la nomme et s'arrête, sans rien écrire :

```
erreur : ligne 9: « Active » est ambigu : A, B répondent tous à ce nom. Nommez l'agent voulu.
```

## 2. Les quatre sections

Un script se lit comme un document : `#` pour une section, `##` pour un bloc nommé,
`-` pour un objet.

| Section | Ce qu'on y met |
|---|---|
| `# declare` | la grille, la graine, les agents, les types, les tags, les champs |
| `# modifiers` | les actions globales : tous les N pas, au pas N, sous condition |
| `# model` | les comportements, groupés en blocs si on veut |
| `# start` | qui est où au premier pas |

L'ordre est imposé ; une section vide peut être omise. **L'indentation ne veut rien
dire** : elle ne sert qu'à l'œil. Une puce se poursuit sur la ligne suivante si celle-ci
commence par `and`, `or`, `;`, `,` ou `)`, ou si une parenthèse reste ouverte.

## 3. Déclarer ce qui existe

```
# declare
- model "Immunité" description "Cellules infectées et réponse immune"
- grid 60 60
- update parallel
- seed 7

- agent Cell [Lime]
- agent Virus [Crimson] mob 1
- agent Debris [Grey] walls

- abstract Immune < Cell
- agent RestImmune < Immune
- agent ActImmune < Immune with Active
- tag Active

- field IL2 diff 0.3 life 4 dirs 8
- Cell produces IL2 1.0
```

- `agent NOM` crée un grain. La couleur est **facultative**, entre crochets :
  `agent Cell [Lime]`. Sans couleur, NISMA en tire une dans la palette vive de
  Centyllion, en évitant celles déjà prises — la règle exacte de NetBioDyn quand on crée
  un agent. Le tirage suit `seed`, donc deux compilations donnent le même fichier.
  Attributs facultatifs : `mob` (probabilité de mouvement), `life` (demi-vie), `icon`,
  `size`, `desc`, et `walls` (bords imperméables au lieu du tore par défaut).
- `abstract` crée un type qui **n'existe pas** dans le fichier produit : il ne sert qu'à
  regrouper des agents.
- `agent NOM < PARENT with TAG` crée un sous-type. Un seul parent, couleur facultative
  après les tags (`agent ActImmune < Immune with Active [Crimson]`). Sans couleur, il
  reçoit la sienne : deux agents distincts doivent se distinguer sur la grille.
- `field NOM [Couleur] diff D life L dirs 4|8` : couleur facultative, `diff` la vitesse de
  diffusion (0 à 1), `life` la demi-vie en pas, `dirs` le nombre de directions (4 ou 8).
- `Agent produces CHAMP VALEUR` : production. `absorbs` pour l'influence, `lets` pour la
  perméabilité.

Les tags peuvent être déclarés après les agents qui les portent : le compilateur lit
toutes les déclarations avant de résoudre les noms.

## 4. Écrire ce qui se passe

Cinq formes, pas une de plus.

```
# model
- Cell produces IL2 1.0

- Cell spawns when finds 3 Cell
- Cell dies when finds Cell < 2 or finds Cell > 3
- Virus -> Debris when finds Cell > 0
- neighbour Virus -> Debris when finds Virus
```

| Forme | Sens |
|---|---|
| `A produces F V` | production du champ F par A |
| `A spawns` | une cellule vide devient A |
| `A dies` | l'agent disparaît |
| `A -> B` | A devient B (forme longue : `A reacts into B`) |
| `neighbour A -> B` | un voisin A devient B |

Une puce peut porter une étiquette, qui devient le nom du comportement dans NetBioDyn :

```
- Birth: Cell spawns when finds 3 Cell
```

Sans étiquette, le nom est fabriqué (`Cell spawns`, `Cell -> Debris`), et préfixé par le
nom du bloc s'il y en a un.

Une règle `neighbour` n'a **pas** de réactif principal : elle décrit une ligne de
réaction, ce qui se passe chez un voisin. C'est exactement la différence entre « cette
cellule devient » et « son voisin devient ».

## 5. Décrire les conditions

`finds` compte les voisins ; le reste compare une grandeur.

| Écriture | Sens |
|---|---|
| `finds 3 Cell` | exactement trois voisins de type Cell |
| `finds Cell < 2` | moins de deux |
| `finds Cell` | au moins un |
| `finds any` | au moins un voisin occupé, quel qu'il soit |
| `finds nothing` | aucun voisin occupé |
| `finds only Cell, Virus` | aucun type de voisin hors de la liste |
| `IL2 > 1` | valeur du champ sur la cellule courante |
| `age > 10` | âge de l'agent |
| `at step 10` | numéro de pas |
| `not` / `!`, `and` / `&&`, `or` / `||` | combinaison, les deux écritures au choix |
| `( )` | groupement, imbriquable |

Les comptages portent sur les huit voisins ; la case centrale n'est jamais comptée.

Les connecteurs s'écrivent en mots ou en symboles, et rien n'interdit de les mêler :

```
- Cell -> Virus when finds Virus >= 3 or (finds Cell < 2 && IL2 > 0.5)
- Cell dies when !(finds Cell >= 2) and age > 10
```

Une règle longue se coupe sur plusieurs lignes : il suffit que la ligne suivante commence
par un connecteur, ou que la ligne courante se termine par l'un d'eux.

```
- Cell dies when finds Cell >= 2 and
  (finds Virus >= 1 || IL2 > 0.9)
```

Le compilateur choisit la forme la plus éditable dans NetBioDyn : une conjonction de
comptages et de comparaisons de champs devient des prédicats graphiques (`count`, `field`,
`age`), le reste devient une formule. La sémantique est la même ; seule la facilité de
retouche ensuite change.

Un comptage sur un **type** (`finds Cell < 2` quand `Cell` a des sous-types) devient une
somme : `(count(0)+count(3)+count(4))<2`. C'est ce que veut dire « moins de deux cellules,
toutes sortes confondues ».

## 6. Types, tags, polymorphisme

C'est la raison d'être du langage : NetBioDyn ne sait pas écrire « n'importe quelle
cellule immunitaire devient active ».

```
# declare
- abstract Immune < Cell
- agent RestImmune < Immune
- agent ActImmune < Immune with Active
- tag Active

# model
- Immune -> ActImmune when IL2 > 1
```

`Immune` n'est pas un agent : c'est un nom qui désigne deux agents concrets. Le
comportement est donc **étendu** en deux :

```
activation/1 : RestImmune -> ActImmune   quand IL2 > 1
activation/2 : ActImmune   -> ActImmune  quand IL2 > 1
```

Le compilateur l'annonce, pour qu'on ne s'étonne pas du nombre de comportements produits :

```
note : ligne 21 : « Immune » s'étend en 2 comportements
```

La seconde ligne transforme un agent en lui-même : elle ne change rien à l'état, mais
elle réinitialise son âge. C'est le comportement de NetBioDyn, pas un effet du langage.

**Un produit ambigu est une erreur, jamais un choix.** Si deux agents portent le tag
`Active`, écrire `-> Active` s'arrête :

```
erreur : ligne 9: « Active » est ambigu : A, B répondent tous à ce nom. Nommez l'agent voulu.
```

Il faut décider : soit un seul agent porte le tag, soit on nomme l'agent voulu. Le
compilateur refuse de tirer à pile ou face à votre place.

## 7. Blocs, probabilités, modificateurs

Un `##` ouvre un bloc nommé. Il ne crée aucune variable : il donne un nom à un groupe de
règles, et ce nom devient un préfixe dans le fichier produit (`infection/3`).

```
# model
## infection
- p 0.2
- neighbour Virus -> Debris when finds Virus
  ; set IL2 0
```

`p 0.2` en tête de bloc s'applique à **chaque règle du bloc, indépendamment** : ce n'est
pas une probabilité de groupe. Une règle qui porte son propre `p` l'emporte.

Les actions suivent `;` et s'exécutent quand la règle se déclenche : `pause`, `capture`,
`set CHAMP VALEUR` (sur la cellule courante), `add N AGENT in square (x1,y1)-(x2,y2)`.

Les modificateurs sont des actions globales, hors de toute cellule :

```
# modifiers
- every 10 steps: add 5 Food in square (2,2)-(10,10)
- at step 30: capture
- when IL2 < 0.01 everywhere: pause
```

`every N steps` devient `step % N == 0`, `at step N` devient `step == N` : le fichier
produit reste un `.nbd2` ordinaire, sans mécanisme nouveau.

## 8. Peupler la grille

```
# start
- 800 Cell random
- 40 Virus random
- 10 RestImmune in (5,5)
```

`N Agent random` place N agents sur des cellules libres, en tirant dans l'ordre des
déclarations ; `seed` rend le tirage reproductible.

## 9. Ce que le prototype ne fait pas encore

Le prototype couvre le cœur du langage, pas son pourtour. Ne sont pas implémentés :

- `IL2 in near` (valeur d'un champ chez un voisin) — écrivez la formule au niveau
  mécanique ;
- l'import de motif dans `# start` (`from "glider.rle"`) ;
- les actions autres que `pause`, `capture`, `set`, `add … in square` ;
- les attributs de champ `wall` et `torus` ;
- le niveau mécanique lui-même : il est spécifié, pas encore compilé ;
- la conversion `.nbd2` → script.

Ces limites sont dans le code, pas dans la conception : chaque forme manquante a déjà sa
place dans la grammaire.

## 10. Pour le programmeur

Trois fichiers, trois responsabilités :

| Fichier | Rôle |
|---|---|
| `Lexer.cs` | découpe en objets logiques (sections, puces, continuations), puis en tokens |
| `Parser.cs` | tokens → `Script` (déclarations, règles, conditions, état initial) |
| `Compiler.cs` | `Script` → `NbdModel` : résolution des noms, expansion, émission |

Ajouter une forme de condition se fait à trois endroits : `ReadAtom` dans le `Parser`,
un cas dans `TryStructured` (forme graphique) et un cas dans `ToFormula` (forme formule).
Oublier le troisième donne une condition silencieusement ignorée — c'est le seul piège
réel de cette architecture, et la raison pour laquelle chaque condition doit être testée
sur les deux chemins.

Garanties tenues par le compilateur :

- **identifiants déterministes** : attribués dans l'ordre de déclaration, après
  expansion ; deux compilations du même script donnent le même fichier ;
- **aucun fichier écrit en cas d'erreur** : les cinq phases passent ou rien ne sort ;
- **erreurs situées** : le numéro de ligne est celui du script, jamais celui du JSON.

Le test du jeu de la vie vérifie les trois choses qui comptent, sans faire confiance au
compilateur :

```
python3 NISMA_Lang/tests/verify_game_of_life.py /tmp/gol.nbd2 \
    "../NetBioDyn_Models/GoL Engine.nbd2"
```

- les tables de vérité des prédicats produits sont comparées à celles du modèle de
  référence de NetBioDyn ;
- la grille produite est animée 60 générations par ces seuls prédicats, et comparée à une
  implémentation de Conway écrite dans le script de test ;
- la structure (un grain, deux comportements) est vérifiée.

Sortie obtenue :

```
table naissance : 000100000
table mort      : 110011111
référence : GoL Engine.nbd2 — tables identiques
grille 50x50, 178 cellules vivantes au départ
60 générations : identiques à Conway, sans divergence
```
