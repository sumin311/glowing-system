using System;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>매장 평판 (0~100). 손님 수, 해금 조건, 팁에 영향을 준다.</summary>
    [DefaultExecutionOrder(-90)]
    public class ReputationManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] private float startingReputation = 50f;

        public float Value { get; private set; }

        /// <summary>(새 값, 변화량, 사유)</summary>
        public event Action<float, float, string> Changed;

        /// <summary>별점 1~5 (0.5 단위)</summary>
        public float Stars
        {
            get { return Mathf.Round(Mathf.Lerp(1f, 5f, Value / 100f) * 2f) / 2f; }
        }

        private void Awake()
        {
            Value = startingReputation;
        }

        public void ResetForNewGame()
        {
            Set(startingReputation);
        }

        public void Set(float value)
        {
            Value = Mathf.Clamp(value, 0f, 100f);
            Changed?.Invoke(Value, 0f, null);
        }

        public void Change(float delta, string reason)
        {
            if (Mathf.Approximately(delta, 0f)) return;
            float before = Value;
            Value = Mathf.Clamp(Value + delta, 0f, 100f);
            GameLog.Info("Reputation", string.Format("{0:0.0} -> {1:0.0} ({2})", before, Value, reason));
            Changed?.Invoke(Value, Value - before, reason);
        }

        public static string StarText(float stars)
        {
            int full = Mathf.FloorToInt(stars);
            bool half = stars - full >= 0.5f;
            var sb = new System.Text.StringBuilder(5);
            for (int i = 0; i < 5; i++)
            {
                if (i < full) sb.Append('★');
                else if (i == full && half) sb.Append('☆');
                else sb.Append('·');
            }
            return sb.ToString();
        }
    }
}
