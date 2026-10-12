"""
prop_trash_bin.py - 페달식 주방 쓰레기통 (높이 0.7 m). TrashBin(실패한 음식 폐기) 상호작용 대상.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(lathe("body", [(0.0, 0.0), (0.17, 0.0), (0.19, 0.6), (0.0, 0.6)], (0, 0, 0.02), "M_StainlessDark", segs=28))
    p.append(lathe("lid", [(0.0, 0.62), (0.2, 0.62), (0.2, 0.64), (0.17, 0.68), (0.0, 0.69)], (0, 0, 0), "M_Stainless", segs=28))
    p.append(cylinder("ring", 0.18, 0.02, (0, 0, 0.01), "M_Rubber", verts=28))
    p.append(box("pedal", (0.12, 0.08, 0.02), (0, -0.2, 0.03), "M_BlackPlastic", bevel=0.005))
    p.append(box("sticker", (0.12, 0.004, 0.06), (0, -0.183, 0.4), "M_YellowPaint", bevel=0, rot=(-2, 0, 0)))
    return join(p, "TrashBin")


if __name__ == "__main__":
    run_main(build, "Props", "TrashBin")
