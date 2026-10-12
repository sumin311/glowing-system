"""
prop_tongs.py - 스테인리스 집게 (0.3 m). 작업대 장식 소품.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    for sx in (-1, 1):
        p.append(box("arm", (0.018, 0.26, 0.004), (sx * 0.018, 0, 0.012), "M_Stainless", bevel=0.001, rot=(0, 0, sx * 4)))
        p.append(box("tip", (0.024, 0.04, 0.006), (sx * 0.028, -0.135, 0.01), "M_Stainless", bevel=0.001))
        p.append(box("grip", (0.02, 0.08, 0.008), (sx * 0.011, 0.1, 0.013), "M_RedPlastic", bevel=0.002))
    p.append(cylinder("hinge", 0.012, 0.03, (0, 0.13, 0.012), "M_StainlessDark", verts=12, rot=(0, 90, 0)))
    return join(p, "Tongs")


if __name__ == "__main__":
    run_main(build, "Props", "Tongs")
