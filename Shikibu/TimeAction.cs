using System;
using System.Threading.Tasks;

namespace SwallowNest.Shikibu
{
    /// <summary>
    /// <see cref="TimeActionScheduler"/>内で扱われるインスタンスです。
    /// </summary>
    public class TimeAction
    {
        #region static member

        /// <summary>
        /// アクションの繰り返し間隔の最小値です。
        /// </summary>
        public static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);

        private static readonly string IntervalErrorMessage = $"時間間隔は{MinimumInterval.TotalSeconds}秒以上でなければなりません。";

        #endregion static member

        /// <summary>
        /// 時間になったら実行されるハンドラです。
        /// </summary>
        private readonly Func<ValueTask> onSchedule;

        /// <summary>
        /// アクションが実行される時刻です。
        /// </summary>
        public DateTime ExecTime { get; internal set; }

        /// <summary>
        /// アクションの繰り返し間隔です。
        /// <see cref="MinimumInterval"/>以上の値を取ります。
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// アクションの実行条件です。
        /// nullの場合は実行されます。
        /// </summary>
        public event Func<bool>? CanExecute;

        /// <summary>
        /// 繰り返しアクションを追加タイミングの種類です。
        /// デフォルトは <see cref="RepeatAdditionType.BeforeExecute"/> です。
        /// </summary>
        public RepeatAdditionType AdditionType { get; set; }

        #region constructor

        /// <summary>
        /// 共通処理用のコンストラクタ
        /// </summary>
        /// <param name="onSchedule"></param>
        protected TimeAction(Func<ValueTask> onSchedule)
        {
            this.onSchedule = onSchedule;
            AdditionType = RepeatAdditionType.BeforeExecute;
        }

        /// <summary>
        /// 指定された時刻にアクションを実行するためのインスタンスを生成します。
        /// </summary>
        /// <param name="onSchedule"></param>
        /// <param name="execTime"></param>
        public TimeAction(
            Func<ValueTask> onSchedule,
            DateTime execTime) : this(onSchedule)
        {
            ExecTime = execTime;
        }

        /// <summary>
        /// 指定された時刻から一定間隔で、
        /// アクションを繰り返し実行するためのインスタンスを生成します。
        ///
        /// <list type="bullet">
        ///     <item>例外：<paramref name="interval"/> が <see cref="MinimumInterval"/> 未満</item>
        /// </list>
        ///
        /// </summary>
        /// <param name="onSchedule"></param>
        /// <param name="execTime">アクションが実行される時刻</param>
        /// <param name="interval">アクションの繰り返し間隔</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public TimeAction(
            Func<ValueTask> onSchedule,
            DateTime execTime,
            TimeSpan interval) : this(onSchedule)
        {
            // 繰り返し間隔が短すぎないかチェック
            if (interval < MinimumInterval)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), IntervalErrorMessage);
            }

            ExecTime = execTime;
            Interval = interval;
        }

        /// <summary>
        /// 指定された間隔で、アクションを繰り返し実行するためのインスタンスを生成します。
        ///
        /// <list type="bullet">
        ///     <item>例外：<paramref name="interval"/> が <see cref="MinimumInterval"/> 未満</item>
        /// </list>
        /// </summary>
        /// <param name="onSchedule"></param>
        /// <param name="interval">アクションの繰り返し間隔</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public TimeAction(
            Func<ValueTask> onSchedule,
            TimeSpan interval) : this(onSchedule, DateTime.Now + interval, interval)
        {
        }

        #region Action から Func<ValueTask> へのコンストラクタ

#pragma warning disable CS1998

        /// <summary>
        /// 共通処理用のコンストラクタ
        /// </summary>
        /// <param name="onSchedule"></param>
        protected TimeAction(Action onSchedule) : this(async () => onSchedule())
        {
        }

        /// <summary>
        /// 指定された時刻にアクションを実行するためのインスタンスを生成します。
        /// </summary>
        /// <param name="onSchedule"></param>
        /// <param name="execTime">アクションが実行される時刻</param>
        public TimeAction(
            Action onSchedule,
            DateTime execTime) : this(async () => onSchedule(), execTime)
        {
        }

        /// <summary>
        /// 指定された間隔で、アクションを繰り返し実行するためのインスタンスを生成します。
        ///
        /// <list type="bullet">
        ///     <item>例外：<paramref name="interval"/> が <see cref="MinimumInterval"/> 未満</item>
        /// </list>
        /// </summary>
        /// <param name="onSchedule"></param>
        /// <param name="interval">アクションの繰り返し間隔</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public TimeAction(
            Action onSchedule,
            TimeSpan interval) : this(async () => onSchedule(), interval)
        {
        }

        /// <summary>
        /// 指定された時刻から一定間隔で、
        /// アクションを繰り返し実行するためのインスタンスを生成します。
        ///
        /// <list type="bullet">
        ///     <item>例外：<paramref name="interval"/> が <see cref="MinimumInterval"/> 未満</item>
        /// </list>
        ///
        /// </summary>
        /// <param name="onSchedule"></param>
        /// <param name="execTime">アクションが実行される時刻</param>
        /// <param name="interval">アクションの繰り返し間隔</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public TimeAction(
            Action onSchedule,
            DateTime execTime,
            TimeSpan interval) : this(async () => onSchedule(), execTime, interval)
        {
        }

#pragma warning restore CS1998

        #endregion Action から Func<ValueTask> へのコンストラクタ

        #endregion constructor

        /// <summary>
        /// 実行条件を満たす場合にアクションを実行します。
        /// </summary>
        /// <returns></returns>
        internal async ValueTask Invoke()
        {
            if (CanExecute?.Invoke() ?? true)
            {
                await onSchedule();
            }
        }
    }
}