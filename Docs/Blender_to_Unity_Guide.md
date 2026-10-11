# Blender 모델 제작 → Unity 연동 가이드

이 문서는 `Blender/scripts` 의 모델을 수정·추가하고 Unity 게임에 연결하는 전체 절차입니다.

## 1. 준비

| 도구 | 버전 | 비고 |
|---|---|---|
| Blender | 4.2 LTS 이상 (5.1 에서 검증) | 또는 `pip install bpy==5.1.2` (Python 3.13) |
| Unity | 2022.3.62f1 | Built-in Render Pipeline |

## 2. 모델 생성/재생성

```bash
# 전체 (매장 → 주방 → 음식/소품 → 손님 → 텍스처 순서)
blender -b -P Blender/scripts/build_all.py
blender -b -P Blender/scripts/build_all.py -- --previews     # 미리보기 PNG 도 렌더링

# 하나만
blender -b -P Blender/scripts/kitchen_fryer.py

# bpy 모듈을 쓰는 경우 "blender -b -P" 대신 "python"
python Blender/scripts/kitchen_fryer.py
```

출력 위치
- FBX: `Assets/ChickenShop/Models/<Shop|Kitchen|Food|Props|Characters>/<이름>.fbx`
- 원본: `Blender/blend/<이름>.blend` (Blender 에서 열어 직접 수정 가능)
- 미리보기: `Blender/previews/<이름>.png`

## 3. 모델링 규칙 (모든 스크립트 공통 – `cs_common.py`)

1. **단위**: 1 Blender unit = 1 m. Scene Unit Scale 1.0, Metric.
2. **정면**: Blender **-Y** 방향. (Unity 에서 `transform.forward` = +Z 가 정면)
3. **원점**: 바닥 중앙 (Z=0). 벽걸이(간판, 선반, 메뉴판, 후드)는 벽에 닿는 면, 천장 조명은 천장 고정점.
4. **베벨**: 각진 모델은 Bevel 모디파이어(폭 0.004~0.02 m, 2~3 세그먼트, Harden Normals) + Weighted Normal 로 미드폴리 실루엣과 부드러운 하이라이트를 만든다.
5. **머티리얼 이름**: `M_` 접두사 + 팔레트 이름 (`M_Stainless`, `M_ChickenFried` …). 같은 이름은 Unity 에서 **하나의 .mat 로 공유**된다.
6. **움직이는 부품**은 별도 오브젝트로 분리하고, 원점을 회전/이동 기준점(관절)에 둔다. 이름은 Unity 코드가 찾는 이름과 같아야 한다.

| 모델 | 부품 이름 | 사용하는 스크립트 |
|---|---|---|
| Fryer | `Basket` | `FryerStation` (조리 중 하강) |
| OpenSign | `Board` | `ShopOpenSign` (OPEN/CLOSED 회전) |
| PendantLamp | `Bulb` | 씬 빌더가 Point Light 배치 |
| Customer | `Head`, `Arm_L`, `Arm_R`, `Leg_L`, `Leg_R`, `Hair_Short/Long/Bun`, `Cap`, `Glasses`, `Backpack` | `CustomerAnimator`, `CustomerAppearance` |

## 4. FBX 내보내기 설정 (`cs_common.export_model`)

| 옵션 | 값 | 이유 |
|---|---|---|
| Selected Objects | ✔ | 모델만 |
| Object Types | Mesh, Empty | |
| Apply Scalings | **FBX All** | Unity Scale Factor 1 |
| Forward / Up | **-Z Forward / Y Up** | |
| Apply Transform (bake_space_transform) | ✘ | 자식 부품 위치가 틀어지는 문제 방지 → 대신 Unity 의 *Bake Axis Conversion* 사용 |
| Apply Modifiers | ✔ | 베벨/변위 적용 |
| Smoothing | Normals Only | 커스텀 노멀 유지 |
| Add Leaf Bones / Bake Animation | ✘ | |

Blender UI 에서 직접 내보낼 때도 위 값을 그대로 사용하면 됩니다.

## 5. Unity 가져오기 설정 (자동)

`Assets/ChickenShop/Editor/ChickenShopModelPostprocessor.cs` 가 `Assets/ChickenShop/Models/` 아래 FBX 에 자동 적용합니다.

