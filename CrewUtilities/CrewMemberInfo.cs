using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewUtilities
{
    public class CrewMemberInfo
    {
        public long Uid;

        /// <summary>
        /// 时间轴
        /// </summary>
        public List<PosedTimeSpan> Timeline { get; protected set; }

        /// <summary>
        /// 最长连续在舰时长
        /// </summary>
        public PosedTimeSpan Longest
        {
            get
            {
                PosedTimeSpan longest = Timeline.FirstOrDefault();
                foreach (var item in Timeline)
                {
                    if (longest.Duration < item.Duration)
                    {
                        longest = item;
                    }
                }
                return longest;
            }
        }

        /// <summary>
        /// 目前是否在舰
        /// </summary>
        public bool IsOnboard
        {
            get => LastOnboard.End > DateTime.Now;
        }

        /// <summary>
        /// 剩余在舰时长
        /// </summary>
        public TimeSpan RemainingOnboradTime
        {
            get => LastOnboard.End - DateTime.Now;
        }

        /// <summary>
        /// 最近一次上舰
        /// </summary>
        public PosedTimeSpan LastOnboard
        {
            get => Timeline.LastOrDefault();
        }

        /// <summary>
        /// 累计在舰时长
        /// </summary>
        public TimeSpan TotalOnboradTime
        {
            get
            {
                TimeSpan ts = new TimeSpan(0);
                foreach (var item in Timeline)
                {
                    ts += item.Duration;
                }
                return ts;
            }
        }

        public CrewMemberInfo(long uid)
        {
            Uid = uid;
            Timeline = new List<PosedTimeSpan>();
        }

        public void AddRecord(PosedTimeSpan pts)
        {
            bool hit = false;
            foreach (var item in Timeline)
            {
                if (!hit)
                {
                    if (item.Continues(pts))
                    {
                        hit = true;
                        item.Add(pts);
                    }
                }
                else
                {
                    item.Shift(pts.Duration);
                }
            }
            if (!hit)
            {
                Timeline.Add(pts);
            }
            Timeline.Sort((a, b) => a.Start.CompareTo(b.Start));
        }
    }
}
