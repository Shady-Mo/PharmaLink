using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Services;

public interface IStripePaymentService
{
    Task<string> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl);
}
