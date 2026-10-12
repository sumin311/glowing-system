"""
shop_counter.py - 주문 카운터 모듈 (2.0 x 0.7 x 1.05 m). Unity 에서 여러 개를 이어 붙인다.
정면(-Y)이 손님 쪽: 원목 세로 루버 + 하단 걸레받이, 상판은 밝은 인조대리석.
뒤쪽(+Y, 직원 쪽)에는 수납 선반.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build(length=2.0):
    D, H = 0.7, 1.05
    p = []
    p.append(box("carcass", (length, D - 0.06, H - 0.05), (0, 0.03, (H - 0.05) / 2), "M_WoodDark", bevel=0.008))
    p.append(box("top", (length + 0.02, D + 0.04, 0.05), (0, 0.0, H - 0.025), "M_CounterTop", bevel=0.012, segments=3))
    p.append(box("kick", (length, 0.03, 0.1), (0, -D / 2 + 0.03, 0.05), "M_BlackPlastic", bevel=0.004))
    # 세로 루버 (손님 쪽)
    n = int(length / 0.08)
    for i in range(n):
        x = -length / 2 + 0.04 + i * (length / n)
        p.append(box("slat%d" % i, (0.05, 0.025, H - 0.2), (x, -D / 2 + 0.02, 0.1 + (H - 0.2) / 2 + 0.005), "M_WoodLight", bevel=0.006))
    # 상판 아래 포인트 띠 (빨간 스트립)
    p.append(box("stripe", (length, 0.01, 0.04), (0, -D / 2 + 0.005, H - 0.08), "M_SignBoard", bevel=0.002))
    # 직원 쪽 수납 선반
    p.append(box("inner_shelf", (length - 0.06, 0.3, 0.025), (0, D / 2 - 0.17, 0.55), "M_WoodLight", bevel=0.004))
    return join(p, "Counter")


if __name__ == "__main__":
    run_main(build, "Shop", "Counter")
