﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Augustine.VietnameseCalendar
{
    public class LunarYear
    {
        /// <summary>
        /// Lunar year
        /// </summary>
        public int Year { get; private set; }

        /// <summary>
        /// Information (month starting day, month index, is leap month) of months 
        /// from "tháng Một (11)" last lunar year to this lunar year.
        /// </summary>
        public Tuple<DateTime, int, bool>[] Months { get; private set; }

        public bool IsLeapYear { get; private set; }

        public double TimeZone { get; private set; }

        private DateTime month11LastYear;
        private DateTime month11ThisYear;
        private int[] majorTermAtMonthBeginnings; // debug
        private double[] sunLongitudeAtMonthBeginnings; // debug

        public LunarYear(int year, double timeZone)
        {
            Year = year;
            TimeZone = timeZone;

            // at 00:00:00 local
            month11LastYear = Astronomy.NewMoonBeforeWinterSolstice(Year - 1, TimeZone).Date;
            month11ThisYear = Astronomy.NewMoonBeforeWinterSolstice(Year, TimeZone).Date;

            // Convert the local date to UTC date time by adding AddHours(-TimeZone).
            double jdMonth11LastYear = month11LastYear.AddHours(-TimeZone).UniversalDateTimeToJulianDate();
            double jdMonth11ThisYear = month11ThisYear.AddHours(-TimeZone).UniversalDateTimeToJulianDate();

            int k = (int)(0.5 + (jdMonth11LastYear - 2415021.076998695) / 29.530588853);

            IsLeapYear = (jdMonth11ThisYear - jdMonth11LastYear) > 365.0;

            if (!IsLeapYear)
            {
                InitNonLeapYear(k);
            }
            else
            {
                InitLeapYear(k);
            }
        }

        private void InitNonLeapYear(int k)
        {
            int numberOfMonths = 13;

            majorTermAtMonthBeginnings = new int[numberOfMonths]; // debug
            sunLongitudeAtMonthBeginnings = new double[numberOfMonths]; // debug

            Months = new Tuple<DateTime, int, bool>[numberOfMonths];
            Months[0] = new Tuple<DateTime, int, bool>(month11LastYear, 11, false);
            Months[numberOfMonths - 1] = new Tuple<DateTime, int, bool>(month11ThisYear, 11, false);
            for (int i = 1; i < numberOfMonths - 1; i++)
            {
                var newMoon =
                    Astronomy.JulianDateToUniversalDateTime(Astronomy.GetNewMoon(k + i)).AddHours(TimeZone).Date;
                Months[i] = new Tuple<DateTime, int, bool>(newMoon, (i + 11) % 12, false);
            }
        }

        private void InitLeapYear(int k)
        {
            int numberOfMonths = 14;
            Months = new Tuple<DateTime, int, bool>[numberOfMonths];
            DateTime[] newMoons = new DateTime[numberOfMonths];
            //int[] majorTermAtMonthBeginnings = new int[numberOfMonths];

            majorTermAtMonthBeginnings = new int[numberOfMonths]; // debug
            sunLongitudeAtMonthBeginnings = new double[numberOfMonths]; // debug

            // get all the new moons, local date
            newMoons[0] = month11LastYear;
            newMoons[numberOfMonths - 1] = month11ThisYear;
            for (int i = 1; i < numberOfMonths - 1; i++)
            {
                newMoons[i] =
                    Astronomy.JulianDateToUniversalDateTime(Astronomy.GetNewMoon(k + i)).AddHours(TimeZone).Date;
            }

            // determine leap month
            bool found = false;

            sunLongitudeAtMonthBeginnings[0] = Astronomy.GetSunLongitudeAtJulianDate(
                newMoons[0].AddHours(-TimeZone).UniversalDateTimeToJulianDate()); // debug

            majorTermAtMonthBeginnings[0] = (int)(Astronomy.GetSunLongitudeAtJulianDate(
                newMoons[0].AddHours(-TimeZone).UniversalDateTimeToJulianDate()) * 6 / Math.PI);
            for (int i = 0; i < numberOfMonths - 1; i++)
            {
                // If major term at the beginning of this month is same as
                // major term at the beginning of next month, i.e. this month 
                // does not have major term, this month is leap month.
                // 
                // Note that only the first month which does not have major term
                // is leap month (as a Lunar year has maximum one leap month).
                if (found)
                {
                    Months[i] = new Tuple<DateTime, int, bool>(newMoons[i], (i - 1 + 11) % 12, false);
                    continue;
                }

                double julianDateAtNextMonthBeginning = 
                    newMoons[i + 1].AddHours(-TimeZone).UniversalDateTimeToJulianDate();

                sunLongitudeAtMonthBeginnings[i + 1] = Astronomy.
                    GetSunLongitudeAtJulianDate(julianDateAtNextMonthBeginning); // debug

                majorTermAtMonthBeginnings[i + 1] = (int)(Astronomy.
                    GetSunLongitudeAtJulianDate(julianDateAtNextMonthBeginning) * 6 / Math.PI);

                // In this algorithm, comparisons will happen with updated element only.
                // Yet be careful: initial value of elements of an int array are zeros.
                // This may confuse the debugging process!
                found = majorTermAtMonthBeginnings[i] == majorTermAtMonthBeginnings[i + 1];
                Months[i] = new Tuple<DateTime, int, bool>(newMoons[i],
                    found ? (i - 1 + 11) % 12 : (i + 11) % 12, found);
            }
            Months[numberOfMonths - 1] =
                new Tuple<DateTime, int, bool>(newMoons[13], 11, false);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("Lunar year: {0} {1}", Year, IsLeapYear ? "(Leap year)" : ""));
            for (int i = 0; i < Months.Length; i++)
            {
                sb.AppendLine(String.Format("Month {0,2}{1}: {2} | {4} - {3} ({5})",
                    Months[i].Item2 == 0 ? 12 : Months[i].Item2,
                    Months[i].Item3 ? "*" : " ",
                    Months[i].Item1,
                    majorTermAtMonthBeginnings[i],
                    sunLongitudeAtMonthBeginnings[i].ToDegrees(),
                    majorTermAtMonthBeginnings[i] * 30));

            }
            return sb.ToString();
        }
    }
}
