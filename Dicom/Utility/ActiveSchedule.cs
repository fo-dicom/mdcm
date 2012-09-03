using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dicom.Utility {
	public class DayOfWeekTimeRange {
		public readonly static DateTime StartOfDay = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		public readonly static DateTime EndOfDay = new DateTime(1970, 1, 1, 23, 59, 59, 999, DateTimeKind.Utc);

		public readonly static DayOfWeekTimeRange AlwaysActive = new DayOfWeekTimeRange {
			Active = true,
			StartTime = StartOfDay,
			EndTime = EndOfDay,
			Sunday = true,
			Monday = true,
			Tuesday = true,
			Wednesday = true,
			Thursday = true,
			Friday = true,
			Saturday = true
		};

		public readonly static DayOfWeekTimeRange AlwaysInactive = new DayOfWeekTimeRange(false);

		#region Private Members
		private bool _active;
		private bool _sun, _mon, _tue, _wed, _thu, _fri, _sat;
		private DateTime _start, _end;
		#endregion

		#region Public Constructor
		public DayOfWeekTimeRange() {
			Active = true;
			StartTime = StartOfDay;
			EndTime = EndOfDay;
		}

		public DayOfWeekTimeRange(bool active) {
			Active = active;
			StartTime = StartOfDay;
			EndTime = EndOfDay;
		}
		#endregion

		#region Public Properties
		public bool Active {
			get { return _active; }
			set { _active = value; }
		}

		public bool Sunday {
			get { return _sun; }
			set { _sun = value; }
		}

		public bool Monday {
			get { return _mon; }
			set { _mon = value; }
		}

		public bool Tuesday {
			get { return _tue; }
			set { _tue = value; }
		}

		public bool Wednesday {
			get { return _wed; }
			set { _wed = value; }
		}

		public bool Thursday {
			get { return _thu; }
			set { _thu = value; }
		}

		public bool Friday {
			get { return _fri; }
			set { _fri = value; }
		}

		public bool Saturday {
			get { return _sat; }
			set { _sat = value; }
		}

		public DateTime StartTime {
			get { return _start; }
			set {
				_start = NormalizeTime(value);

				if (_start > _end) {
					DateTime temp = _start;
					_start = _end;
					_end = temp;
				}
			}
		}

		public DateTime EndTime {
			get { return _end; }
			set {
				_end = NormalizeTime(value);

				if (_start > _end) {
					DateTime temp = _start;
					_start = _end;
					_end = temp;
				}
			}
		}
		#endregion

		#region Public Methods
		public void SetDayOfWeek(DayOfWeek day, bool active) {
			switch (day) {
				case DayOfWeek.Sunday: Sunday = active; return;
				case DayOfWeek.Monday: Monday = active; return;
				case DayOfWeek.Tuesday: Tuesday = active; return;
				case DayOfWeek.Wednesday: Wednesday = active; return;
				case DayOfWeek.Thursday: Thursday = active; return;
				case DayOfWeek.Friday: Friday = active; return;
				case DayOfWeek.Saturday: Saturday = active; return;
				default:
					break;
			}
		}

		public void SetDaysOfWeek(DayOfWeek begin, DayOfWeek end, bool active) {
			for (; begin <= end; begin++)
				SetDayOfWeek(begin, active);
		}

		public bool Contains(DateTime time) {
			time = NormalizeTime(time);
			if (time < _start || time >= _end)
				return false;
			return true;
		}

		public bool IsActiveNow() {
			return IsActiveAt(DateTime.Now);
		}

		public bool IsActiveAt(DateTime time) {
			switch (time.DayOfWeek) {
				case DayOfWeek.Sunday:
					if (!Sunday) return !Active;
					break;
				case DayOfWeek.Monday:
					if (!Monday) return !Active;
					break;
				case DayOfWeek.Tuesday:
					if (!Tuesday) return !Active;
					break;
				case DayOfWeek.Wednesday:
					if (!Wednesday) return !Active;
					break;
				case DayOfWeek.Thursday:
					if (!Thursday) return !Active;
					break;
				case DayOfWeek.Friday:
					if (!Friday) return !Active;
					break;
				case DayOfWeek.Saturday:
					if (!Saturday) return !Active;
					break;
				default:
					return false;
			}

			time = NormalizeTime(time);
			if (time < _start || time >= _end)
				return !Active;

			return Active;
		}

		public DayOfWeekTimeRange Clone() {
			DayOfWeekTimeRange range = new DayOfWeekTimeRange();
			range.Active = Active;
			range.Sunday = Sunday;
			range.Monday = Monday;
			range.Tuesday = Tuesday;
			range.Wednesday = Wednesday;
			range.Thursday = Thursday;
			range.Friday = Friday;
			range.Saturday = Saturday;
			range.StartTime = StartTime;
			range.EndTime = EndTime;
			return range;
		}
		#endregion

		#region Static Methods
		public static DateTime NormalizeTime(DateTime time) {
			return new DateTime(1970, 1, 1, time.Hour, time.Minute, time.Second, time.Millisecond, time.Kind);
		}

		public static DayOfWeek ParseDayOfWeek(string day) {
			switch (day.ToLower()) {
				case "sun": case "0": case "sunday":	return DayOfWeek.Sunday;
				case "mon": case "1": case "monday":	return DayOfWeek.Monday;
				case "tue": case "2": case "tuesday":	return DayOfWeek.Tuesday;
				case "wed": case "3": case "wednesday":	return DayOfWeek.Wednesday;
				case "thu": case "4": case "thursday":	return DayOfWeek.Thursday;
				case "fri": case "5": case "friday":	return DayOfWeek.Friday;
				case "sat": case "6": case "saturday":	return DayOfWeek.Saturday;
				default:
					throw new ArgumentException("Expected a day of the week in 3 letter ('sun') or full format ('sunday')", "day");
			}
		}

		private static string[] TimeFormats = { "ht", "htt", "h:mmt", "h:mmtt", "HHmmss", "HH", "HHmm", "HH.mm", "HH.mm.ss","HH:mm", "HH:mm:ss" };
		public static DateTime ParseTime(string time) {
			if (String.IsNullOrEmpty(time))
				throw new ArgumentException("Time string cannot be null or empy", "time");
			switch (time[time.Length - 1]) {
				case 'a': case 'A':
				case 'p': case 'P':
					time += 'm';
					break;
				default:
					break;
			}
			return NormalizeTime(
				DateTime.ParseExact(time, TimeFormats, 
				CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault));
		}

		public static DayOfWeekTimeRange[] Parse(string daysOfWeek, string timeRanges, bool active) {
			List<DayOfWeekTimeRange> list = new List<DayOfWeekTimeRange>();

			DayOfWeekTimeRange template = new DayOfWeekTimeRange();
			template.Active = active;

			if (!String.IsNullOrEmpty(daysOfWeek)) {
				string[] days = daysOfWeek.ToLower().Split(',');
				foreach (string day in days) {
					if (day == "-") {
						template.SetDaysOfWeek(DayOfWeek.Sunday, DayOfWeek.Saturday, true);
						break;
					}

					string[] range = day.Split('-');
					if (range.Length == 1) {
						DayOfWeek d = ParseDayOfWeek(range[0]);
						template.SetDayOfWeek(d, true);
					} else if (range.Length == 2) {
						DayOfWeek d1 = ParseDayOfWeek(range[0]);
						DayOfWeek d2 = ParseDayOfWeek(range[1]);
						for (; d1 <= d2; d1++) {
							template.SetDayOfWeek(d1, true);
						}
					} else {
						throw new ArgumentException("Invalid day range", "daysOfWeek");
					}
				}
			} else {
				template.SetDaysOfWeek(DayOfWeek.Sunday, DayOfWeek.Saturday, true);
			}

			if (!String.IsNullOrEmpty(timeRanges)) {
				string[] times = timeRanges.Split(',');
				foreach (string time in times) {
					DayOfWeekTimeRange clone = template.Clone();
					string[] range = time.Split('-');
					if (range.Length == 2) {
						if (String.IsNullOrEmpty(range[0]))
							clone.StartTime = StartOfDay;
						else
							clone.StartTime = ParseTime(range[0]);

						if (String.IsNullOrEmpty(range[1]))
							clone.EndTime = EndOfDay;
						else
							clone.EndTime = ParseTime(range[1]);

						list.Add(clone);
					} else {
						throw new ArgumentException("Invalid time range", "timeRanges");
					}
				}
			} else {
				list.Add(template);
			}

			return list.ToArray();
		}
		#endregion
	}

	public class ActiveSchedule {
		#region Private Members
		private List<DayOfWeekTimeRange> _ranges;
		#endregion

		#region Public Constructors
		public ActiveSchedule() {
			_ranges = new List<DayOfWeekTimeRange>();
		}
		#endregion

		#region Public Methods
		public void AddTimeRange(DayOfWeekTimeRange range) {
			_ranges.Add(range);
		}

		public void AddTimeRange(string daysOfWeek, string timeRanges, bool active) {
			_ranges.AddRange(DayOfWeekTimeRange.Parse(daysOfWeek, timeRanges, active));
		}

		public bool IsActiveNow() {
			return IsActiveAt(DateTime.Now);
		}

		public bool IsActiveAt(DateTime time) {
			bool active = false;
			foreach (DayOfWeekTimeRange range in _ranges) {
				if (range.Contains(time))
					active = range.IsActiveAt(time);
			}
			return active;
		}
		#endregion
	}
}
