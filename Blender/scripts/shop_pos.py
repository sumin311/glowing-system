"""
shop_pos.py - 카운터 포스(POS) 계산대. 화면이 직원 쪽(+Y)을 향한다 (카운터 뒤에서 사용).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(box("drawer", (0.42, 0.42, 0.1), (0, 0, 0.05), "M_BlackPlastic", bevel=0.01))
    p.append(box("drawer_front", (0.38, 0.01, 0.06), (0, 0.21, 0.05), "M_StainlessDark", bevel=0.003))
    p.append(cylinder("stand", 0.025, 0.12, (0, -0.05, 0.16), "M_StainlessDark", verts=14))
    scr = []
    scr.append(box("housing", (0.36, 0.04, 0.26), (0, 0, 0), "M_BlackPlastic", bevel=0.01))
    scr.append(box("screen", (0.32, 0.005, 0.21), (0, 0.021, 0.0), "M_Screen", bevel=0))
    scr.append(box("ui", (0.1, 0.002, 0.05), (-0.08, 0.024, 0.05), "M_YellowPaint", bevel=0))
    scr.append(box("ui2", (0.26, 0.002, 0.04), (0, 0.024, -0.06), "M_CrateGreen", bevel=0))
    s = join(scr, "scr")
    s.rotation_euler = (math.radians(20), 0, 0)
    s.location = (0, -0.05, 0.33)
    p.append(s)
    p.append(box("card_reader", (0.08, 0.14, 0.04), (0.17, -0.12, 0.12), "M_WhitePlastic", bevel=0.008))
    return join(p, "PosRegister")


if __name__ == "__main__":
    run_main(build, "Shop", "PosRegister")
