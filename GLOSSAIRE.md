# Glossaire des commandes

Une entrée par mot du langage, dans l'ordre alphanumérique : d'abord les symboles et les
sections, puis les mots de A à Z. Chaque entrée donne la syntaxe exacte, ce que la commande
produit, et le piège ordinaire.

Les formes marquées **refusé** existent dans le vocabulaire du prototype mais ne sont pas
traduisibles en `.nbd2` : le compilateur les rejette avec un message explicite plutôt que de
les approximer.

## Symboles et sections

### `#`
Ouvre une section. Le nom qui suit décide de ce que la suite du script décrit. L'ordre des
sections est imposé : `declare`, puis `modifiers`, `model`, `process` si besoin, `start`.

### `# declare`
Ce qui existe : grille, graine, agents, types, tags, champs, productions.
Exemple : `# declare` puis `- grid 60 60`.

### `# modifiers`
Événements globaux, hors de toute cellule. Vient après `# declare` et avant `# model`.
Exemple : `- every 10 steps: add 5 Food in square (2,2)-(10,10)`.

### `# model`
Les comportements : naissances, morts, transformations, productions.
Exemple : `- Cell dies when finds Cell > 3`.

### `# process`
Machine à états d'un type. Chaque `##` nomme un état, et chaque état devient un grain
distinct dans le fichier produit (`Cell.Infected`).
Exemple : `# process Cell` puis `## Infected`.

### `# start`
État initial de la grille. Exemple : `- 800 Cell random`.

### `##`
Ouvre un bloc nommé dans `# model` ou un état dans `# process`. Le nom devient un préfixe
dans le fichier produit (`infection/3`). Un bloc ne crée aucune variable.

### `-`
Commence un objet : une déclaration, une règle, un peuplement. L'indentation n'a aucune
valeur ; c'est ce tiret qui fait la ligne.

### `//`
Commentaire, jusqu'à la fin de la ligne. Ignoré par le compilateur.

### `->`
Transforme : `A -> B` remplace l'agent `A` par `B`. Forme longue : `A reacts into B`.

### `<`
Héritage : `agent ActImmune < Immune` déclare `ActImmune` comme sous-type de `Immune`.

### `;`
Sépare la règle de ses actions. `- Cell -> Debris when finds Virus ; set IL2 0`.

### `[Couleur]`
Couleur facultative d'un agent ou d'un champ, entre crochets, après le nom :
`agent Cell [Lime]`. Sans couleur, NISMA en tire une dans la palette vive de Centyllion, en
évitant celles déjà prises.

## Mots, de A à Z

### `absorbs`
Influence d'un agent sur un champ : `Agent absorbs CHAMP VALEUR`. C'est le contraire de
`produces` — l'agent consomme ou repousse au lieu d'apporter.

### `abstract`
Déclare un type qui n'apparaît jamais sur la grille et sert seulement à grouper :
`abstract Immune < Cell`. Une règle écrite sur un type abstrait est distribuée à tous ses
descendants concrets.

### `add`
Action d'un modificateur : dépose des agents dans un rectangle.
Syntaxe : `add N AGENT in square (x1,y1)-(x2,y2)`.
Exemple : `- every 5 steps: add 3 Food in square (5,5)-(15,15)`.

### `after`
Sortie d'état après une durée : `dies after 6 steps`. Se combine avec `stays` pour exprimer
un séjour puis une disparition.

### `age`
Âge de l'agent courant, en pas depuis sa création ou depuis l'entrée dans son état.
S'emploie comme une grandeur : `age > 10`.

### `agent`
Déclare un type d'agent concret.
Syntaxe : `agent NOM [Couleur] [< PARENT] [with TAG] [mob p] [life n] [icon nom]
[size n] [walls] [desc "texte"]`.
Exemple : `agent Virus [Crimson] mob 1`.

### `and`
Conjonction de deux conditions. Synonyme : `&&`.

### `any`
Dans un comptage de voisins : au moins un voisin occupé, quel qu'il soit.
Exemple : `Cell dies when finds any`.

### `at step`
Dans un modificateur : événement à un pas précis. `at step 30` devient `step == 30`.

### `becomes`
Dans un processus : passage d'un état à un autre. `becomes Active` depuis l'état courant.
Une périodicité peut s'y ajouter : `becomes Active every 3 steps`.

### `capture`
Action : prend une image de la simulation. Utilisable dans un modificateur
(`- at step 40: capture`) ou après une règle.

### `desc`
Description courte d'un agent, en attribut : `agent Cell [Lime] desc "cellule cible"`.

### `description`
Description textuelle, dans la déclaration du modèle :
`- model "Immunité" description "Cellules infectées et réponse immune"`.

### `dies`
Disparition de l'agent. `Cell dies when finds Cell > 3`, ou `dies after 6 steps` dans un
état.

### `diff`
Vitesse de diffusion d'un champ, de 0 à 1 : `field IL2 diff 0.3 life 4 dirs 8`.

### `dirs`
Directions de diffusion d'un champ : `4` (orthogonales) ou `8` (avec les diagonales).

### `every`
Période : `every 3 steps` devient `step % 3 == 0`. S'emploie dans un modificateur, ou sur
une sortie d'état. Un effet (`produces`, `moves`) ne prend pas de période : le compilateur le
refuse et propose un modificateur.

