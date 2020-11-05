/*
 * This file is part of the TomNet package.
 *
 * (a) ������ <qunshuok@gmail.com>
 *
 *  2020/09/10
 */

using System;
using System.Collections.Generic;

namespace TomNet.FSM
{
	public class FiniteStateMachine
	{
		public delegate void OnStateChangeDelegate(int fromStateName, int toStateName);

		private List<FiniteState> states = new List<FiniteState>();

		private volatile int currentStateName;

		public OnStateChangeDelegate onStateChange;

		private object locker = new object();

		public void AddState(object st)
		{
			int stateName = (int)st;
			FiniteState fSMState = new FiniteState();
			fSMState.SetStateName(stateName);
			states.Add(fSMState);
		}

		public void AddAllStates(Type statesEnumType)
		{
			foreach (object value in Enum.GetValues(statesEnumType))
			{
				AddState(value);
			}
		}

		public void AddStateTransition(object from, object to, object tr)
		{
			int num = (int)from;
			int outputState = (int)to;
			int transition = (int)tr;
			FiniteState fSMState = FindStateObjByName(num);
			fSMState.AddTransition(transition, outputState);
		}

		public int ApplyTransition(object tr)
		{
			lock (locker)
			{
				int transition = (int)tr;
				int num = currentStateName;
				currentStateName = FindStateObjByName(currentStateName).ApplyTransition(transition);
				if (num != currentStateName && onStateChange != null)
				{
					onStateChange(num, currentStateName);
				}
				return currentStateName;
			}
		}

		public int GetCurrentState()
		{
			lock (locker)
			{
				return currentStateName;
			}
		}

		public void SetCurrentState(object state)
		{
			int toStateName = (int)state;
			if (onStateChange != null)
			{
				onStateChange(currentStateName, toStateName);
			}
			currentStateName = toStateName;
		}

		private FiniteState FindStateObjByName(object st)
		{
			int num = (int)st;
			foreach (FiniteState state in states)
			{
				if (num.Equals(state.GetStateName()))
				{
					return state;
				}
			}
			return null;
		}
	}
}
