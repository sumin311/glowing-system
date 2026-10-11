using UnityEngine;

namespace ChickenShop
{
    /// <summary>주문을 마친 손님이 음식을 기다리는 자리 (자리마다 1명 - 겹침 방지).</summary>
    public class WaitingArea : MonoBehaviour
    {
        [SerializeField] private Transform[] spots = new Transform[0];

        private CustomerAgent[] occupants;

        private void Awake()
        {
            if (spots == null || spots.Length == 0)
            {
                spots = new Transform[transform.childCount];
                for (int i = 0; i < spots.Length; i++) spots[i] = transform.GetChild(i);
            }
            occupants = new CustomerAgent[spots.Length];
            if (spots.Length == 0) GameLog.Error("Waiting", "대기 자리가 없습니다.", this);
        }

        public int Capacity { get { return spots.Length; } }

        public int FreeCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < occupants.Length; i++) if (occupants[i] == null) n++;
                return n;
            }
        }

        /// <summary>빈 자리를 예약하고 index 반환 (-1 = 없음). 가까운 빈 자리를 우선한다.</summary>
        public int Reserve(CustomerAgent c, Vector3 from)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < occupants.Length; i++)
            {
                if (occupants[i] != null) continue;
                float d = (spots[i].position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            if (best >= 0) occupants[best] = c;
            return best;
        }

        public Transform GetSpot(int index)
        {
            return index >= 0 && index < spots.Length ? spots[index] : null;
        }

        public void Release(int index, CustomerAgent c)
        {
            if (index >= 0 && index < occupants.Length && occupants[index] == c) occupants[index] = null;
        }

        public void Clear()
        {
            if (occupants == null) return;
            for (int i = 0; i < occupants.Length; i++) occupants[i] = null;
        }
    }
}
