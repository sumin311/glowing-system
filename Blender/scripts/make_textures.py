"""
make_textures.py - 바닥/벽용 타일링 텍스처를 절차적으로 생성한다 (numpy + Blender 이미지 저장).
출력: Assets/ChickenShop/Textures/*.png  (모두 이음매 없이 반복되는 512x512 텍스처)
Unity 에디터 스크립트(ChickenShopMaterialLibrary)가 이 텍스처로 바닥/벽 머티리얼을 만든다.
"""
import os
import sys

import bpy
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import REPO_ROOT  # noqa: E402

OUT = os.path.join(REPO_ROOT, "Assets", "ChickenShop", "Textures")
N = 512
rng = np.random.default_rng(7)


def tileable_noise(scale, octaves=4):
    """주기적(타일링) 값 노이즈."""
    out = np.zeros((N, N))
    amp, total = 1.0, 0.0
    for o in range(octaves):
        cells = scale * (2 ** o)
        grid = rng.random((cells, cells))
        xs = np.linspace(0, cells, N, endpoint=False)
        i0 = np.floor(xs).astype(int); t = xs - i0; i1 = (i0 + 1) % cells
        t = t * t * (3 - 2 * t)
        a = grid[np.ix_(i0, i0)]; b = grid[np.ix_(i0, i1)]; c = grid[np.ix_(i1, i0)]; d = grid[np.ix_(i1, i1)]
        tx = t[None, :]; ty = t[:, None]
        out += amp * ((a * (1 - tx) + b * tx) * (1 - ty) + (c * (1 - tx) + d * tx) * ty)
        total += amp; amp *= 0.5
    return out / total


def save(name, rgb):
    rgb = np.clip(rgb, 0, 1)
    img = bpy.data.images.new(name, N, N, alpha=False)
    px = np.ones((N, N, 4))
    px[..., :3] = rgb[::-1]  # Blender 이미지는 아래쪽부터
    img.pixels = px.ravel().tolist()
    img.filepath_raw = os.path.join(OUT, name + ".png")
    img.file_format = "PNG"
    img.save()
    print("[TEX]", name)


def grout_mask(tiles_x, tiles_y, width, offset_rows=False):
    y, x = np.mgrid[0:N, 0:N] / N
    row = np.floor(y * tiles_y)
    xx = x * tiles_x + (0.5 * (row % 2) if offset_rows else 0)
    fx = xx - np.floor(xx); fy = y * tiles_y - row
    d = np.minimum(np.minimum(fx, 1 - fx) / tiles_x * tiles_y, np.minimum(fy, 1 - fy))
    return (d < width).astype(float), np.floor(xx) + row * 37


def tile_floor():
    # 홀 바닥: 따뜻한 베이지 600각 타일 2x2 (텍스처 1장 = 1.2 m)
    g, idx = grout_mask(2, 2, 0.012)
    base = np.array([0.86, 0.78, 0.66])
    n = tileable_noise(4)[..., None]
    var = (np.sin(idx * 12.9898) * 43758.5453 % 1.0)[..., None] * 0.06
    rgb = base * (0.9 + 0.15 * n) - var
    rgb = rgb * (1 - g[..., None]) + np.array([0.55, 0.5, 0.45]) * g[..., None]
    save("Floor_HallTile", rgb)


def kitchen_floor():
    # 주방 바닥: 회색 미끄럼 방지 타일 4x4 + 점 패턴
    g, idx = grout_mask(4, 4, 0.02)
    base = np.array([0.55, 0.56, 0.57])
    n = tileable_noise(8)[..., None]
    y, x = np.mgrid[0:N, 0:N]
    dots = (((x % 16) - 8) ** 2 + ((y % 16) - 8) ** 2 < 6).astype(float)[..., None]
    rgb = base * (0.92 + 0.12 * n) - dots * 0.05
    rgb = rgb * (1 - g[..., None]) + np.array([0.3, 0.3, 0.3]) * g[..., None]
    save("Floor_KitchenTile", rgb)


def subway_tile():
    # 주방 벽: 흰색 서브웨이 타일 (엇갈림)
    g, idx = grout_mask(4, 8, 0.03, offset_rows=True)
    n = tileable_noise(6)[..., None]
    base = np.array([0.95, 0.95, 0.93])
    rgb = base * (0.95 + 0.06 * n)
    rgb = rgb * (1 - g[..., None]) + np.array([0.72, 0.72, 0.7]) * g[..., None]
    save("Wall_SubwayTile", rgb)


def plaster():
    n = tileable_noise(3, 5)[..., None]
    base = np.array([0.96, 0.9, 0.78])
    save("Wall_Plaster", base * (0.93 + 0.1 * n))


def wood_planks():
    # 홀 벽 하단 웨인스코팅/천장용 나무 판재 (세로 6장)
    y, x = np.mgrid[0:N, 0:N] / N
    plank = np.floor(x * 6)
    fx = x * 6 - plank
    grain = tileable_noise(3, 3)
    stripes = np.sin((y * 40 + grain * 6 + plank * 1.7) * np.pi) * 0.5 + 0.5
    base = np.array([0.62, 0.42, 0.25])
    shade = (np.sin(plank * 7.1) * 0.5 + 0.5)[..., None] * 0.12
    rgb = base[None, None, :] * (0.85 + 0.15 * stripes[..., None]) - shade
    gap = ((fx < 0.015) | (fx > 0.985)).astype(float)[..., None]
    rgb = rgb * (1 - gap) + np.array([0.25, 0.16, 0.1]) * gap
    save("Wood_Planks", rgb)


def brick():
    g, idx = grout_mask(4, 8, 0.05, offset_rows=True)
    var = (np.sin(idx * 78.233) * 12543.21 % 1.0)[..., None]
    n = tileable_noise(10)[..., None]
    base = np.array([0.62, 0.3, 0.22])
    rgb = base * (0.8 + 0.25 * var) * (0.9 + 0.15 * n)
    rgb = rgb * (1 - g[..., None]) + np.array([0.75, 0.72, 0.68]) * g[..., None]
    save("Wall_Brick", rgb)


def sidewalk():
    g, idx = grout_mask(2, 2, 0.01)
    n = tileable_noise(12)[..., None]
    rgb = np.array([0.6, 0.6, 0.6]) * (0.85 + 0.2 * n)
    rgb = rgb * (1 - g[..., None]) + np.array([0.4, 0.4, 0.4]) * g[..., None]
    save("Ground_Sidewalk", rgb)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for f in (tile_floor, kitchen_floor, subway_tile, plaster, wood_planks, brick, sidewalk):
        f()
