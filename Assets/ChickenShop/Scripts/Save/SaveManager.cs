using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 저장/불러오기 (Application.persistentDataPath/chickenshop_save.json).
    ///  - 영업 준비(PreOpen) 또는 정산(Settlement) 단계에서만 저장 가능 (영업 중 상태는 저장하지 않음)
    ///  - 정산 화면에서 자동 저장, 다음 날 시작 시 자동 저장, 일시정지 메뉴에서 수동 저장
    ///  - 임시 파일에 먼저 쓰고 교체하여 저장 중 종료돼도 기존 파일이 깨지지 않는다
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class SaveManager : MonoBehaviour
    {
        public const string FileName = "chickenshop_save.json";
        private const int MaxHistory = 14;

        private readonly List<DailyReport> history = new List<DailyReport>();

        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static bool SaveExists()
        {
            try { return File.Exists(SavePath); }
            catch (Exception) { return false; }
        }

        public bool HasSave { get { return SaveExists(); } }
        public IReadOnlyList<DailyReport> History { get { return history; } }

        public event Action Saved;
        public event Action Loaded;

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Day != null) gm.Day.DayEnded += OnDayEnded;
        }

        private void Start()
        {
            // GameManager 가 늦게 준비된 경우 대비
            var gm = GameManager.Instance;
            if (gm != null && gm.Day != null)
            {
                gm.Day.DayEnded -= OnDayEnded;
                gm.Day.DayEnded += OnDayEnded;
            }
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Day != null) gm.Day.DayEnded -= OnDayEnded;
        }

        private void OnDayEnded(DailyReport report)
        {
            history.Add(report.Clone());
            while (history.Count > MaxHistory) history.RemoveAt(0);
        }

        public bool CanSaveNow(out string reason)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Day == null) { reason = "게임이 준비되지 않았습니다."; return false; }
            if (gm.Day.Phase == ShopPhase.Open || gm.Day.Phase == ShopPhase.Closing)
            {
                reason = "영업 중에는 저장할 수 없습니다. 영업 준비 또는 정산 화면에서 저장하세요.";
                return false;
            }
            reason = null;
            return true;
        }

        public SaveData Capture()
        {
            var gm = GameManager.Instance;
            var data = new SaveData
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                // 정산 단계에서 저장하면 다음 날부터 시작
                day = gm.Day.Phase == ShopPhase.Settlement ? gm.Day.Day + 1 : gm.Day.Day,
                money = gm.Economy.Money,
                lifetimeRevenue = gm.Economy.LifetimeRevenue,
                reputation = gm.Reputation.Value,
                inventory = gm.Inventory.ToArray(),
                upgrades = gm.Progression.GetLevelEntries(),
                unlockedMenus = gm.Progression.GetUnlockedMenus(),
                history = new List<DailyReport>(history),
            };
            return data;
        }

        public bool TrySave(out string error)
        {
            if (!CanSaveNow(out error)) return false;
            try
            {
                var data = Capture();
                string json = JsonUtility.ToJson(data, true);
                string path = SavePath;
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                GameLog.Info("Save", "저장 완료: " + path + " (" + data.day + "일차, " + MoneyFormat.Won(data.money) + ")");
                error = null;
                Saved?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                GameLog.Error("Save", "저장 실패: " + ex);
                return false;
            }
        }

        public bool TryLoad(out string error)
        {
            var gm = GameManager.Instance;
            if (gm == null) { error = "GameManager 없음"; return false; }
            SaveData data;
            try
            {
                if (!File.Exists(SavePath)) { error = "저장 파일이 없습니다."; return false; }
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception ex)
            {
                error = "저장 파일을 읽을 수 없습니다: " + ex.Message;
                GameLog.Error("Save", error);
                return false;
            }
            if (data == null) { error = "저장 파일 형식이 올바르지 않습니다."; return false; }
            if (data.version > SaveData.CurrentVersion)
            {
                error = "더 새로운 버전의 저장 파일입니다 (v" + data.version + ").";
                return false;
            }
            Apply(data);
            error = null;
            GameLog.Info("Save", "불러오기 완료: " + data.day + "일차, " + MoneyFormat.Won(data.money) + ", 평판 " + data.reputation.ToString("0.0"));
            Loaded?.Invoke();
            return true;
        }

        private void Apply(SaveData data)
        {
            var gm = GameManager.Instance;
            gm.ResetWorldForLoad();
            gm.Economy.LoadState(data.money, data.lifetimeRevenue);
            gm.Inventory.LoadState(data.inventory);
            gm.Reputation.Set(data.reputation);
            gm.Progression.LoadState(data.upgrades, data.unlockedMenus);
            history.Clear();
            if (data.history != null) history.AddRange(data.history);
            gm.Day.BeginDay(Mathf.Max(1, data.day));
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                GameLog.Info("Save", "저장 파일 삭제");
            }
            catch (Exception ex)
            {
                GameLog.Error("Save", "삭제 실패: " + ex.Message);
            }
        }
    }
}
