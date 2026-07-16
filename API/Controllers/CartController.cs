using Application.DTOs.Cart.Requests;
using Application.DTOs.Cart.Responses;
using Application.Services.Cart;

namespace API.Controllers;

[Authorize(Roles = AppRoles.Patient)]
public class CartController(ICartService cartService) : BaseApiController
{
    /// <summary>
    /// Retrieves the authenticated patient's cart and all its items.
    /// </summary>
    /// <remarks>
    /// **Caching:** The response is served from Redis when available (cache-aside pattern).
    /// On a cache miss the database is queried and the result is stored in Redis (TTL: 2 h sliding / 24 h absolute).
    ///
    /// Returns an empty cart object (no items) when the patient has not added anything yet.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the cart and its line-items.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(CartResponseDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await cartService.GetCartAsync(User.GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Adds a medicine to the authenticated patient's cart.
    /// </summary>
    /// <remarks>
    /// **Auto-creation:** A new cart is created automatically on first use.
    ///
    /// **Upsert behaviour:** If the specified drug is already present in the cart,
    /// its quantity is incremented by the requested amount rather than creating a duplicate row.
    ///
    /// **Price snapshot:** The drug's current unit price is captured at add-time and stored
    /// with the item to protect against price changes before checkout.
    ///
    /// **Cache:** Redis is invalidated immediately after the DB write.
    /// </remarks>
    /// <param name="request">The drug to add and the desired quantity (must be &gt; 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the updated cart.
    /// **400 Bad Request** if validation fails.
    /// **404 Not Found** if the specified drug does not exist.
    /// </returns>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddCartItemRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.AddItemAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Updates the quantity of a specific cart item.
    /// </summary>
    /// <remarks>
    /// **Ownership:** The item must belong to the authenticated patient's cart.
    /// Cross-patient access returns 404 (not 403) to avoid leaking cart-item IDs.
    ///
    /// **Cache:** Redis is invalidated immediately after the DB write.
    /// </remarks>
    /// <param name="itemId">The ID of the cart item to update.</param>
    /// <param name="request">The new quantity (must be &gt; 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the updated cart.
    /// **400 Bad Request** if the quantity is invalid.
    /// **404 Not Found** if the cart item does not exist or belongs to another patient.
    /// </returns>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(
        Guid itemId,
        [FromBody] UpdateCartItemRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.UpdateItemAsync(User.GetUserId(), itemId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Removes a medicine from the authenticated patient's cart.
    /// </summary>
    /// <remarks>
    /// **Ownership:** The item must belong to the authenticated patient's cart.
    ///
    /// **Cache:** Redis is invalidated immediately after the DB write.
    /// </remarks>
    /// <param name="itemId">The ID of the cart item to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **204 No Content** on success.
    /// **404 Not Found** if the cart item does not exist or belongs to another patient.
    /// </returns>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await cartService.RemoveItemAsync(User.GetUserId(), itemId, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
