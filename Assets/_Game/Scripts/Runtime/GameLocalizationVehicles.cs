using System.Collections.Generic;

namespace Tycoon
{
    public static partial class GameLocalization
    {
        static void AddVehicles(Dictionary<string, string> entries)
        {
            entries["GA TRUNG TÂM"] = "CENTRAL STATION";
            entries["SÂN GA 1"] = "PLATFORM 1";
            entries["SÂN GA 2"] = "PLATFORM 2";
            entries["LỐI QUA HAI SÂN GA"] = "ACCESS TO BOTH PLATFORMS";
            entries["Xe buýt"] = "City bus";
            entries["Xe khách"] = "Passenger coach";
            entries["Taxi"] = "Taxi";
            entries["Xe hơi"] = "Car";
            entries["Xe tải nhỏ"] = "Small truck";
            entries["Xe tải chở hàng"] = "Cargo truck";
            entries["Xe bồn"] = "Tank truck";
            entries["Xe sang trọng"] = "Luxury car";
            entries["Xe thể thao"] = "Sports car";
            entries["Tàu hỏa"] = "Passenger train";
        }
    }
}
