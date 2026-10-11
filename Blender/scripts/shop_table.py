"""
shop_table.py - 매장 홀 4인 테이블 (0.8 x 0.8 x 0.75 m, 원목 상판 + 금속 다리)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(box("top", (0.8, 0.8, 0.04), (0, 0, 0.73), "M_WoodLight", bevel=0.012, segments=3))
    p.append(box("apron", (0.68, 0.68, 0.06), (0, 0, 0.68), "M_WoodDark", bevel=0.005))
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.append(cylinder("leg", 0.022, 0.68, (sx * 0.33, sy * 0.33, 0.34), "M_BlackPlastic", verts=16, radius2=0.018))
            p.append(cylinder("foot", 0.026, 0.012, (sx * 0.33, sy * 0.33, 0.006), "M_Rubber", verts=16))
    # 테이블 위 소품: 냅킨통 + 양념통
    p.append(box("napkin", (0.12, 0.08, 0.1), (0.2, 0.25, 0.8), "M_StainlessDark", bevel=0.008))
    p.append(box("napkin_paper", (0.1, 0.06, 0.02), (0.2, 0.25, 0.855), "M_BoxWhite", bevel=0.004))
    p.append(lathe("salt", [(0, 0), (0.022, 0), (0.024, 0.07), (0.018, 0.09), (0, 0.095)], (0.05, 0.27, 0.75), "M_Glass", segs=14))
    p.append(lathe("pepper", [(0, 0), (0.022, 0), (0.024, 0.07), (0.018, 0.09), (0, 0.095)], (0.0, 0.27, 0.75), "M_WoodDark", segs=14))
    return join(p, "Table")


if __name__ == "__main__":
    run_main(build, "Shop", "Table")
