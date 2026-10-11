"""
prop_chicken_box.py - 치킨 포장 상자 (0.3 x 0.3 x 0.1 m, 흰색 + 빨간 띠 + 로고).
포장 완료(Packed) 아이템의 모델. 원점 = 바닥 중앙.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa
from shop_sign import drumstick  # noqa: E402


def build():
    p = []
    p.append(box("base", (0.3, 0.3, 0.085), (0, 0, 0.0425), "M_BoxWhite", bevel=0.004))
    p.append(box("lid", (0.306, 0.306, 0.02), (0, 0, 0.095), "M_BoxWhite", bevel=0.004))
    # 빨간 띠 (뚜껑 + 옆면)
    p.append(box("band_top", (0.08, 0.308, 0.002), (0, 0, 0.106), "M_BoxRed", bevel=0))
    p.append(box("band_front", (0.08, 0.002, 0.1), (0, -0.154, 0.055), "M_BoxRed", bevel=0))
    p.append(box("band_back", (0.08, 0.002, 0.1), (0, 0.154, 0.055), "M_BoxRed", bevel=0))
    # 뚜껑 로고 (위에서 보이도록 눕힌 글자)
    t = text_mesh("logo", "바삭치킨", size=0.026, extrude=0.001, mat="M_BoxRed")
    t.rotation_euler = (0, 0, math.radians(0))  # 위에서 정면(-Y) 쪽에서 읽히도록 눕힘
    t.location = (-0.097, -0.06, 0.106)
    apply_transform(t)
    p.append(t)
    # 측면 통풍구
    for sx in (-1, 1):
        for k in range(3):
            p.append(box("vent", (0.002, 0.04, 0.008), (sx * 0.151, -0.06 + k * 0.06, 0.06), "M_Kraft", bevel=0))
    return join(p, "ChickenBox")


if __name__ == "__main__":
    run_main(build, "Props", "ChickenBox")
