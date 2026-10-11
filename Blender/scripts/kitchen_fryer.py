"""
kitchen_fryer.py - 업소용 1구 치킨 튀김기 (스테인리스, 기름통 + 탈착식 바구니)

Unity 연동:
  - 루트 "Fryer" : 본체 (BoxCollider 는 Unity 에디터 스크립트가 bounds 기반으로 자동 추가)
  - 자식 "Basket" : 튀김 바구니. FryerStation 이 조리 중 로컬 Y 를 -0.12 만큼 내린다 (치킨이 반쯤 잠김).
  - 바구니 위쪽 중심이 음식이 놓이는 위치 (Unity 에서 "ItemAnchor" 생성)
실행: blender -b -P kitchen_fryer.py   또는   python kitchen_fryer.py (bpy 모듈)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa: E402,F401,F403


def build():
    parts = []
    W, D = 0.6, 0.75
    # 다리 4개
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(cylinder("leg", 0.022, 0.08, (sx * (W / 2 - 0.05), sy * (D / 2 - 0.06), 0.04), "M_StainlessDark", verts=16))
    # 본체 하부 (z 0.08 ~ 0.80)
    parts.append(box("body", (W, D, 0.72), (0, 0, 0.44), "M_Stainless", bevel=0.012))
    # 상부 테두리(기름통 구멍을 둘러싼 4개 판) z 0.80 ~ 0.915
    h, zc = 0.115, 0.8575
    parts.append(box("top_front", (W, 0.195, h), (0, -0.2775, zc), "M_Stainless", bevel=0.01))
    parts.append(box("top_back", (W, 0.135, h), (0, 0.3075, zc), "M_Stainless", bevel=0.01))
    parts.append(box("top_left", (0.09, 0.42, h), (-0.255, 0.03, zc), "M_Stainless", bevel=0.006))
    parts.append(box("top_right", (0.09, 0.42, h), (0.255, 0.03, zc), "M_Stainless", bevel=0.006))
    # 기름 표면
    parts.append(box("oil", (0.42, 0.42, 0.01), (0, 0.03, 0.86), "M_Oil", bevel=0))
    # 전면 하부 도어 패널 + 손잡이
    parts.append(box("door", (W - 0.08, 0.012, 0.5), (0, -D / 2 - 0.004, 0.42), "M_StainlessDark", bevel=0.006))
    parts.append(tube_path("door_handle", [(-0.18, -0.405, 0.6), (-0.18, -0.43, 0.6), (0.18, -0.43, 0.6), (0.18, -0.405, 0.6)], 0.009, "M_Stainless"))
    # 전면 상부 컨트롤 패널 (살짝 기울어짐)
    panel = box("panel", (W - 0.02, 0.05, 0.12), (0, -0.395, 0.82), "M_StainlessDark", bevel=0.008, rot=(-12, 0, 0))
    parts.append(panel)
    for i, x in enumerate((-0.17, 0.0)):
        parts.append(cylinder("knob%d" % i, 0.025, 0.03, (x, -0.43, 0.82), "M_BlackPlastic", verts=20, bevel=0.004, rot=(90, 0, 0)))
        parts.append(box("knob_mark%d" % i, (0.004, 0.006, 0.018), (x, -0.447, 0.83), "M_WhitePlastic", bevel=0))
    parts.append(sphere("lamp_on", 0.012, (0.16, -0.425, 0.83), "M_RedPlastic", segs=12, rings=8))
    parts.append(sphere("lamp_ready", 0.012, (0.21, -0.425, 0.83), "M_YellowPaint", segs=12, rings=8))
    # 뒤쪽 백스플래시 + 바구니 걸이
    parts.append(box("backsplash", (W, 0.04, 0.32), (0, D / 2 - 0.02, 1.07), "M_Stainless", bevel=0.01))
    parts.append(tube_path("hanger", [(-0.12, 0.33, 1.12), (-0.12, 0.27, 1.12), (0.12, 0.27, 1.12), (0.12, 0.33, 1.12)], 0.008, "M_StainlessDark"))
    root = join(parts, "Fryer")

    # ---- 바구니 (별도 부품) ----
    bparts = []
    bw, bd, bh = 0.34, 0.34, 0.17
    bz = 0.96  # 바구니 바닥 높이 (들어 올린 상태)
    t = 0.008
    bparts.append(box("b_bottom", (bw, bd, t), (0, 0.03, bz), "M_StainlessDark", bevel=0))
    bparts.append(box("b_front", (bw, t, bh), (0, 0.03 - bd / 2, bz + bh / 2), "M_StainlessDark", bevel=0))
    bparts.append(box("b_back", (bw, t, bh), (0, 0.03 + bd / 2, bz + bh / 2), "M_StainlessDark", bevel=0))
    bparts.append(box("b_left", (t, bd, bh), (-bw / 2, 0.03, bz + bh / 2), "M_StainlessDark", bevel=0))
    bparts.append(box("b_right", (t, bd, bh), (bw / 2, 0.03, bz + bh / 2), "M_StainlessDark", bevel=0))
    # 철망 느낌의 가로 띠
    for k in range(1, 4):
        z = bz + bh * k / 4
        bparts.append(tube_path("b_wire%d" % k, [(-bw / 2 - 0.004, 0.03 - bd / 2 - 0.004, z), (bw / 2 + 0.004, 0.03 - bd / 2 - 0.004, z),
                                                  (bw / 2 + 0.004, 0.03 + bd / 2 + 0.004, z), (-bw / 2 - 0.004, 0.03 + bd / 2 + 0.004, z),
                                                  (-bw / 2 - 0.004, 0.03 - bd / 2 - 0.004, z)], 0.003, "M_Stainless", segs=6))
    # 손잡이 (앞쪽으로)
    bparts.append(tube_path("b_handle", [(0, 0.03 - bd / 2, bz + bh), (0, -0.3, bz + bh + 0.06), (0, -0.42, bz + bh + 0.08)], 0.009, "M_Stainless"))
    bparts.append(cylinder("b_grip", 0.017, 0.12, (0, -0.47, bz + bh + 0.085), "M_BlackPlastic", verts=16, bevel=0.004, rot=(90, 0, 0)))
    part(bparts, "Basket", pivot=(0, 0.03, bz), parent=root)
    return root


if __name__ == "__main__":
    run_main(build, "Kitchen", "Fryer")
