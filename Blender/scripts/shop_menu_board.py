"""
shop_menu_board.py - 카운터 위 메뉴판 (2.4 x 0.8 m). 원점 = 하단 중앙 벽면.
메뉴명/가격은 Unity 의 MenuBoardDisplay 가 TextMesh 로 표시(해금 상태 반영).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(box("frame", (2.4, 0.06, 0.8), (0, -0.03, 0.4), "M_WoodDark", bevel=0.015, segments=3))
    p.append(box("panel", (2.28, 0.01, 0.68), (0, -0.064, 0.4), "M_Pants", bevel=0.004))
    t = text_mesh("title", "MENU", size=0.1, extrude=0.006, font_file="NanumSquareRoundB.ttf", mat="M_YellowPaint")
    t.location = (0, -0.072, 0.68)
    p.append(t)
    for i in range(3):
        p.append(box("line%d" % i, (0.62, 0.004, 0.006), (-0.75 + i * 0.75, -0.07, 0.6), "M_SignText", bevel=0))
    return join(p, "MenuBoard")


if __name__ == "__main__":
    run_main(build, "Shop", "MenuBoard")
