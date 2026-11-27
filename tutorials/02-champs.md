# 2. Champs : quelque chose qui diffuse

Un agent est une cellule discrète : elle est là ou elle n'est pas. Un champ est une grandeur
continue posée sur toute la grille — une concentration, une odeur, une ressource. NISMA
déclare les deux de la même façon, dans `# declare`.

## Le script

```ni
# declare
- model "Herbe" description "Un champ qui pousse, diffuse et se broute"
- grid 60 60
- seed 11

- agent Sheep [White]
- field Grass diff 0.2 life 6 dirs 4
- Sheep produces Grass 0.4

# model
## pâturage
- Sheep spawns when finds Sheep >= 2 and Grass > 0.6
- Sheep dies when finds Sheep >= 4 or Grass < 0.1

# start
- 200 Sheep random
```

## Les quatre nombres d'un champ

`field Grass diff 0.2 life 6 dirs 4` :

- `diff 0.2` — vitesse de diffusion : la quantité s'étale vers les cases voisines ;
- `life 6` — demi-vie en pas de temps : sans production, la valeur retombe de moitié tous
  les six pas ;
- `dirs 4` — diffusion dans les quatre directions, ou `8` en incluant les diagonales ;
- la couleur est facultative, comme pour un agent.

`Sheep produces Grass 0.4` : chaque mouton dépose 0,4 unité de champ sur sa propre case à
chaque pas. C'est la source qui compense la demi-vie.

## Se servir d'un champ dans une condition

`Grass > 0.6` se lit comme une grandeur ordinaire : le nom du champ, un opérateur, une
valeur. On peut mélanger un décompte de voisins et une valeur de champ dans la même
condition :

```
- Sheep spawns when finds Sheep >= 2 and Grass > 0.6
```

Un champ ne se déclare pas dans `# start` : il n'a pas de position. Sa valeur initiale est
zéro partout, et ce sont les agents qui l'alimentent.

## À toi

1. Compile et regarde le résumé : deux grains, un champ.
2. Monte `life` à 30 : l'herbe persiste, les moutons pullulent. Descends-le à 2 : ils
   meurent de faim.
3. Passe `dirs 4` à `dirs 8` et observe la forme des taches d'herbe.
4. Retire la ligne `Sheep produces Grass 0.4` : le champ reste à zéro, plus aucune naissance.

## Erreurs fréquentes

- **Utiliser un champ comme un agent** : `finds Grass` n'a pas de sens — un champ n'a pas
  d'occupants, seulement une valeur. Écrivez `Grass > 0.5`.
- **Oublier `produces`** : le champ existe, mais rien ne l'alimente ; il reste nul.
- `life 0` : sans demi-vie, la valeur ne décroît jamais. C'est parfois voulu, rarement
  utile.

## Ce qu'il faut retenir

- Un champ est une grandeur continue, déclarée avec `diff`, `life` et `dirs`.
- Les agents l'alimentent avec `produces`.
- Il s'utilise dans les conditions comme n'importe quelle valeur.
