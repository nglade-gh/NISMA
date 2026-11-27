# 3. Types abstraits et tags

Un modèle grossit vite en familles d'agents : des cellules saines, infectées, mortes ; des
lymphocytes au repos et activés. NISMA permet de nommer la famille, d'écrire les règles une
seule fois, et de laisser le compilateur les distribuer aux membres.

## Le script

```ni
# declare
- model "Immunité" description "Cellules infectées et réponse immune"
- grid 60 60
- seed 7

- agent Cell [Lime]
- agent Virus [Crimson] mob 1
- agent Debris [Grey]

- abstract Immune < Cell
- agent RestImmune < Immune
- agent ActImmune < Immune with Active
- tag Active

- field IL2 diff 0.3 life 4 dirs 8
- Cell produces IL2 1.0

# model
## activation
- Immune -> ActImmune when IL2 > 1

## infection
- neighbour Virus -> Debris when finds Virus
  ; set IL2 0
- Cell -> Debris when finds Virus >= 3 and finds Cell < 2

# start
- 800 Cell random
- 40 Virus random
- 20 RestImmune random
```

## Deux choses différentes

`abstract Immune < Cell` déclare un type **abstrait** : il n'apparaît jamais sur la grille,
mais il peut porter des règles. `agent RestImmune < Immune` déclare un type concret, membre
de la famille. Écrire une règle sur `Immune` revient à l'écrire sur chacun de ses descendants
concrets : ici, sur `RestImmune` et `ActImmune`.

`tag Active` est une **étiquette**, pas un type. `agent ActImmune < Immune with Active` dit
qu'un `ActImmune` porte l'étiquette `Active`. Une étiquette traverse la hiérarchie : elle
sert à désigner un état partagé par des agents de familles différentes, sans créer de type.

## Le polymorphisme, sans mécanisme nouveau

```
- Immune -> ActImmune when IL2 > 1
```

`Immune` désigne deux types concrets. Le compilateur écrit donc deux règles : une pour
`RestImmune`, une pour `ActImmune`, chacune avec sa propre ligne de réaction. Rien
n'apparaît dans le fichier produit qui ne soit un comportement ordinaire de NetBioDyn — et
si un produit devenait ambigu, la compilation échouerait au lieu de choisir à votre place.

## Les actions

`; set IL2 0` suit une règle et s'exécute quand elle se déclenche : la cellule courante met
son champ `IL2` à zéro. Les actions viennent après le point-virgule, sur la même ligne ou en
retrait.

`neighbour Virus -> Debris` : le sujet est le **voisin** `Virus`, pas la cellule courante.
La règle transforme un virus voisin en débris.

## À toi

1. Compile et compte : le résumé annonce six comportements pour quatre lignes de règles.
   C'est l'expansion des types abstraits.
2. Ajoute `- agent Memory < Immune with Active` : la règle `Immune -> ActImmune` s'étend
   d'elle-même au nouveau type.
3. Remplace `finds Virus` par `finds Virus >= 2` et observe la vitesse de contagion.

## Erreurs fréquentes

- **Déclarer un agent sous un type abstrait sans héritier concret** : la famille n'a aucun
  membre, la règle ne produit rien.
- **Confondre tag et type** : `finds Active` ne compte pas des agents — un tag ne se compte
  pas, il se teste sur un agent précis.
- **Espérer un ordre de priorité** : les règles d'un même bloc sont indépendantes, l'ordre
  d'écriture ne les hiérarchise pas.

## Ce qu'il faut retenir

- `abstract` nomme une famille ; les agents concrets en héritent avec `<`.
- Une règle sur un type abstrait est distribuée à ses descendants.
- Un tag étiquette un état, il ne crée pas de type.
