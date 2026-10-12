"""
cs_common.py - 바삭치킨(Chicken Shop) Blender 모델 공용 유틸리티.

모든 모델 스크립트가 이 모듈을 import 해서 사용한다.
- 단위: 1 Blender unit = 1 m (Unity 1 unit = 1 m 와 동일)
- 정면: 모든 모델은 Blender -Y 방향을 정면으로 만든다. (FBX 내보내기 후 Unity +Z = transform.forward)
- 원점: 바닥 중앙(Z=0)에 둔다. Unity 에서 y=0 에 놓으면 바닥에 딱 붙는다.
- 머티리얼 이름은 M_ 접두사를 사용하며, Unity 에디터 스크립트가 같은 이름의 .mat 로 공유/리맵한다.

Blender 4.2+ / 5.x (bpy 모듈 포함) 에서 동작하도록 작성했다.
"""
import math
import os
import random
import sys

import bpy
import bmesh
from mathutils import Vector, Matrix

# ---------------------------------------------------------------------------
# 경로
# ---------------------------------------------------------------------------
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
UNITY_MODELS_DIR = os.path.join(REPO_ROOT, "Assets", "ChickenShop", "Models")
BLEND_DIR = os.path.join(REPO_ROOT, "Blender", "blend")
FONT_DIR = os.path.join(REPO_ROOT, "Assets", "ChickenShop", "Resources", "Fonts")


# ---------------------------------------------------------------------------
# 머티리얼 팔레트 (이름 -> (RGBA, roughness, metallic))
# Unity 측 ChickenShopMaterialLibrary 와 이름을 맞춘다.
# ---------------------------------------------------------------------------
PALETTE = {
    "M_Stainless":     ((0.72, 0.74, 0.76, 1), 0.28, 0.9),
    "M_StainlessDark": ((0.38, 0.40, 0.42, 1), 0.35, 0.85),
    "M_BlackPlastic":  ((0.05, 0.05, 0.055, 1), 0.45, 0.0),
    "M_WhitePlastic":  ((0.92, 0.92, 0.9, 1), 0.4, 0.0),
    "M_RedPlastic":    ((0.78, 0.09, 0.07, 1), 0.4, 0.0),
    "M_YellowPaint":   ((0.98, 0.72, 0.12, 1), 0.45, 0.0),
    "M_WoodLight":     ((0.72, 0.52, 0.32, 1), 0.6, 0.0),
    "M_WoodDark":      ((0.36, 0.22, 0.13, 1), 0.55, 0.0),
    "M_CounterTop":    ((0.93, 0.91, 0.86, 1), 0.25, 0.0),
    "M_Fabric":        ((0.75, 0.16, 0.12, 1), 0.85, 0.0),
    "M_Glass":         ((0.75, 0.88, 0.92, 0.35), 0.05, 0.0),
    "M_Oil":           ((0.85, 0.58, 0.12, 1), 0.08, 0.0),
    "M_Rubber":        ((0.08, 0.08, 0.08, 1), 0.9, 0.0),
    "M_Screen":        ((0.08, 0.16, 0.28, 1), 0.15, 0.0),
    "M_LightBulb":     ((1.0, 0.92, 0.7, 1), 0.2, 0.0),
    "M_Kraft":         ((0.70, 0.52, 0.33, 1), 0.85, 0.0),
    "M_BoxWhite":      ((0.96, 0.95, 0.92, 1), 0.7, 0.0),
    "M_BoxRed":        ((0.80, 0.12, 0.08, 1), 0.6, 0.0),
    "M_Cola":          ((0.18, 0.06, 0.03, 1), 0.1, 0.0),
    "M_LabelRed":      ((0.85, 0.08, 0.08, 1), 0.5, 0.0),
    "M_Flour":         ((0.95, 0.93, 0.86, 1), 0.95, 0.0),
    "M_Sack":          ((0.82, 0.75, 0.6, 1), 0.95, 0.0),
    "M_CrateBlue":     ((0.15, 0.35, 0.7, 1), 0.5, 0.0),
    "M_CrateGreen":    ((0.2, 0.55, 0.3, 1), 0.5, 0.0),
    "M_ChickenRaw":    ((0.94, 0.70, 0.62, 1), 0.35, 0.0),
    "M_ChickenBatter": ((0.96, 0.9, 0.76, 1), 0.85, 0.0),
    "M_ChickenFried":  ((0.80, 0.47, 0.13, 1), 0.55, 0.0),
    "M_ChickenYangnyeom": ((0.66, 0.08, 0.03, 1), 0.18, 0.0),
    "M_ChickenGanjang":   ((0.36, 0.17, 0.05, 1), 0.2, 0.0),
    "M_ChickenBurnt":  ((0.12, 0.07, 0.04, 1), 0.85, 0.0),
    "M_Bone":          ((0.93, 0.88, 0.78, 1), 0.5, 0.0),
    "M_Sesame":        ((0.98, 0.95, 0.85, 1), 0.5, 0.0),
    "M_SauceRed":      ((0.72, 0.1, 0.04, 1), 0.2, 0.0),
    "M_SauceBrown":    ((0.33, 0.16, 0.06, 1), 0.2, 0.0),
    "M_Skin":          ((0.93, 0.76, 0.62, 1), 0.6, 0.0),
    "M_Shirt":         ((0.25, 0.45, 0.8, 1), 0.8, 0.0),
    "M_Pants":         ((0.2, 0.22, 0.3, 1), 0.8, 0.0),
    "M_Shoes":         ((0.12, 0.1, 0.1, 1), 0.5, 0.0),
    "M_Hair":          ((0.12, 0.08, 0.06, 1), 0.6, 0.0),
    "M_Eyes":          ((0.03, 0.03, 0.03, 1), 0.2, 0.0),
    "M_Accessory":     ((0.85, 0.25, 0.2, 1), 0.5, 0.0),
    "M_SignBoard":     ((0.85, 0.12, 0.08, 1), 0.35, 0.0),
    "M_SignText":      ((1.0, 0.95, 0.85, 1), 0.3, 0.0),
    "M_Tile":          ((0.95, 0.95, 0.93, 1), 0.2, 0.0),
    "M_Plant":         ((0.2, 0.5, 0.2, 1), 0.7, 0.0),
    "M_Pot":           ((0.7, 0.35, 0.22, 1), 0.7, 0.0),
}


