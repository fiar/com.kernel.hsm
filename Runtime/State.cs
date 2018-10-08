using System;
using System.Collections.Generic;

namespace Kernel.HSM
{
	public class State
	{
		public string Name { get; private set; }
		public string Path { get; private set; }
		public RootState Root { get; protected set; }
		public State Parent { get; private set; }
		public State ActiveChild { get; private set; }

		public bool IsActive { get; protected set; }

		private Action _awakeAction;
		private Action _startAction;
		private Action _enterAction;
		private Action _exitAction;
		private Action _destroyAction;
		private Action _updateAction;

		private Dictionary<string, State> _children = new Dictionary<string, State>();
		private Dictionary<string, Action<EventArgs>> _events = new Dictionary<string, Action<EventArgs>>();


		public State(string stateName, State parentState)
		{
			Name = stateName;
			Path = stateName.ToString();
			Parent = parentState;
			Root = (Parent != null) ? Parent.Root : null;
			ActiveChild = null;
			IsActive = false;

			if (Parent != null)
			{
				Path = Parent.Path + "/" + Path;
			}
		}

		public void Awake()
		{
			if (_awakeAction != null)
			{
				_awakeAction();
			}
		}

		public void Start()
		{
			if (_startAction != null)
			{
				_startAction();
			}
		}

		public void Enter()
		{
			IsActive = true;

			if (_enterAction != null)
			{
				_enterAction();
			}
		}

		public void Exit()
		{
			IsActive = false;

			if (ActiveChild != null)
			{
				ActiveChild.Exit();
				ActiveChild = null;
			}

			if (_exitAction != null)
			{
				_exitAction();
			}
		}

		public void Destroy()
		{
			if (IsActive)
			{
				Exit();
			}

			if (_destroyAction != null)
			{
				_destroyAction();
			}

			_children.Clear();
			_events.Clear();
		}

		public void Update()
		{
			if (ActiveChild != null)
			{
				ActiveChild.Update();
			}

			if (_updateAction != null)
			{
				_updateAction();
			}
		}

		public void PushState(string stateName)
		{
			State state;
			if (!_children.TryGetValue(stateName, out state))
			{
				throw new KeyNotFoundException("Tried to push to state \"" + stateName + "\", but it is not in the list of children. State: \"" + Name + "\"");
			}

			if (!Root.CanTransitSelf && Root.CurrentState == state) return;

			if (ActiveChild != null)
			{
				ActiveChild.Exit();
			}

			ActiveChild = state;
			Root.SetCurrentState(ActiveChild);
			ActiveChild.Enter();
		}

		public void ChangeState(string stateName)
		{
			if (Parent == null)
			{
				throw new KeyNotFoundException("Tried to change to state \"" + stateName + "\", but parent state is not exists. State: \"" + Name + "\"");
			}

			State state;
			if (!Parent._children.TryGetValue(stateName, out state))
			{
				throw new KeyNotFoundException("Tried to change to state \"" + stateName + "\", but it is not in the list of children. State: \"" + Name + "\"");
			}

			if (!Root.CanTransitSelf && Root.CurrentState == state) return;

			if (Parent.ActiveChild != null)
			{
				Parent.ActiveChild.Exit();
				Parent.ActiveChild = null;
			}

			Parent.ActiveChild = state;
			Root.SetCurrentState(Parent.ActiveChild);
			Parent.ActiveChild.Enter();
		}

		public void PopState()
		{
			if (Parent == null)
			{
				throw new KeyNotFoundException("Tried to pop state, but parent state is not exists. State: \"" + Name + "\"");
			}

			if (Parent.ActiveChild != null)
			{
				Parent.ActiveChild.Exit();
				Parent.ActiveChild = null;
			}

			Root.SetCurrentState(Parent);
		}

		public void AddChildState(State state)
		{
			if (_children.ContainsKey(state.Name))
			{
				throw new ApplicationException("State with name \"" + state.Name + "\" already exists in list of children. State: \"" + Name + "\"");
			}

			_children.Add(state.Name, state);
		}

		public void AddAwake(Action action)
		{
			_awakeAction += action;
		}

		public void AddStart(Action action)
		{
			_startAction += action;
		}

		public void AddEnter(Action action)
		{
			_enterAction += action;
		}

		public void AddExit(Action action)
		{
			_exitAction += action;
		}

		public void AddDestroy(Action action)
		{
			_destroyAction += action;
		}

		public void AddUpdate(Action action)
		{
			_updateAction += action;
		}

		public void AddEvent(string eventName, Action<State> action)
		{
			if (_events.ContainsKey(eventName))
			{
				throw new ApplicationException("Event with name \"" + eventName + "\" already exists in list of events. State: \"" + Name + "\"");
			}

			_events.Add(eventName, _ => action(this));
		}

		public void AddEvent<TEvent>(string eventName, Action<State, TEvent> action) where TEvent : EventArgs
		{
			if (_events.ContainsKey(eventName))
			{
				throw new ApplicationException("Event with name \"" + eventName + "\" already exists in list of events. State: \"" + Name + "\"");
			}

			_events.Add(eventName, args => action(this, args as TEvent));
		}

		public void TriggerEvent(string eventName, EventArgs args = null)
		{
#if UNITY_EDITOR
			if (!Root.IsRun) throw new ApplicationException("State machine is not runned.");
#endif

			if (Root.CurrentState == null)
			{
				throw new ApplicationException("TriggerEvent with name \"" + eventName + "\" is failed. Current state in null.");
			}

			TriggerEvent_Internal(Root.CurrentState, eventName, args);
		}

		public void TriggerEventUpwards(string eventName, EventArgs args = null)
		{
#if UNITY_EDITOR
			if (!Root.IsRun) throw new ApplicationException("State machine is not runned.");
#endif

			if (Root.CurrentState == null)
			{
				throw new ApplicationException("TriggerEvent with name \"" + eventName + "\" is failed. Current state in null.");
			}

			TriggerEventUpwards_Internal(Root.CurrentState, eventName, args);
		}

		public void BroadcastEvent(string eventName, EventArgs args = null)
		{
#if UNITY_EDITOR
			if (!Root.IsRun) throw new ApplicationException("State machine is not runned.");
#endif

			if (Root.CurrentState == null)
			{
				throw new ApplicationException("TriggerEvent with name \"" + eventName + "\" is failed. Current state in null.");
			}

			BroadcastEvent_Internal(Root.CurrentState, eventName, args);
		}

		public IEnumerable<State> Children
		{
			get { return _children.Values; }
		}

		public IEnumerable<string> Events
		{
			get { return _events.Keys; }
		}

		private void TriggerEvent_Internal(State state, string eventName, EventArgs args)
		{
			if (state._events.ContainsKey(eventName))
			{
				state._events[eventName](args);
			}
		}

		private void TriggerEventUpwards_Internal(State state, string eventName, EventArgs args)
		{
			if (state._events.ContainsKey(eventName))
			{
				state._events[eventName](args);
				return;
			}

			if (state.Parent != null)
			{
				state.TriggerEventUpwards_Internal(state.Parent, eventName, args);
			}
		}

		private void BroadcastEvent_Internal(State state, string eventName, EventArgs args)
		{
			if (state._events.ContainsKey(eventName))
			{
				state._events[eventName](args);
			}

			if (state.Parent != null)
			{
				state.BroadcastEvent_Internal(state.Parent, eventName, args);
			}
		}
	}
}
