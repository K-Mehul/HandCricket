using Nakama;
using Nakama.TinyJson;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AuthService
{
    IClient client =>
        NakamaService.Client;

    private Task<AuthResult> _pendingLoginTask;

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
        Debug.Log($"AuthService: Starting Register for {email}...");
        if (!AuthValidator.ValidateEmail(email))
            return AuthResult.Fail("Invalid Email");

        if (!AuthValidator.ValidatePassword(password))
            return AuthResult.Fail("Weak Password");

        if (!AuthValidator.ValidateUsername(username))
            return AuthResult.Fail("Invalid Username");

        try
        {
            ISession session = NakamaSessionManager.Session;

            if (session != null)
            {
                Debug.Log("AuthService: Existing session found. Linking email to current account...");
                // Link the email to the existing device account
                await client.LinkEmailAsync(session, email, password);
                
                // Update the username for this account
                await client.UpdateAccountAsync(session, username);
                
                Debug.Log("AuthService: Linking Successful.");
            }
            else
            {
                Debug.Log("AuthService: No session found. Creating fresh account...");
                // Standard registration
                session = await client.AuthenticateEmailAsync(
                        email,
                        password,
                        username,
                        create: true,
                        vars: GetAuthVars());
                
                NakamaSessionManager.Save(session);
                Debug.Log("AuthService: Fresh Registration Successful.");
            }

            return AuthResult.Success();
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"AuthService: Register Error: {e.Message}");
            return AuthResult.Fail(e.Message);
        }
    }


    public async Task<AuthResult> LoginEmail(
        string email,
        string password)
    {
        Debug.Log($"AuthService: Starting Login for {email}...");
        try
        {
            var session =
                await client.AuthenticateEmailAsync(
                    email,
                    password,
                    create: false,
                    vars: GetAuthVars());

            Debug.Log("AuthService: Login Successful.");
            NakamaSessionManager.Save(session);

            return AuthResult.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"AuthService: Login Error: {e.Message}");
            return AuthResult.Fail(e.Message);
        }
    }


    public async Task<AuthResult> LoginUsername(
        string username)
    {
        Debug.Log($"AuthService: Starting LoginUsername for {username}...");
        try
        {
            var session =
                await client.AuthenticateCustomAsync(
                    username,
                    create: true,
                    vars: GetAuthVars());

            Debug.Log("AuthService: LoginUsername Successful.");
            NakamaSessionManager.Save(session);

            return AuthResult.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"AuthService: LoginUsername Error: {e.Message}");
            return AuthResult.Fail(e.Message);
        }
    }

    public async Task<AuthResult> LoginDevice()
    {
        if (_pendingLoginTask != null && !_pendingLoginTask.IsCompleted)
        {
            Debug.Log("AuthService: Redundant LoginDevice call detected. Awaiting existing task...");
            return await _pendingLoginTask;
        }

        _pendingLoginTask = PerformLoginDevice();
        return await _pendingLoginTask;
    }

    private async Task<AuthResult> PerformLoginDevice()
    {
        Debug.Log("AuthService: Starting LoginDevice...");
        try
        {
            var deviceId = NakamaService.GetDeviceId();
            var session = await client.AuthenticateDeviceAsync(deviceId, create: true, username: null, vars: GetAuthVars());
            
            Debug.Log("AuthService: LoginDevice Successful.");
            NakamaSessionManager.Save(session);
            return AuthResult.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"AuthService: LoginDevice Error: {e.Message}");
            return AuthResult.Fail(e.Message);
        }
    }

    public async Task<bool> CheckUsername(string username)
    {
        if (client == null)
        {
            UnityEngine.Debug.LogError("AuthService: Nakama Client not initialized.");
            return false;
        }

        try
        {
            // If no session exists (common during registration), get a temporary device session
            if (NakamaSessionManager.Session == null)
            {
                Debug.Log("AuthService: No session for CheckUsername, attempting device login...");
                await LoginDevice();
            }

            if (NakamaSessionManager.Session == null) 
            {
                Debug.LogError("AuthService: Failed to acquire session for CheckUsername.");
                return false;
            }

            Debug.Log($"AuthService: Calling Rpc 'check_username' for {username}...");
            var payload = new System.Collections.Generic.Dictionary<string, string> { { "username", username } }.ToJson();
            var result = await client.RpcAsync(NakamaSessionManager.Session, "check_username", payload);
            
            var data = result.Payload.FromJson<System.Collections.Generic.Dictionary<string, bool>>();
            bool available = data.ContainsKey("available") && data["available"];
            Debug.Log($"AuthService: CheckUsername Available = {available}");
            return available;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"AuthService: CheckUsername Info: {e.Message}");
            return false;
        }
    }
}
