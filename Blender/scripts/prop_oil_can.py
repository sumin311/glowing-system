"""
prop_oil_can.py - 18L 식용유 말통 (0.24 x 0.24 x 0.35 m). 기름 재고 표시/장식.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(box("can", (0.24, 0.24, 0.34), (0, 0, 0.17), "M_Stainless", bevel=0.01, segments=3))
    p.append(box("label", (0.242, 0.242, 0.16), (0, 0, 0.16), "M_YellowPaint", bevel=0.008))
    t = text_mesh("txt", "식용유", size=0.05, extrude=0.002, mat="M_LabelRed")
    t.location = (0, -0.123, 0.17)
    p.append(t)
    p.append(cylinder("cap", 0.022, 0.03, (0.06, 0.06, 0.355), "M_RedPlastic", verts=16, bevel=0.003))
    p.append(tube_path("handle", [(-0.06, -0.02, 0.34), (-0.05, -0.02, 0.39), (0.03, -0.02, 0.39), (0.04, -0.02, 0.34)], 0.007, "M_BlackPlastic"))
    return join(p, "OilCan")


if __name__ == "__main__":
    run_main(build, "Props", "OilCan")
