"""
kitchen_sink.py - 업소용 스테인리스 싱크대 (1.0 x 0.7 x 0.9 m, 수전 포함). 장식용(손 씻기 연출).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    W, D = 1.0, 0.7
    p = []
    # 상판 (가운데 구멍 주변 4개 판)
    bw, bd = 0.6, 0.45   # 개수대 크기
    by = -0.02
    p.append(box("t_front", (W, (D / 2 + by - bd / 2), 0.04), (0, -D / 2 + (D / 2 + by - bd / 2) / 2, 0.88), "M_Stainless", bevel=0.006))
    back_len = D / 2 - (by + bd / 2)
    p.append(box("t_back", (W, back_len, 0.04), (0, D / 2 - back_len / 2, 0.88), "M_Stainless", bevel=0.006))
    side = (W - bw) / 2
    p.append(box("t_left", (side, bd, 0.04), (-W / 2 + side / 2, by, 0.88), "M_Stainless", bevel=0.004))
    p.append(box("t_right", (side, bd, 0.04), (W / 2 - side / 2, by, 0.88), "M_Stainless", bevel=0.004))
    # 개수대 (안쪽 벽 + 바닥)
    depth = 0.28
    z0 = 0.9 - depth
    t = 0.01
    p.append(box("b_floor", (bw, bd, t), (0, by, z0), "M_StainlessDark", bevel=0))
    p.append(box("b_f", (bw, t, depth), (0, by - bd / 2, z0 + depth / 2), "M_Stainless", bevel=0))
    p.append(box("b_b", (bw, t, depth), (0, by + bd / 2, z0 + depth / 2), "M_Stainless", bevel=0))
    p.append(box("b_l", (t, bd, depth), (-bw / 2, by, z0 + depth / 2), "M_Stainless", bevel=0))
    p.append(box("b_r", (t, bd, depth), (bw / 2, by, z0 + depth / 2), "M_Stainless", bevel=0))
    p.append(cylinder("drain", 0.035, 0.006, (0, by, z0 + 0.008), "M_StainlessDark", verts=20))
    p.append(box("splash", (W, 0.025, 0.25), (0, D / 2 - 0.0125, 1.025), "M_Stainless", bevel=0.006))
    # 다리 + 하부 선반
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (W / 2 - 0.05), sy * (D / 2 - 0.05)
            p.append(cylinder("leg", 0.02, 0.86, (x, y, 0.43), "M_Stainless", verts=14))
            p.append(cylinder("foot", 0.026, 0.03, (x, y, 0.015), "M_BlackPlastic", verts=14, bevel=0.004))
    p.append(box("shelf", (W - 0.08, D - 0.08, 0.02), (0, 0, 0.2), "M_Stainless", bevel=0.005))
    # 수전
    p.append(cylinder("faucet_base", 0.03, 0.05, (0, D / 2 - 0.08, 0.925), "M_Stainless", verts=18, bevel=0.005))
    p.append(tube_path("faucet", [(0, D / 2 - 0.08, 0.93), (0, D / 2 - 0.08, 1.2), (0, D / 2 - 0.12, 1.26), (0, by + 0.02, 1.24), (0, by - 0.02, 1.16)], 0.014, "M_Stainless", segs=12))
    for sx in (-1, 1):
        p.append(tube_path("lever", [(sx * 0.07, D / 2 - 0.08, 0.95), (sx * 0.07, D / 2 - 0.12, 0.99), (sx * 0.12, D / 2 - 0.16, 1.0)], 0.008, "M_Stainless"))
        p.append(sphere("lever_cap", 0.016, (sx * 0.12, D / 2 - 0.16, 1.0), "M_RedPlastic" if sx < 0 else "M_CrateBlue", segs=12, rings=8))
    return join(p, "Sink")


if __name__ == "__main__":
    run_main(build, "Kitchen", "Sink")
