using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 카운터 앞 줄 관리. slots[0] 이 주문 위치(카운터 바로 앞), 이후는 뒤로 서는 자리.
    /// 손님이 빠지면 뒤 손님들이 한 칸씩 앞으로 이동한다 (겹치지 않도록 자리 단위로 관리).
    /// </summary>
    public class CustomerQueue : MonoBehaviour
    {
        [SerializeField] private Transform[] slots = new Transform[0];

        private readonly List<CustomerAgent> line = new List<CustomerAgent>();

        public int Capacity { get { return slots != null ? slots.Length : 0; } }
        public int Count { get { return line.Count; } }
        public bool HasSpace { get { return line.Count < Capacity; } }
        public CustomerAgent Front { get { return line.Count > 0 ? line[0] : null; } }

        private void Awake()
        {
            if (slots == null || slots.Length == 0)
            {
                // 자식 Transform 을 자리로 사용
                slots = new Transform[transform.childCount];
                for (int i = 0; i < slots.Length; i++) slots[i] = transform.GetChild(i);
                if (slots.Length == 0) GameLog.Error("Queue", "줄 서기 자리가 없습니다.", this);
            }
        }

        public Transform GetSlot(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        public int IndexOf(CustomerAgent c) { return line.IndexOf(c); }

        public bool Join(CustomerAgent c)
        {
            if (c == null || line.Contains(c) || !HasSpace) return false;
            line.Add(c);
            c.SetQueueSlot(line.Count - 1);
            return true;
        }

        public void Leave(CustomerAgent c)
        {
            int idx = line.IndexOf(c);
            if (idx < 0) return;
            line.RemoveAt(idx);
            for (int i = idx; i < line.Count; i++)
            {
                if (line[i] != null) line[i].SetQueueSlot(i);
            }
        }

        public void Clear() { line.Clear(); }
    }
}
