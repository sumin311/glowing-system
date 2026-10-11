"""
preview_scene_layout.py - Unity 씬 빌더(ChickenShopSceneBuilder.cs)의 배치를 Blender 로 재현해 미리보기를 렌더링한다.

Unity 없이 레이아웃(겹침, 동선, 크기)을 검증하기 위한 도구:
  - C# 빌더 소스에서 Block / Place / Decor 호출의 좌표를 정규식으로 읽는다 (반복문 배치는 동일 규칙으로 재현)
  - Unity 좌표 (x, y, z) -> Blender (-x, -z, y), Unity Yaw θ -> Blender Z 회전 -θ
  - 손님 동선(줄 서기, 대기석, 입구)과 플레이어 시작 위치를 색 표시
출력: Blender/previews/Scene_TopDown.png, Scene_Hall.png, Scene_Kitchen.png
실제 Unity 조명/재질과는 차이가 있다 (레이아웃 확인용).
"""
import math
import os
import re
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import REPO_ROOT, UNITY_MODELS_DIR  # noqa: E402

BUILDER = os.path.join(REPO_ROOT, "Assets", "ChickenShop", "Editor", "ChickenShopSceneBuilder.cs")
TEX = os.path.join(REPO_ROOT, "Assets", "ChickenShop", "Textures")
OUT = os.path.join(REPO_ROOT, "Blender", "previews")

FLOAT = r"(-?[\d.]+)f?"
VEC = r"new Vector3\(\s*" + FLOAT + r",\s*" + FLOAT + r",\s*" + FLOAT + r"\s*\)"

# 빌더의 환경 머티리얼 변수 -> (텍스처, 색, UV 크기 m)
ENV = {
    "mFloorHall": ("Floor_HallTile", None, 1.2), "mFloorKitchen": ("Floor_KitchenTile", None, 1.2),
    "mPlaster": ("Wall_Plaster", None, 2.0), "mTile": ("Wall_SubwayTile", None, 1.2), "mWood": ("Wood_Planks", None, 1.2),
    "mBrick": ("Wall_Brick", None, 1.6), "mSidewalk": ("Ground_Sidewalk", None, 2.0), "mAsphalt": (None, (0.17, 0.17, 0.19), 1),
    "mCeiling": ("Wall_Plaster", None, 2.0), "mTrim": (None, (0.3, 0.19, 0.11), 1), "mLightPanel": (None, (1, 0.97, 0.9), 1),
    "mLane": (None, (0.95, 0.9, 0.6), 1),
}

PREFAB_MODELS = {
    "p.fridge": ["Kitchen/Fridge"], "p.prep": ["Kitchen/Worktable", ("Props/BatterBowl", (0.38, 0.9, -0.08))],
    "p.fryer": ["Kitchen/Fryer"], "p.trash": ["Props/TrashBin"], "p.terminal": ["Kitchen/OrderTerminal"],
    "p.sauceYangnyeom": ["Kitchen/SauceTub_Yangnyeom"], "p.sauceGanjang": ["Kitchen/SauceTub_Ganjang"],
    "p.packaging": ["Kitchen/Worktable"] + [("Props/ChickenBox", (0.45, 0.9 + i * 0.105, -0.12)) for i in range(4)] + [("Props/PaperBag", (-0.5, 0.9, -0.15))],
    "p.pos": ["Shop/PosRegister"], "p.delivery": [], "p.openSign": ["Shop/OpenSign"],
}

mat_cache = {}


def u2b(v):
    """Unity 좌표 -> Blender 좌표"""
    return Vector((-v[0], -v[2], v[1]))


def env_mat(var):
    if var in mat_cache:
        return mat_cache[var]
    tex, color, uvm = ENV.get(var, (None, (0.8, 0.8, 0.8), 1))
    m = bpy.data.materials.new(var)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.6
    if tex:
        img = bpy.data.images.load(os.path.join(TEX, tex + ".png"))
        it = nt.nodes.new("ShaderNodeTexImage")
        it.image = img
        it.projection = "BOX"
        it.projection_blend = 0.2
        tc = nt.nodes.new("ShaderNodeTexCoord")
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (1 / uvm, 1 / uvm, 1 / uvm)
        nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
        nt.links.new(mp.outputs["Vector"], it.inputs["Vector"])
        nt.links.new(it.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = (*color, 1)
        if var == "mLightPanel":
            bsdf.inputs["Emission Color"].default_value = (1, 0.97, 0.9, 1)
            bsdf.inputs["Emission Strength"].default_value = 4
    mat_cache[var] = m
    return m


def block(name, center, size, var, coll):
    bpy.ops.mesh.primitive_cube_add(size=1, location=u2b(center))
    o = bpy.context.active_object
    o.name = name
    o.scale = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(env_mat(var))
    move(o, coll)
    return o


def move(o, coll):
    for c in o.users_collection:
        c.objects.unlink(o)
    coll.objects.link(o)


def place_model(rel, pos, yaw, coll, parent_offset=None):
    path = os.path.join(UNITY_MODELS_DIR, rel + ".fbx")
    if not os.path.exists(path):
        print("[WARN] 모델 없음", rel)
        return None
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, axis_forward="-Z", axis_up="Y")
    new = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in new if o.parent is None]
    for o in new:
        move(o, coll)
    for r in roots:
        local = Vector(parent_offset) if parent_offset else Vector((0, 0, 0))
        # 프리팹 내부 오프셋(Unity 로컬) 을 yaw 회전 후 더한다
        th = math.radians(yaw)
        lx = local.x * math.cos(th) + local.z * math.sin(th)
        lz = -local.x * math.sin(th) + local.z * math.cos(th)
        r.location = u2b((pos[0] + lx, pos[1] + local.y, pos[2] + lz))
        r.rotation_euler = (0, 0, math.radians(-yaw))
    return roots


