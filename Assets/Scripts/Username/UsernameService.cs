using Nakama;
using System.Linq;
using System.Threading.Tasks;

public class UsernameService
{
    IClient client => NakamaService.Client;

    ISession session =>
        NakamaSessionManager.Session;


    public async Task<bool> UsernameExists(
        string username)
    {
        var users =
            await client.GetUsersAsync(
                session,
                null,
                new string[] { username });

        return users.Users.Count() > 0;
    }
}
