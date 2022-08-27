using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewUtilities
{
    public class PosedTimeSpan
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime Start;
        /// <summary>
        /// 时长
        /// </summary>
        public TimeSpan Duration;
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime End
        {
            get => Start + Duration;
            set
            {
                Duration = value - Start;
            }
        }

        /// <summary>
        /// 平移整个时间段
        /// </summary>
        /// <param name="offset">偏移</param>
        public void Shift(TimeSpan offset)
        {
            Start += offset;
        }

        /// <summary>
        /// 一个时间段是否包含另一个时间段
        /// </summary>
        /// <remark>
        /// pts的始末时间都落在本时间段的[Start,End]内时，认为pts被本时间段包含。
        /// </remark>
        /// <param name="pts">被包含的时间段</param>
        /// <returns></returns>
        public bool Contains(PosedTimeSpan pts)
        {
            return Start <= pts.Start && End >= pts.End;
        }

        /// <summary>
        /// pts是否是当前时间段的接续
        /// </summary>
        /// <remark>
        /// pts的开始时间落在本时间段的[Start,End]内时，认为pts是本时间段的接续。
        /// </remark>
        /// <param name="pts">用于接续的时间段</param>
        /// <returns></returns>
        public bool Continues(PosedTimeSpan pts)
        {
            return Start <= pts.Start && End >= pts.Start;
        }

        /// <summary>
        /// 将另一时间段与本时间段合并
        /// </summary>
        /// <remark>
        /// 合并的原则是保持总体开始时间不变，时长相加。
        /// </remark>
        /// <remark>
        /// 合并的两者必须满足接续条件。
        /// </remark>
        /// <param name="a"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public PosedTimeSpan Add(PosedTimeSpan a)
        {
            if (Continues(a))
            {
                Duration += a.Duration;
            }
            else if (a.Continues(this))
            {
                Start = a.Start;
                Duration += a.Duration;
            }
            else
            {
                throw new ArgumentOutOfRangeException("无法将断开的两段时间相加");
            }
            return this;
        }

        public PosedTimeSpan Clone()
        {
            return new PosedTimeSpan
            {
                Start = Start,
                Duration = Duration
            };
        }

        public static PosedTimeSpan operator +(PosedTimeSpan a, PosedTimeSpan b)
        {
            var c = a.Clone();
            c.Add(b);
            return c;
        }
    }
}
