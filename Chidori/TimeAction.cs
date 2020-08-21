using System;

namespace SwallowNest.Chidori
{
	public class TimeAction
	{
		#region static member

		/// <summary>
		/// アクションの繰り返し間隔の最小値です。
		/// </summary>
		public static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);

		private static readonly string intervalMinimumErrorMessage = $"時間間隔は{MinimumInterval.TotalSeconds}秒以上でなければなりません。";

		#endregion static member

		/// <summary>
		/// 時間になったら実行されるハンドラです。
		/// </summary>
		public event Action OnSchedule;

		public string? Name { get; }

		/// <summary>
		/// アクションが実行される時刻
		/// </summary>
		public DateTime ExecTime { get; internal set; }

		/// <summary>
		/// アクションの繰り返し間隔
		/// </summary>
		public TimeSpan Interval { get; internal set; }

		/// <summary>
		/// アクションの実行条件。
		/// nullの場合は実行されます。
		/// </summary>
		public event Func<bool>? CanExecute;

		/// <summary>
		/// 繰り返しアクションを追加タイミングの種類です。
		/// </summary>
		public RepeatAdditionType AdditionType { get; set; }

		#region constructor

		/// <summary>
		/// 共通処理用のコンストラクタ
		/// </summary>
		/// <param name="onSchedule"></param>
		/// <param name="name"></param>
		protected TimeAction(Action onSchedule, string? name)
		{
			OnSchedule = onSchedule;
			Name = name;
			AdditionType = RepeatAdditionType.BeforeExecute;
		}

		/// <summary>
		/// 指定された時刻にアクションを実行するためのインスタンスを生成します。
		/// </summary>
		/// <param name="onSchedule"></param>
		/// <param name="execTime">アクションが実行される時刻</param>
		/// <param name="name"></param>
		public TimeAction(
			Action onSchedule,
			DateTime execTime,
			string? name = null) : this(onSchedule, name)
		{
			ExecTime = execTime;
		}

		/// <summary>
		/// 指定された間隔でアクションを
		/// 繰り返し実行するためのインスタンスを生成します。
		/// </summary>
		/// <param name="onSchedule"></param>
		/// <param name="interval">アクションの繰り返し間隔</param>
		/// <param name="name"></param>
		public TimeAction(
			Action onSchedule,
			TimeSpan interval,
			string? name = null) : this(onSchedule, name)
		{
			// 繰り返し間隔が短すぎないかチェック
			if (interval < MinimumInterval)
			{
				throw new ArgumentOutOfRangeException(nameof(interval), intervalMinimumErrorMessage);
			}

			// 最初の実行時刻はinterval後
			ExecTime = DateTime.Now + interval;
			Interval = interval;
		}

		/// <summary>
		/// 指定された時刻から一定間隔で、
		/// アクションを繰り返し実行するためのインスタンスを生成します。
		/// </summary>
		/// <param name="onSchedule"></param>
		/// <param name="execTime">アクションが実行される時刻</param>
		/// <param name="interval">アクションの繰り返し間隔</param>
		/// <param name="name"></param>
		public TimeAction(
			Action onSchedule,
			DateTime execTime,
			TimeSpan interval,
			string? name = null) : this(onSchedule, name)
		{
			// 繰り返し間隔が短すぎないかチェック
			if (interval < MinimumInterval)
			{
				throw new ArgumentOutOfRangeException(nameof(interval), intervalMinimumErrorMessage);
			}

			ExecTime = execTime;
			Interval = interval;
		}

		#endregion constructor

		internal void Invoke()
		{
			if (CanExecute?.Invoke() ?? true)
			{
				OnSchedule();
			}
		}
	}
}
