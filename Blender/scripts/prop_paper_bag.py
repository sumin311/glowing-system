"""
prop_paper_bag.py - 크래프트 종이봉투 (0.34 x 0.2 x 0.36 m, 끈 손잡이). 손님이 들고 나가는 포장 봉투.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(box("body", (0.34, 0.2, 0.36), (0, 0, 0.18), "M_Kraft", bevel=0.004))
    p.append(box("fold", (0.342, 0.202, 0.04), (0, 0, 0.34), "M_Kraft", bevel=0.003))
    p.append(box("print", (0.18, 0.002, 0.12), (0, -0.101, 0.18), "M_BoxRed", bevel=0))
    t = text_mesh("logo", "바삭치킨", size=0.04, extrude=0.001, mat="M_SignText")
    t.location = (0, -0.104, 0.18)
    p.append(t)
    for sy in (-1, 1):
        p.append(tube_path("handle", [(-0.07, sy * 0.06, 0.34), (-0.06, sy * 0.06, 0.43), (0.06, sy * 0.06, 0.43), (0.07, sy * 0.06, 0.34)], 0.005, "M_Kraft", segs=6))
    return join(p, "PaperBag")


if __name__ == "__main__":
    run_main(build, "Props", "PaperBag")
