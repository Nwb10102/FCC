using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

// String Table 조회를 한 곳으로 모은 도우미.
//
// Localization 패키지의 조회는 원래 비동기(AsyncOperationHandle)다. 이 프로젝트는 비동기 처리에
// 코루틴만 쓰기로 되어 있어(async/await·UniTask 금지) 코루틴용 ResolveAsync를 기본으로 두고,
// 코루틴을 만들 수 없는 자리(목표 체크리스트처럼 그 자리에서 문자열이 필요한 곳)에서만 동기 Resolve를 쓴다.
//
// **동기 조회를 써도 되는 이유:** 테이블이 플레이어에 함께 빌드되는 로컬 에셋이라 첫 호출에서만
// 로드가 걸리고 그 뒤로는 캐시에서 즉시 돌아온다. 다만 그 첫 로드가 화면을 그리는 프레임에 걸리면
// 눈에 띄게 끊기므로, 그리기 전에 WaitForInitialization으로 한 번 기다리게 해 두었다.
public static class LocalizationText {
    #region 조회

    // 코루틴용 조회. 코루틴은 반환값을 가질 수 없어 결과를 onDone으로 넘긴다.
    public static IEnumerator ResolveAsync(LocalizedString source, Action<string> onDone) {
        if (!IsUsable(source)) {
            onDone?.Invoke(string.Empty);
            yield break;
        }

        AsyncOperationHandle<string> handle = source.GetLocalizedStringAsync();
        yield return handle; // AsyncOperationHandle은 IEnumerator라 코루틴에서 그대로 기다릴 수 있다.

        onDone?.Invoke(handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : string.Empty);
    }

    // 즉시 조회. 아직 로드되지 않았다면 그 자리에서 로드를 끝내고 돌려준다.
    // 번역이 비어 있는 칸도 빈 문자열로 돌아오므로 fallback으로 대체한다 — 화면에 아무것도
    // 안 뜨는 것보다 원문 언어 문구나 에셋 이름이라도 보이는 편이 문제를 찾기 쉽다.
    public static string Resolve(LocalizedString source, string fallback = "") {
        if (!IsUsable(source)) return fallback;

        string value = source.GetLocalizedString();
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    // 화면을 처음 그리기 전에 한 번 기다린다. 건너뛰면 첫 동기 조회가 초기화까지 통째로 끌어와
    // 한 프레임 끊긴다.
    public static IEnumerator WaitForInitialization() {
        if (!LocalizationSettings.HasSettings) yield break;
        yield return LocalizationSettings.InitializationOperation;
    }

    #endregion
    #region 검사

    static bool warnedMissingSettings; // 같은 경고가 매 프레임 쏟아지지 않도록 한 번만 찍는다.

    // 키를 비워둔 칸은 정상이다(이름표 없는 대사, 설명 없는 미션). 조용히 빈 문자열로 넘어간다.
    // 반면 설정 에셋 자체가 없으면 모든 문구가 통째로 사라지는데 에러는 안 나서 원인을 찾기 어렵다.
    // 그래서 이 경우만 크게 알린다.
    static bool IsUsable(LocalizedString source) {
        if (source == null || source.IsEmpty) return false;

        if (!LocalizationSettings.HasSettings) {
            if (!warnedMissingSettings) {
                warnedMissingSettings = true;
                Debug.LogError("[LocalizationText] Localization 설정 에셋이 없어 문자열을 찾을 수 없습니다. " +
                    "Tools ▸ FCC ▸ Localization ▸ 초기 셋업 을 한 번 실행하세요.");
            }
            return false;
        }

        return true;
    }

    #endregion
}
