using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SwallowNest.Shikibu
{
    /// <summary>
    /// 優先度付きキュー。スレッドセーフではない。
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TPriority"></typeparam>
    public class PriorityQueue<TElement, TPriority> : IEnumerable<(TElement element, TPriority priority)>
        where TElement : notnull
        where TPriority : IComparable<TPriority>
    {
        private static readonly string EmptyErrorMessage = "キューの中身が空です。";

        // 実行待ちのアクションを格納する実行時刻をキーとする順序付き辞書
        private readonly SortedDictionary<TPriority, LinkedList<TElement>> queue = new();

        /// <summary>
        /// 指定された優先度に基づいてキューに要素を追加する。
        /// </summary>
        /// <param name="element">キューに追加する要素</param>
        /// <param name="priority">優先度</param>
        public void Enqueue(TElement element, TPriority priority)
        {
            //指定の時間に既にタスクが入っている場合、そのタスクのあとに追加
            if (queue.TryGetValue(priority, out var list))
            {
                list.AddLast(element);
            }
            //そうでない場合は新しくキューを作成して追加
            else
            {
                list = new LinkedList<TElement>();
                list.AddLast(element);
                queue[priority] = list;

            }

            Count++;
        }

        /// <summary>
        /// 最も優先度の高い要素を取り出す。
        /// 優先度が同一の要素は追加された順に取り出される。
        /// </summary>
        /// <returns></returns>
        public TElement Dequeue()
        {
            var (time, list) = queue.First();

            // 先頭のキューの先頭の要素を取り出す。
            TElement element = list.First.Value;
            list.RemoveFirst();

            // 先頭のキューの要素数が0なら削除
            if (list.Count == 0)
            {
                queue.Remove(time);
            }
            
            Count--;

            return element;
        }

        /// <summary>
        /// キューの要素数。
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// 次の <see cref="Dequeue"/> 時に取り出される要素の優先度を返す。
        /// 要素数が 0 の時、例外を投げる。
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public TPriority PriorityPeek()
        {
            try
            {
                var (priority, _) = queue.First();
                return priority;
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(EmptyErrorMessage, e);
            }
        }

        /// <summary>
        /// 次の <see cref="Dequeue"/> 時に取り出される要素を返す。
        /// 要素数が 0 の時、例外を投げる。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"/>
        public TElement Peek()
        {
            try
            {
                var element = queue.First().Value.First.Value;
                return element;
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(EmptyErrorMessage, e);
            }
        }

        /// <summary>
        /// 要素の取り出しを試みる。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryDequeue([NotNullWhen(true)] out TElement? result)
        {
            var (time, list) = queue.FirstOrDefault();
            if (list is null)
            {
                result = default;
                return false;
            }

            // 先頭のキューの先頭の要素を取り出す。
            TElement element = list.First.Value;
            list.RemoveFirst();

            // 先頭のキューの要素数が0なら削除
            if (list.Count == 0)
            {
                queue.Remove(time);
            }

            Count--;

            result = element;
            return true;
        }

        /// <summary>
        /// 次の <see cref="Dequeue"/> 時に取り出される要素の取得を試みる。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryPeek([NotNullWhen(true)] out TElement? result)
        {
            var (_, list) = queue.FirstOrDefault();
            if(list is null)
            {
                result = default;
                return false;
            }

            result = list.First.Value;
            return true;
        }

        /// <summary>
        /// 次の <see cref="Dequeue"/> 時に取り出される要素の優先度の取得を試みる。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryPriorityPeek([NotNullWhen(true)] out TPriority? result)
        {
            var (priority, list) = queue.FirstOrDefault();
            if(list is null)
            {
                result = default;
                return false;
            }

            result = priority;
            return true;
        }

        /// <summary>
        /// キューの中身を削除する。
        /// </summary>
        public void Clear()
        {
            queue.Clear();
            Count = 0;
        }

        #region Implements of IEnumerable

        /// <summary>
        /// 要素とその優先度の列挙を行う。
        /// </summary>
        /// <returns></returns>
        public IEnumerator<(TElement element, TPriority priority)> GetEnumerator()
        {
            foreach (var (priority, list) in queue)
            {
                foreach (var element in list)
                {
                    yield return (element, priority);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}