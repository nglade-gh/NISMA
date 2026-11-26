#!/usr/bin/env python3
"""Vérifie qu'un modèle compilé depuis un `# process` se comporte comme le script le dit.

Le test ne fait pas confiance au compilateur : il lit le `.nbd2`, en extrait les grains,
les comportements et leurs prédicats, puis rejoue un petit scénario avec la sémantique
documentée du moteur (âge incrémenté par pas, transformation, âge du produit selon
`sourceReactive`).

Scénario : une cellule saine voisine d'un virus. On attend
  - infection au premier pas,
  - passage à l'état malade 4 pas plus tard,
  - mort 6 pas après.

Usage : python3 verify_process.py <modele.nbd2>
"""

import json
import re
import sys


def age_for_source(source, current_age, ages, neighbours):
    """Reproduit ageForSource de simulator.kt."""
    if source is None or source < 0:
        return 0
    if source == 0:
        return current_age
    index = source - 1
    if index < len(neighbours):
        return ages[neighbours[index]]
    return 0


def parse_formula(formula, cell, grid, ages, width, height, grain_index):
    """Évalue les formules simples du modèle : count(id), age, sums, parenthèses."""
    x, y = cell
    counts = {gid: 0 for gid in grain_index}
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            nx, ny = x + dx, y + dy
            if not (0 <= nx < width and 0 <= ny < height):
                continue
            agent = grid[ny * width + nx]
            if agent >= 0:
                counts[agent] = counts.get(agent, 0) + 1

    text = formula
    text = re.sub(r"count\(\s*(-?\d+)\s*\)",
                  lambda m: str(sum(counts.values()) if int(m.group(1)) < 0 else counts.get(int(m.group(1)), 0)),
                  text)
    text = re.sub(r"\bage\b", str(ages[y * width + x]), text)
    text = text.replace("&&", " and ").replace("||", " or ")
    text = re.sub(r"!(?!=)", " not ", text)
    return bool(eval(text, {"__builtins__": {}}, {}))


def holds(behaviour, cell, grid, ages, width, height, grain_index):
    x, y = cell
    age = ages[y * width + x]

    predicate = behaviour.get("agePredicate")
    if predicate and not compare(age, predicate["op"], predicate["constant"]):
        return False

    counts = None
    for count in behaviour.get("agentCountPredicates") or []:
        if counts is None:
            counts = neighbour_counts(cell, grid, width, height)
        actual = counts.get(count["grainId"], 0)
        if not compare(actual, count["op"], count["count"]):
            return False

    if behaviour.get("formula"):
        return parse_formula(behaviour["formula"], cell, grid, ages, width, height, grain_index)

    return True


def neighbour_counts(cell, grid, width, height):
    x, y = cell
    counts = {}
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            nx, ny = x + dx, y + dy
            if 0 <= nx < width and 0 <= ny < height:
                agent = grid[ny * width + nx]
                if agent >= 0:
                    counts[agent] = counts.get(agent, 0) + 1
    return counts


def compare(actual, op, expected):
    return {
        "Equals": actual == expected,
        "NotEquals": actual != expected,
        "LessThan": actual < expected,
        "LessThanOrEquals": actual <= expected,
        "GreaterThan": actual > expected,
        "GreaterThanOrEquals": actual >= expected,
    }[op]


def step(grid, ages, behaviours, width, height, grain_index):
    birth = list(grid)
    birth_age = list(ages)

    for y in range(height):
        for x in range(width):
            index = y * width + x
            agent = grid[index]

            for behaviour in behaviours:
                main = behaviour.get("mainReactiveId")
                product = behaviour.get("mainProductId")

                if main is None:
                    # génération spontanée : la case doit être vide
                    if agent >= 0 or product is None:
                        continue
                    if holds(behaviour, (x, y), grid, ages, width, height, grain_index):
                        birth[index] = product
                        birth_age[index] = 0
                    continue

                if agent != main:
                    continue
                if not holds(behaviour, (x, y), grid, ages, width, height, grain_index):
                    continue

                source = behaviour.get("sourceReactive")
                if product is None:
                    birth[index] = -1
                    birth_age[index] = -1
                elif product != main:
                    birth[index] = product
                    birth_age[index] = age_for_source(source, ages[index], ages, [])

                # lignes de réaction : un voisin est transformé, le principal reste
                for reaction in behaviour.get("reaction") or []:
                    reactive = reaction.get("reactiveId")
                    produced = reaction.get("productId")
                    if reactive is None or produced is None:
                        continue
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            if dx == 0 and dy == 0:
                                continue
                            nx, ny = x + dx, y + dy
                            if not (0 <= nx < width and 0 <= ny < height):
                                continue
                            nindex = ny * width + nx
                            if grid[nindex] == reactive and birth[nindex] == reactive:
                                birth[nindex] = produced
                                birth_age[nindex] = age_for_source(reaction.get("sourceReactive"),
                                                                   ages[nindex], ages, [])
                                break
                        else:
                            continue
                        break

    return birth, birth_age


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2

    model = json.load(open(sys.argv[1], encoding="utf-8"))
    grains = {g.get("id", 0): g["name"] for g in model["model"]["grains"]}
    behaviours = model["model"]["behaviours"]
    grain_index = {gid for gid in grains}

    healthy = next(gid for gid, name in grains.items() if name.endswith(".Healthy"))
    infected = next(gid for gid, name in grains.items() if name.endswith(".Infected"))
    sick = next(gid for gid, name in grains.items() if name.endswith(".Sick"))
    virus = next(gid for gid, name in grains.items() if name == "Virus")

    width = height = 12
    grid = [-1] * (width * height)
    ages = [-1] * (width * height)
    grid[5 * width + 5] = healthy
    ages[5 * width + 5] = 0
    grid[5 * width + 6] = virus
    ages[5 * width + 6] = 0

    timeline = {healthy: "healthy", infected: "infected", sick: "sick"}
    seen = []
    for tick in range(1, 20):
        for i in range(len(ages)):
            if ages[i] >= 0:
                ages[i] += 1
        grid, ages = step(grid, ages, behaviours, width, height, grain_index)
        state = timeline.get(grid[5 * width + 5], "empty")
        seen.append((tick, state))

    print("chronologie au centre :", " ".join(f"{t}:{s[:3]}" for t, s in seen))

    infected_at = next((t for t, s in seen if s == "infected"), None)
    sick_at = next((t for t, s in seen if s == "sick"), None)
    empty_at = next((t for t, s in seen if s == "empty"), None)

    print(f"infection: pas {infected_at} | malade: pas {sick_at} | mort: pas {empty_at}")
    assert infected_at is not None, "la cellule n'a jamais été infectée"
    assert sick_at is not None, "la cellule n'est jamais devenue malade"
    assert empty_at is not None, "la cellule n'est jamais morte"
    assert sick_at - infected_at == 4, f"séjour infecté : {sick_at - infected_at} pas au lieu de 4"
    assert empty_at - sick_at == 6, f"séjour malade : {empty_at - sick_at} pas au lieu de 6"
    print("durées respectées : 4 pas infecté, 6 pas malade")
    return 0


if __name__ == "__main__":
    sys.exit(main())
