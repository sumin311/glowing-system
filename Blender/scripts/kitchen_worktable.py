"""
kitchen_worktable.py - 스테인리스 조리대 (1.4 x 0.7 x 0.9 m, 하부 선반, 백스플래시)
반죽 작업대 / 소스 작업대 / 포장 작업대가 이 모델을 공유하고, 위에 소품을 올려 구분한다.
작업 상판 높이 = 0.9 m (Unity 에서 아이템 놓는 높이)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build(width=1.4, depth=0.7):
    p = []
    p.append(box("top", (width, depth, 0.04), (0, 0, 0.88), "M_Stainless", bevel=0.012))
    # 앞쪽 롤 엣지
    p.append(cylinder("edge", 0.015, width - 0.01, (0, -depth / 2 + 0.005, 0.875), "M_Stainless", verts=12, rot=(0, 90, 0)))
    p.append(box("splash", (width, 0.025, 0.14), (0, depth / 2 - 0.0125, 0.97), "M_Stainless", bevel=0.006))
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (width / 2 - 0.05), sy * (depth / 2 - 0.05)
            p.append(box("leg", (0.04, 0.04, 0.84), (x, y, 0.44), "M_Stainless", bevel=0.006))
            p.append(cylinder("foot", 0.024, 0.03, (x, y, 0.015), "M_BlackPlastic", verts=14, bevel=0.004))
    p.append(box("shelf", (width - 0.08, depth - 0.08, 0.02), (0, 0, 0.22), "M_Stainless", bevel=0.005))
    # 상판 아래 보강 프레임
    p.append(box("apron_f", (width - 0.1, 0.02, 0.08), (0, -depth / 2 + 0.06, 0.82), "M_StainlessDark", bevel=0.003))
    p.append(box("apron_b", (width - 0.1, 0.02, 0.08), (0, depth / 2 - 0.06, 0.82), "M_StainlessDark", bevel=0.003))
    return join(p, "Worktable")


if __name__ == "__main__":
    run_main(build, "Kitchen", "Worktable")
