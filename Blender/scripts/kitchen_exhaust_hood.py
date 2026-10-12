"""
kitchen_exhaust_hood.py - 튀김기 위 배기 후드 (2.0 x 0.9 m). 벽에 붙여 바닥 기준 높이 1.9 m 에 설치.
원점은 후드 아래면 중앙(뒤쪽 벽 면). Unity 에서 y=1.9 에 배치.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def build():
    p = []
    W, D = 2.0, 0.9
    # 사다리꼴 캐노피: 아래 박스 + 위로 좁아지는 판
    p.append(box("lip", (W, D, 0.12), (0, -D / 2, 0.06), "M_Stainless", bevel=0.01))
    bm = bmesh.new()
    v = [bm.verts.new(c) for c in [(-W / 2, -D, 0.12), (W / 2, -D, 0.12), (W / 2, 0, 0.12), (-W / 2, 0, 0.12),
                                    (-W / 2 + 0.2, -0.45, 0.5), (W / 2 - 0.2, -0.45, 0.5), (W / 2 - 0.2, 0, 0.5), (-W / 2 + 0.2, 0, 0.5)]]
    for f in ((0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7), (3, 2, 1, 0)):
        bm.faces.new([v[i] for i in f])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    canopy = bpy.data.objects.new("canopy", bpy.data.meshes.new("canopy"))
    bm.to_mesh(canopy.data); bm.free(); link(canopy)
    canopy.data.materials.append(get_mat("M_Stainless"))
    add_bevel(canopy, 0.01, 2)
    p.append(canopy)
    p.append(box("duct", (0.5, 0.35, 0.8), (0, -0.2, 0.9), "M_Stainless", bevel=0.01))
    # 필터 (아래면)
    for i in range(4):
        p.append(box("filter%d" % i, (0.42, 0.6, 0.01), (-0.69 + i * 0.46, -0.45, 0.0), "M_StainlessDark", bevel=0.002))
        for k in range(8):
            p.append(box("slat%d_%d" % (i, k), (0.4, 0.008, 0.008), (-0.69 + i * 0.46, -0.72 + k * 0.077, -0.006), "M_Stainless", bevel=0))
    # 조명 2개
    for sx in (-1, 1):
        p.append(box("lamp%d" % sx, (0.3, 0.08, 0.012), (sx * 0.75, -0.85, -0.002), "M_LightBulb", bevel=0.003))
    return join(p, "ExhaustHood")


if __name__ == "__main__":
    run_main(build, "Kitchen", "ExhaustHood")
