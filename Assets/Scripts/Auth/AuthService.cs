using Nakama;
using Nakama.TinyJson;
using System;
using System.Threading.Tasks;

public class AuthService
{
    IClient client =>
        NakamaService.Client;

    private System.Collections.Generic.Dictionary<string, string> GetAuthVars()
    {
        return new System.Collections.Generic.Dictionary<string, string> {
            { "instance_id", NakamaSessionManager.InstanceId }
        };
    }

    public async Task<AuthResult> Register(
        string email,
        string password,
        string username)
    {
        if (!AuthValidator.ValidateEmail(email))
            return AuthResult.Fail("Invalid Email");

        if (!AuthValidator.ValidatePassword(password))
            return AuthResult.Fail("Weak Password");

        if (!AuthValidator.ValidateUsername(username))
            return AuthResult.Fail("Invalid Username");

        try
        {
            var session =
                await client.AuthenticateEmailAsync(
                    email,
                    password,
                    username,
                    create: true,
                    vars: GetAuthVars());

            NakamaSessionManager.Save(session);

            return AuthResult.Success();
        }
        catch (ApiResponseException e)
        {
            return AuthResult.Fail(e.Message);
        }
    }


    public async Task<AuthResult> LoginEmail(
        string email,
        string password)
    {
        try
        {
            var session =
                await client.AuthenticateEmailAsync(
                    email,
                    password,
                    create: false,
                    vars: GetAuthVars());

            NakamaSessionManager.Save(session);

            return AuthResult.Success();
        }
        catch (Exception e)
        {
            return AuthResult.Fail(e.Message);
        }
    }


    public async Task<AuthResult> LoginUsername(
        string username)
    {
        try
        {
            var session =
                await client.AuthenticateCustomAsync(
                    username,
                    create: true,
                    vars: GetAuthVars());

            NakamaSessionManager.Save(session);

            return AuthResult.Success();
        }
        catch (Exception e)
        {
            return AuthResult.Fail(e.Message);
        }
    }

    public async Task<AuthResult> LoginDevice()
    {
        try
        {
            var deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            var session = await client.AuthenticateDeviceAsync(deviceId, create: true, username: null, vars: GetAuthVars());
            
            NakamaSessionManager.Save(session);
            return AuthResult.Success();
        }
        catch (Exception e)
        {
            return AuthResult.Fail(e.Message);
        }
    }

    public async Task<bool> CheckUsername(string username)
    {
        if (client == null)
        {
            UnityEngine.Debug.LogError("Nakama Client not initialized.");
            return false;
        }

        try
        {
            // If no session exists (common during registration), get a temporary device session
            if (NakamaSessionManager.Session == null)
            {
                await LoginDevice();
            }

            if (NakamaSessionManager.Session == null) return false;

            var payload = new System.Collections.Generic.Dictionary<string, string> { { "username", username } }.ToJson();
            var result = await client.RpcAsync(NakamaSessionManager.Session, "check_username", payload);
            var data = result.Payload.FromJson<System.Collections.Generic.Dictionary<string, bool>>();
            return data.ContainsKey("available") && data["available"];
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"CheckUsername info: {e.Message}");
            return false;
        }
    }
}
