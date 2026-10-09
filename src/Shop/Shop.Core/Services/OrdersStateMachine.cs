namespace Shop.Core.Services;

public enum OrderStatus
{
	Created = 0, // order is open waiting for realization
	Processing = 1, // order is being processed, it is being assigned to proper user library
	Succeeded = 2, // if payment is good and order items are assigned to proper user library
	Failed = 3, // if payment not good or order items are not assigned
	Expired = 4 // if you don't pay for order in proper time it is set to expired state
}

public class OrdersStateMachine
{

}