def marker(name, pos, color, coll, radius=0.15, height=0.05):
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, location=u2b((pos[0], height / 2, pos[2])))
    o = bpy.context.active_object
    o.name = name
    m = bpy.data.materials.new(name + "_m")
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Emission Color"].default_value = (*color, 1)
    b.inputs["Emission Strength"].default_value = 2
    o.data.materials.append(m)
    move(o, coll)


def wall_x(name, x0, x1, z, thick, height, var, openings, coll):
    openings = sorted(openings)
    cursor = x0
    i = 0

    def piece(a, b, y0, y1):
        nonlocal i
        if b - a < 0.01 or y1 - y0 < 0.01:
            return
        block("%s_%d" % (name, i), ((a + b) / 2, (y0 + y1) / 2, z), (b - a, y1 - y0, thick), var, coll)
        i += 1
    for (ox0, ox1, oy0, oy1) in openings:
        if ox0 > cursor:
            piece(cursor, ox0, 0, height)
        if oy0 > 0:
            piece(ox0, ox1, 0, min(oy0, height))
        if oy1 < height:
            piece(ox0, ox1, oy1, height)
        cursor = max(cursor, ox1)
    if cursor < x1:
        piece(cursor, x1, 0, height)


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    src = open(BUILDER, encoding="utf-8").read()
    game = src[src.index("private static void BuildGameScene"):src.index("private static GameObject Item(")]
    env = bpy.data.collections.new("Env"); bpy.context.scene.collection.children.link(env)
    ceil = bpy.data.collections.new("Ceiling"); bpy.context.scene.collection.children.link(ceil)
    props = bpy.data.collections.new("Props"); bpy.context.scene.collection.children.link(props)
    marks = bpy.data.collections.new("Markers"); bpy.context.scene.collection.children.link(marks)

    # Block 리터럴
    n_block = 0
    for m in re.finditer(r'Block\("(\w+)",\s*\w+,\s*' + VEC + r",\s*" + VEC + r",\s*(m\w+)", game):
        name = m.group(1)
        c = tuple(float(m.group(i)) for i in (2, 3, 4)); s = tuple(float(m.group(i)) for i in (5, 6, 7))
        block(name, c, s, m.group(8), ceil if name == "Ceiling" else env)
        n_block += 1
    # 반복 배치: 주방 조명 패널
    for lp in [(-5.5, 3.18, 3.2), (-1.8, 3.18, 4.1), (1.6, 3.18, 2.8), (5.2, 3.18, 3.2)]:
        block("LightPanel", lp, (1.2, 0.04, 0.6), "mLightPanel", ceil)
    for i in range(8):
        block("Lane", (-17.5 + i * 5, -0.115, -17), (2.4, 0.01, 0.15), "mLane", env)
    # 앞벽 (문 + 창문 개구부)
    door = (-7.0, -5.6, 0.0, 2.2)
    wins = [(-3.9, -2.1, 0.9, 2.3), (-0.4, 1.4, 0.9, 2.3), (3.6, 5.4, 0.9, 2.3)]
    wall_x("Wall_Front", -8.2, 8.2, -6.1, 0.2, 3.2, "mPlaster", [door] + wins, env)
    wall_x("Facade", -8.26, 8.26, -6.23, 0.06, 3.7, "mBrick", [door] + wins, env)
    wall_x("Wainscot_Front", -8, 8, -5.99, 0.02, 0.9, "mWood", [door], env)

    # Decor 리터럴
    n_decor = 0
    for m in re.finditer(r'Decor\("([\w/]+)",\s*\w+,\s*' + VEC + r",\s*" + FLOAT + r"f?", game):
        place_model(m.group(1), tuple(float(m.group(i)) for i in (2, 3, 4)), float(m.group(5)), props)
        n_decor += 1
    # 반복 배치: 카운터, 창문, 테이블/의자, 펜던트
    for i in range(4):
        place_model("Shop/Counter", (-4.2 + i * 2, 0, -0.4), 180, props)
    for w in wins:
        place_model("Shop/Window", ((w[0] + w[1]) / 2, 0.9, -6.1), 180, props)
    for tx in (2.4, 5.6):
        place_model("Shop/Table", (tx, 0, -4.4), 0, props)
        place_model("Shop/Chair", (tx - 0.62, 0, -4.4), 90, props)
        place_model("Shop/Chair", (tx + 0.62, 0, -4.4), -90, props)
    for hp in [(-2.2, 3.2, -2.6), (1.0, 3.2, -2.6), (2.4, 3.2, -4.4), (5.6, 3.2, -4.4)]:
        place_model("Shop/PendantLamp", hp, 0, ceil)

    # Place(프리팹) 리터럴
    n_place = 0
    for m in re.finditer(r"Place\((p\.\w+),\s*\w+,\s*" + VEC + r",\s*" + FLOAT + r"f?", game):
        key = m.group(1)
        pos = tuple(float(m.group(i)) for i in (2, 3, 4)); yaw = float(m.group(5))
        for entry in PREFAB_MODELS.get(key, []):
            if isinstance(entry, tuple):
                place_model(entry[0], pos, yaw, props, entry[1])
            else:
                place_model(entry, pos, yaw, props)
        if key == "p.delivery":
            block("PickupTray", (pos[0], pos[1] + 0.012, pos[2]), (0.6, 0.024, 0.45), "mTrim", props)
        n_place += 1

    # 손님 동선 / 플레이어
    for i in range(4):
        marker("QueueSlot%d" % i, (-2.2, 0, -1.3 - i * 0.9), (1, 0.6, 0.1), marks)
    for i in range(7):
        marker("WaitSpot%d" % i, (0.3 + i * 1.05, 0, -2.45), (0.2, 0.6, 1), marks)
    for name, p in (("Spawn", (-12, 0, -7.8)), ("DoorOut", (-6.3, 0, -7.0)), ("DoorIn", (-6.3, 0, -5.25)), ("QueueEntry", (-2.2, 0, -4.8)), ("Pickup", (2.2, 0, -1.3))):
        marker(name, p, (0.2, 1, 0.3), marks, 0.12)
    marker("Player", (-3.5, 0, 1.5), (1, 0.1, 0.6), marks, 0.3, 1.8)
    print("[LAYOUT] blocks=%d decor=%d prefabs=%d" % (n_block, n_decor, n_place))
    return ceil


