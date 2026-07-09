namespace API.Controllers;

/// <summary>
/// Handles public authentication operations such as patient self-registration.
/// </summary>
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

        // AC #6: 201 Created with the new UserID.
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
    /// **403 Forbidden** if the account is suspended.  
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