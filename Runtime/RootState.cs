using System;
using UnityEngine;

namespace Kernel.HSM
{
	public class RootState : State
	{
		public State CurrentState { get; private set; }

		private int _changeStateFrameCount;
		private int _changeStateCounter;
#if UNITY_EDITOR
		private string _statesStack;
#endif


		public RootState(string stateName, State parentState)
			: base(stateName, parentState)
		{
			Root = this;
			IsActive = true;

			_changeStateFrameCount = -1;
			_changeStateCounter = 0;

			CurrentState = this;

#if UNITY_EDITOR
			_statesStack = string.Empty;
#endif
		}

		public void SetCurrentState(State state)
		{
			if (_changeStateFrameCount != Time.frameCount)
			{
				_changeStateFrameCount = Time.frameCount;
				_statesStack = string.Empty;
			}

#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(_statesStack)) _statesStack += " > ";
			_statesStack += state.Name;
#endif

			if ((++_changeStateCounter) >= 100)
			{
				throw new OverflowException("StateMachine: " + Name
#if UNITY_EDITOR
					+ "\nStack: " + _statesStack
#endif
				);
			}

			CurrentState = state;
		}

		public void Terminate()
		{
			DestroyChildren(this);
		}

		private void DestroyChildren(State state)
		{
			foreach (var child in state.Children)
			{
				DestroyChildren(child);
			}

			state.Destroy();
		}
	}
}
