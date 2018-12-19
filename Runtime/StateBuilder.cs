using System;

namespace Kernel.HSM
{
	public class StateBuilder<TParent>
	{
		private TParent _parentBuilder;
		private State _state;


		public StateBuilder(string stateName, State parentState, TParent parentBuilder)
		{
			_parentBuilder = parentBuilder;

			_state = new State(stateName, parentState);
			parentState.AddChildState(_state);
		}

		public StateBuilder<StateBuilder<TParent>> State(string stateName)
		{
			return new StateBuilder<StateBuilder<TParent>>(stateName, _state, this);
		}

		public TParent End()
		{
			return _parentBuilder;
		}

		public StateBuilder<TParent> Awake(Action<State> action)
		{
			_state.AddAwake(() => action(_state));

			return this;
		}

		public StateBuilder<TParent> Start(Action<State> action)
		{
			_state.AddStart(() => action(_state));

			return this;
		}

		public StateBuilder<TParent> Enter(Action<State> action)
		{
			_state.AddEnter(() => action(_state));

			return this;
		}

		public StateBuilder<TParent> Exit(Action<State> action)
		{
			_state.AddExit(() => action(_state));

			return this;
		}

		public StateBuilder<TParent> Destroy(Action<State> action)
		{
			_state.AddDestroy(() => action(_state));

			return this;
		}

		public StateBuilder<TParent> Update(Action<State> action)
		{
			_state.AddUpdate(() => action(_state));

			return this;
		}

		public StateBuilder<TParent> Event(string eventName, Action<State> action)
		{
			_state.AddEvent(eventName, state => action(state));

			return this;
		}

		public StateBuilder<TParent> Event<TEvent>(string eventName, Action<State, TEvent> action) where TEvent : class
		{
			_state.AddEvent<TEvent>(eventName, (state, args) => action(state, args));

			return this;
		}

		public StateBuilder<TParent> Message<TMessage>(Action<State, TMessage> action) where TMessage : class
		{
			_state.AddMessage<TMessage>((state, msg) => action(state, msg));

			return this;
		}
	}
}
