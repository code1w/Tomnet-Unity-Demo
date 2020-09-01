using Tom.Bitswarm;
using Tom.FSM;
using Tom.Logging;

namespace Tom.Core.Sockets
{
	public class BaseSocketLayer
	{
		protected enum States
		{
			Disconnected,
			Connecting,
			Connected
		}

		protected enum Transitions
		{
			StartConnect,
			ConnectionSuccess,
			ConnectionFailure,
			Disconnect
		}

		protected Logger log;

		protected ISocketClient socketClient;

		protected FiniteStateMachine fsm;

		protected volatile bool isDisconnecting = false;

		protected States State => (States)fsm.GetCurrentState();

		protected void InitStates()
		{
			fsm = new FiniteStateMachine();
			fsm.AddAllStates(typeof(States));
			fsm.AddStateTransition(States.Disconnected, States.Connecting, Transitions.StartConnect);
			fsm.AddStateTransition(States.Connecting, States.Connected, Transitions.ConnectionSuccess);
			fsm.AddStateTransition(States.Connecting, States.Disconnected, Transitions.ConnectionFailure);
			fsm.AddStateTransition(States.Connected, States.Disconnected, Transitions.Disconnect);
			fsm.SetCurrentState(States.Disconnected);
		}
	}
}