### `everywhere`
Portée globale d'une condition de modificateur : `when IL2 < 0.01 everywhere: pause`
interroge la grille entière, pas le voisinage d'une cellule.

### `field`
Déclare un champ continu.
Syntaxe : `field NOM [Couleur] diff D life L dirs 4|8 [opacity N]`.
Exemple : `field Grass diff 0.2 life 6 dirs 4`.

### `finds`
Compte les voisins, ou désigne un ensemble de voisins. Ne compte jamais la case centrale.
`finds 3 Cell`, `finds Cell < 2`, `finds Cell`, `finds any`, `finds nothing`,
`finds only Cell, Virus`.

### `grid`
Taille de la grille : `grid 60 60` pour soixante colonnes et soixante lignes.

### `icon`
Forme de l'icône d'un agent, en attribut : `agent Cell icon circle`.

### `in`
Placement d'un agent sur une case : `RestImmune in (5,5)` en pose **un**. La forme avec
compteur (`10 RestImmune in (5,5)`) est refusée par le prototype ; pour un lot, c'est
`N Agent random`. Dans une action, `add N AGENT in square (x1,y1)-(x2,y2)` désigne un
rectangle.

### `into`
Seconde moitié de la forme longue de transformation : `A reacts into B`.

### `is`
Égalité, dans une condition : `finds Cell is 3` vaut `finds Cell == 3`.

### `lets`
Perméabilité d'un agent à un champ : `Agent lets CHAMP VALEUR`.

### `life`
Demi-vie. Pour un agent : nombre de pas avant de disparaître, en attribut. Pour un champ :
demi-vie de la valeur en pas : `field Grass diff 0.2 life 6 dirs 4`.

### `mob`
Probabilité de mouvement d'un agent, en attribut : `agent Virus [Crimson] mob 1`.

### `model`
Ouvre la déclaration du modèle et lui donne son nom :
`- model "Jeu de la vie" description "Automate cellulaire de Conway"`.

### `moves`
Effet de déplacement dans un état de processus : `moves 0.1`. Permanent tant que l'agent est
dans l'état.

### `near`
Valeur d'un champ chez un voisin. **Refusé** : non traduisible en `.nbd2`. Le compilateur
renvoie vers le niveau mécanique, où la formule s'écrit directement.

### `neighbour`
Sujet d'une règle : agit sur un voisin, pas sur la cellule courante.
`neighbour Virus -> Debris when finds Virus`.

### `not`
Négation d'une condition. Synonyme : `!`. Exemple : `Cell dies when not (finds Cell >= 2)`.

### `nothing`
Aucun voisin occupé : `Cell spawns when finds nothing`.

### `only`
Aucun type de voisin hors de la liste : `finds only Cell, Virus`.

### `opacity`
Opacité d'affichage d'un champ, en pourcentage : `field IL2 diff 0.3 life 4 dirs 8 opacity 40`.

### `or`
Disjonction de deux conditions. Synonyme : `||`.

### `p`
Probabilité d'une règle : en tête de puce (`- p 0.2`) ou en tête de bloc, où elle s'applique
à chaque règle du bloc, indépendamment.

### `pause`
Action : met la simulation en pause. Utilisable dans un modificateur :
`- when IL2 < 0.01 everywhere: pause`.

### `process`
Section de processus : `# process Cell`. Un seul processus par type.

### `produces`
Production d'un champ par un agent : `Cell produces IL2 1.0`. Permanent : aucun rythme.

### `random`
Placement au hasard dans `# start` : `- 800 Cell random` pose huit cents cellules sur des
cases libres. Le tirage suit `seed`, donc il est reproductible.

### `reacts`
Forme longue de la transformation : `A reacts into B`.

### `remembers`
Mémoire par agent. **Refusé** : une cellule de `.nbd2` ne connaît que son grain et son âge.

### `seed`
Graine du hasard : `seed 42`. Deux compilations de la même graine produisent le même
fichier, couleurs tirées comprises.

### `set`
Action sur la cellule courante : `set CHAMP VALEUR`. Exemple : `; set IL2 0`.

### `size`
Taille relative de l'icône d'un agent, en attribut : `agent Cell icon circle size 1.4`.

### `spawns`
Naissance : une cellule vide devient l'agent. `Cell spawns when finds 3 Cell`.

### `stays`
Durée de séjour dans un état : `stays 4 steps`. Se compile en `age >= 4` sur les sorties de
l'état, l'âge étant remis à zéro à l'entrée.

### `step`
Numéro du pas courant. S'emploie dans une condition (`step % 3 == 0`) et dans les
modificateurs (`at step 30`, `every 5 steps`).

### `tag`
Étiquette, qui n'est pas un type : `tag Active`. Un agent la porte avec
`agent ActImmune < Immune with Active`.

### `update`
Mode de mise à jour : `update parallel` (défaut) ou `update sequential`.

### `walls`
Bords imperméables pour un agent, au lieu du tore par défaut : `agent Debris [Grey] walls`.

### `waits`
Attente d'événement. **Refusé** : le moteur avance par pas, sans file d'échéances.
Synonyme refusé : `await`.

### `when`
Introduit la condition d'une règle ou d'une sortie d'état :
`- Cell dies when finds Cell > 3`.

### `with`
Introduit les tags d'un sous-type : `agent ActImmune < Immune with Active`.
