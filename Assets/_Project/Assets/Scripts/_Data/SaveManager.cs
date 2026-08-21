using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// 세이브의 소유자. 파일 입출력(Write/Read)과 게임 상태 수집/복원(SaveGame/LoadGame)을 모두 여기서 한다.
// 정식 저장 지점은 거울(SaveMirror)이고, Player_move의 F5/F9는 개발용 단축키로 남겨두었다.
public class SaveManager : MonoBehaviour {
    public static SaveManager Instance;

    #region 인스펙터 변수

    [Header("파일")]
    public string fileName = "save.json"; // Application.persistentDataPath 아래에 저장된다.

    [Header("대상")]
    public string playerTag = "Player"; // 저장·복원할 플레이어를 찾을 태그. SaveManager는 씬을 넘어 살아남으므로 매번 새로 찾는다.

    #endregion
    #region 이벤트

    public event Action<SaveData> OnSaved; // 저장 직후. 저장 알림 UI가 구독할 자리.
    public event Action<SaveData> OnLoaded; // 복원 직후.

    #endregion
    #region 컴포넌트 변수

    string savePath;

    public bool HasSave => !string.IsNullOrEmpty(savePath) && File.Exists(savePath);

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance == null) {
            Instance = this;
            transform.SetParent(null); // DontDestroyOnLoad는 루트 오브젝트에서만 동작 (GAME_MANAGER 하위에 정리용으로 배치되어 있음)
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }

        savePath = Path.Combine(Application.persistentDataPath, fileName);
    }

    #endregion
    #region 파일 입출력

    public void Write(SaveData data) {
        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
    }

    public SaveData Read() {
        if (!HasSave) return null;
        return JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath));
    }

    public void DeleteSave() {
        if (HasSave) File.Delete(savePath);
    }

    #endregion
    #region 저장

    // 거울에서 호출하는 정식 저장. respawnPosition은 불러왔을 때 플레이어가 설 위치(보통 거울 발밑)다.
    // 플레이어 현재 위치를 그대로 쓰면 거울에 딱 붙어 있다 저장했을 때 복귀 지점이 어긋난다.
    public SaveData SaveGame(string checkpointId, Vector2 respawnPosition) {
        SaveData data = new SaveData {
            sceneName = SceneManager.GetActiveScene().name,
            checkpointId = checkpointId,
            posX = respawnPosition.x,
            posY = respawnPosition.y,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        GameObject player = FindPlayer();
        if (player != null && player.TryGetComponent(out Health health)) {
            data.currentHealth = health.CurrentHealth;
            data.maxHealth = health.MaxHealth;
        }

        if (player != null && player.TryGetComponent(out Player_MemoryShardInventory shards)) {
            data.memoryShardCount = shards.Count;
        }

        if (ObjectiveManager.Instance != null) {
            data.objectives = ObjectiveManager.Instance.CaptureObjectives();
            data.startedMissions = ObjectiveManager.Instance.CaptureStartedMissions();
        }

        if (ArenaManager.Instance != null) {
            data.clearedArenaIds = ArenaManager.Instance.CaptureClearedArenas();
        }

        Write(data);
        OnSaved?.Invoke(data);
        return data;
    }

    // 복귀 지점을 따로 정하지 않는 퀵세이브. 플레이어가 지금 서 있는 자리를 그대로 저장한다.
    public SaveData SaveGame(string checkpointId) {
        GameObject player = FindPlayer();
        if (player == null) {
            Debug.LogWarning($"[SaveManager] '{playerTag}' 태그가 붙은 플레이어를 찾지 못해 저장을 건너뜁니다.", this);
            return null;
        }

        return SaveGame(checkpointId, player.transform.position);
    }

    #endregion
    #region 불러오기

    // 같은 씬 안에서 저장 시점 상태로 되돌린다.
    // 씬 전환이 필요한 경우(메인 메뉴 → 이어하기)는 Read()로 sceneName을 먼저 확인해 씬을 연 뒤 호출할 것.
    public SaveData LoadGame() {
        SaveData data = Read();
        if (data == null) return null;

        Apply(data);
        OnLoaded?.Invoke(data);
        return data;
    }

    void Apply(SaveData data) {
        GameObject player = FindPlayer();
        if (player != null) {
            // z는 건드리지 않는다. 2D라도 카메라/정렬 때문에 z를 따로 잡아둔 경우가 있다.
            player.transform.position = new Vector3(data.posX, data.posY, player.transform.position.z);

            // 남아 있던 속도를 지우지 않으면 복원 직후 그대로 미끄러지거나 낙하 속도를 이어받는다.
            if (player.TryGetComponent(out Rigidbody2D rigid)) rigid.linearVelocity = Vector2.zero;

            // maxHealth가 0이면 체력을 기록하지 않던 구버전 세이브이므로 현재 체력을 그대로 둔다.
            if (data.maxHealth > 0 && player.TryGetComponent(out Health health)) health.SetHealth(data.currentHealth);

            if (player.TryGetComponent(out Player_MemoryShardInventory shards)) shards.SetCount(data.memoryShardCount);
        }

        if (ObjectiveManager.Instance != null) {
            ObjectiveManager.Instance.RestoreState(data.objectives, data.startedMissions);
        }

        if (ArenaManager.Instance != null) {
            ArenaManager.Instance.RestoreClearedArenas(data.clearedArenaIds);
        }
    }

    GameObject FindPlayer() {
        return GameObject.FindGameObjectWithTag(playerTag);
    }

    #endregion
}
