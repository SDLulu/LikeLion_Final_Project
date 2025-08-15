leaderboardUuid 값은 아래 방법을 통해 확인할 수 있습니다.

uuid 값은 뒤끝 콘솔에서 랭킹을 생성 후 해당 랭킹 정보에서 uuid 값 확인
모든 유저 리더보드 정보 조회 함수를 이용하여 uuid 값 확인
설명
uuid 값을 이용하여 자신이 속한 그룹의 랭커들의 리스트를 조회합니다.
limit와 offset을 이용해 특정 위치의 랭커들을 조회할 수 있습니다.
이를 사용하면 한 번에 최대 50명씩, 1위부터 마지막 랭커까지 조회가 가능합니다.

그룹에 속해 있지 않는 유저의 경우, NULL그룹의 리더보드를 조회합니다.
공동 순위의 경우, 랭킹 정렬 기준(오름차순, 내림차순)과 같은 기준으로 gamer_id(회원번호)를 사전식 순서로 정렬합니다.

닉네임이 존재하지 않는 유저의 경우, ""으로 출력됩니다.
limit를 0이하로 입력할 경우 0으로 적용됩니다.
offset을 0이하로 입력할 경우 0으로 적용됩니다.
해당 함수는 SendQueue로 호출할 수 없습니다.
BackendUserLeaderboardReturnObject
namespace BackEnd.Leaderboard
{
    public class UserLeaderboardItem
    {
        public string gamerInDate;
        public string nickname = string.Empty;
        public string score;
        public string index;
        public string rank;
        public string extraData = string.Empty;
        public string extraName = string.Empty;
    }

    public class BackendUserLeaderboardReturnObject : BackendReturnObject
    {    
        public long GetTotalCount();
        public List<UserLeaderboardItem> GetUserLeaderboardList();
    }
}


// example 1. 1위부터 150위까지 조회.
// leaderboardUuid 랭킹에서 1 ~ 50등 랭커 조회
Backend.Leaderboard.User.GetLeaderboard("leaderboardUuid", 50, callback=> {
    if(callback.IsSuccess() == false) {
        return;
    }

    Debug.Log("리더보드 총 유저 등록 수 : " + callback.GetTotalCount());

    foreach(BackEnd.Leaderboard.UserLeaderboardItem item in callback.GetUserLeaderboardList())
    {
        Debug.Log($"{item.rank}위 : {item.nickname}");
        Debug.Log(item.ToString());
    }
});
// leaderboardUuid 랭킹에서 51 ~ 100등 랭커 조회
Backend.Leaderboard.User.GetLeaderboard("leaderboardUuid", 50, 50 callback=> {
    // 이후 처리
});
// leaderboardUuid 랭킹에서 101 ~ 150등 랭커 조회
Backend.Leaderboard.User.GetLeaderboard("leaderboardUuid", 50, 100 callback=> {
    // 이후 처리
});

// example 2. 특정 위치 조회 및 기본값 조회
// leaderboardUuid 랭킹에서 11등 ~ 30등 랭커 조회
Backend.Leaderboard.User.GetLeaderboard("leaderboardUuid", 20, 10, callback=> {
    // 이후 처리
});
// leaderboardUuid 랭킹에서 1 ~ 10등 랭커 조회
Backend.Leaderboard.User.GetLeaderboard("leaderboardUuid", callback=> {
    // 이후 처리
});