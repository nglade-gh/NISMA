#!/usr/bin/env python3
"""Vérifie qu'un rythme régulier écrit dans un état est bien celui du modèle compilé.

Scénario : une cellule quiescente qui ne s'active que tous les 3 pas, et qui reste
active 2 pas. On rejoue le `.nbd2` avec la sémantique du moteur (âge, `step`,
transformation) et on mesure les écarts entre activations.

Usage : python3 verify_cycle.py <modele.nbd2>
"""

import json
import sys

sys.path.insert(0, __file__.rsplit("/", 1)[0])

from verify_process import step  # noqa: E402


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2

    model = json.load(open(sys.argv[1], encoding="utf-8"))
    grains = {g.get("id", 0): g["name"] for g in model["model"]["grains"]}
    behaviours = model["model"]["behaviours"]
    grain_index = set(grains)

    quiescent = next(gid for gid, name in grains.items() if name.endswith(".Quiescent"))
    active = next(gid for gid, name in grains.items() if name.endswith(".Active"))

    width = height = 12
    grid = [-1] * (width * height)
    ages = [-1] * (width * height)
    grid[5 * width + 5] = quiescent
    ages[5 * width + 5] = 0

    timeline = []
    for tick in range(1, 25):
        for i in range(len(ages)):
            if ages[i] >= 0:
                ages[i] += 1
        grid, ages = step(grid, ages, behaviours, width, height, grain_index, tick)
        timeline.append((tick, grid[5 * width + 5]))

    labels = {quiescent: "q", active: "A", -1: "."}
    print("chronologie :", " ".join(f"{tick}:{labels.get(state, '?')}" for tick, state in timeline))

    activations = [tick for i, (tick, state) in enumerate(timeline)
                   if state == active and (i == 0 or timeline[i - 1][1] != active)]
    print("activations aux pas :", activations)

    assert len(activations) >= 3, "pas assez d'activations pour mesurer un rythme"
    gaps = [b - a for a, b in zip(activations, activations[1:])]
    assert all(gap == 3 for gap in gaps), f"écarts {gaps} au lieu de 3"

    # durée = nombre de pas consécutifs à l'état actif depuis l'activation
    # la dernière activation touche l'horizon de simulation : sa durée est tronquée
    durations = []
    for start in activations[:-1]:
        length = 0
        for tick, state in timeline:
            if tick < start:
                continue
            if state != active:
                break
            length += 1
        durations.append(length)
    assert all(d == 2 for d in durations), f"durées {durations} au lieu de 2"

    print(f"rythme respecté : activations tous les 3 pas ({gaps}), durée 2 pas")
    return 0


if __name__ == "__main__":
    sys.exit(main())
