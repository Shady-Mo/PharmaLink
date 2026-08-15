using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

/// <summary>
/// Native SK plugin that allows the AI to manage the user's cart and create orders.
/// </summary>
public sealed class CartOrderPlugin(IServiceScopeFactory scopeFactory, ILogger<CartOrderPlugin> logger)
{
    [KernelFunction("get_user_cart")]
    [Description(
        "Retrieves the current contents of the user's shopping cart. Use this when the user asks what is in their cart, or before checking out.")]
    public async Task<object> GetUserCartAsync(
        [Description("The patient's user ID")] Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CartOrderPlugin.GetUserCartAsync for Patient {PatientId}", patientUserId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var result = await cartService.GetCartAsync(patientUserId, cancellationToken);
        if (result.IsSuccess) return result.Value;
        return new { Error = result.Error.Description };
    }

    [KernelFunction("add_to_cart")]
    [Description("Adds a specific drug to the user's shopping cart. Requires the drugId and the quantity.")]
    public async Task<object> AddToCartAsync(
        [Description(
            "The ID (GUID) of the drug to add. You must find this using the get_drug_info or search_drugs tools first.")]
        Guid drugId,
        [Description("The quantity to add")] int quantity,
        [Description("The patient's user ID")] Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CartOrderPlugin.AddToCartAsync Drug {DrugId} Qty {Qty} for Patient {PatientId}", drugId,
            quantity, patientUserId);

        if (quantity > 5)
        {
            return new { Success = false, Error = "الحد الأقصى لطلب أي دواء هو 5 عبوات فقط. يرجى إبلاغ المريض والاعتذار له." };
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var request = new AddCartItemRequestDTO { DrugId = drugId, Quantity = quantity };
        var result = await cartService.AddItemAsync(patientUserId, request, cancellationToken);
        if (result.IsSuccess)
            return new { Success = true, Message = "Item added to cart successfully.", Cart = result.Value };
        return new { Success = false, Error = result.Error.Description };
    }

    [KernelFunction("remove_from_cart")]
    [Description("Removes a specific item from the user's shopping cart by cartItemId.")]
    public async Task<object> RemoveFromCartAsync(
        [Description("The cart item ID (GUID) to remove, which you can get from get_user_cart")]
        Guid cartItemId,
        [Description("The patient's user ID")] Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CartOrderPlugin.RemoveFromCartAsync Item {ItemId} for Patient {PatientId}", cartItemId,
            patientUserId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var result = await cartService.RemoveItemAsync(patientUserId, cartItemId, cancellationToken);
        if (result.IsSuccess) return new { Success = true, Message = "Item removed from cart successfully." };
        return new { Success = false, Error = result.Error.Description };
    }

    [KernelFunction("get_user_addresses")]
    [Description(
        "Retrieves the user's saved delivery addresses. Use this to ask the user which address they want to deliver to before creating an order.")]
    public async Task<object> GetUserAddressesAsync(
        [Description("The patient's user ID")] Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CartOrderPlugin.GetUserAddressesAsync for Patient {PatientId}", patientUserId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var addressService = scope.ServiceProvider.GetRequiredService<IAddressService>();

        var result = await addressService.GetAllForPatientAsync(patientUserId, cancellationToken);
        if (result.IsSuccess) return new { Addresses = result.Value };
        return new { Error = result.Error.Description };
    }

    [KernelFunction("create_order")]
    [Description(
        "Creates an order from the user's cart. You must first get the user's addresses and ask them to select one, then pass the selected address ID here. Note: only use this when the user explicitly confirms they want to place the order.")]
    public async Task<object> CreateOrderAsync(
        [Description("The delivery address ID (GUID) chosen by the user from get_user_addresses")]
        Guid deliveryAddressId,
        [Description("The fulfillment mode: 1 for Delivery, 2 for Pickup")]
        int fulfillmentMode,
        [Description("The patient's user ID")] Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CartOrderPlugin.CreateOrderAsync for Patient {PatientId}", patientUserId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var request = new CreateOrderDTO
        {
            DeliveryAddressId = deliveryAddressId,
            FulfillmentMode = (FulfillmentMode)fulfillmentMode
        };

        var result = await orderService.CreateOrder(patientUserId, request);
        if (result.IsSuccess)
            return new { Success = true, Message = "Order created successfully.", OrderDetails = result.Value };
        return new { Success = false, Error = result.Error.Description };
    }
}