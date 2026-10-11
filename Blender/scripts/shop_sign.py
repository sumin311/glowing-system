"""
shop_sign.py - 매장 외부 간판 "바삭치킨" (3.2 x 0.9 m). 원점: 간판 아래 중앙, 벽면(+Y)에 붙임.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def drumstick(loc, scale=1.0, mat="M_YellowPaint"):
    parts = []
    x, y, z = loc
    meat = sphere("meat", 0.12 * scale, (x, y, z), mat, segs=20, rings=12, scale=(1.0, 0.35, 1.25))
    meat.rotation_euler = (0, math.radians(35), 0)
    parts.append(meat)
    bone = cylinder("bone", 0.03 * scale, 0.16 * scale, (x - 0.13 * scale, y, z - 0.17 * scale), "M_SignText", verts=12, rot=(0, 35, 0))
    parts.append(bone)
    for dz in (-0.03, 0.03):
        parts.append(sphere("knob", 0.035 * scale, (x - 0.19 * scale + dz, y, z - 0.25 * scale - dz), "M_SignText", segs=12, rings=8, scale=(1, 0.6, 1)))
    return parts


def build():
    W, H, T = 3.2, 0.9, 0.16
    p = []
    p.append(box("frame", (W, T, H), (0, -T / 2, H / 2), "M_WoodDark", bevel=0.02, segments=3))
    p.append(box("board", (W - 0.12, 0.02, H - 0.12), (0, -T - 0.005, H / 2), "M_SignBoard", bevel=0.008))
    t = text_mesh("title", "바삭치킨", size=0.5, extrude=0.03, mat="M_SignText")
    t.location = (0.25, -T - 0.03, H / 2 + 0.02)
    p.append(t)
    p += drumstick((-1.15, -T - 0.04, H / 2 + 0.05), scale=1.3)
    sub = text_mesh("sub", "CRISPY CHICKEN  since 2024", size=0.08, extrude=0.008, font_file="NanumSquareRoundR.ttf", mat="M_YellowPaint")
    sub.location = (0.25, -T - 0.025, 0.14)
    p.append(sub)
    # 간판 조명 (위쪽 갓등 3개)
    for i in range(3):
        x = -1.0 + i
        p.append(tube_path("arm%d" % i, [(x, 0, H + 0.02), (x, -0.35, H + 0.15)], 0.012, "M_BlackPlastic"))
        p.append(cylinder("shade%d" % i, 0.07, 0.08, (x, -0.38, H + 0.13), "M_BlackPlastic", verts=16, radius2=0.03, rot=(-30, 0, 0)))
    return join(p, "ShopSign")


if __name__ == "__main__":
    run_main(build, "Shop", "ShopSign")