def get_mat(name):
    """팔레트 이름으로 Principled BSDF 머티리얼을 만들거나 재사용한다."""
    mat = bpy.data.materials.get(name)
    if mat is not None:
        return mat
    rgba, rough, metal = PALETTE.get(name, ((0.8, 0.8, 0.8, 1), 0.5, 0.0))
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.roughness = rough
    mat.metallic = metal
    try:
        mat.use_nodes = True
    except Exception:
        pass
    nt = getattr(mat, "node_tree", None)
    if nt is not None:
        bsdf = nt.nodes.get("Principled BSDF")
        if bsdf is None:
            for n in nt.nodes:
                if n.type == "BSDF_PRINCIPLED":
                    bsdf = n
                    break
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = rgba
            bsdf.inputs["Roughness"].default_value = rough
            bsdf.inputs["Metallic"].default_value = metal
            if rgba[3] < 1.0 and "Alpha" in bsdf.inputs:
                bsdf.inputs["Alpha"].default_value = rgba[3]
    return mat


# ---------------------------------------------------------------------------
# 씬 관리
# ---------------------------------------------------------------------------
def reset_scene():
    """빈 씬으로 초기화하고 단위를 미터로 설정한다."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    random.seed(1234)


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _new_obj_from_bmesh(name, bm, mat_name=None):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    link(obj)
    if mat_name:
        obj.data.materials.append(get_mat(mat_name))
    return obj


# ---------------------------------------------------------------------------
# 기본 도형 (bmesh 기반 - 오퍼레이터 컨텍스트 의존성 없음)
# ---------------------------------------------------------------------------
def box(name, size, loc=(0, 0, 0), mat="M_Stainless", bevel=0.01, segments=2, rot=None):
    """size=(x,y,z) 크기의 박스. bevel>0 이면 모서리를 둥글게 처리한다(미드폴리)."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    obj = _new_obj_from_bmesh(name, bm, mat)
    obj.location = loc
    if rot:
        obj.rotation_euler = [math.radians(a) for a in rot]
    if bevel > 0:
        add_bevel(obj, bevel, segments)
    return obj


