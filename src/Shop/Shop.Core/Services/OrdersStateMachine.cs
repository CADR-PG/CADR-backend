namespace Shop.Core.Services;

public enum OrderStatus
{
	Created = 0,
	Processing = 1,
	Succeeded = 2,
	Failed = 3,
	Expired = 4
}

public class OrdersStateMachine
{

}