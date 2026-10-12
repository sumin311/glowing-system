"""
render_preview.py - 내보낸 FBX 를 다시 불러와(가져오기 검증) 미리보기 PNG 를 렌더링한다.

사용: python render_preview.py <Category/Name> [<Category/Name> ...]
      (Assets/ChickenShop/Models/<Category>/<Name>.fbx 를 읽어 Blender/previews/<Name>.png 저장)
FBX 를 다시 가져온 뒤 루트의 회전/스케일, 크기, 머티리얼 슬롯을 출력해 Unity 연동 설정을 점검한다.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from cs_common import UNITY_MODELS_DIR, REPO_ROOT  # noqa: E402

PREVIEW_DIR = os.path.join(REPO_ROOT, "Blender", "previews")


def render(spec, samples=24, res=420):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path = os.path.join(UNITY_MODELS_DIR, spec + ".fbx")
    # Unity 와 같은 축 설정으로 다시 가져와 Blender 좌표로 되돌린다
    bpy.ops.import_scene.fbx(filepath=path, axis_forward="-Z", axis_up="Y")
    hide = [h for h in os.environ.get("PREVIEW_HIDE", "").split(",") if h]
    for o in list(bpy.context.scene.objects):
        if any(o.name.startswith(h) for h in hide):
            bpy.data.objects.remove(o, do_unlink=True)
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    roots = [o for o in bpy.context.scene.objects if o.parent is None]
    mins = Vector((1e9, 1e9, 1e9)); maxs = Vector((-1e9, -1e9, -1e9))
    mats = set()
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mins = Vector(map(min, mins, w)); maxs = Vector(map(max, maxs, w))
        for s in o.material_slots:
            if s.material:
                mats.add(s.material.name)
    size = maxs - mins
    for r in roots:
        print("[CHECK] %s root=%s rot=(%.1f,%.1f,%.1f) scale=(%.3f,%.3f,%.3f) size=(%.2f,%.2f,%.2f) minZ=%.3f mats=%s" % (
            spec, r.name, *[math.degrees(a) for a in r.rotation_euler], *r.scale, size.x, size.y, size.z, mins.z, sorted(mats)))
    center = (mins + maxs) / 2
    radius = max(size.length / 2, 0.05)
    # 카메라: 정면(-Y) 왼쪽 위 3/4 시점
    cam_data = bpy.data.cameras.new("cam"); cam_data.lens = 50
    cam = bpy.data.objects.new("cam", cam_data); bpy.context.scene.collection.objects.link(cam)
    direction = Vector((-0.55, -1.0, 0.55)).normalized()
    dist = radius / math.tan(math.radians(18)) * 1.05
    cam.location = center + direction * dist
    cam.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam
    # 조명
    for loc, energy in (((2, -3, 4), 260), ((-3, -1, 2), 90), ((1, 3, 3), 140)):
        ld = bpy.data.lights.new("l", "AREA"); ld.energy = energy * max(radius, 0.3) ** 2; ld.size = 3
        lo = bpy.data.objects.new("l", ld); bpy.context.scene.collection.objects.link(lo)
        lo.location = center + Vector(loc) * max(radius, 0.3) * 1.5
        lo.rotation_euler = (center - lo.location).to_track_quat("-Z", "Y").to_euler()
    world = bpy.data.worlds.new("w"); bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.82, 0.85, 0.9, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.35
    # 바닥
    bpy.ops.mesh.primitive_plane_add(size=radius * 20, location=(center.x, center.y, mins.z))
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    sc.render.resolution_x = res; sc.render.resolution_y = res
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "Standard"
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    suffix = os.environ.get("PREVIEW_SUFFIX", "")
    sc.render.filepath = os.path.join(PREVIEW_DIR, os.path.basename(spec) + suffix + ".png")
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    for s in sys.argv[1:]:
        render(s)