def setup_render(res_x, res_y, samples=32):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    sc.render.resolution_x, sc.render.resolution_y = res_x, res_y
    sc.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("w"); sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.75, 0.82, 0.92, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.6


def add_light(kind, loc, energy, size=1.0, rot=None):
    ld = bpy.data.lights.new("L", kind); ld.energy = energy
    if kind == "AREA": ld.size = size
    if kind == "POINT": ld.shadow_soft_size = 0.3
    lo = bpy.data.objects.new("L", ld); bpy.context.scene.collection.objects.link(lo)
    lo.location = loc
    if rot: lo.rotation_euler = rot
    return lo


def camera(loc, look_at, lens=24, ortho=None, roll=0.0):
    cd = bpy.data.cameras.new("cam")
    if ortho:
        cd.type = "ORTHO"; cd.ortho_scale = ortho
    else:
        cd.lens = lens
    cam = bpy.data.objects.new("cam", cd); bpy.context.scene.collection.objects.link(cam)
    cam.location = loc
    d = Vector(look_at) - Vector(loc)
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    if roll:
        cam.rotation_euler.rotate_axis("Z", math.radians(roll))
    bpy.context.scene.camera = cam
    return cam


def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[RENDER]", path)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    ceil = build()
    setup_render(1600, 1300, 24)
    add_light("SUN", (0, 0, 10), 2.2, rot=(math.radians(35), 0, math.radians(200)))
    # 실내 조명 (Unity 포인트 라이트 위치)
    for p in [(-5.5, 2.83, 3.2), (-1.8, 2.83, 4.1), (1.6, 2.83, 2.8), (5.2, 2.83, 3.2), (-2.2, 2.25, -2.6), (1.0, 2.25, -2.6), (2.4, 2.25, -4.4), (5.6, 2.25, -4.4)]:
        add_light("POINT", u2b(p), 80)
    # 1) 탑뷰 (천장 숨김)
    ceil.hide_render = True
    # 위가 주방(+Z), 오른쪽이 Unity +X 가 되도록 180도 롤
    camera(u2b((0, 30, -2.6)), u2b((0, 0, -2.6)), ortho=26, roll=180)
    render(os.path.join(OUT, "Scene_TopDown.png"))
    ceil.hide_render = False
    # 2) 홀 시점 (입구 쪽에서 카운터/주방 방향)
    bpy.context.scene.render.resolution_x, bpy.context.scene.render.resolution_y = 1600, 900
    camera(u2b((-6.0, 1.7, -5.3)), u2b((0.5, 1.2, 0.5)), lens=18)
    render(os.path.join(OUT, "Scene_Hall.png"))
    # 3) 플레이어 시작 위치에서 주방 뒷벽 방향
    camera(u2b((1.5, 1.62, -0.0)), u2b((-2.5, 1.0, 5.5)), lens=18)
    render(os.path.join(OUT, "Scene_Kitchen.png"))
