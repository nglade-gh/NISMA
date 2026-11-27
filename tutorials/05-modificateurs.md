# 5. Modificateurs : agir sur la simulation entière

Tout ce qui précède se passe dans une cellule : une règle examine ses voisines et décide de
son sort. Un modificateur, lui, n'appartient à aucune cellule. Il s'exécute une fois par pas,
au-dessus de la grille, et peut apporter de la matière, mettre en pause ou prendre une
capture.

## Le script

```ni
# declare
- model "Apports" description "Un apport régulier de nourriture"
- grid 60 60
- seed 9
- agent Food [Gold]
- agent Grazer [Chocolate]

# modifiers
- every 5 steps: add 3 Food in square (5,5)-(15,15)
- at step 40: capture

# model
## pâturage
- Grazer spawns when finds Food >= 2
- Grazer dies when finds Food < 1

# start
- 60 Grazer random
```

## Une troisième section, hors de la biologie

`# modifiers` vient après `# declare` et avant `# model` : l'ordre des sections est imposé,
comme les déclarations d'un programme Pascal — d'abord ce qui existe, puis ce qui pilote la
simulation, puis la biologie. Chaque ligne décrit un événement daté, dans la même langue que
le reste : une période ou un instant, puis une action.

`every 5 steps` devient `step % 5 == 0`, `at step 40` devient `step == 40`. Là encore, rien
de nouveau dans le fichier produit : le `.nbd2` ne connaît que des conditions sur le pas.

`add 3 Food in square (5,5)-(15,15)` dépose trois agents `Food` dans le rectangle, sur des
cases libres, tirées au hasard.

`capture` prend une image de la simulation : elle se retrouve dans le dossier de captures de
Centyllion, avec l'horodatage de la simulation.

## L'autre forme : une condition globale

Un modificateur peut aussi observer toute la grille avant d'agir :

```
# modifiers
- when IL2 < 0.01 everywhere: pause
```

`everywhere` interroge la grille entière, pas le voisinage d'une cellule. La simulation
s'arrête d'elle-même quand le champ est retombé.

## Ce qui distingue un modificateur d'une règle

- une règle a un sujet : une cellule, un voisin, un type ;
- un modificateur n'a pas de sujet : il est daté, ou conditionné par un état global ;
- une règle ne peut pas ajouter d'agents dans un rectangle ; un modificateur, si.

## À toi

1. Compile : le résumé annonce un modificateur.
2. Remplace `every 5 steps` par `every 1 steps` : l'apport devient continu, les brouteurs
   prolifèrent.
3. Change le rectangle en `(0,0)-(59,59)` : l'apport couvre toute la grille.
4. Ajoute `- at step 1: pause` : la simulation s'arrête au premier pas. Pratique pour
   vérifier l'état initial.

## Erreurs fréquentes

- **Écrire une action de cellule dans un modificateur** : `set CHAMP VALEUR` vise la cellule
  courante ; un modificateur n'en a pas. Utilisez `add`, `pause`, `capture`.
- **Un rectangle hors grille** : les coordonnées dépassent `grid` ; rien n'est ajouté, et le
  compilateur vous prévient.
- **Confondre `every N steps` et `stays N steps`** : le premier est une période de la
  simulation, le second une durée passée dans un état.

## Ce qu'il faut retenir

- `# modifiers` décrit des événements globaux, datés ou conditionnés.
- `every N steps` et `at step N` sont traduits en conditions sur le pas courant.
- Un modificateur n'a pas de cellule : il ne peut pas lire ni écrire un champ local.
