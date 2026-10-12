"""
kitchen_fridge.py - 업소용 2도어 냉장고 (1.2 x 0.75 x 2.0 m). 생닭/식재료 보관 (IngredientFridge).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    W, D, H = 1.2, 0.75, 2.0
    p = []
    p.append(box("kick", (W - 0.04, D - 0.06, 0.1), (0, 0.02, 0.05), "M_BlackPlastic", bevel=0.005))
    p.append(box("body", (W, D, H - 0.1), (0, 0, 0.1 + (H - 0.1) / 2), "M_Stainless", bevel=0.02, segments=3))
    # 상단 컨트롤/그릴 패널
    p.append(box("top_panel", (W - 0.06, 0.02, 0.18), (0, -D / 2 - 0.006, H - 0.12), "M_StainlessDark", bevel=0.004))
    for i in range(12):
        x = -0.45 + i * 0.07
        p.append(box("grill%d" % i, (0.035, 0.01, 0.1), (x, -D / 2 - 0.018, H - 0.12), "M_BlackPlastic", bevel=0.002))
    p.append(box("display", (0.12, 0.012, 0.05), (0.48, -D / 2 - 0.02, H - 0.12), "M_Screen", bevel=0.003))
    # 도어 2개 + 가스켓 + 세로 손잡이
    for sx in (-1, 1):
        x = sx * 0.296
        p.append(box("gasket", (0.585, 0.012, 1.66), (x, -D / 2 - 0.004, 1.0), "M_Rubber", bevel=0.004))
        p.append(box("door", (0.57, 0.04, 1.64), (x, -D / 2 - 0.022, 1.0), "M_Stainless", bevel=0.012, segments=3))
        hx = x - sx * 0.23
        p.append(tube_path("handle", [(hx, -D / 2 - 0.04, 0.75), (hx, -D / 2 - 0.08, 0.78), (hx, -D / 2 - 0.08, 1.32), (hx, -D / 2 - 0.04, 1.35)], 0.012, "M_StainlessDark"))
        # 도어 하단 로고판
        p.append(box("plate", (0.16, 0.006, 0.04), (x, -D / 2 - 0.045, 0.3), "M_StainlessDark", bevel=0.002))
    # 경첩
    for sx in (-1, 1):
        for z in (0.3, 1.7):
            p.append(cylinder("hinge", 0.012, 0.06, (sx * 0.585, -D / 2 - 0.02, z), "M_StainlessDark", verts=12))
    return join(p, "Fridge")


if __name__ == "__main__":
    run_main(build, "Kitchen", "Fridge")
