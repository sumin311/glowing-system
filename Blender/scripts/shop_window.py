"""
shop_window.py - 매장 전면 창문 (1.8 x 1.4 m 프레임 + 유리 + 창턱). 원점 = 창 아래 중앙.
Unity 에서 벽 개구부에 맞춰 배치. 유리는 반투명 머티리얼.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build(w=1.8, h=1.4):
    p = []
    t = 0.06
    p.append(box("bottom", (w, 0.14, t), (0, 0, t / 2), "M_WhitePlastic", bevel=0.006))
    p.append(box("top", (w, 0.14, t), (0, 0, h - t / 2), "M_WhitePlastic", bevel=0.006))
    p.append(box("left", (t, 0.14, h), (-w / 2 + t / 2, 0, h / 2), "M_WhitePlastic", bevel=0.006))
    p.append(box("right", (t, 0.14, h), (w / 2 - t / 2, 0, h / 2), "M_WhitePlastic", bevel=0.006))
    p.append(box("mullion", (0.04, 0.1, h - 2 * t), (0, 0, h / 2), "M_WhitePlastic", bevel=0.004))
    p.append(box("sill", (w + 0.1, 0.22, 0.04), (0, -0.03, -0.02), "M_CounterTop", bevel=0.008))
    root = join(p, "Window")
    g = box("Glass", (w - 2 * t, 0.01, h - 2 * t), (0, 0, h / 2), "M_Glass", bevel=0)
    part([g], "Glass", pivot=(0, 0, 0), parent=root)
    return root


if __name__ == "__main__":
    run_main(build, "Shop", "Window")
