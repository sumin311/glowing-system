"""
shop_wall_shelf.py - 홀 벽 장식 선반 (1.2 m, 화분/병/액자 소품 포함). 원점 = 벽면 하단 중앙, 높이 0.
Unity 에서 벽에 붙이고 y=1.4 정도에 둔다.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    for k in range(2):
        z = k * 0.4
        p.append(box("plank%d" % k, (1.2, 0.25, 0.03), (0, -0.125, z), "M_WoodLight", bevel=0.006))
        for sx in (-1, 1):
            p.append(box("bracket", (0.02, 0.2, 0.12), (sx * 0.45, -0.1, z - 0.075), "M_BlackPlastic", bevel=0.003))
    # 소품
    p.append(lathe("pot", [(0, 0), (0.05, 0), (0.065, 0.11), (0.07, 0.12), (0, 0.12)], (-0.4, -0.12, 0.015), "M_Pot", segs=18))
    for i in range(6):
        leaf = sphere("leaf%d" % i, 0.06, (-0.4 + math.cos(i) * 0.04, -0.12 + math.sin(i) * 0.04, 0.17 + (i % 3) * 0.03), "M_Plant", segs=10, rings=6, scale=(1.0, 0.5, 0.35))
        leaf.rotation_euler = (0.3 * i, 0.5, i)
        p.append(leaf)
    for i in range(3):
        p.append(lathe("bottle%d" % i, [(0, 0), (0.035, 0), (0.035, 0.18), (0.012, 0.24), (0.012, 0.27), (0, 0.27)], (0.15 + i * 0.09, -0.12, 0.015), "M_Cola" if i != 1 else "M_Glass", segs=14))
    p.append(box("frame", (0.3, 0.025, 0.22), (0.1, -0.03, 0.53), "M_WoodDark", bevel=0.005))
    p.append(box("photo", (0.24, 0.005, 0.16), (0.1, -0.044, 0.53), "M_YellowPaint", bevel=0))
    p.append(box("books", (0.25, 0.16, 0.2), (-0.35, -0.12, 0.5), "M_CrateBlue", bevel=0.006))
    return join(p, "WallShelf")


if __name__ == "__main__":
    run_main(build, "Shop", "WallShelf")
