"""
char_customer.py - 손님 캐릭터 베이스 모델 (키 약 1.72 m, 정면 -Y).

리깅 대안 (세그먼트 방식):
  스킨드 메시 + 아마추어 대신 관절별로 분리된 부품을 계층으로 내보낸다.
  Unity 의 CustomerAnimator 가 각 부품의 피벗(관절)을 기준으로 회전시켜 걷기/대기/손 흔들기 애니메이션을 절차적으로 재생한다.
  -> Animator Controller/Avatar 설정 없이 바로 동작하며, 폴리곤도 적다.

계층:
  Customer (몸통/골반/목)
    ├─ Head (피벗: 목)
    │    ├─ Hair_Short / Hair_Long / Hair_Bun  (하나만 활성화)
    │    ├─ Cap      (선택 액세서리)
    │    └─ Glasses  (선택 액세서리)
    ├─ Arm_L / Arm_R (피벗: 어깨)
    ├─ Leg_L / Leg_R (피벗: 고관절)
    └─ Backpack      (선택 액세서리)

머티리얼: M_Skin, M_Shirt, M_Pants, M_Shoes, M_Hair, M_Eyes, M_Accessory
  -> Unity CustomerAppearance 가 MaterialPropertyBlock 으로 손님마다 다른 색을 입힌다.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa


def ellip_lathe(name, prof, mat, sy=0.68, segs=20):
    o = lathe(name, prof, (0, 0, 0), mat, segs=segs)
    o.scale = (1, sy, 1)
    apply_transform(o)
    return o


def hair_shell(name, center, radius, front_cut=1.66, back_cut=1.55, extra=None):
    """머리 위를 덮는 머리카락 껍질. 얼굴 앞쪽은 이마 위까지만 덮는다."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=22, v_segments=14, radius=radius)
    bmesh.ops.scale(bm, vec=Vector((1.0, 1.02, 1.05)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(center), verts=bm.verts)
    kill = []
    for v in bm.verts:
        y, z = v.co.y, v.co.z
        # 앞(-Y)일수록 높은 곳에서 자른다
        t = max(0.0, min(1.0, (-y - (-0.03)) / 0.09))
        cut = back_cut + (front_cut - back_cut) * t
        if z < cut:
            kill.append(v)
    bmesh.ops.delete(bm, geom=kill, context="VERTS")
    # 두께
    bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.012)
    o = _new_obj(name, bm, "M_Hair")
    smooth_all(o)
    return o


def _new_obj(name, bm, mat):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    link(obj)
    obj.data.materials.append(get_mat(mat))
    return obj


