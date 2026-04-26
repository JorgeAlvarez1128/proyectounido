using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FrontBlazor_AppiGenericaCsharp.Services;

public sealed class CustomAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));

    public void SetAuthenticatedUser(string jwt)
    {
        var claims = JwtClaimsParser.ParseClaimsFromJwt(jwt);
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt"));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SetAnonymous()
    {
        _currentUser = Anonymous.User;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
