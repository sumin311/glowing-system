"""
shop_pendant_lamp.py - 천장 펜던트 조명. 원점 = 천장 고정점(위쪽). 아래로 0.9 m 늘어진다.
Unity 에서 천장 높이에 두고 자식 "Bulb" 위치에 Point Light 를 붙인다.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(cylinder("canopy", 0.06, 0.03, (0, 0, -0.015), "M_BlackPlastic", verts=20, bevel=0.005))
    p.append(cylinder("cord", 0.005, 0.7, (0, 0, -0.38), "M_BlackPlastic", verts=8))
    p.append(lathe("shade", [(0.02, -0.68), (0.04, -0.7), (0.07, -0.74), (0.16, -0.84), (0.2, -0.9), (0.195, -0.905), (0.155, -0.845), (0.065, -0.745), (0.0, -0.73)],
                   (0, 0, 0), "M_YellowPaint", segs=32, cap_top=False, cap_bottom=False))
    root = join(p, "PendantLamp")
    bulb = sphere("Bulb", 0.05, (0, 0, -0.86), "M_LightBulb", segs=16, rings=10)
    part([bulb], "Bulb", pivot=(0, 0, -0.86), parent=root)
    return root


if __name__ == "__main__":
    run_main(build, "Shop", "PendantLamp")
