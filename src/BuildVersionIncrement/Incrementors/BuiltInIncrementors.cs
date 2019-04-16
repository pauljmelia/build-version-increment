namespace BuildVersionIncrement.Incrementors
{
    using System;

    using Helpers;

    internal static class BuiltInIncrementors
    {
        public static DayStampIncrementor DayStamp => new DayStampIncrementor();
        public static DayOfYearIncrementor DayOfYear => new DayOfYearIncrementor();
        public static DeltaBaseDateIncrementor DeltaBaseDate => new DeltaBaseDateIncrementor();
        public static DeltaBaseDateInDaysIncrementor DeltaBaseDateInDays
            => new DeltaBaseDateInDaysIncrementor();
        public static DeltaBaseYearIncrementor DeltaBaseYear => new DeltaBaseYearIncrementor();
        public static DeltaBaseYearDayOfYearIncrementor DeltaBaseYearDayOfYear
            => new DeltaBaseYearDayOfYearIncrementor();
        public static IncrementIncrementor Increment => new IncrementIncrementor();
        public static MonthAndDayStampIncrementor MonthAndDayStamp => new MonthAndDayStampIncrementor();
        public static MonthStampIncrementor MonthStamp => new MonthStampIncrementor();
        public static NoneIncrementor None => new NoneIncrementor();
        public static OfficeIncrementor Office => new OfficeIncrementor();
        public static TimeStampIncrementor TimeStamp => new TimeStampIncrementor();
        public static YearDayOfYearIncrementor YearDayOfYear => new YearDayOfYearIncrementor();
        public static YearDecadeStampIncrementor YearDecade => new YearDecadeStampIncrementor();
        public static YearStampIncrementor YearStamp => new YearStampIncrementor();
        public static YearMonthStampIncrementor YearMonthStamp => new YearMonthStampIncrementor();
        public static YearMonthDayStampIncrementor YearMonthDayStamp => new YearMonthDayStampIncrementor();

        internal class DayStampIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Day stamp (dd)";
            public override string Name => "DayStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                return $"{buildStart.Day:00}";
            }
        }

        internal class DayOfYearIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Day of the year (dayOfYear)";
            public override string Name => "DayOfYearStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var dayOfyear = buildStart.DayOfYear;
                return $"{dayOfyear:000}";
            }
        }

        internal class DeltaBaseDateIncrementor : BuiltInIncrementorBase
        {
            public override string Description
                => "Delta base date (y * 12 + month, dayOfMonth since start date)";

            public override string Name => "DeltaBaseDate";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var ds = DateSpan.GetDateDifference(buildStart, projectStart);
                return $"{(ds.Years * 12) + ds.Months}{ds.Days:00}";
            }
        }

        internal class DeltaBaseDateInDaysIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Delta base date in days";
            public override string Name => "DeltaBaseDateInDays";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var ts = buildStart.Subtract(projectStart);
                return ((int)ts.TotalDays).ToString();
            }
        }

        internal class DeltaBaseYearDayOfYearIncrementor : BuiltInIncrementorBase
        {
            public override string Description
                => "Delta base year including day of year (years since start date, dayOfYear)";

            public override string Name => "DeltaBaseYearDayOfYear";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var dayOfyear = buildStart.DayOfYear;
                var deltaYears = buildStart.Year - projectStart.Year;
                return $"{deltaYears}{dayOfyear:000}";
            }
        }

        internal class DeltaBaseYearIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Delta base year (years since start date)";
            public override string Name => "DeltaBaseYear";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var deltaYears = buildStart.Year - projectStart.Year;
                return deltaYears.ToString();
            }
        }

        internal class IncrementIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Simple increment";
            public override string Name => "Increment";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                int v;
                int.TryParse(value, out v);
                if (v < 0)
                {
                    v = 0;
                }

                return (v + 1).ToString();
            }
        }

        internal class MonthAndDayStampIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Month and Day stamp (mmdd)";
            public override string Name => "MonthAndDayStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                return $"{buildStart.Month:00}{buildStart.Day:00}";
            }
        }

        internal class MonthStampIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Month stamp (mm)";
            public override string Name => "MonthStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                return $"{buildStart.Month:00}";
            }
        }

        internal class NoneIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "No increment";
            public override string Name => "None";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                return value;
            }
        }

        internal class OfficeIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Delta base year start in days";
            public override string Name => "Office";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var start = new DateTime(projectStart.Year, 1, 1);
                var ts = buildStart.Subtract(start);
                return ((int)ts.TotalDays).ToString();
            }
        }

        internal class TimeStampIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Time stamp (hhmm)";
            public override string Name => "TimeStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                return $"{buildStart.Hour:00}{buildStart.Minute:00}";
            }
        }

        internal class YearDayOfYearIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Year followed by the day of the year (yy, dayOfYear)";
            public override string Name => "YearDayOfYearStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var dayOfyear = buildStart.DayOfYear;
                var yearDecade = buildStart.ToString("yy");

                return $"{yearDecade}{dayOfyear:000}";
            }
        }

        internal class YearDecadeStampIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Year decade stamp (yy)";
            public override string Name => "YearDecadeStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var yearDecade = buildStart.ToString("yy");
                return yearDecade;
            }
        }

        internal class YearMonthDayStampIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Year month day stamp (yymmdd)";
            public override string Name => "YearMonthDayStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var yearDecade = buildStart.ToString("yy");
                return $"{yearDecade}{buildStart.Month:00}{buildStart.Day:00}";
            }
        }

        internal class YearMonthStampIncrementor: BuiltInIncrementorBase
        {
            public override string Description => "Year month stamp (yymm)";
            public override string Name => "YearMonthStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                var yearDecade = buildStart.ToString("yy");
                return $"{yearDecade}{buildStart.Month:00}";
            }
        }



        internal class YearStampIncrementor : BuiltInIncrementorBase
        {
            public override string Description => "Year stamp (yyyy)";
            public override string Name => "YearStamp";

            internal override string IncrementImpl(string value, DateTime buildStart, DateTime projectStart)
            {
                return buildStart.Year.ToString();
            }
        }
    }
}