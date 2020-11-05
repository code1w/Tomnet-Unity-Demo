/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/10
 */


using TomNet.FSM;
using TomNet.NetWork;

namespace TomNet.Sockets
{
	public class BaseSocket
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


		protected INetWorkClient networkclient;

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
