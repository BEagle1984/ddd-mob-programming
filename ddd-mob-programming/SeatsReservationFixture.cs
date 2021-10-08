using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace ddd_mob_programming
{
    public class SeatsReservationFixture
    {
        [Fact]
        public void SeatsReservation_ShouldSucceed_WhenSeatsAreAvailable()
        {
            Screenings screenings = new();
            screenings.Add(new Screening
            {
                Id = 1
            });
            SeatsReservationCommandHandler handler = new(screenings);

            var reserveSeatsCommand = new ReserveSeatsCommand(1, 1, new[] { 1, 2, 3 });

            bool result = handler.Handle(reserveSeatsCommand);

            result.Should().BeTrue();
        }

        [Fact]
        public void SeatsReservation_ShouldFail_WhenSeatsAreNotAvailable()
        {
            Screenings screenings = new();
            var screening = new Screening
            {
                Id = 1
            };
            screening.ReserveSeats(new[] { 1, 2 }, 42);
            screenings.Add(screening);
            SeatsReservationCommandHandler handler = new(screenings);

            var reserveSeatsCommand = new ReserveSeatsCommand(1, 1, new[] { 1, 2, 3 });

            bool result = handler.Handle(reserveSeatsCommand);

            result.Should().BeFalse();
        }
    }

    public class SeatsReservationCommandHandler
    {
        private readonly IScreenings _screenings;

        public SeatsReservationCommandHandler(Screenings screenings)
        {
            _screenings = screenings;
        }

        public bool Handle(ReserveSeatsCommand command)
        {
            Screening screening = _screenings.Get(command.ScreeningId);

            return screening.ReserveSeats(command.Seats, command.CustomerId);
        }
    }

    public record ReserveSeatsCommand(int ScreeningId, int CustomerId, IReadOnlyCollection<int> Seats);

    internal interface IScreenings
    {
        Screening Get(int screeningId);
    }

    public class Screening
    {
        private readonly List<ReservedSeat> _reservedSeats = new();

        public int Id { get; set; }

        public bool ReserveSeats(IReadOnlyCollection<int> seats, int customerId)
        {
            if (_reservedSeats.Any(reservedSeat => seats.Contains(reservedSeat.SeatNumber)))
                return false;

            foreach (int seat in seats)
            {
                _reservedSeats.Add(new ReservedSeat
                {
                    CustomerId = customerId,
                    SeatNumber = seat
                });
            }

            return true;
        }
    }

    public class ReservedSeat
    {
        public int CustomerId { get; set; }

        public int SeatNumber { get; set; }
    }


    public class Screenings : IScreenings
    {
        private readonly Dictionary<int, Screening> _set = new();

        public void Add(Screening screening) => _set.Add(screening.Id, screening);

        public Screening Get(int screeningId) => _set[screeningId];
    }
}
