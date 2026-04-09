using Nakama;

public class FriendModel
{
    public string UserId;
    public string Username;
    public int State;
    public IApiFriend Raw;

    public FriendModel(IApiFriend friend)
    {
        Raw = friend;
        UserId = friend.User.Id;
        Username = friend.User.Username;
        State = friend.State;
    }
}