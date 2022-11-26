using GliderView.Service;
using GliderView.Service.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace GliderView.UnitTests
{
    public class IgcServiceTests
    {
        private IgcService _service;
        private Mock<IIgcFileRepository> _mockFileRepository;
        private Mock<IFlightRepository> _mockFlightRepository;
        private Mock<ILogger<IgcService>> _mockLogger;
        private Mock<IFaaDatabaseProvider> _mockFaaDatabaseProvider;
        private Mock<IFlightBookClient> _mockFlightBookClient;

        [SetUp]
        public void Setup()
        {
            _mockFileRepository = new Mock<IIgcFileRepository>();
            _mockFlightRepository= new Mock<IFlightRepository>();
            _mockLogger = new Mock<ILogger<IgcService>>();
            _mockFaaDatabaseProvider= new Mock<IFaaDatabaseProvider>();
            _mockFlightBookClient= new Mock<IFlightBookClient>();

            _service = new IgcService(
                _mockFileRepository.Object,
                _mockFlightRepository.Object,
                _mockLogger.Object,
                _mockFaaDatabaseProvider.Object,
                _mockFlightBookClient.Object,
                new FlightAnalyzer(new Mock<ILogger<FlightAnalyzer>>().Object)
            );
        }

        [Test]
        public async Task DownloadAndProcess_HappyPath()
        {
            const string tracker = FILE_1_Tracker;
            const string airfield = "92A";

            Guid aircraftId = Guid.NewGuid();
            Guid flightId = Guid.NewGuid();

            byte[] igcFileBytes = System.Text.Encoding.UTF8.GetBytes(FILE_1);

            Flight? generatedFlight = null;

            // Setup
            _mockFlightBookClient.Setup(x => x.DownloadIgcFile(tracker, null, null))
                .ReturnsAsync(() => new MemoryStream(igcFileBytes));

            // No existing flights
            _mockFlightRepository.Setup(x => x.GetFlights(
                It.Is<FlightSearch>(search =>
                    search.StartDate == FILE_1_EVENTDATE
                        && search.EndDate == FILE_1_EVENTDATE.AddDays(1))
            )).ReturnsAsync(() => new List<Flight>());

            _mockFlightRepository.Setup(x => x.GetAircraftByTrackerId(tracker))
                .ReturnsAsync(() => null);

            _mockFlightRepository.Setup(x => x.AddAircraft(It.IsAny<Aircraft>()))
                .Callback<Aircraft>((aircraft) => aircraft.AircraftId = aircraftId);

            _mockFlightRepository.Setup(x => x.AddFlight(It.IsAny<Flight>()))
                .Callback<Flight>(flight =>
                {
                    flight.FlightId = flightId;
                    generatedFlight = flight;
                });

            _mockFileRepository.Setup(x => x.SaveFile(It.IsAny<Stream>(), airfield, FILE_1_REGISTRATION, tracker, FILE_1_EVENTDATE))
                .ReturnsAsync(() => FILE_1_FILENAME);

            // Execute
            await _service.DownloadAndProcess(airfield, tracker);

            // Assert

            _mockFlightRepository.Verify(x => x.AddFlight(It.IsAny<Flight>()), Times.Once());

            Assert.IsNotEmpty(generatedFlight.Waypoints.Where(x => x.FlightEvent != null));
        }

        [Test]
        public async Task ReadAndProcess_HappyPath()
        {
            const string tracker = FILE_1_Tracker;
            const string airfield = "92A";
            string fileName = @"\2022\11\13\92A\" + FILE_1_FILENAME;

            Guid aircraftId = Guid.NewGuid();
            Guid flightId = Guid.NewGuid();

            byte[] igcFileBytes = System.Text.Encoding.UTF8.GetBytes(FILE_1);

            // Setup
            _mockFileRepository.Setup(x => x.GetFile(fileName))
                .Returns(() => new MemoryStream(igcFileBytes));

            // No existing flights
            _mockFlightRepository.Setup(x => x.GetFlights(
                It.Is<FlightSearch>(search =>
                    search.StartDate == FILE_1_EVENTDATE
                        && search.EndDate == FILE_1_EVENTDATE.AddDays(1))
            )).ReturnsAsync(() => new List<Flight>());

            _mockFlightRepository.Setup(x => x.GetAircraftByTrackerId(tracker))
                .ReturnsAsync(() => null);

            _mockFlightRepository.Setup(x => x.AddAircraft(It.IsAny<Aircraft>()))
                .Callback<Aircraft>((aircraft) => aircraft.AircraftId = aircraftId);

            _mockFlightRepository.Setup(x => x.AddFlight(It.IsAny<Flight>()))
                .Callback<Flight>(flight => flight.FlightId = flightId);

            // Execute
            await _service.ReadAndProcess(airfield, fileName);

            // Assert

            _mockFlightRepository.Verify(x => x.AddFlight(It.IsAny<Flight>()), Times.Once());
        }

        #region IgcFiles
        private const string FILE_1_FILENAME = "2022-11-13_N2750H.C7720C.igc";
        private const string FILE_1_Tracker = "C7720C";
        private const string FILE_1_REGISTRATION = "N2750H";
        private DateTime FILE_1_EVENTDATE = new DateTime(2022, 11, 13);
        private const string FILE_1 =
@"
AOGNOGN
HFDTE131122
HFFXA050
HFPLTPILOTINCHARGE:
HFGTYGLIDERTYPE:SGS 2-33A
HFGIDGLIDERID:N2750H
HFDTM100GPSDATUM:WGS-1984
HFRFWFIRMWAREVERSION:
HFRHWHARDWAREVERSION:
HFFTYFRTYPE:OGN-FLIGHTBOOK
HFGPSGENERIC
HFPRSPRESSALTSENSOR:
HFCIDCOMPETITIONID:450
B1700313513446N08435201WA0000000228
B1700373513490N08435165WA0000000229
B1700373513490N08435165WA0000000229
B1700453513579N08435088WA0000000236
B1700503513636N08435035WA0000000258
B1700553513684N08434985WA0000000274
B1701003513738N08434942WA0000000288
B1701053513788N08434891WA0000000303
B1701103513845N08434847WA0000000316
B1701153513924N08434792WA0000000333
B1701203513979N08434772WA0000000353
B1701253514054N08434774WA0000000381
B1701303514116N08434798WA0000000398
B1701353514176N08434853WA0000000417
B1701403514222N08434927WA0000000431
B1701453514254N08435009WA0000000450
B1701503514267N08435100WA0000000463
B1701553514257N08435199WA0000000470
B1702013514214N08435319WA0000000482
B1702063514155N08435412WA0000000495
B1702113514084N08435496WA0000000511
B1702163513998N08435560WA0000000529
B1702213513904N08435590WA0000000547
B1702263513802N08435624WA0000000580
B1702313513697N08435621WA0000000600
B1702363513593N08435601WA0000000615
B1702423513479N08435559WA0000000640
B1702473513388N08435513WA0000000656
B1702523513301N08435463WA0000000674
B1702573513218N08435413WA0000000687
B1703033513115N08435349WA0000000701
B1703103513009N08435268WA0000000699
B1703153512930N08435218WA0000000709
B1703203512852N08435165WA0000000720
B1703253512772N08435113WA0000000735
B1703303512694N08435065WA0000000764
B1703353512619N08435011WA0000000782
B1703403512542N08434964WA0000000806
B1703483512416N08434874WA0000000817
B1703543512310N08434818WA0000000827
B1703593512219N08434770WA0000000840
B1704043512136N08434713WA0000000853
B1704103512036N08434642WA0000000867
B1704153511958N08434582WA0000000882
B1704213511865N08434501WA0000000899
B1704273511779N08434413WA0000000920
B1704323511716N08434330WA0000000944
B1704373511666N08434243WA0000000969
B1704423511633N08434148WA0000000990
B1704473511612N08434049WA0000001005
B1704523511599N08433962WA0000001008
B1704583511596N08433869WA0000000994
B1705033511610N08433803WA0000000980
B1705103511645N08433715WA0000000967
B1705153511676N08433660WA0000000960
B1705223511731N08433599WA0000000954
B1705283511792N08433562WA0000000944
B1705333511844N08433534WA0000000940
B1705393511907N08433507WA0000000932
B1705443511959N08433484WA0000000925
B1705493512009N08433449WA0000000920
B1705583512101N08433387WA0000000903
B1706053512177N08433352WA0000000883
B1706123512266N08433311WA0000000873
B1706213512371N08433273WA0000000871
B1706273512441N08433252WA0000000875
B1706333512503N08433238WA0000000864
B1706413512592N08433224WA0000000855
B1706483512671N08433204WA0000000858
B1706573512748N08433152WA0000000854
B1707043512809N08433108WA0000000840
B1707103512872N08433124WA0000000819
B1707173512925N08433200WA0000000802
B1707223512974N08433250WA0000000809
B1707293513041N08433320WA0000000806
B1707343513095N08433366WA0000000803
B1707433513185N08433457WA0000000800
B1707483513222N08433521WA0000000794
B1707543513268N08433595WA0000000782
B1708033513342N08433704WA0000000773
B1708083513381N08433764WA0000000766
B1708143513427N08433840WA0000000754
B1708193513468N08433904WA0000000742
B1708243513511N08433968WA0000000734
B1708313513572N08434054WA0000000720
B1708363513615N08434114WA0000000706
B1708413513658N08434176WA0000000697
B1708473513710N08434245WA0000000687
B1708533513761N08434313WA0000000682
B1709003513820N08434388WA0000000667
B1709063513872N08434461WA0000000657
B1709113513918N08434519WA0000000648
B1709183513974N08434618WA0000000637
B1709303514057N08434801WA0000000634
B1709393514119N08434921WA0000000631
B1709453514157N08435004WA0000000622
B1709503514188N08435071WA0000000618
B1709553514217N08435140WA0000000607
B1710013514257N08435223WA0000000590
B1710063514308N08435258WA0000000577
B1710113514361N08435246WA0000000565
B1710163514354N08435168WA0000000546
B1710213514269N08435147WA0000000537
B1710263514193N08435197WA0000000522
B1710313514117N08435263WA0000000500
B1710363514037N08435332WA0000000489
B1710413513959N08435401WA0000000477
B1710463513881N08435469WA0000000470
B1710513513804N08435529WA0000000459
B1710563513725N08435589WA0000000446
B1711013513640N08435646WA0000000434
B1711063513553N08435704WA0000000425
B1711113513470N08435765WA0000000417
B1711183513345N08435774WA0000000388
B1711233513273N08435717WA0000000380
B1711283513210N08435657WA0000000370
B1711333513158N08435592WA0000000358
B1711383513148N08435521WA0000000339
B1711433513164N08435464WA0000000312
B1711483513199N08435410WA0000000289
B1711533513245N08435364WA0000000275
B1711593513295N08435318WA0000000256
B1712043513340N08435283WA0000000237
B1712103513397N08435237WA0000000227
B1712153513443N08435197WA0000000225
B1712233513507N08435148WA0000000226
";
        #endregion
    }
}