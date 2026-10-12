"""
kitchen_storage_rack.py - 식재료 보관 선반 (1.2 x 0.5 x 1.7 m)
플라스틱 상자, 밀가루(튀김가루) 포대, 콜라 상자, 치킨 상자 묶음 등을 함께 배치한 장식 겸 보관함.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def crate(name, loc, mat, w=0.42, d=0.34, h=0.22):
    parts = []
    x, y, z = loc
    parts.append(box(name, (w, d, h), (x, y, z + h / 2), mat, bevel=0.012))
    # 손잡이 구멍(어두운 홈)과 측면 리브
    parts.append(box(name + "_hole", (0.1, 0.004, 0.03), (x, y - d / 2 - 0.001, z + h - 0.045), "M_BlackPlastic", bevel=0))
    for k in range(3):
        parts.append(box(name + "_rib%d" % k, (w + 0.006, 0.01, 0.012), (x, y - d / 2 + 0.002, z + 0.04 + k * 0.05), mat, bevel=0.002))
    return parts


def sack(name, loc, w=0.34, d=0.24, h=0.42):
    s = box(name, (w, d, h), (loc[0], loc[1], loc[2] + h / 2), "M_Sack", bevel=0.07, segments=4)
    # 포대 윗부분 접힌 끝
    top = box(name + "_fold", (w * 0.8, d * 0.35, 0.05), (loc[0], loc[1], loc[2] + h + 0.01), "M_Sack", bevel=0.015)
    band = box(name + "_band", (w * 0.9, 0.004, 0.1), (loc[0], loc[1] - d / 2 - 0.005, loc[2] + h * 0.55), "M_LabelRed", bevel=0)
    return [s, top, band]


def build():
    W, D = 1.2, 0.5
    p = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.append(box("post", (0.03, 0.03, 1.7), (sx * (W / 2 - 0.015), sy * (D / 2 - 0.015), 0.85), "M_StainlessDark", bevel=0.004))
    shelf_z = [0.12, 0.62, 1.12, 1.62]
    for i, z in enumerate(shelf_z):
        p.append(box("shelf%d" % i, (W, D, 0.025), (0, 0, z), "M_Stainless", bevel=0.005))
        for k in range(5):
            p.append(box("wire%d_%d" % (i, k), (W - 0.02, 0.006, 0.006), (0, -D / 2 + 0.05 + k * 0.1, z + 0.016), "M_StainlessDark", bevel=0))
    # 아래칸: 튀김가루 포대 2개
    p += sack("sack1", (-0.3, 0.02, 0.135))
    p += sack("sack2", (0.12, 0.04, 0.135), w=0.32, h=0.38)
    # 가운데칸: 플라스틱 상자 (파랑/초록)
    p += crate("crateA", (-0.3, 0, 0.635), "M_CrateBlue")
    p += crate("crateB", (0.28, 0, 0.635), "M_CrateGreen")
    # 콜라 병 몇 개 (상자 위로 머리)
    for i in range(3):
        p.append(lathe("bottle%d" % i, [(0.0, 0.0), (0.04, 0.0), (0.045, 0.03), (0.045, 0.2), (0.03, 0.26), (0.016, 0.3), (0.016, 0.31), (0.0, 0.31)],
                       (0.18 + i * 0.1, 0.0, 0.66), "M_Cola", segs=16))
        p.append(cylinder("cap%d" % i, 0.018, 0.02, (0.18 + i * 0.1, 0.0, 0.98), "M_RedPlastic", verts=12))
    # 윗칸: 접힌 치킨 상자 묶음
    for i in range(5):
        p.append(box("flatbox%d" % i, (0.45, 0.4, 0.02), (-0.25, 0, 1.145 + i * 0.022), "M_BoxWhite" if i % 2 == 0 else "M_BoxRed", bevel=0.003))
    p.append(lathe("oil_jug", [(0.0, 0.0), (0.09, 0.0), (0.1, 0.02), (0.1, 0.24), (0.06, 0.3), (0.03, 0.32), (0.0, 0.32)], (0.3, 0, 1.135), "M_Oil", segs=20))
    # 맨 윗칸 위: 상자
    p += crate("crateC", (0.0, 0, 1.635), "M_CrateBlue", h=0.06)
    return join(p, "StorageRack")


if __name__ == "__main__":
    run_main(build, "Kitchen", "StorageRack")
