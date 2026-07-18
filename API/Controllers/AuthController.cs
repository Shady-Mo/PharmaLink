namespace API.Controllers;

public class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>
    /// Registers a new patient account.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - RoleID / RoleName are not accepted in the request body. The server always assigns the Patient role.
    /// - Pharmacist and System Admin accounts cannot be created via this endpoint.
    /// - Passwords are hashed before persistence and are never logged.
    /// - IsEmailVerified and IsPhoneVerified default to false on creation.
    /// </remarks>
    /// <param name="request">Patient registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **201 Created** with the new `UserId` on success.  
    /// **409 Conflict** if the email or phone number is already registered.  
    /// **400 Bad Request** if validation fails.
    /// </returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterPatientAsync(request, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return CreatedAtAction(
            actionName: nameof(Register),
            routeValues: new { },
            value: result.Value);
    }


    /// <summary>
    /// Authenticates a user and issues a role-scoped JWT.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - The JWT `RoleName` claim is always the user's actual assigned role — exactly one of
    ///   Patient, Pharmacist, or System Admin.
    /// - Pharmacist tokens additionally embed PharmacyID and BranchID claims for every
    ///   verified pharmacy/branch the pharmacist owns. These are the basis for all branch-ownership
    ///   checks in downstream endpoints.
    /// - System Admin tokens carry a platform-wide scope: "platform" claim.
    /// - Patient tokens carry no branch or platform claims — access is implicitly restricted
    ///   to that patient's own UserID.
    /// - On invalid credentials, the response never reveals whether the email or the password
    ///   was incorrect.
    /// - Suspended accounts (Status = Suspended) cannot log in even with correct credentials.
    /// </remarks>
    /// <param name="request">Login credentials (email and password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the access token, its expiry, and the caller's role name on success.  
    /// **401 Unauthorized** if the email or password is incorrect.  
    /// **403 Forbidden** if the account is suspended, or if the patient's phone number is not yet verified.  
    /// **400 Bad Request** if validation fails.
    /// </returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    [HttpPost("ForgotPassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswardDTO forgotPasswardDTO,
        CancellationToken cancellationToken)
    {
        var result = await authService.ForgotPassword(forgotPasswardDTO.Email, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    [HttpPost("ResetPassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDTO resetPasswordDTO,
        CancellationToken cancellationToken)
    {
        var result = await authService.ResetPassword(resetPasswordDTO, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(new { Message = result.Value });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDTO request,
        CancellationToken cancellationToken)
    {
        var authResult = await authService.GetRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);

        return authResult.IsSuccess ? Ok(authResult.Value) : authResult.ToProblem();
    }

    [HttpPost("revoke-refresh-token")]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RevokeRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    /// Changes the password for an authenticated user.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - Requires authentication. Only authenticated users can change their own password.
    /// - The current password is validated before the new password is applied.
    /// - The new password is hashed using ASP.NET Identity's PBKDF2-based algorithm.
    /// - Prevents changing to the same password.
    /// - The new password must comply with the password policy:
    ///   - Minimum 8 characters
    ///   - At least one uppercase letter
    ///   - At least one lowercase letter
    ///   - At least one digit
    ///   - At least one special character
    /// </remarks>
    /// <param name="request">Change password request with current and new passwords.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** on success.  
    /// **401 Unauthorized** if the current password is incorrect.  
    /// **400 Bad Request** if validation fails or new password policy is not met.  
    /// **404 Not Found** if the user is not found.  
    /// **500 Internal Server Error** if password change fails.
    /// </returns>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ChangePasswordResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDTO request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtClaimTypes.UserId);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized("Invalid or missing user ID in token.");
        }

        var result = await authService.ChangePasswordAsync(parsedUserId, request, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return Ok(new ChangePasswordResponseDTO { Message = "Password changed successfully." });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Pharmacist}")]
    [HttpGet("test")]
    public IActionResult TestAuth()
    {
        var pharmacies = User
            .FindAll(JwtClaimTypes.PharmacyId)
            .Select(c => c.Value)
            .ToList();

        return Ok(pharmacies);
    }
}
