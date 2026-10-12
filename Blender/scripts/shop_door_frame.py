"""
shop_door_frame.py - 출입문 프레임 (개구부 1.4 x 2.2 m, 열린 유리문 2짝). 원점 = 문턱 중앙.
손님이 지나가는 통로이므로 문짝은 바깥쪽으로 열린 상태로 고정되어 있다.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build(w=1.4, h=2.2):
    p = []
    t = 0.08
    p.append(box("left", (t, 0.2, h), (-w / 2 - t / 2, 0, h / 2), "M_WoodDark", bevel=0.008))
    p.append(box("right", (t, 0.2, h), (w / 2 + t / 2, 0, h / 2), "M_WoodDark", bevel=0.008))
    p.append(box("head", (w + 2 * t, 0.2, 0.12), (0, 0, h + 0.06), "M_WoodDark", bevel=0.008))
    p.append(box("threshold", (w, 0.24, 0.015), (0, 0, 0.0075), "M_StainlessDark", bevel=0.003))
    # 열린 문짝 (바깥쪽 -Y 로 80도 열림)
    for sx in (-1, 1):
        leaf = []
        lw = w / 2 - 0.01
        leaf.append(box("leaf_frame", (lw, 0.04, h - 0.04), (sx * (w / 2 - lw / 2), 0, (h - 0.04) / 2 + 0.01), "M_StainlessDark", bevel=0.006))
        leaf.append(box("leaf_glass", (lw - 0.1, 0.045, h - 0.24), (sx * (w / 2 - lw / 2), 0, (h - 0.04) / 2 + 0.03), "M_Glass", bevel=0))
        leaf.append(box("leaf_handle", (0.03, 0.06, 0.4), (sx * (w / 2 - lw + 0.08), -0.04, 1.05), "M_Stainless", bevel=0.006))
        o = join(leaf, "leaf")
        # 경첩(바깥쪽 모서리) 기준 회전
        pivot = Vector((sx * w / 2, 0, 0))
        o.data.transform(Matrix.Translation(-pivot))
        o.location = pivot
        o.rotation_euler = (0, 0, math.radians(80 * sx))
        p.append(o)
    p.append(box("open_sticker", (0.3, 0.005, 0.1), (0, -0.105, h + 0.06), "M_YellowPaint", bevel=0))
    return join(p, "DoorFrame")


if __name__ == "__main__":
    run_main(build, "Shop", "DoorFrame")
