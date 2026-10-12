"""
food_chicken_common.py - 치킨 조각 더미(쟁반 위) 공용 생성기.
조리 단계별 모델이 같은 형태를 공유하고 표면만 바뀐다 (Unity 에서 단계 전환 시 모델 교체가 자연스럽게 보이도록).
  raw      : 분홍빛 생닭 토막, 매끈한 표면
  battered : 튀김옷(반죽) 입힌 상태, 약간 울퉁불퉁
  fried    : 황금색 바삭한 후라이드
  yangnyeom: 빨간 양념 코팅 + 깨
  ganjang  : 짙은 간장 코팅 + 깨
  burnt    : 과조리(탄) 상태
쟁반 크기 0.3 x 0.22 m, 원점 = 쟁반 바닥 중앙.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cs_common import *  # noqa

VARIANTS = {
    #            material,             crust(변위 세기), sesame
    "raw":       ("M_ChickenRaw",       0.0,   False),
    "battered":  ("M_ChickenBatter",    0.006, False),
    "fried":     ("M_ChickenFried",     0.014, False),
    "yangnyeom": ("M_ChickenYangnyeom", 0.008, True),
    "ganjang":   ("M_ChickenGanjang",   0.008, True),
    "burnt":     ("M_ChickenBurnt",     0.014, False),
}

# (종류, x, y, z, 회전Z(도), 기울기X(도))
PIECES = [
    ("drum", -0.075, -0.035, 0.035, 15, 0),
    ("drum", 0.065, 0.045, 0.035, 195, 0),
    ("wing", 0.075, -0.05, 0.03, -30, 0),
    ("wing", -0.08, 0.06, 0.03, 140, 0),
    ("chunk", 0.0, 0.0, 0.04, 0, 0),
    ("chunk", -0.015, -0.065, 0.035, 45, 10),
    ("chunk", 0.02, 0.065, 0.035, 80, -8),
    ("chunk", -0.005, 0.0, 0.085, 30, 15),
]


def _crust(obj, strength, seed):
    """굵은 덩어리 + 잔 튀김옷 2단 변위로 바삭한 표면을 만든다."""
    # 형태 불규칙성 (모든 단계 공통 - 생닭도 매끈한 공 모양이 되지 않도록)
    add_displace_noise(obj, strength=0.012, size=0.035, seed=seed * 3 + 1)
    if strength > 0:
        add_displace_noise(obj, strength=strength, size=0.006, seed=seed * 3 + 2)


def drumstick(mat, crust, seed):
    """북채(닭다리): 회전체 프로파일로 끝이 두툼하고 뼈 쪽이 가늘어지는 형태."""
    parts = []
    prof = [(0.0, 0.0), (0.016, 0.003), (0.03, 0.014), (0.037, 0.034), (0.036, 0.055),
            (0.029, 0.074), (0.018, 0.088), (0.011, 0.096), (0.0, 0.098)]
    meat = lathe("drum_meat", prof, (0, 0, 0), mat, segs=20)
    meat.rotation_euler = (math.radians(-90), 0, 0)   # +Y 방향으로 눕힘
    _crust(meat, crust, seed)
    parts.append(meat)
    parts.append(cylinder("drum_bone", 0.0075, 0.04, (0, 0.11, 0), "M_Bone", verts=10, rot=(90, 0, 0)))
    parts.append(sphere("drum_knob", 0.012, (0, 0.13, 0), "M_Bone", segs=10, rings=6, scale=(1.4, 1, 0.9)))
    return parts


def wing(mat, crust, seed):
    """날개: 납작한 두 마디가 꺾인 형태."""
    a = sphere("wing_a", 0.03, (0, -0.022, 0), mat, segs=18, rings=10, scale=(0.95, 1.5, 0.55))
    b = sphere("wing_b", 0.024, (0.022, 0.032, 0), mat, segs=16, rings=10, scale=(0.9, 1.7, 0.45))
    b.rotation_euler = (0, 0, math.radians(-40))
    _crust(a, crust, seed)
    _crust(b, crust, seed + 7)
    return [a, b]


def chunk(mat, crust, seed):
    """순살/몸통 토막: 불규칙한 덩어리."""
    rnd = random.Random(seed)
    c = sphere("chunk", 0.042, (0, 0, 0), mat, segs=22, rings=14,
               scale=(1.0 + rnd.uniform(-0.1, 0.15), 0.8 + rnd.uniform(-0.1, 0.1), 0.62))
    # 버텍스 무작위 흔들기 (실루엣 다양화)
    for v in c.data.vertices:
        v.co.x += rnd.uniform(-0.004, 0.004)
        v.co.y += rnd.uniform(-0.004, 0.004)
    c.rotation_euler = (math.radians(90), 0, 0)  # 극점(핀치)이 옆을 보도록
    apply_transform(c)
    _crust(c, crust * 1.15, seed)
    return [c]


def tray():
    parts = []
    parts.append(box("tray_base", (0.3, 0.22, 0.006), (0, 0, 0.003), "M_Stainless", bevel=0.002))
    for (sx, sy, w, d) in ((0, -1, 0.3, 0.008), (0, 1, 0.3, 0.008), (-1, 0, 0.008, 0.22), (1, 0, 0.008, 0.22)):
        parts.append(box("rim", (w, d, 0.02), (sx * 0.146, sy * 0.106, 0.012), "M_Stainless", bevel=0.002))
    parts.append(box("paper", (0.26, 0.18, 0.002), (0, 0, 0.007), "M_BoxWhite" if True else "M_Kraft", bevel=0))
    return parts


def build_pile(variant):
    random.seed(42)
    mat, crust, sesame = VARIANTS[variant]
    p = tray()
    for i, (kind, x, y, z, rz, rx) in enumerate(PIECES):
        if kind == "drum":
            pcs = drumstick(mat, crust, i)
        elif kind == "wing":
            pcs = wing(mat, crust, i)
        else:
            pcs = chunk(mat, crust, i)
        g = join(pcs, "piece%d" % i)
        g.rotation_euler = (math.radians(rx), 0, math.radians(rz))
        g.location = (x, y, z)
        p.append(g)
    if sesame:
        for k in range(26):
            ang = random.uniform(0, 2 * math.pi)
            r = random.uniform(0.0, 0.09)
            s = sphere("sesame%d" % k, 0.0035, (math.cos(ang) * r, math.sin(ang) * r * 0.75, 0.075 + random.uniform(0, 0.06)),
                       "M_Sesame", segs=6, rings=4, scale=(1.0, 0.6, 0.4))
            s.rotation_euler = (0, 0, ang)
            p.append(s)
    return join(p, "Chicken")


def export_variant(variant, name):
    reset_scene()
    root = build_pile(variant)
    root.name = name
    export_model(root, "Food", name)
