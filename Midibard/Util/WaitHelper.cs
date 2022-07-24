using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard.Util
{
	class WaitTimer
	{
		public WaitTimer(TimeSpan timeToWait)
		{
			TimeToWait = timeToWait;
			StartTime = DateTime.UtcNow;
		}

		public DateTime StartTime { get; private set; }

		public TimeSpan TimeToWait { get; }

		public void Reset() => StartTime = DateTime.UtcNow;
		public bool IsWaiting => StartTime < DateTime.UtcNow && !IsFinished;
		public bool IsFinished => TimeLeft <= TimeSpan.Zero;
		public TimeSpan TimeLeft => StartTime + TimeToWait - DateTime.UtcNow;
	}
	/// <summary>
	/// Helper class to manage time waits. Used so we do not pulse certain tasks 30 times a second.
	/// </summary>
	internal class WaitHelper
	{
		#region Singleton

		private static WaitHelper _waitHelper;
		internal static WaitHelper Instance => _waitHelper ??= new WaitHelper();

		#endregion

		/// <summary>
		/// Holds a list of all managed waits.
		/// </summary>
		private readonly Dictionary<string, WaitTimer> _waitTimerList;

		/// <summary>
		/// Constructor WaitHelper()
		/// </summary>
		private WaitHelper()
		{
			_waitTimerList = new Dictionary<string, WaitTimer>();
		}

		/// <summary>
		/// <para>Adds a wait key.</para>
		/// <para>Will remove old keys under the same name before adding.</para>
		/// </summary>
		/// <param name="name">Name of the wait key to add</param>
		/// <param name="timeToWait">TimeSpan to wait</param>
		internal void StartWait(string name, TimeSpan timeToWait)
		{
			RemoveWait(name);

			WaitTimer timerToAdd = new WaitTimer(timeToWait);
			_waitTimerList.Add(name, timerToAdd);
			_waitTimerList[name].Reset();
		}

		/// <summary>
		/// Checks whether a given wait exists.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private bool HasWait(string name)
		{
			return _waitTimerList.ContainsKey(name);
		}

		/// <summary>
		/// Syntax sugar, opposite of IsWaiting.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal bool IsFinished(string name)
		{
			return _waitTimerList.TryGetValue(name, out var timer) && timer.IsFinished;
		}

		/// <summary>
		/// Checks if a wait exists and is still waiting.
		/// </summary>
		/// <param name="name">Name of the wait key to remove</param>
		/// <returns></returns>
		internal bool IsWaiting(string name)
		{
			return _waitTimerList.TryGetValue(name, out var timer) && timer.IsWaiting;
		}

		/// <summary>
		/// Removes a given wait.
		/// </summary>
		/// <param name="name">Name of the wait key to remove</param>
		public void RemoveWait(string name)
		{
			if (HasWait(name))
				_waitTimerList.Remove(name);
		}

		/// <summary>
		/// Resets a given wait.
		/// </summary>
		/// <param name="name">Name of the wait key to check</param>
		/// <returns></returns>
		internal void ResetWait(string name)
		{
			if (HasWait(name))
				_waitTimerList[name].Reset();
		}

		/// <summary>
		/// Returns the time left on a wait.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal TimeSpan TimeLeft(string name)
		{
			if (HasWait(name))
				return _waitTimerList[name].TimeLeft;

			return TimeSpan.Zero;
		}
		/// <summary>
		/// Returns the total time to wait.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal TimeSpan TimeToWait(string name)
		{
			if (HasWait(name))
				return _waitTimerList[name].TimeToWait;

			return TimeSpan.Zero;
		}

		/// <summary>
		/// Returns the total time to wait.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal WaitTimer? GetTimer(string name)
		{
			if (_waitTimerList.TryGetValue(name, out var timer) && timer.IsWaiting)
				return timer;

			return null;
		}
	}
}