def cylinder(name, radius, depth, loc=(0, 0, 0), mat="M_Stainless", verts=24, bevel=0.0, segments=2, rot=None, radius2=None):
    """Z 축 원기둥(또는 radius2 지정 시 원뿔대). loc 은 중심."""
    bm = bmesh.new()
    r2 = radius if radius2 is None else radius2
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=verts,
                          radius1=radius, radius2=r2, depth=depth)
    obj = _new_obj_from_bmesh(name, bm, mat)
    obj.location = loc
    if rot:
        obj.rotation_euler = [math.radians(a) for a in rot]
    if bevel > 0:
        add_bevel(obj, bevel, segments)
    smooth_by_angle(obj, 40)
    return obj


def sphere(name, radius, loc=(0, 0, 0), mat="M_Stainless", segs=20, rings=12, scale=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=radius)
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    obj = _new_obj_from_bmesh(name, bm, mat)
    obj.location = loc
    smooth_all(obj)
    return obj


def torus(name, major, minor, loc=(0, 0, 0), mat="M_Stainless", major_segs=24, minor_segs=10, rot=None, arc=360.0):
    """토러스(손잡이, 링 등). arc<360 이면 일부만 생성."""
    bm = bmesh.new()
    rings = []
    count = major_segs if arc >= 360 else major_segs + 1
    for i in range(count):
        a = math.radians(arc) * i / major_segs
        center = Vector((math.cos(a) * major, math.sin(a) * major, 0))
        ring = []
        for j in range(minor_segs):
            b = 2 * math.pi * j / minor_segs
            d = Vector((math.cos(a), math.sin(a), 0)) * math.cos(b) * minor + Vector((0, 0, math.sin(b) * minor))
            ring.append(bm.verts.new(center + d))
        rings.append(ring)
    n = len(rings)
    last = n if arc >= 360 else n - 1
    for i in range(last):
        r0 = rings[i]
        r1 = rings[(i + 1) % n]
        for j in range(minor_segs):
            bm.faces.new((r0[j], r0[(j + 1) % minor_segs], r1[(j + 1) % minor_segs], r1[j]))
    obj = _new_obj_from_bmesh(name, bm, mat)
    obj.location = loc
    if rot:
        obj.rotation_euler = [math.radians(a) for a in rot]
    smooth_all(obj)
    return obj


def lathe(name, profile, loc=(0, 0, 0), mat="M_Stainless", segs=24, cap_top=True, cap_bottom=True):
    """(반지름, 높이) 프로파일을 Z 축으로 회전시켜 회전체(병, 그릇 등)를 만든다."""
    bm = bmesh.new()
    rings = []
    for (r, z) in profile:
        ring = []
        for i in range(segs):
            a = 2 * math.pi * i / segs
            ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        for i in range(segs):
            a, b = rings[k][i], rings[k][(i + 1) % segs]
            c, d = rings[k + 1][(i + 1) % segs], rings[k + 1][i]
            bm.faces.new((a, b, c, d))
    if cap_bottom and profile[0][0] > 1e-4:
        bm.faces.new(list(reversed(rings[0])))
    if cap_top and profile[-1][0] > 1e-4:
        bm.faces.new(rings[-1])
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = _new_obj_from_bmesh(name, bm, mat)
    obj.location = loc
    smooth_by_angle(obj, 50)
    return obj


