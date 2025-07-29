using Restaurant_Menu.Models;

namespace Restaurant_Menu.Interface
{
    public interface IInvoiceGenerator
    {
        byte[] GenerateCustomerInvoice(Order order);
        byte[] GenerateOwnerInvoice(Order order);
    }

}
