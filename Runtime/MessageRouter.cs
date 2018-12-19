using System;
using System.Collections.Generic;
using System.Linq;

namespace Kernel.HSM
{
	public class MessageRouter
	{
		private abstract class Subscriber : IDisposable
		{
			public abstract void Dispose();
		}

		private class Subscriber<T> : Subscriber
		{
			public Action<T> Subscription;

			public override void Dispose()
			{
				MessageRouter.Unsubscribe<T>(Subscription);
			}
		}

		private static MessageRouter _instance;
		private readonly Dictionary<Type, List<Delegate>> _delegates;

		private static MessageRouter Instance
		{
			get
			{
				if (_instance == null)
					_instance = new MessageRouter();
				return _instance;
			}
		}

		private MessageRouter()
		{
			_delegates = new Dictionary<Type, List<Delegate>>();
		}

		public static void Publish<T>(T message)
		{
			if (message == null)
			{
				return;
			}
			if (!Instance._delegates.ContainsKey(typeof(T)))
			{
				return;
			}
			var delegates = Instance._delegates[typeof(T)];
			if (delegates == null || delegates.Count == 0) return;

			for (var i = delegates.Count - 1; i >= 0; i--)
			{
				var handler = delegates[i] as Action<T>;
				if (handler != null)
				{
					handler.Invoke(message);
				}
			}
		}

		public static IDisposable Subscribe<T>(Action<T> subscription)
		{
			var delegates = Instance._delegates.ContainsKey(typeof(T)) ?
							Instance._delegates[typeof(T)] : new List<Delegate>();

			var subscriber = new Subscriber<T>
			{
				Subscription = subscription
			};

			if (!delegates.Contains(subscription))
			{
				delegates.Add(subscription);
			}
			Instance._delegates[typeof(T)] = delegates;

			return subscriber;
		}

		private static void Unsubscribe<T>(Action<T> subscription)
		{
			if (_instance == null)
			{
				return;
			}

			if (Instance._delegates.ContainsKey(typeof(T)))
			{
				var delegates = Instance._delegates[typeof(T)];
				if (delegates.Contains(subscription))
				{
					delegates.Remove(subscription);
				}
				if (delegates.Count == 0)
				{
					Instance._delegates.Remove(typeof(T));
				}
			}
		}

		public void Dispose()
		{
			if (_delegates != null)
			{
				_delegates.Clear();
			}
		}
	}
}