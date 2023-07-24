using GliderView.Service;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.UnitTests
{
    public class FlightServiceTests
    {
        const string CHILHOWEE_ID = "92A";
        const int CHILHOWEE_ELEVATION = 235;

        private Mock<IFlightRepository> _fakeFlightRepo;
        private Mock<IRateRepo> _fakeRateRepo;
        private Mock<IAirfieldRepo> _fakeAirfieldRepo;
        private Mock<ILogger<FlightService>> _fakeLogger;
        private FlightService _flightService;

        [SetUp]
        public void SetUp()
        {
            _fakeFlightRepo = new Mock<IFlightRepository>();
            _fakeRateRepo = new Mock<IRateRepo>();
            _fakeAirfieldRepo = new Mock<IAirfieldRepo>();
            _fakeLogger = new Mock<ILogger<FlightService>>();

            _flightService = new FlightService(
                _fakeFlightRepo.Object,
                new FlightAnalyzer(
                    new Mock<ILogger<FlightAnalyzer>>().Object
                ),
                _fakeRateRepo.Object,
                _fakeAirfieldRepo.Object,
                _fakeLogger.Object
            );
        }

        [Test]
        public async Task CalculateCost_LessThanMinDuration_UsesMinDuration()
        {
            // Arrange
            Flight flight = GenerateFlight();

            Rates rates = GetRates();
            _fakeRateRepo.Setup(repo => repo.GetRates())
                .ReturnsAsync(rates);

            AircraftRates aircraftRates = GetAircraftRates();
            _fakeRateRepo.Setup(repo => repo.GetAircraftRates(It.Is<Guid>(x => x == flight.Aircraft!.AircraftId)))
                .ReturnsAsync(aircraftRates);

            _fakeAirfieldRepo.Setup(repo => repo.GetAirfield(flight.AirfieldId))
                .ReturnsAsync(new Airfield()
                {
                    ElevationMeters = CHILHOWEE_ELEVATION
                });

            // Act
            PricingInfo result = await _flightService.CalculateCost(flight);

            // Assert
            Assert.That(result.Total, Is.EqualTo(52.00M));
            Assert.That(result.LineItems, Is.Not.Empty);
        }

        private Flight GenerateFlight(int releaseHeight = 539, int durationSeconds = 60*6)
        {
            DateTime startDate = DateTime.Now;

            return new Flight()
            {
                Aircraft = new Aircraft()
                {
                    AircraftId = Guid.NewGuid(),
                    IsGlider = true
                },
                AirfieldId = CHILHOWEE_ID,
                Statistics = new FlightStatistics()
                {
                    ReleaseHeight = releaseHeight
                },
                StartDate = startDate,
                EndDate = startDate.AddSeconds(durationSeconds)
            };
        }

        private Rates GetRates()
        {
            return new Rates()
            {
                CostPerHundredFeet = 1.75M,
                HookupCost = 22.50M,
                MinTowHeight = 1000
            };
        }

        private AircraftRates GetAircraftRates()
        {
            return new AircraftRates()
            {
                MinRentalHours = 0.25f,
                RentalCostPerHour = 48.00M
            };
        }
    }
}
