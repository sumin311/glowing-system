"""
prop_batter_bowl.py - 튀김가루(반죽) 스테인리스 볼. 반죽 작업대(PrepStation) 위 소품.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    prof = [(0.0, 0.0), (0.08, 0.0), (0.15, 0.05), (0.19, 0.11), (0.2, 0.12), (0.192, 0.122), (0.183, 0.112), (0.143, 0.055), (0.075, 0.008), (0.0, 0.008)]
    p.append(lathe("bowl", prof, (0, 0, 0), "M_Stainless", segs=32, cap_top=False))
    flour = lathe("flour", [(0.0, 0.07), (0.17, 0.07), (0.13, 0.08), (0.0, 0.095)], (0, 0, 0), "M_Flour", segs=32)
    add_displace_noise(flour, strength=0.008, size=0.03, seed=3)
    p.append(flour)
    p.append(tube_path("whisk", [(0.05, 0.0, 0.09), (0.16, 0.08, 0.22), (0.2, 0.11, 0.27)], 0.008, "M_WoodDark"))
    return join(p, "BatterBowl")


if __name__ == "__main__":
    run_main(build, "Props", "BatterBowl")
