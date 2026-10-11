"""
build_all.py - 모든 모델(.blend + .fbx)과 텍스처를 한 번에 다시 생성한다.

실행 방법 (둘 중 하나):
  1) Blender 설치본:   blender -b -P Blender/scripts/build_all.py
  2) bpy 파이썬 모듈:  pip install bpy==5.1.2  (Python 3.13)  후  python Blender/scripts/build_all.py
  선택: --previews  를 붙이면 Blender/previews/*.png 미리보기도 렌더링 (Cycles CPU, 수 분 소요)

제작 순서: 매장 -> 주방 기구 -> 음식/소품 -> 손님
"""
import os
import runpy
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

ORDER = [
    # 매장
    "shop_counter", "shop_table", "shop_chair", "shop_sign", "shop_open_sign", "shop_pendant_lamp",
    "shop_wall_shelf", "shop_menu_board", "shop_window", "shop_door_frame", "shop_plant", "shop_pos",
    # 주방
    "kitchen_fryer", "kitchen_worktable", "kitchen_fridge", "kitchen_sink", "kitchen_sauce_tub",
    "kitchen_storage_rack", "kitchen_exhaust_hood", "kitchen_order_terminal",
    # 음식 / 소품
    "food_chicken_raw", "food_chicken_battered", "food_chicken_fried", "food_chicken_yangnyeom",
    "food_chicken_ganjang", "food_chicken_burnt",
    "prop_chicken_box", "prop_paper_bag", "prop_tongs", "prop_oil_can", "prop_drink", "prop_trash_bin", "prop_batter_bowl",
    # 손님
    "char_customer",
    # 텍스처
    "make_textures",
]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    t0 = time.time()
    failed = []
    for name in ORDER:
        try:
            runpy.run_path(os.path.join(HERE, name + ".py"), run_name="__main__")
        except Exception as ex:  # 하나가 실패해도 나머지는 계속
            failed.append((name, ex))
            print("[FAIL]", name, ex)
    if "--previews" in argv:
        import render_preview
        for root, _, files in os.walk(render_preview.UNITY_MODELS_DIR):
            for f in sorted(files):
                if f.endswith(".fbx"):
                    rel = os.path.relpath(os.path.join(root, f), render_preview.UNITY_MODELS_DIR)[:-4]
                    render_preview.render(rel.replace(os.sep, "/"))
    print("[DONE] %d scripts, %d failed, %.1fs" % (len(ORDER), len(failed), time.time() - t0))
    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
