using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kernel.HSM
{
	public class StateMachineBuilder
	{
		private RootState _rootState;


		public StateMachineBuilder(string stateMachineName)
		{
			_rootState = new RootState(stateMachineName, null);
		}

		public StateBuilder<StateMachineBuilder> State(string stateName)
		{
			return new StateBuilder<StateMachineBuilder>(stateName, _rootState, this);
		}

		public RootState Build()
		{
			Awake(_rootState);
			return _rootState;
		}

		private void Awake(State state)
		{
			state.Awake();

			foreach (var child in state.Children)
			{
				Awake(child);
			}
		}
	}
}
