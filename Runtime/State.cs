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
			if (ActiveChild != null)
			{
				ActiveChild.Exit();
			}

			State state;
			if (!_children.TryGetValue(stateName, out state))
			{
				throw new KeyNotFoundException("Tried to push to state \"" + stateName + "\", but it is not in the list of children. State: \"" + Name + "\"");
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

			if (Parent.ActiveChild != null)
			{
				Parent.ActiveChild.Exit();
				Parent.ActiveChild = null;
			}

			State state;
			if (!Parent._children.TryGetValue(stateName, out state))
			{
				throw new KeyNotFoundException("Tried to change to state \"" + stateName + "\", but it is not in the list of children. State: \"" + Name + "\"");
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
			// TODO: check errors
			_events.Add(eventName, args => action(this, args as TEvent));
		}

		public void TriggerEvent(string eventName, EventArgs args = null)
		{
			if (_events.ContainsKey(eventName))
			{
				_events[eventName](args);
			}
		}

		public void TriggerEventUpwards(string eventName, EventArgs args = null)
		{
			if (_events.ContainsKey(eventName))
			{
				_events[eventName](args);
				return;
			}

			if (Parent != null)
			{
				Parent.TriggerEventUpwards(eventName, args);
			}
		}

		public void BroadcastEvent(string eventName, EventArgs args = null)
		{
			if (_events.ContainsKey(eventName))
			{
				_events[eventName](args);
			}

			if (Parent != null)
			{
				Parent.BroadcastEvent(eventName, args);
			}
		}

		public IEnumerable<State> Children
		{
			get { return _children.Values; }
		}

		public IEnumerable<string> Events
		{
			get { return _events.Keys; }
		}
	}
}
