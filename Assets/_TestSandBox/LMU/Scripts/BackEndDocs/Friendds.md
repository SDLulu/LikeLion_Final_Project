Backend.Friend.GetFriendList((callback) => {
  // 이후 처리
});
Backend.Friend.GetFriendList(5, (callback) => {
  // 5명 친구 조회(1-5)
  // 이후 처리
});
Backend.Friend.GetFriendList(5, 5, (callback) => {
  // 처음 5명 이후의 5명 친구 조회(6-10)
  // 이후 처리
});