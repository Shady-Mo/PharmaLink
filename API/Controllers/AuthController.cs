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
}