def tube_path(name, points, radius, mat="M_Stainless", segs=10):
    """점 목록을 따라가는 파이프(손잡이, 전선, 수도꼭지 등)."""
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = max(1, segs // 4)
    curve.use_fill_caps = True
    spl = curve.splines.new("POLY")
    spl.points.add(len(points) - 1)
    for i, p in enumerate(points):
        spl.points[i].co = (p[0], p[1], p[2], 1)
    cobj = bpy.data.objects.new(name + "_curve", curve)
    link(cobj)
    obj = convert_to_mesh(cobj, name)
    obj.data.materials.clear()
    obj.data.materials.append(get_mat(mat))
    smooth_all(obj)
    return obj


def text_mesh(name, body, size=0.2, extrude=0.01, font_file="NanumSquareRoundB.ttf", mat="M_SignText", align="CENTER"):
    """한글 텍스트를 입체 메시로 만든다 (간판 글자 등). XZ 평면(정면 -Y)에 세운다."""
    curve = bpy.data.curves.new(name, "FONT")
    font_path = os.path.join(FONT_DIR, font_file)
    if os.path.exists(font_path):
        curve.font = bpy.data.fonts.load(font_path, check_existing=True)
    curve.body = body
    curve.size = size
    curve.extrude = extrude
    curve.bevel_depth = extrude * 0.3
    curve.bevel_resolution = 0  # 1단 챔퍼만 (폴리곤 절약)
    curve.resolution_u = 2  # 글자 곡선 해상도 (폴리곤 수 절약)
    curve.align_x = align
    curve.align_y = "CENTER"
    cobj = bpy.data.objects.new(name + "_curve", curve)
    link(cobj)
    cobj.rotation_euler = (math.radians(90), 0, 0)
    obj = convert_to_mesh(cobj, name)
    obj.data.materials.clear()
    obj.data.materials.append(get_mat(mat))
    return obj


# ---------------------------------------------------------------------------
# 모디파이어/쉐이딩
# ---------------------------------------------------------------------------
def add_bevel(obj, width, segments=2):
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(35)
    try:
        mod.harden_normals = True
    except Exception:
        pass
    smooth_all(obj)
    return mod


def add_subsurf(obj, levels=1):
    mod = obj.modifiers.new("Subsurf", "SUBSURF")
    mod.levels = levels
    mod.render_levels = levels
    smooth_all(obj)
    return mod


def add_displace_noise(obj, strength=0.01, size=0.02, seed=0):
    """튀김옷 같은 울퉁불퉁한 표면용 노이즈 변위."""
    tex = bpy.data.textures.new(obj.name + "_noise", "CLOUDS")
    tex.noise_scale = size
    tex.noise_depth = 2
    mod = obj.modifiers.new("Displace", "DISPLACE")
    mod.texture = tex
    mod.strength = strength
    mod.mid_level = 0.5
    mod.texture_coords = "OBJECT"
    empty = bpy.data.objects.new(obj.name + "_noiseOrigin", None)
    link(empty)
    empty.location = (seed * 1.37, seed * 2.11, seed * 0.73)
    mod.texture_coords_object = empty
    return mod


def smooth_all(obj):
    for p in obj.data.polygons:
        p.use_smooth = True


def smooth_by_angle(obj, angle_deg=35):
    """각도 기준으로 하드 엣지를 지정한다 (Blender 4.1+ auto smooth 대체)."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    limit = math.radians(angle_deg)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = e.calc_face_angle(0.0) < limit
        else:
            e.smooth = False
    bm.to_mesh(me)
    bm.free()


def convert_to_mesh(obj, new_name=None):
    """모디파이어/커브를 평가한 결과를 새 메시 오브젝트로 굳힌다 (오퍼레이터 미사용)."""
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    ev = obj.evaluated_get(dg)
    mesh = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
    name = new_name or obj.name
    mats = [s.material for s in obj.material_slots]
    new_obj = bpy.data.objects.new(name + "_tmp", mesh)
    link(new_obj)
    new_obj.matrix_world = obj.matrix_world.copy()
    if obj.type != "MESH":
        mesh.materials.clear()
        for m in mats:
            if m:
                mesh.materials.append(m)
    bpy.data.objects.remove(obj, do_unlink=True)
    new_obj.name = name
    mesh.name = name
    return new_obj


def apply_modifiers(obj):
    if obj.type != "MESH" or len(obj.modifiers) == 0:
        return obj
    weighted = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL") if any(m.type == "BEVEL" for m in obj.modifiers) else None
    if weighted is not None:
        weighted.keep_sharp = True
    return convert_to_mesh(obj)


def apply_transform(obj):
    """위치를 제외한 회전/스케일을 메시에 굳힌다."""
    bpy.context.view_layer.update()
    mw = obj.matrix_world.copy()
    loc = mw.to_translation()
    obj.data.transform(Matrix.Translation(-loc) @ mw)
    obj.matrix_world = Matrix.Translation(loc)


def join(objs, name):
    """여러 메시를 하나로 합친다 (멀티 머티리얼 유지). 원점은 월드 원점."""
    bpy.context.view_layer.update()  # location/rotation 변경을 matrix_world 에 반영
    objs = [apply_modifiers(o) for o in objs]
    bpy.context.view_layer.update()
    bm = bmesh.new()
    mats = []
    for o in objs:
        me = o.data
        local_to_index = []
        for slot in o.material_slots:
            m = slot.material
            if m not in mats:
                mats.append(m)
            local_to_index.append(mats.index(m))
        tmp = me.copy()
        tmp.transform(o.matrix_world)
        for poly in tmp.polygons:
            if local_to_index:
                poly.material_index = local_to_index[min(poly.material_index, len(local_to_index) - 1)]
        bm.from_mesh(tmp)
        bpy.data.meshes.remove(tmp)
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for m in mats:
        mesh.materials.append(m)
    obj = bpy.data.objects.new(name, mesh)
    link(obj)
    return obj


def part(objs, name, pivot=(0, 0, 0), parent=None):
    """objs 를 합쳐 이름 있는 부품을 만들고, 원점을 pivot(월드 좌표)으로 옮긴다.
    Unity 코드가 이름으로 찾아 애니메이션(바구니 이동, 팔다리 흔들기 등)하는 부품에 사용."""
    obj = join(objs, name)
    pv = Vector(pivot)
    obj.data.transform(Matrix.Translation(-pv))
    obj.location = pv
    if parent is not None:
        set_parent(obj, parent)
    return obj


def set_parent(child, parent):
    bpy.context.view_layer.update()
    world = child.matrix_world.copy()
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()
    child.matrix_world = world


def cleanup_helpers():
    """노이즈용 빈 오브젝트/커브 등 내보내기에 불필요한 오브젝트 제거."""
    for o in list(bpy.data.objects):
        if o.type == "EMPTY" and o.name.endswith("_noiseOrigin"):
            bpy.data.objects.remove(o, do_unlink=True)


def stats(objs):
    tris = 0
    for o in objs:
        if o.type == "MESH":
            for p in o.data.polygons:
                tris += len(p.vertices) - 2
    return tris


# ---------------------------------------------------------------------------
# 내보내기
# ---------------------------------------------------------------------------
def export_model(root, category, model_name=None):
    """root(와 그 자식들)를 FBX + .blend 로 저장한다.

    FBX 설정 (Unity 호환):
      - apply_scale_options = FBX_SCALE_ALL : Unity Scale Factor 1, 1 unit = 1 m
      - axis_forward = -Z, axis_up = Y (+ Unity ModelImporter.bakeAxisConversion = true) :
        Unity 에서 회전 (0,0,0), Blender -Y 정면 -> Unity +Z 정면
      - mesh_smooth_type = OFF : 커스텀 노멀(베벨 하든 노멀/웨이티드 노멀)을 그대로 내보냄
    """
    model_name = model_name or root.name
    cleanup_helpers()
    # 모든 메시의 모디파이어를 굳힌다 (자식 포함)
    def walk(o):
        yield o
        for c in o.children:
            yield from walk(c)
    objs = list(walk(root))
    for o in objs:
        if o.type == "MESH" and len(o.modifiers) > 0:
            # 부모 관계 유지하면서 굳히기
            parent = o.parent
            children = list(o.children)
            world = o.matrix_world.copy()
            new_o = apply_modifiers(o)
            new_o.matrix_world = world
            if parent:
                set_parent(new_o, parent)
            for c in children:
                set_parent(c, new_o)
            if o is root:
                root = new_o
    objs = list(walk(root))

    out_dir = os.path.join(UNITY_MODELS_DIR, category)
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(BLEND_DIR, exist_ok=True)
    fbx_path = os.path.join(out_dir, model_name + ".fbx")
    blend_path = os.path.join(BLEND_DIR, model_name + ".blend")

    # 내보내기 대상 외 오브젝트 제거 (blend 파일도 깔끔하게)
    keep = set(o.name for o in objs)
    for o in list(bpy.data.objects):
        if o.name not in keep:
            bpy.data.objects.remove(o, do_unlink=True)

    for o in bpy.context.scene.objects:
        o.select_set(o.name in keep)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,  # 자식 부품 변환 보존 (Unity 쪽 bakeAxisConversion 사용)
        use_mesh_modifiers=True,
        mesh_smooth_type="OFF",
        use_tspace=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )
    bpy.ops.wm.save_as_mainfile(filepath=blend_path, compress=True)
    dims = root.dimensions
    print("[EXPORT] %-24s tris=%6d  root dims=(%.2f, %.2f, %.2f) -> %s" % (
        model_name, stats(objs), dims.x, dims.y, dims.z, os.path.relpath(fbx_path, REPO_ROOT)))
    return fbx_path


def run_main(build_func, category, name):
    """각 모델 스크립트의 공통 진입점."""
    reset_scene()
    root = build_func()
    root.name = name
    export_model(root, category, name)