def build():
    HEAD_C = Vector((0, 0, 1.6))
    # ---------------- 몸통 ----------------
    body = []
    body.append(ellip_lathe("pelvis", [(0.0, 0.86), (0.13, 0.86), (0.155, 0.92), (0.16, 1.0), (0.15, 1.04), (0.0, 1.04)], "M_Pants"))
    body.append(ellip_lathe("torso", [(0.0, 1.0), (0.152, 1.0), (0.148, 1.1), (0.155, 1.24), (0.18, 1.37), (0.165, 1.43), (0.1, 1.47), (0.05, 1.48), (0.0, 1.48)], "M_Shirt", sy=0.62))
    body.append(box("belt", (0.31, 0.205, 0.035), (0, 0, 1.0), "M_Shoes", bevel=0.01))
    body.append(box("buckle", (0.045, 0.01, 0.03), (0, -0.103, 1.0), "M_Stainless", bevel=0.002))
    body.append(cylinder("neck", 0.045, 0.1, (0, 0.0, 1.5), "M_Skin", verts=14))
    # 옷깃
    body.append(torus("collar", 0.055, 0.014, (0, 0.0, 1.47), "M_Shirt", major_segs=18, minor_segs=6))
    root = join(body, "Customer")

    # ---------------- 머리 ----------------
    head = []
    head.append(sphere("skull", 0.112, HEAD_C, "M_Skin", segs=24, rings=16, scale=(0.94, 1.0, 1.12)))
    head.append(sphere("jaw", 0.085, HEAD_C + Vector((0, -0.02, -0.06)), "M_Skin", segs=18, rings=10, scale=(1.0, 1.0, 0.9)))
    for sx in (-1, 1):
        head.append(sphere("eye", 0.0145, HEAD_C + Vector((sx * 0.04, -0.098, 0.015)), "M_Eyes", segs=12, rings=8, scale=(1, 0.6, 1.15)))
        head.append(sphere("eye_hl", 0.004, HEAD_C + Vector((sx * 0.04 + 0.004, -0.106, 0.022)), "M_BoxWhite", segs=8, rings=4))
        head.append(box("brow", (0.035, 0.01, 0.008), HEAD_C + Vector((sx * 0.042, -0.1, 0.05)), "M_Hair", bevel=0.003, rot=(0, sx * -8, 0)))
        head.append(sphere("ear", 0.026, HEAD_C + Vector((sx * 0.104, 0.0, 0.0)), "M_Skin", segs=12, rings=8, scale=(0.45, 0.8, 1.15)))
        head.append(sphere("cheek", 0.018, HEAD_C + Vector((sx * 0.06, -0.085, -0.025)), "M_Accessory", segs=10, rings=6, scale=(1, 0.4, 0.7)))
    head.append(sphere("nose", 0.018, HEAD_C + Vector((0, -0.11, -0.01)), "M_Skin", segs=12, rings=8, scale=(0.8, 0.9, 1.1)))
    head.append(box("mouth", (0.035, 0.008, 0.008), HEAD_C + Vector((0, -0.1, -0.05)), "M_LabelRed", bevel=0.003))
    head_obj = part(head, "Head", pivot=(0, 0, 1.5), parent=root)

    # 헤어 변형 (Head 자식)
    hs = hair_shell("Hair_Short", HEAD_C + Vector((0, 0.005, 0.012)), 0.122)
    part([hs], "Hair_Short", pivot=(0, 0, 1.5), parent=head_obj)
    hl = [hair_shell("hl_cap", HEAD_C + Vector((0, 0.005, 0.012)), 0.124, back_cut=1.5)]
    back = box("hl_back", (0.23, 0.07, 0.26), HEAD_C + Vector((0, 0.075, -0.13)), "M_Hair", bevel=0.03, segments=3)
    hl.append(back)
    for sx in (-1, 1):
        hl.append(box("hl_side", (0.035, 0.08, 0.2), HEAD_C + Vector((sx * 0.108, 0.01, -0.09)), "M_Hair", bevel=0.015, segments=2))
    part(hl, "Hair_Long", pivot=(0, 0, 1.5), parent=head_obj)
    hb = [hair_shell("hb_cap", HEAD_C + Vector((0, 0.005, 0.012)), 0.123)]
    hb.append(sphere("bun", 0.055, HEAD_C + Vector((0, 0.09, 0.11)), "M_Hair", segs=16, rings=10))
    part(hb, "Hair_Bun", pivot=(0, 0, 1.5), parent=head_obj)

    # 모자 / 안경
    cap = [lathe("crown", [(0.0, 1.66), (0.128, 1.66), (0.127, 1.69), (0.115, 1.74), (0.08, 1.77), (0.0, 1.78)], (0, 0, 0), "M_Accessory", segs=24)]
    brim = cylinder("brim", 0.08, 0.012, (0, -0.15, 1.665), "M_Accessory", verts=20, bevel=0.003)
    brim.scale = (1.15, 0.8, 1)
    cap.append(brim)
    cap.append(sphere("button", 0.012, (0, 0, 1.78), "M_Accessory", segs=10, rings=6))
    part(cap, "Cap", pivot=(0, 0, 1.5), parent=head_obj)
    gl = []
    for sx in (-1, 1):
        gl.append(torus("lens_ring", 0.026, 0.004, HEAD_C + Vector((sx * 0.042, -0.112, 0.015)), "M_BlackPlastic", major_segs=18, minor_segs=6, rot=(90, 0, 0)))
        gl.append(box("temple", (0.004, 0.11, 0.006), HEAD_C + Vector((sx * 0.1, -0.06, 0.02)), "M_BlackPlastic", bevel=0))
    gl.append(box("bridge", (0.03, 0.005, 0.005), HEAD_C + Vector((0, -0.113, 0.02)), "M_BlackPlastic", bevel=0))
    part(gl, "Glasses", pivot=(0, 0, 1.5), parent=head_obj)

    # ---------------- 팔 ----------------
    for side, sx in (("L", -1), ("R", 1)):
        # Blender -Y 정면 기준: 캐릭터의 왼쪽 = -X (Unity 에서도 캐릭터 왼쪽)
        arm = []
        sh = Vector((sx * 0.2, 0, 1.4))
        arm.append(sphere("shoulder", 0.058, sh, "M_Shirt", segs=14, rings=10))
        arm.append(cylinder("upper", 0.052, 0.27, sh + Vector((sx * 0.01, 0, -0.14)), "M_Shirt", verts=14, radius2=0.047))
        arm.append(cylinder("fore", 0.04, 0.26, sh + Vector((sx * 0.015, 0, -0.39)), "M_Skin", verts=12, radius2=0.034))
        hand = sphere("hand", 0.042, sh + Vector((sx * 0.018, -0.005, -0.54)), "M_Skin", segs=12, rings=8, scale=(0.8, 0.6, 1.2))
        arm.append(hand)
        part(arm, "Arm_" + side, pivot=tuple(sh), parent=root)

    # ---------------- 다리 ----------------
    for side, sx in (("L", -1), ("R", 1)):
        leg = []
        hip = Vector((sx * 0.085, 0, 0.92))
        leg.append(cylinder("thigh", 0.072, 0.44, hip + Vector((0, 0, -0.2)), "M_Pants", verts=16, radius2=0.062))
        leg.append(cylinder("shin", 0.06, 0.38, hip + Vector((0, 0.005, -0.6)), "M_Pants", verts=16, radius2=0.05))
        leg.append(box("shoe", (0.1, 0.25, 0.08), hip + Vector((0, -0.045, -0.88)), "M_Shoes", bevel=0.03, segments=3))
        leg.append(box("sole", (0.104, 0.254, 0.02), hip + Vector((0, -0.045, -0.91)), "M_WhitePlastic", bevel=0.006))
        part(leg, "Leg_" + side, pivot=tuple(hip), parent=root)

    # ---------------- 가방 ----------------
    bp = [box("pack", (0.26, 0.12, 0.32), (0, 0.16, 1.22), "M_Accessory", bevel=0.04, segments=3)]
    bp.append(box("pocket", (0.18, 0.04, 0.12), (0, 0.23, 1.14), "M_Accessory", bevel=0.02, segments=2))
    for sx in (-1, 1):
        bp.append(box("strap", (0.035, 0.03, 0.3), (sx * 0.1, -0.105, 1.28), "M_Shoes", bevel=0.008))
    part(bp, "Backpack", pivot=(0, 0, 1.2), parent=root)
    return root


if __name__ == "__main__":
    run_main(build, "Characters", "Customer")
