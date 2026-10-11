"""
shop_chair.py - 원목 의자 (좌판 높이 0.45 m). 등받이가 +Y 쪽 (정면 -Y 로 앉음).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(box("seat", (0.42, 0.42, 0.04), (0, 0, 0.45), "M_WoodLight", bevel=0.01, segments=3))
    p.append(box("cushion", (0.38, 0.36, 0.03), (0, -0.01, 0.485), "M_Fabric", bevel=0.012, segments=3))
    for sx in (-1, 1):
        for sy in (-1, 1):
            h = 0.43 if sy < 0 else 0.92
            p.append(box("leg", (0.035, 0.035, h), (sx * 0.18, sy * 0.18, h / 2), "M_WoodDark", bevel=0.006))
    for k in range(3):
        p.append(box("back%d" % k, (0.36, 0.022, 0.06), (0, 0.18, 0.62 + k * 0.11), "M_WoodLight", bevel=0.006))
    for sx in (-1, 1):
        p.append(box("rung", (0.02, 0.36, 0.02), (sx * 0.18, 0, 0.15), "M_WoodDark", bevel=0.003))
    return join(p, "Chair")


if __name__ == "__main__":
    run_main(build, "Shop", "Chair")
