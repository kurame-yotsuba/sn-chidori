namespace SwallowNest.Chidori
{
	/// <summary>
	/// 繰り返しアクションを加える際のタイミングを表します。
	/// </summary>
	public enum NextAdditionType
	{
		/// <summary>
		/// アクション実行前に追加されます。
		/// デフォルトはこれです。
		/// </summary>
		PreExecute,

		/// <summary>
		/// アクション実行後に追加されます。
		/// </summary>
		PostExecute,
	}
}
