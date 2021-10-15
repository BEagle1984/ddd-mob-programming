using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace ddd_mob_programming_event_sourcing
{
    public class SeatsReservationEventSourcingFixture
    {
        private readonly List<object> _history = new();
        private readonly List<object> _publishedEvents = new();
        private object _response;

        private void Given(params SeatReservedEvent[] seatReservedEvent)
        {
            _history.AddRange(seatReservedEvent);
        }

        private void When(ReserveSeatsCommand reserveSeatsCommand)
        {
            var handler = new CommandsHandler(_history, publishedEvent => _publishedEvents.Add(publishedEvent));
            handler.Handle(reserveSeatsCommand);
        }

        private void Query(MyReservedSeatsQuery myReservedSeatsQuery)
        {
            var readModel = new ReadModel(_history.Union(_publishedEvents).ToList());
            var handler = new QueryHandler(readModel);
            _response = handler.Execute(myReservedSeatsQuery);
        }

        private void Expect(params object[] events)
        {
            _publishedEvents.Should().BeEquivalentTo(events);
        }

        private void ExpectResponse(object response)
        {
            _response.Should().BeEquivalentTo(response);
        }

        [Fact]
        public void SeatsReservation_ShouldSucceed_WhenSeatsAreAvailable()
        {
            Given(new SeatReservedEvent(1, 1, new[] { 1, 2, 3 }),
                new SeatReservedEvent(1, 2, new[] { 4, 5, 6 }));

            When(new ReserveSeatsCommand(1, 3, new[] { 7, 8 }));

            Expect(new SeatReservedEvent(1, 3, new[] { 7, 8 }));
        }

        [Fact]
        public void SeatsReservation_ShouldFail_WhenSeatsAreNotAvailable()
        {
            Given(new SeatReservedEvent(1, 1, new[] { 1, 2, 3 }),
                new SeatReservedEvent(1, 2, new[] { 4, 5, 6 }));

            When(new ReserveSeatsCommand(1, 3, new[] { 3, 4 }));

            Expect();
        }

        [Fact]
        public void QueryMyReservations_ShouldReturnAvailableSeats()
        {
            Given(new SeatReservedEvent(1, 1, new[] { 1, 2, 3 }),
                new SeatReservedEvent(1, 2, new[] { 4, 5, 6 }));

            Query(new MyReservedSeatsQuery(1));

            ExpectResponse(new[] { 1, 2, 3 });
        }


        [Fact]
        public void IntegrationTest()
        {
            Given(new SeatReservedEvent(1, 1, new[] { 1, 2, 3 }));

            When(new ReserveSeatsCommand(1, 1, new[] { 4, 5 }));

            Query(new MyReservedSeatsQuery(1));

            ExpectResponse(new[] { 1, 2, 3, 4, 5 });
        }
    }

    public class SeatReservedEvent : IEquatable<SeatReservedEvent>
    {
        public SeatReservedEvent(int screeningId, int customerId, IReadOnlyCollection<int> seats)
        {
            ScreeningId = screeningId;
            CustomerId = customerId;
            Seats = seats.ToArray();
        }

        public int ScreeningId { get; }

        public int CustomerId { get; }

        public int[] Seats { get; }

        #region Equality members

        public bool Equals(SeatReservedEvent other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return ScreeningId == other.ScreeningId && CustomerId == other.CustomerId &&
                   CompareSeats(Seats, other.Seats);
        }

        private bool CompareSeats(int[] seats, int[] otherSeats)
        {
            if (seats.Length != otherSeats.Length)
                return false;

            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i] != otherSeats[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((SeatReservedEvent)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ScreeningId, CustomerId, Seats);
        }

        public static bool operator ==(SeatReservedEvent left, SeatReservedEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SeatReservedEvent left, SeatReservedEvent right)
        {
            return !Equals(left, right);
        }

        #endregion
    }

    public record ReserveSeatsCommand(int ScreeningId, int CustomerId, IReadOnlyCollection<int> Seats);

    public record MyReservedSeatsQuery(int CustomerId);

    public class CommandsHandler
    {
        private readonly List<object> _history;
        private readonly Action<object> _eventPublisher;

        public CommandsHandler(List<object> history, Action<object> eventPublisher)
        {
            _history = history;
            _eventPublisher = eventPublisher;
        }

        public void Handle(ReserveSeatsCommand reserveSeatsCommand)
        {
            List<SeatReservedEvent> events = _history.OfType<SeatReservedEvent>()
                .Where(reservedEvent => reservedEvent.ScreeningId == reserveSeatsCommand.ScreeningId)
                .ToList();

            Screening screening = new(reserveSeatsCommand.ScreeningId, events, _eventPublisher);

            screening.ReserveSeats(reserveSeatsCommand.Seats, reserveSeatsCommand.CustomerId);
        }
    }

    public class Screening
    {
        private readonly Action<object> _eventPublisher;
        private readonly List<ReservedSeat> _reservedSeats = new();

        public Screening(int id, List<SeatReservedEvent> events, Action<object> eventPublisher)
        {
            Id = id;
            _eventPublisher = eventPublisher;

            foreach (var seatReservedEvent in events)
            {
                Apply(seatReservedEvent);
            }
        }

        public int Id { get; set; }

        private void Apply(SeatReservedEvent seatReservedEvent)
        {
            foreach (int seatNumber in seatReservedEvent.Seats)
            {
                _reservedSeats.Add(new ReservedSeat(seatReservedEvent.CustomerId, seatNumber));
            }
        }

        public bool ReserveSeats(IReadOnlyCollection<int> seats, int customerId)
        {
            if (_reservedSeats.Any(reservedSeat => seats.Contains(reservedSeat.SeatNumber)))
                return false;

            foreach (int seat in seats)
            {
                _reservedSeats.Add(new ReservedSeat(customerId, seat));
            }

            _eventPublisher.Invoke(new SeatReservedEvent(Id, customerId, seats));

            return true;
        }

        private record ReservedSeat(int CustomerId, int SeatNumber);
    }

    internal class QueryHandler
    {
        private readonly ReadModel _readModel;

        public QueryHandler(ReadModel readModel)
        {
            _readModel = readModel;
        }

        public IReadOnlyCollection<int> Execute(MyReservedSeatsQuery myReservedSeatsQuery)
        {
            return _readModel.ReservationsForCustomer(myReservedSeatsQuery.CustomerId);
        }
    }

    // TODO: ScreeningId omitted for simplicity
    public class ReadModel
    {
        private Dictionary<int, List<int>> _reservations = new();

        public ReadModel(List<object> events)
        {
            foreach (var @event in events)
            {
                switch (@event)
                {
                    case SeatReservedEvent seatReservedEvent:
                        Apply(seatReservedEvent);
                        break;
                }
            }
        }

        private void Apply(SeatReservedEvent seatReservedEvent)
        {
            if (!_reservations.TryGetValue(seatReservedEvent.CustomerId, out List<int> seats))
            {
                seats = new List<int>();
                _reservations.Add(seatReservedEvent.CustomerId, seats);
            }

            seats.AddRange(seatReservedEvent.Seats);
        }

        public IReadOnlyCollection<int> ReservationsForCustomer(int customerId)
        {
            return _reservations[customerId];
        }
    }
}
