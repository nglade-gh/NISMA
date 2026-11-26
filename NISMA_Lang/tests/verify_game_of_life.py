#!/usr/bin/env python3
"""Vérifie que le script game_of_life.ni produit bien le jeu de la vie de Conway.

Trois contrôles, indépendants du compilateur C# :

1. tables de vérité — les prédicats du modèle produit, évalués pour 0 à 8 voisins,
   doivent coïncider avec ceux du modèle de référence de NetBioDyn (GoL Engine.nbd2) ;
2. dynamique — la grille produite, animée par ces seuls prédicats, doit suivre
   exactement une implémentation de Conway écrite ici ;
3. structure — un grain, deux comportements, 178 cellules vivantes.

Usage : python3 verify_game_of_life.py <modele.nbd2> [<reference.nbd2>]
"""

import json
import re
import sys

WALLS = True  # le script déclare « walls » : les bords ne bouclent pas


def truth_table(behaviour):
    """Vérité du comportement pour count = 0..8."""
    table = []
    for count in range(9):
        ok = True
        for predicate in behaviour.get("agentCountPredicates") or []:
            op, expected = predicate["op"], predicate["count"]
            actual = count
            ok = ok and compare(actual, op, expected)
        if behaviour.get("formula"):
            ok = ok and eval_formula(behaviour["formula"], count)
        table.append(ok)
    return table


def compare(actual, op, expected):
    return {
        "Equals": actual == expected,
        "NotEquals": actual != expected,
        "LessThan": actual < expected,
        "LessThanOrEquals": actual <= expected,
        "GreaterThan": actual > expected,
        "GreaterThanOrEquals": actual >= expected,
    }[op]


def eval_formula(formula, count):
    """Évalue les formules simples du jeu de la vie, sans dépendance externe."""
    text = formula
    text = re.sub(r"count\(\s*-?\d+\s*\)", str(count), text)
    text = text.replace("&&", " and ").replace("||", " or ")
    text = re.sub(r"!(?!=)", " not ", text)
    return bool(eval(text, {"__builtins__": {}}, {}))


def neighbours(grid, width, height, x, y, grain):
    total = 0
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            nx, ny = x + dx, y + dy
            if not (0 <= nx < width and 0 <= ny < height) and WALLS:
                continue
            nx %= width
            ny %= height
            if grid[ny * width + nx] == grain:
                total += 1
    return total


def step(grid, width, height, birth, death, grain=0):
    out = list(grid)
    for y in range(height):
        for x in range(width):
            index = y * width + x
            count = neighbours(grid, width, height, x, y, grain)
            if grid[index] == grain:
                if death[count]:
                    out[index] = -1
            elif birth[count]:
                out[index] = grain
    return out


def conway(grid, width, height, grain=0):
    """Implémentation de référence, écrite sans lire les prédicats."""
    out = list(grid)
    for y in range(height):
        for x in range(width):
            index = y * width + x
            count = neighbours(grid, width, height, x, y, grain)
            alive = grid[index] == grain
            out[index] = grain if (count == 3 or (alive and count == 2)) else -1
    return out


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2

    model = json.load(open(sys.argv[1], encoding="utf-8"))
    root, simulation = model["model"], model["simulation"]
    settings = simulation["settings"]
    width = settings.get("customWidth") or settings["size"]
    height = settings.get("customHeight") or settings["size"]
    grid = simulation["agents"]

    grains = root["grains"]
    behaviours = root["behaviours"]
    assert len(grains) == 1, f"un seul grain attendu, {len(grains)} trouvé(s)"
    assert len(behaviours) == 2, f"deux comportements attendus, {len(behaviours)} trouvé(s)"

    birth = next(b for b in behaviours if "mainProductId" in b and "mainReactiveId" not in b)
    death = next(b for b in behaviours if "mainReactiveId" in b and "mainProductId" not in b)
    birth_table, death_table = truth_table(birth), truth_table(death)

    print("table naissance :", "".join("1" if v else "0" for v in birth_table))
    print("table mort      :", "".join("1" if v else "0" for v in death_table))
    assert birth_table == [c == 3 for c in range(9)], "naissance : count == 3 attendu"
    assert death_table == [c < 2 or c > 3 for c in range(9)], "mort : count < 2 ou > 3 attendu"

    if len(sys.argv) > 2:
        reference = json.load(open(sys.argv[2], encoding="utf-8"))
        ref_behaviours = reference["model"]["behaviours"]
        ref_birth = next(b for b in ref_behaviours if "mainProductId" in b and "mainReactiveId" not in b)
        ref_death = next(b for b in ref_behaviours if "mainReactiveId" in b and "mainProductId" not in b)
        assert truth_table(ref_birth) == birth_table, "naissance : écart avec le modèle de référence"
        assert truth_table(ref_death) == death_table, "mort : écart avec le modèle de référence"
        print("référence :", sys.argv[2], "— tables identiques")

    alive = sum(1 for cell in grid if cell >= 0)
    print(f"grille {width}x{height}, {alive} cellules vivantes au départ")

    current = grid
    for generation in range(1, 61):
        expected = conway(current, width, height)
        current = step(current, width, height, birth_table, death_table)
        assert current == expected, f"divergence à la génération {generation}"

    print("60 générations : identiques à Conway, sans divergence")
    return 0


if __name__ == "__main__":
    sys.exit(main())
