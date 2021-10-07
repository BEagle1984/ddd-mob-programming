using System;

namespace ddd_mob_programming.Model.ValueObjects
{
    public record Screening(Day Day, ScreeningTime ScreeningTime);

    public record Day(int Value, string Name)
    {
        public static Day Monday = new(1,"Monday");
        public static Day Tuesday = new(2, "Tuesday");
        public static Day Wednesday = new(3, "Wednesday");
        public static Day Thursday = new(4,"Thursday");
        public static Day Friday = new(5, "Friday");
        public static Day Saturday = new(6, "Saturday");
        public static Day Sunday = new(7, "Sunday");
    }

    public record ScreeningTime(ScreeningHour Hour, TimeMinutes Minutes);

    public record ScreeningHour
    {
        public ScreeningHour(int value)
        {
            if (value is < 8 or > 22)
                throw new ArgumentException("Outside cinema working hours", nameof(value));

            Value = value;
        }

        public int Value { get;  }
    }

    public record TimeMinutes
    {
        public TimeMinutes(int value)
        {
            if (value is < 0 or >= 60)
                throw new ArgumentException("Minutes can only be between 0 to 59", nameof(value));

            Value = value;
        }

        public int Value { get; }
    }
}
