"""
prop_drink.py - 콜라 1.25L 페트병 (높이 0.31 m). 음료 주문 시 포장에 함께 담긴다.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    prof = [(0.0, 0.0), (0.03, 0.0), (0.042, 0.01), (0.045, 0.03), (0.043, 0.05), (0.045, 0.07), (0.045, 0.19),
            (0.04, 0.22), (0.026, 0.26), (0.016, 0.285), (0.015, 0.295), (0.0, 0.295)]
    p.append(lathe("bottle", prof, (0, 0, 0), "M_Cola", segs=24))
    p.append(lathe("label", [(0.0458, 0.09), (0.0458, 0.17)], (0, 0, 0), "M_LabelRed", segs=24, cap_top=False, cap_bottom=False))
    p.append(cylinder("cap", 0.017, 0.022, (0, 0, 0.305), "M_RedPlastic", verts=16, bevel=0.002))
    return join(p, "ColaBottle")


if __name__ == "__main__":
    run_main(build, "Props", "ColaBottle")
