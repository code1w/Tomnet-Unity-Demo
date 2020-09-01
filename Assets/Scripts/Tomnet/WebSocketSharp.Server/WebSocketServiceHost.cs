using System;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp.Server
{
	public abstract class WebSocketServiceHost
	{
		private Logger _log;

		private string _path;

		private WebSocketSessionManager _sessions;

		internal ServerState State => _sessions.State;

		protected Logger Log => _log;

		public bool KeepClean
		{
			get
			{
				return _sessions.KeepClean;
			}
			set
			{
				_sessions.KeepClean = value;
			}
		}

		public string Path => _path;

		public WebSocketSessionManager Sessions => _sessions;

		public abstract Type BehaviorType
		{
			get;
		}

		public TimeSpan WaitTime
		{
			get
			{
				return _sessions.WaitTime;
			}
			set
			{
				_sessions.WaitTime = value;
			}
		}

		protected WebSocketServiceHost(string path, Logger log)
		{
			_path = path;
			_log = log;
			_sessions = new WebSocketSessionManager(log);
		}

		internal void Start()
		{
			_sessions.Start();
		}

		internal void StartSession(WebSocketContext context)
		{
			CreateSession().Start(context, _sessions);
		}

		internal void Stop(ushort code, string reason)
		{
			_sessions.Stop(code, reason);
		}

		protected abstract WebSocketBehavior CreateSession();
	}
	internal class WebSocketServiceHost<TBehavior> : WebSocketServiceHost where TBehavior : WebSocketBehavior
	{
		private Func<TBehavior> _creator;

		public override Type BehaviorType => typeof(TBehavior);

		internal WebSocketServiceHost(string path, Func<TBehavior> creator, Logger log)
			: this(path, creator, (Action<TBehavior>)null, log)
		{
		}

		internal WebSocketServiceHost(string path, Func<TBehavior> creator, Action<TBehavior> initializer, Logger log)
			: base(path, log)
		{
			_creator = createCreator(creator, initializer);
		}

		private Func<TBehavior> createCreator(Func<TBehavior> creator, Action<TBehavior> initializer)
		{
			if (initializer == null)
			{
				return creator;
			}
			return delegate
			{
				TBehavior val = creator();
				initializer(val);
				return val;
			};
		}

		protected override WebSocketBehavior CreateSession()
		{
			return _creator();
		}
	}
}