- Scale Factor 1, **Convert Units / Use File Scale** ✔
- **Bake Axis Conversion** ✔ → 루트 회전이 (-90,0,0) 이 아닌 (0,0,0)
- Import Normals: Import, Tangents: Calculate Mikktspace
- Import Cameras / Lights / Visibility / BlendShapes / Animation ✘, Animation Type None
- Generate Colliders ✘ (프리팹에서 BoxCollider 로 처리)
- Material Creation Mode: Import via MaterialDescription

수동으로 확인하려면 FBX 선택 → Inspector → Model 탭에서 위 값이 들어가 있는지 보면 됩니다.

## 6. 머티리얼 공유 / 보정

`Chicken Shop > Create Game Scene` (또는 `Chicken Shop > Reimport Models And Share Materials`) 실행 시:
1. 프로젝트 색 공간을 Linear 로 설정 (바뀌었다면 FBX 를 다시 가져와 Blender 색상과 일치시킴)
2. FBX 안의 `M_*` 머티리얼을 `Assets/ChickenShop/Materials/Models/M_*.mat` 로 추출
3. 모든 FBX 의 같은 이름 머티리얼을 그 .mat 로 리맵 (Remapped Materials)
4. 이름 규칙으로 재질감 보정: `M_Stainless*` 금속 0.85, 소스/양념/기름/콜라 광택 0.82, `M_Glass` 반투명, `M_LightBulb`·`M_Screen` 발광 …

→ `.mat` 의 색이나 광택을 Unity 에서 바꾸면 그 머티리얼을 쓰는 모든 모델에 반영됩니다.

## 7. 프리팹 구성

```
Prefabs/Stations/FryerStation.prefab
  FryerStation (FryerStation, BoxCollider)       ← 게임 로직/충돌
  ├─ Model (Fryer.fbx 인스턴스)                  ← 모델만 교체하면 됨
  │    └─ Basket
  ├─ ItemAnchor                                   ← 음식이 놓이는 위치
  ├─ ProgressBar (WorldProgressBar)               ← 조리 진행도
  ├─ OilBubbles (ParticleSystem)
  └─ NameLabel (WorldLabel)                       ← 상태 표시
```

- Collider: 모델의 Renderer 경계로 계산한 BoxCollider (+약간의 여유). 손님은 CapsuleCollider + Kinematic Rigidbody.
- 모델을 바꾸려면: ① Blender 스크립트 수정 후 재생성 (같은 이름이면 자동 반영) 또는 ② 프리팹을 열어 `Model` 자식을 다른 모델로 교체.
- FBX 가 없으면 빌더가 같은 크기의 기본 도형(대체 모델)을 만들어 게임은 계속 동작합니다 (Console 경고 표시).

## 8. 새 모델 추가 예시

1. `Blender/scripts/prop_napkin.py` 생성:
   ```python
   import os, sys
   sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
   from cs_common import *

   def build():
       p = [box("holder", (0.12, 0.08, 0.1), (0, 0, 0.05), "M_StainlessDark", bevel=0.008)]
       return join(p, "Napkin")

   if __name__ == "__main__":
       run_main(build, "Props", "Napkin")
   ```
2. `build_all.py` 의 `ORDER` 에 `"prop_napkin"` 추가 후 실행 → `Models/Props/Napkin.fbx` 생성
3. Unity 로 돌아오면 자동 가져오기. 씬에 배치하려면 `ChickenShopSceneBuilder.BuildGameScene` 에
   `Decor("Props/Napkin", hall, new Vector3(2.4f, 0.75f, -4.4f), 0f, false);` 추가 → `Create Game Scene` 재실행.
4. 레이아웃 미리보기: `python Blender/scripts/preview_scene_layout.py` (Unity 없이 배치 확인)

## 9. 점검 체크리스트

- [ ] 크기: 미리보기 로그의 `root dims` 가 실제 크기(m)와 맞는가 (튀김기 0.6 x 0.82 x 1.23 등)
- [ ] 피벗: 바닥 중앙에 있는가 (`render_preview.py` 출력의 `minZ=0.000`)
- [ ] 회전: Unity 에서 루트 회전 (0,0,0), 정면이 +Z 인가
- [ ] 머티리얼: `Materials/Models` 에 `M_*.mat` 가 있고 분홍색(누락)이 없는가
- [ ] Collider: 프리팹 루트에 BoxCollider 가 모델을 감싸는가
- [ ] 부품 이름: 위 표의 이름과 일치하는가
