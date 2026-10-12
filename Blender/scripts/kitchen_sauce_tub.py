"""
kitchen_sauce_tub.py - 펌프식 소스통 (양념 / 간장 2종).
SauceStation 위에 올라가며, Unity 의 SauceDispenser(상호작용 대상) 에 연결된다.
모델 2개를 내보낸다: SauceTub_Yangnyeom, SauceTub_Ganjang
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build(sauce_mat="M_SauceRed", label_text="양념"):
    p = []
    prof = [(0.0, 0.0), (0.105, 0.0), (0.115, 0.01), (0.12, 0.03), (0.12, 0.26), (0.125, 0.27), (0.125, 0.285), (0.0, 0.285)]
    p.append(lathe("tub", prof, (0, 0, 0), "M_WhitePlastic", segs=32))
    # 소스가 비치는 반투명 느낌의 띠 + 라벨
    p.append(lathe("sauce_band", [(0.1215, 0.04), (0.1215, 0.2)], (0, 0, 0), sauce_mat, segs=32, cap_top=False, cap_bottom=False))
    p.append(box("label", (0.14, 0.006, 0.08), (0, -0.122, 0.13), "M_BoxWhite", bevel=0.002))
    t = text_mesh("label_text", label_text, size=0.045, extrude=0.002, mat=sauce_mat)
    t.location = (0, -0.127, 0.13)
    p.append(t)
    # 뚜껑 + 펌프
    p.append(lathe("lid", [(0.0, 0.285), (0.13, 0.285), (0.13, 0.305), (0.11, 0.315), (0.0, 0.315)], (0, 0, 0), "M_BlackPlastic", segs=32))
    p.append(cylinder("pump_shaft", 0.012, 0.1, (0, 0, 0.36), "M_WhitePlastic", verts=12))
    p.append(cylinder("pump_head", 0.03, 0.03, (0, 0, 0.42), "M_BlackPlastic", verts=18, bevel=0.006))
    p.append(tube_path("nozzle", [(0, 0, 0.42), (0, -0.07, 0.42), (0, -0.1, 0.4)], 0.008, "M_BlackPlastic"))
    return join(p, "SauceTub")


if __name__ == "__main__":
    for name, mat, label in (("SauceTub_Yangnyeom", "M_SauceRed", "양념"), ("SauceTub_Ganjang", "M_SauceBrown", "간장")):
        reset_scene()
        root = build(mat, label)
        root.name = name
        export_model(root, "Kitchen", name)
