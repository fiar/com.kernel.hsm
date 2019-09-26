using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kernel.HSM
{
	public class RootState : State
	{
		public event Action Runned;
		public event Action Stopped;
		public event Action Terminated;
		public event Action<State, State> StateChanged;

		public State CurrentState { get; private set; }

		public bool CanTransitSelf { get; set; }
		public bool IsRun { get; private set; }

		private int _changeStateFrameCount;
		private int _changeStateCounter;
#if UNITY_EDITOR
		private string _statesStack;
#endif

		private Dictionary<object, int> _firedMessages = new Dictionary<object, int>();


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
				_changeStateCounter = 0;
#if UNITY_EDITOR
				_statesStack = string.Empty;
#endif
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

			var previous = CurrentState;
			CurrentState = state;

			if (StateChanged != null)
				StateChanged.Invoke(previous, state);
		}

		public void Run(string stateName)
		{
			if (!IsRun)
			{
				IsRun = true;

				if (Runned != null)
					Runned.Invoke();

				StartChildren(this);

				PushState(stateName);
			}
		}

		public void Stop()
		{
			if (IsRun)
			{
				IsRun = false;

				Exit();

				CurrentState = this;
				IsActive = true;

				if (Stopped != null)
					Stopped.Invoke();
			}
		}

		public void Terminate()
		{
			if (IsRun)
			{
				IsRun = false;
				DestroyChildren(this);

				if (Terminated != null)
					Terminated.Invoke();
			}

			_firedMessages.Clear();
		}

		public void RegisterFiredMessage(object message)
		{
			_firedMessages[message] = Time.frameCount;
		}

		public bool CanFireMessage(object message)
		{
			return _firedMessages.ContainsKey(message) ? _firedMessages[message] != Time.frameCount : true;
		}

		private void StartChildren(State state)
		{
			state.Start();

			foreach (var child in state.Children)
			{
				StartChildren(child);
			}
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
