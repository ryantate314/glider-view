using GliderView.Data;

namespace GliderView.Jobs
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string connectionString = "Data Source=home-server;Database=GliderView;Integrated Security=false;User ID=gliderViewer;Password=Passw0rd;Encrypt=true;TrustServerCertificate=true;";
            
            var repo = new FlightRepository(connectionString);


        }
    }
}