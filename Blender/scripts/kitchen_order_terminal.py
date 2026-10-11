"""
kitchen_order_terminal.py - 재료 발주 단말기 (스탠드형 태블릿 키오스크, 높이 1.3 m)
SupplyTerminal(재고 구매 지점) 에 사용. 화면은 정면(-Y)을 향한다.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    p.append(box("base", (0.42, 0.36, 0.04), (0, 0, 0.02), "M_StainlessDark", bevel=0.012))
    p.append(cylinder("pole", 0.035, 1.05, (0, 0.04, 0.565), "M_Stainless", verts=20))
    p.append(cylinder("collar", 0.05, 0.05, (0, 0.04, 0.065), "M_BlackPlastic", verts=20, bevel=0.008))
    head = []
    head.append(box("housing", (0.5, 0.05, 0.36), (0, 0, 0), "M_WhitePlastic", bevel=0.015, segments=3))
    head.append(box("bezel", (0.46, 0.006, 0.32), (0, -0.026, 0), "M_BlackPlastic", bevel=0.004))
    head.append(box("screen", (0.42, 0.006, 0.27), (0, -0.03, 0.01), "M_Screen", bevel=0.002))
    # 화면 속 UI 블록 느낌
    for i in range(3):
        head.append(box("ui%d" % i, (0.12, 0.002, 0.07), (-0.13 + i * 0.13, -0.034, 0.05), "M_YellowPaint" if i == 0 else "M_WhitePlastic", bevel=0))
    head.append(box("ui_bar", (0.38, 0.002, 0.04), (0, -0.034, -0.08), "M_LabelRed", bevel=0))
    head.append(box("printer", (0.22, 0.12, 0.08), (0, 0.04, -0.2), "M_WhitePlastic", bevel=0.01))
    head.append(box("slot", (0.16, 0.01, 0.006), (0, -0.02, -0.17), "M_BlackPlastic", bevel=0))
    h = join(head, "head")
    h.rotation_euler = (math.radians(-18), 0, 0)
    h.location = (0, 0.0, 1.2)
    p.append(h)
    return join(p, "OrderTerminal")


if __name__ == "__main__":
    run_main(build, "Kitchen", "OrderTerminal")
