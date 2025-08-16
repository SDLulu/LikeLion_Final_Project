

[문서내용 일부]
2.2.1. "생성 후 갱신(Insert-then-Update)" 생명주기
뒤끝 리더보드 시스템은 기존에 존재하는 데이터를 '갱신'하는 방식으로 동작합니다. 따라서 플레이어의 데이터가 테이블에 한 번도 저장된 적이 없다면, 갱신할 대상이 없으므로 에러가 발생합니다. 이를 해결하기 위해 "생성 후 갱신" 생명주기를 반드시 따라야 합니다.

최초 점수 기록: 플레이어가 게임에서 처음으로 점수를 기록하는 시점에는 Backend.GameData.Insert() 함수를 사용하여 테이블에 새로운 데이터 행(row)을 생성해야 합니다. 이 함수는 성공 시 해당 행의 고유 식별자인 inDate 값을 반환합니다. 이 inDate 값은 이후의 모든 업데이트에 필요하므로, 반드시 클라이언트에 저장(예: PlayerPrefs)하거나 필요할 때마다 다시 조회해야 합니다.

이후 점수 갱신: 한 번 데이터가 생성된 이후부터는 UpdateMyDataAndRefreshLeaderboard() 함수와 저장해 둔 inDate 값을 사용하여 기존 데이터 행을 갱신합니다.

이 생명주기를 무시하고 처음부터 업데이트 함수를 호출하는 것은 개발자가 가장 흔하게 저지르는 실수이며, 반드시 신규 유저와 기존 유저를 구분하는 로직을 구현해야 합니다.   

2.2.2. 최신 접근법: Backend.Leaderboard.User.UpdateMyDataAndRefreshLeaderboard
최신 뒤끝 SDK에서는 이 함수를 사용하여 리더보드를 갱신하는 것이 표준입니다. 함수의 주요 파라미터는 다음과 같습니다.   

leaderboardUuid (string): 뒤끝 콘솔에서 복사한 리더보드의 고유 UUID.

tableName (string): 점수 데이터가 저장된 게임 데이터 테이블의 이름 (예: "USER_DATA").

rowIndate (string): 갱신할 데이터 행의 inDate 값.

param (Param): 갱신할 데이터를 담는 딕셔너리 형태의 객체. 이 객체에는 반드시 리더보드 집계 필드로 설정한 컬럼과 동일한 이름의 키가 포함되어야 합니다. (예: param.Add("score", 100);).   

이 param 객체에는 점수 외에 다른 컬럼의 데이터도 함께 담아 전송할 수 있습니다. 예를 들어, 점수와 함께 플레이어 레벨도 동시에 갱신해야 한다면 param.Add("player_level", 5);와 같이 추가하면 됩니다. 이는 Backend.GameData.Update() 함수를 별도로 호출하는 것보다 훨씬 효율적인 방식이며, API 호출 횟수를 줄여 비용을 절감하는 데 도움이 됩니다.   


[예시코드]

Param param = new Param();
param.Add("score", i);
param.Add("extraData", "추가 정보");

Backend.Leaderboard.User.UpdateMyDataAndRefreshLeaderboard("리더보드 uuid", "테이블 이름", "갱신할 row inDate", param, callback => 
{
    // 이후 처리
});


@UserData.cs @LeaderBoard.cs 