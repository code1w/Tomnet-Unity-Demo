/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/10
 */


using System.Collections.Generic;

namespace TomNet.FSM
{
	public class FiniteState
	{
		private int stateName;

		private Dictionary<int, int> transitions = new Dictionary<int, int>();

		public void SetStateName(int newStateName)
		{
			stateName = newStateName;
		}

		public int GetStateName()
		{
			return stateName;
		}

		public void AddTransition(int transition, int outputState)
		{
			transitions[transition] = outputState;
		}

		public int ApplyTransition(int transition)
		{
			int result = stateName;
			if (transitions.ContainsKey(transition))
			{
				result = transitions[transition];
			}
			return result;
		}
	}
}
