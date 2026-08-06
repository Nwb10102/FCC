using UnityEngine;

// 인스펙터 드롭다운에 표시할 미션 유형. 새 유형을 추가하려면 항목만 늘리면 되고,
// 완료 판정은 ObjectiveManager.CompleteObjective(즉시 완료)/AddProgress(누적)를
// 어느 트리거가 호출하느냐로 결정되므로 이 enum 자체에는 로직이 없다.
public enum MissionType
{
    [InspectorName("특정 지역 도착하기")] ReachZone,
    [InspectorName("특정 물품 수집하기")] CollectItem,
}

// 목표 하나를 표현하는 데이터. ReachZone은 지역 도달/대화 완료처럼 즉시 달성되는 목표,
// CollectItem은 거울 조각 수집처럼 targetCount까지 누적되는 목표.
[System.Serializable]
public class Objective
{
    public string id;
    [TextArea(1, 2)] public string description;
    public MissionType type = MissionType.ReachZone;
    public int targetCount = 1;

    [HideInInspector] public int currentCount;
    [HideInInspector] public bool isCompleted;
}
