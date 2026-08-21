using System;
using System.Collections.Generic;

// 세이브 파일에 그대로 직렬화되는 데이터 덩어리. JsonUtility가 다루므로 전부 public 필드여야 한다.
// 필드를 새로 추가해도 기존 세이브 파일은 그대로 읽히며(없는 필드는 기본값 0/null이 된다),
// 그래서 오염도·기억 조각 같은 후속 시스템은 이 클래스에 필드만 늘리면 붙는다.
[Serializable]
public class SaveData {
    public string sceneName;    // 저장 시점의 씬 이름. 메인 메뉴에서 "이어하기"로 어느 씬을 열지 판단한다.
    public string checkpointId; // 저장을 발생시킨 거울(SaveMirror)의 id. 비어 있으면 개발용 퀵세이브다.

    public float posX;          // 불러왔을 때 플레이어가 서 있을 위치.
    public float posY;

    public int currentHealth;   // 자아 게이지.
    public int maxHealth;       // 0이면 체력 정보가 없는 구버전 세이브로 취급해 체력을 건드리지 않는다.

    public List<ObjectiveSaveEntry> objectives = new List<ObjectiveSaveEntry>();

    // 이미 시작된 미션의 id. 목표의 Active 상태는 "선행 조건이 채워졌는가"로 다시 계산되지만,
    // "미션이 시작됐는가"는 목표 진행도에서 파생되지 않아 따로 기록해야 한다
    // (시작 구역을 밟고 저장했으면 그 사실 자체를 기억해야 한다).
    // 이 필드가 없던 구버전 세이브는 진행 흔적이 있는 미션을 시작된 것으로 보고 복구한다.
    public List<string> startedMissions = new List<string>();

    public string savedAt;      // 표시용 저장 시각. 나중에 불러오기 슬롯 UI에서 쓴다.

    public int memoryShardCount; // 보유한 기억 조각 수.
    public List<string> clearedArenaIds = new List<string>(); // 뒷세계 등 1회성 몬스터 구역 중 이미 클리어한 arenaId 목록.
}

// 목표 하나의 진행도. Objective 자체를 직렬화하면 description·targetCount 같은
// 기획 데이터까지 세이브에 굳어버려서, 진행도만 따로 떼어 저장한다.
[Serializable]
public class ObjectiveSaveEntry {
    public string id;
    public int currentCount;
    public bool isCompleted;
}
