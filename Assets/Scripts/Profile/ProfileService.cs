using Nakama;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ProfileService
{
    IClient client =>
        NakamaService.Client;

    ISession session =>
        NakamaSessionManager.Session;

    public async Task SaveProfile(
        string name,
        string dob)
    {
        var profile =
            new UserProfile
            {
                UserId = session.UserId,
                Name = name,
                DOB = dob
            };

        var storage =
            new WriteStorageObject
            {
                Collection = "profile",
                Key = "data",
                Value =
                JsonUtility.ToJson(profile),
                PermissionRead = 2,
                PermissionWrite = 1
            };

        await client.WriteStorageObjectsAsync(
            session,
            new[] { storage });
    }


    public async Task<UserProfile> LoadProfile()
    {
        var result =
            await client.ReadStorageObjectsAsync(
                session,
                new[]
                {
                    new StorageObjectId
                    {
                        Collection = "profile",
                        Key = "data",
                        UserId = session.UserId
                    }
                });

        if (!result.Objects.Any())
            return null;

        return JsonUtility.FromJson<UserProfile>(
            result.Objects.First().Value);
    }
}
