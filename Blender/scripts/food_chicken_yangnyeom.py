"""
food_chicken_yangnyeom.py - 치킨 조리 단계 모델: Chicken_Yangnyeom (yangnyeom). 형태는 food_chicken_common.py 공용.
Unity: ChickenItem 이 단계(ChickenStage/SauceType)에 맞춰 이 모델로 교체한다.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from food_chicken_common import export_variant  # noqa: E402

if __name__ == "__main__":
    export_variant("yangnyeom", "Chicken_Yangnyeom")
