"""
shop_plant.py - 홀 장식용 대형 화분 (높이 1.2 m).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    random.seed(7)
    p = []
    p.append(lathe("pot", [(0, 0), (0.16, 0), (0.2, 0.36), (0.21, 0.38), (0.19, 0.38), (0, 0.36)], (0, 0, 0), "M_Pot", segs=24))
    p.append(cylinder("soil", 0.185, 0.02, (0, 0, 0.36), "M_WoodDark", verts=20))
    for i in range(14):
        a = i * 2.4
        r = 0.05 + (i % 4) * 0.03
        z = 0.5 + (i % 5) * 0.13
        stem = tube_path("stem%d" % i, [(0, 0, 0.36), (math.cos(a) * r * 0.5, math.sin(a) * r * 0.5, z * 0.8), (math.cos(a) * r, math.sin(a) * r, z)], 0.008, "M_Plant", segs=6)
        p.append(stem)
        leaf = sphere("leaf%d" % i, 0.13, (math.cos(a) * (r + 0.08), math.sin(a) * (r + 0.08), z + 0.03), "M_Plant", segs=12, rings=8, scale=(1.0, 0.45, 0.12))
        leaf.rotation_euler = (random.uniform(-0.5, 0.5), random.uniform(-0.6, -0.2), a)
        p.append(leaf)
    return join(p, "Plant")


if __name__ == "__main__":
    run_main(build, "Shop", "Plant")
