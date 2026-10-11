"""
shop_open_sign.py - 영업 시작/마감 팻말 (스탠드형, 높이 1.2 m).
Unity 의 ShopOpenSign(상호작용) 이 자식 "Board" 를 회전시켜 OPEN/CLOSED 면을 보여준다.
Board 앞면(-Y) = OPEN(초록), 뒷면(+Y) = CLOSED(빨강). 글자는 Unity TextMesh 로 표시.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(cylinder("base", 0.18, 0.04, (0, 0, 0.02), "M_BlackPlastic", verts=28, bevel=0.01))
    p.append(cylinder("pole", 0.02, 0.95, (0, 0, 0.5), "M_StainlessDark", verts=14))
    p.append(sphere("cap", 0.03, (0, 0, 0.98), "M_StainlessDark", segs=14, rings=8))
    root = join(p, "OpenSign")
    b = []
    b.append(box("frame", (0.5, 0.04, 0.34), (0, 0, 1.15), "M_WoodDark", bevel=0.01))
    b.append(box("open_face", (0.44, 0.006, 0.28), (0, -0.022, 1.15), "M_CrateGreen", bevel=0.003))
    b.append(box("closed_face", (0.44, 0.006, 0.28), (0, 0.022, 1.15), "M_LabelRed", bevel=0.003))
    part(b, "Board", pivot=(0, 0, 1.15), parent=root)
    return root


if __name__ == "__main__":
    run_main(build, "Shop", "OpenSign")
